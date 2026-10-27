using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
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
            _embyInterfaces = new EmbyInterfaces()
            {
                _logger = logManager.GetLogger( "Statistics2026 - RunAllTasksTask" ),
                _appHost = appHost,
                _authenticationRepository = authenticationRepository,
                _fileSystem = fileSystem,
                _httpClient = httpClient,
                _jsonSerializer = jsonSerializer,
                _libraryManager = libraryManager,
                _logManager = logManager,
                _providerManager = providerManager,
                _serverApplicationPaths = serverApplicationPaths,
                _configManager = configManager,
                _sessionManager = sessionManager,
                _taskManager = taskManager,
                _userDataManager = userDataManager,
                _userManager = userManager,
                _apiService = apiService,
            };
        }

        private static PluginConfiguration? PluginConfiguration => Plugin.Instance?.Configuration ?? null;
        string IScheduledTask.Name => "Calculate Media and User Information for all library media and users";

        string IScheduledTask.Key => "Statistics2026CalculateStatsTask";

        string IScheduledTask.Description => "Task that will calculate Statistics for all media in library.";

        string IScheduledTask.Category => "Statistics 2026";

        async Task IScheduledTask.Execute( CancellationToken cancellationToken, IProgress<double> progress )
        {
            if( TaskDef.IsStatistics2026TaskRunning( _embyInterfaces, GetType(), false ) )
            {
                _embyInterfaces?.Logger().Info( "Analysis is currently running" );
                return;
            }
            if( PluginConfiguration == null )
                throw new ArgumentNullException( nameof( PluginConfiguration ) );

            var db = StatisticsDB.GetInstance( _embyInterfaces );
            
            db.Initialize( this, cancellationToken, progress, PluginConfiguration.resetPlayCount );
            cancellationToken.ThrowIfCancellationRequested();

            var taskName = "Analyze All";
            _embyInterfaces.Logger().Info( $"Starting Statistics 2026 {taskName} task" );
            var now = DateTimeOffset.UtcNow;

            PluginConfiguration.LastUpdated = HelperFuncs.ToDateTimeParamValue( now )!;
            PluginConfiguration.Version = Plugin.Instance?.Version.ToString( 4 ) ?? "<UNKNOWN>";
            PluginConfiguration.BuildDate = HelperFuncs.ToDateTimeParamValue( BuildDateInfo.GetBuildDate() )!;
            Plugin.Instance?.UpdateConfiguration( PluginConfiguration );

            db.ClearLastUpdated( StatisticsDB.EAction.System );

            var overAllTimer = new AutoTimer( $"Adding All Data", _embyInterfaces.Logger(), false );
            var tasks = TaskDef.GetKnownTasks( false );

            for( var ii = 0; ii < tasks?.Count; ii++ )
            {
                progress.Report( 100.0 * ii / ( 1.0 * tasks.Count ) );
                var task = tasks[ ii ];
                if( task != null )
                {
                    task.Result = await TaskDef.ExecuteSubTask( _embyInterfaces, task, cancellationToken, true );
                    tasks[ ii ] = task;
                }
            }

            var overall = overAllTimer.ElapsedMilliseconds();
            db.UpdateLastUpdated( StatisticsDB.EAction.System, overall );
            EmbyHelperFuncs.UpdateLastAdded( _embyInterfaces, db );

            overAllTimer.Dispose();
            _embyInterfaces.Logger().Info( $"=======================================" );
            _embyInterfaces.Logger().Info( $"Time to Add: {overall} ms" );
            if( tasks != null )
            {
                var maxLen = 0;
                foreach( var task in tasks )
                {
                    if( task == null )
                        continue;

                    if( task.Description.Length > maxLen )
                        maxLen = task.Description.Length;
                }

                foreach( var task in tasks )
                {
                    if( task == null )
                        continue;
                    _embyInterfaces.Logger().Info( $"{task.Description.PadLeft( maxLen )}: {task.RunTimeMS} ms" );
                }
            }

            _embyInterfaces.Logger().Info( $"=======================================" );
            _embyInterfaces.Logger().Info( $"Finished Statistics 2026 {taskName} task" );

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