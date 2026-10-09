using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Statistics2026.Api;
using Statistics2026.Configuration;
using Statistics2026.Data;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.ScheduledTasks
{
    public class RunAllTasksTask : IScheduledTask
    {
        private readonly EmbyInterfaces _embyInterfaces;

        public RunAllTasksTask(
            IFileSystem fileSystem,
            ILibraryManager libraryManager,
            ILogManager logManager,
            IServerApplicationPaths serverApplicationPaths,
            IUserDataManager userDataManager,
            IUserManager userManager,
            IApplicationHost appHost,
            Statistics2026API apiService,
            IJsonSerializer jsonSerializer,
            IProviderManager providerManager,
            IServerConfigurationManager configManager,
            ITaskManager taskManager,
            ISessionManager sessionManager,
            IHttpClient httpClient,
            IAuthenticationRepository authenticationRepository
            )
        {
            _embyInterfaces = new EmbyInterfaces( appHost )
            {
                _fileSystem = fileSystem,
                _libraryManager = libraryManager,
                _logManager = logManager,
                _logger = logManager.GetLogger( "Statistics2026 - RunAllTasksTask" ),
                _serverApplicationPaths = serverApplicationPaths,
                _userDataManager = userDataManager,
                _userManager = userManager,
                _appHost = appHost,
                _apiService = apiService,
                _jsonSerializer = jsonSerializer,
                _providerManager = providerManager,
                _configManager = configManager,
                _taskManager = taskManager,
            };
        }

        private static PluginConfiguration? PluginConfiguration => Plugin.Instance?.Configuration ?? null;
        string IScheduledTask.Name => "Calculate Media and User Information for all library media and users";

        string IScheduledTask.Key => "Statistics2026CalculateStatsTask";

        string IScheduledTask.Description => "Task that will calculate Statistics for all media in library.";

        string IScheduledTask.Category => "Statistics 2026";

        Task IScheduledTask.Execute( CancellationToken cancellationToken, IProgress<double> progress )
        {
            return RunAllTasks( this, _embyInterfaces, true, progress, cancellationToken );
        }

        private static void UpdateLastAdded( StatisticsDB db )
        {
            var lastMediaAdded = db.GetLastMovieOrEpisodeAdded();
            DateTime? lastAdded = null;
            if( lastMediaAdded != null && lastMediaAdded!.DateCreated != DateTime.MinValue )
            {
                lastAdded = lastMediaAdded!.DateCreated.DateTime;
            }
            var lastUserAdded = db.GetLastAddedUser();
            if( lastUserAdded != null && lastUserAdded!.DateCreated != DateTime.MinValue )
            {
                if( lastAdded == null )
                    lastAdded = lastUserAdded!.DateCreated.DateTime;
                else if( lastAdded < lastUserAdded!.DateCreated.DateTime )
                    lastAdded = lastUserAdded!.DateCreated.DateTime;
            }
            if( lastAdded != null && lastAdded != DateTime.MinValue )
            {
                db.UpdateLastUpdated( StatisticsDB.EAction.LastMediaOrUserAdded, 0, lastAdded.Value );
            }
        }

        private static bool AnalysisOutOfDate( StatisticsDB db, ILogger? logger  )
        {
            bool needToRun = false;

            var lastAnalysisAdded = db.GetLastUpdated( StatisticsDB.EAction.LastMediaOrUserAdded );
            if( lastAnalysisAdded == null || lastAnalysisAdded.Value == DateTime.MinValue )
            {
                needToRun = true;
            }

            if( !needToRun )
            {
                List<DateTime> datesOfItemsAdded = [];

                var lastMediaAddedToServer = db.GetLastMovieOrEpisodeAdded();
                if( lastMediaAddedToServer != null && lastMediaAddedToServer!.DateCreated != DateTime.MinValue )
                {
                    datesOfItemsAdded.Add( lastMediaAddedToServer.DateCreated.DateTime );
                }
                var lastUserAddedToServer = db.GetLastAddedUser();
                if( lastUserAddedToServer != null && lastUserAddedToServer!.DateCreated != DateTime.MinValue )
                {
                    datesOfItemsAdded.Add( lastUserAddedToServer!.DateCreated.DateTime );
                }

                DateTime? latestDate = null;
                foreach( var date in datesOfItemsAdded )
                {
                    if( latestDate == null )
                        latestDate = date;
                    else
                    {
                        latestDate = ( date > latestDate ) ? date : latestDate.Value;
                    }
                }

                if ( latestDate != null )
                    needToRun = lastAnalysisAdded < latestDate;
            }

            if( !needToRun && logger != null)
            {
                logger.Info( "No media has been added since last run, not re-running Statistics Analysis" );
            }
            return needToRun;
        }

        public static async Task RunAllTasks( object sender, EmbyInterfaces embyInterfaces, bool force, IProgress<double> progress, CancellationToken cancellationToken )
        {
            if( Plugin.Instance != null && Plugin.Instance.IsStatistics2026TaskRunning( sender.GetType() ) )
            {
                throw new Exception( "Statistics 2026 task is running" );
            }
            if( PluginConfiguration == null )
                throw new ArgumentNullException( nameof( PluginConfiguration ) );

            var db = StatisticsDB.GetInstance( embyInterfaces );
            var schedTask = sender as IScheduledTask;
            db.Initialize( schedTask, cancellationToken, progress, PluginConfiguration.resetPlayCount );
            cancellationToken.ThrowIfCancellationRequested();

            if( !force && !AnalysisOutOfDate( db, embyInterfaces._logger ) )
                return;

            var taskName = "Analyze All";
            embyInterfaces._logger!.Info( $"Statistics 2026 : Starting Statistics 2026 {taskName} task" );
            // purely for progress reporting
            var now = DateTime.UtcNow;

            PluginConfiguration.LastUpdated = now.ToString( "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture );
            PluginConfiguration.Version = Plugin.Instance?.Version.ToString( 4 ) ?? "<UNKNOWN>";
            PluginConfiguration.BuildDate = BuildDateInfo.GetBuildDate().ToString( "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture );
            Plugin.Instance?.UpdateConfiguration( PluginConfiguration );

            db.ClearLastUpdated( StatisticsDB.EAction.System );

            var overAllTimer = new AutoTimer( $"Adding All Data", embyInterfaces._logger, false );
            var tasks = Plugin.GetKnownTasks( false );

            for( var ii = 0; ii < tasks?.Count; ii++ )
            {
                progress.Report( 100.0 * ii / ( 1.0 * tasks.Count ) );
                var task = tasks[ ii ];
                task.RunTime = await Plugin.launchSubTask( embyInterfaces, task, cancellationToken );
                tasks[ ii ] = task;
            }

            var overall = overAllTimer.ElapsedMilliseconds();
            db.UpdateLastUpdated( StatisticsDB.EAction.System, overall );
            UpdateLastAdded( db );

            overAllTimer.Dispose();
            embyInterfaces._logger.Info( $"=======================================" );
            embyInterfaces._logger.Info( $"Time to Add: {overall} ms" );
            if( tasks != null )
            {
                var maxLen = 0;
                foreach( var task in tasks )
                {
                    if( task.Description.Length > maxLen )
                        maxLen = task.Description.Length;
                }

                foreach( var task in tasks )
                {
                    embyInterfaces._logger.Info( $"{task.Description.PadLeft( maxLen )}: {task.RunTime} ms" );
                }
            }

            embyInterfaces._logger.Info( $"=======================================" );
            embyInterfaces._logger.Info( $"Statistics 2026 : Finished Statistics 2026 {taskName} task" );

            Plugin.Instance?.AddDBState( EDBState.eFullyInitialized );
            Plugin.Instance?.SaveConfiguration();
            db.ResetCancellationToken();
            return;
        }

        IEnumerable<TaskTriggerInfo> IScheduledTask.GetDefaultTriggers()
        {
            return new[] {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromMinutes(30).Ticks
                }
            };
        }
    }
}