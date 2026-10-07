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
            return RunAllTasks( this, _embyInterfaces, progress, cancellationToken );
        }

        public static Task RunAllTasks( object sender, EmbyInterfaces embyInterfaces, IProgress<double> progress, CancellationToken cancellationToken )
        {
            if( Plugin.Instance != null && Plugin.Instance.IsStatistics2026TaskRunning( sender.GetType() ) )
            {
                throw new Exception( "Statistics 2026 task is running" );
            }

            var taskName = "Analyze All";
            embyInterfaces._logger!.Info( $"Statistics 2026 : Starting Statistics 2026 {taskName} task" );
            // purely for progress reporting
            var now = DateTime.UtcNow;
            if( PluginConfiguration == null )
                throw new ArgumentNullException( nameof( PluginConfiguration ) );

            PluginConfiguration.LastUpdated = now.ToString( "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture );
            PluginConfiguration.Version = Plugin.Instance?.Version.ToString( 4 ) ?? "<UNKNOWN>";
            PluginConfiguration.BuildDate = BuildDateInfo.GetBuildDate().ToString( "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture );
            Plugin.Instance?.UpdateConfiguration( PluginConfiguration );

            var db = StatisticsDB.GetInstance( embyInterfaces );
            db.Initialize( cancellationToken, progress, PluginConfiguration.resetPlayCount );

            db.ClearLastUpdated( StatisticsDB.EAction.System );

            var overAllTimer = new AutoTimer( $"Adding All Data", embyInterfaces._logger, false );
            var tasks = Plugin.Instance?.GetKnownTasks( false );

            for( var ii = 0; ii < tasks?.Count; ii++ )
            {
                progress.Report( 100.0 * ii / ( 1.0 * tasks.Count ) );
                var task = tasks[ ii ];
                task.RunTime = launchSubTask( embyInterfaces, task, cancellationToken );
                tasks[ ii ] = task;
            }

            db.Initialize( cancellationToken, progress );
            cancellationToken.ThrowIfCancellationRequested();

            var overall = overAllTimer.ElapsedMilliseconds();
            db.UpdateLastUpdated( StatisticsDB.EAction.System, overall );
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

                if( maxLen > 20 )
                    maxLen = 20;

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
            return Task.CompletedTask;
        }

        private static long launchSubTask( EmbyInterfaces embyInterfaces, TaskDef task, CancellationToken cancellationToken )
        {
            long retVal = 0;
            using( var timer = new AutoTimer( task.Description, embyInterfaces._logger ) )
            {
                var taskToRun = embyInterfaces._taskManager!.ScheduledTasks.FirstOrDefault( taskToRun => taskToRun.ScheduledTask.GetType() == task.TaskType );
                if( taskToRun == null )
                    throw new Exception( $"Task not found {task.TaskType?.Name}" );

                var options = new TaskOptions() { HasManualInteraction = false };
                embyInterfaces._taskManager.Execute( taskToRun, options ).ConfigureAwait( false ).GetAwaiter().GetResult();
                cancellationToken.ThrowIfCancellationRequested();

                var taskResult = taskToRun.LastExecutionResult;
                switch( taskResult.Status )
                {
                    case TaskCompletionStatus.Completed:
                        break;
                    case TaskCompletionStatus.Cancelled:
                        _ = embyInterfaces._taskManager.CancelIfRunning<RunAllTasksTask>();
                        break;
                    case TaskCompletionStatus.Failed:
                    case TaskCompletionStatus.Aborted:
                    default:
                        throw new Exception( $"{task.TaskType?.Name} failed to run successfully" );
                }

                retVal = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            return retVal;
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