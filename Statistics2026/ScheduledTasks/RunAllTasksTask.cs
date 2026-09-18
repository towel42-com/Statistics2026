using MediaBrowser.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Statistics2026.Api;
using Statistics2026.Configuration;
using Statistics2026.Data;
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
            ILogManager logManager,
            IServerConfigurationManager configManager,
            IUserManager userManager,
            IUserDataManager userDataManager,
            ILibraryManager libraryManager,
            IFileSystem fileSystem,
            IJsonSerializer jsonSerializer,
            IServerApplicationPaths serverApplicationPaths,
            IApplicationHost appHost,
            IProviderManager providerManager,
            Statistics2026API apiService,
            ITaskManager taskManager
            )
        {
            _embyInterfaces = new EmbyInterfaces( fileSystem, libraryManager, logManager, logManager.GetLogger( "Statistics2026 - CalculateDataTask" ), serverApplicationPaths, userDataManager, userManager, appHost, apiService, jsonSerializer, providerManager, configManager, taskManager );
        }

        private static PluginConfiguration? PluginConfiguration => Plugin.Instance?.Configuration ?? null;
        string IScheduledTask.Name => "Calculate Media and User Information for all library media and users";

        string IScheduledTask.Key => "Statistics2026CalculateStatsTask";

        string IScheduledTask.Description => "Task that will calculate Statistics for all media in library.";

        string IScheduledTask.Category => "Statistics 2026";

        Task IScheduledTask.Execute( CancellationToken cancellationToken, IProgress<double> progress )
        {
            if( Plugin.Instance != null && Plugin.Instance.IsStatistics2026TaskRunning( GetType() ) )
            {
                throw new Exception( "Statistics 2026 task is running" );
            }

            var taskName = "Analyze All";
            _embyInterfaces!._logger.Info( $"Statistics 2026 : Starting Statistics 2026 {taskName} task" );
            // purely for progress reporting
            var now = DateTime.Now;
            if( PluginConfiguration == null )
                throw new ArgumentNullException( nameof( PluginConfiguration ) );

            PluginConfiguration.LastUpdated = now.ToString( "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture );
            PluginConfiguration.Version = Plugin.Instance?.Version.ToString( 4 ) ?? "<UNKNOWN>";
            PluginConfiguration.BuildDate = BuildDateInfo.GetBuildDate().ToString( "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture );
            Plugin.Instance?.UpdateConfiguration( PluginConfiguration );

            var db = StatisticsDB.GetInstance( _embyInterfaces );
            db.Initialize( cancellationToken, progress, PluginConfiguration.resetPlayCount );

            var overAllTimer = new AutoTimer( $"Adding All Data", _embyInterfaces._logger, false );

            var tasks = Plugin.Instance?.GetKnownTasks( false );

            for( var ii = 0; ii < tasks?.Count; ii++ )
            {
                progress.Report( 100.0 * ii / ( 1.0 * tasks.Count ) );
                var task = tasks[ ii ];
                task.RunTime = launchSubTask( task, cancellationToken );
                tasks[ ii ] = task;
            }

            db.UpdateLastUpdated( now, BuildDateInfo.GetBuildDate(), PluginConfiguration.Version );
            db.Initialize( cancellationToken, progress );
            cancellationToken.ThrowIfCancellationRequested();

            var overall = overAllTimer.ElapsedMilliseconds();
            overAllTimer.Dispose();
            _embyInterfaces._logger.Info( $"=======================================" );
            _embyInterfaces._logger.Info( $"Time to Add: {overall} ms" );
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
                    _embyInterfaces._logger.Info( $"{task.Description.PadLeft( maxLen )}: {task.RunTime} ms" );
                }
            }

            _embyInterfaces._logger.Info( $"=======================================" );
            _embyInterfaces._logger.Info( $"Statistics 2026 : Finished Statistics 2026 {taskName} task" );

            Plugin.Instance?.AddDBState( EDBState.eFullyInitialized );
            Plugin.Instance?.SaveConfiguration();
            db.ResetCancellationToken();
            return Task.CompletedTask;
        }

        private long launchSubTask( TaskDef task, CancellationToken cancellationToken )
        {
            long retVal = 0;
            using( var timer = new AutoTimer( task.Description, _embyInterfaces._logger ) )
            {
                var taskToRun = _embyInterfaces._taskManager.ScheduledTasks.FirstOrDefault( taskToRun => taskToRun.ScheduledTask.GetType() == task.TaskType );
                if( taskToRun == null )
                    throw new Exception( $"Task not found {task.TaskType?.Name}" );

                var options = new TaskOptions() { HasManualInteraction = false };
                _embyInterfaces._taskManager.Execute( taskToRun, options ).ConfigureAwait( false ).GetAwaiter().GetResult();
                cancellationToken.ThrowIfCancellationRequested();

                var taskResult = taskToRun.LastExecutionResult;
                switch( taskResult.Status )
                {
                    case TaskCompletionStatus.Completed:
                        break;
                    case TaskCompletionStatus.Cancelled:
                        _ = _embyInterfaces._taskManager.CancelIfRunning<RunAllTasksTask>();
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