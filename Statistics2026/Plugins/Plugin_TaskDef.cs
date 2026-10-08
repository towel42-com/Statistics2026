using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Tasks;
using Statistics2026.Configuration;
using Statistics2026.ScheduledTasks;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;

namespace Statistics2026
{
    public class TaskDef
    {
        public TaskDef() { }
        public TaskDef( string description, Type? typeOf )
        {
            Description = description;
            TaskType = typeOf;
            RunTime = 0;
        }

        public string Description { get; private set; } = string.Empty;
        public Type? TaskType { get; private set; } = null;
        public long RunTime { get; set; } = 0;
    }

    public partial class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        public bool IsStatistics2026TaskRunning()
        {
            return IsStatistics2026TaskRunning( [] );
        }

        public bool IsStatistics2026TaskRunning( MediaBrowser.Model.Tasks.IScheduledTask? runningTask )
        {
            if ( runningTask != null )
                return IsStatistics2026TaskRunning( [ typeof( RunAllTasksTask ), runningTask.GetType() ] );
            else
                return IsStatistics2026TaskRunning( [] );
        }

        public bool IsStatistics2026TaskRunning( System.Type okIfRunning )
        {
            return IsStatistics2026TaskRunning( [ typeof( RunAllTasksTask ), okIfRunning ] );
        }

        public bool IsStatistics2026TaskRunning( List<System.Type> okIfRunning )
        {
            if( _embyInterfaces._taskManager == null )
                return false;

            var knownTasks = Plugin.GetKnownTasks( true );
            if( knownTasks == null )
                return false;

            var allTasks = _embyInterfaces._taskManager.ScheduledTasks;
            foreach( var task in allTasks )
            {
                if( task.State == MediaBrowser.Model.Tasks.TaskState.Idle )
                    continue;

                foreach( var currTask in knownTasks )
                {
                    var okToBeRunning = false;
                    foreach( var okTask in okIfRunning )
                    {
                        if( okTask == task.ScheduledTask.GetType() )
                        {
                            okToBeRunning = true;
                            break;
                        }
                    }

                    if( okToBeRunning )
                        continue;

                    if( currTask.TaskType == task.ScheduledTask.GetType() )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static TaskDef? GetTaskDef( Type taskType )
        {
            var tasks = GetKnownTasks( true );
            foreach( var task in tasks )
            {
                if( task.TaskType == taskType )
                    return task;
            }
            return null;
        }

        public static List<TaskDef> GetKnownTasks( bool includeRunAll )
        {
            var tasks = new List<TaskDef>
            {
                new($"Analyzing Users", typeof(AnalyzeUsersTask)),
                new($"Analyzing Media", typeof(AnalyzeMediaTask)),
                new($"Analyzing Watched Media Data", typeof(AnalyzeWatchedMediaDataTask)),
                new($"Analyzing Collections", typeof(AnalyzeCollectionsTask)),
                new($"Analyzing Series", typeof(AnalyzeSeriesTask)),
                new($"Analyzing Missing Episodes", typeof(AnalyzeMissingEpisodesTask)),
                new($"Analyzing Missing Movies", typeof(AnalyzeMissingMoviesTask)),
            };
            if( includeRunAll )
            {
                tasks.Add( new TaskDef( $"RunAll", typeof( RunAllTasksTask ) ) );
            }

            return tasks;
        }


        public static async Task<long> launchSubTask( EmbyInterfaces embyInterfaces, Type taskType, CancellationToken cancellationToken )
        {
            var taskDef = Plugin.GetTaskDef( taskType );
            if( taskDef == null )
                return 0;

            var retVal = await launchSubTask( embyInterfaces, taskDef, cancellationToken );
            return retVal;
        }

        public static async Task<long> launchSubTask( EmbyInterfaces embyInterfaces, TaskDef? task, CancellationToken cancellationToken )
        {
            if( task == null )
                return 0;

            long retVal = 0;
            using( var timer = new AutoTimer( task.Description, embyInterfaces._logger ) )
            {
                var taskToRun = embyInterfaces._taskManager!.ScheduledTasks.FirstOrDefault( taskToRun => taskToRun.ScheduledTask.GetType() == task.TaskType );
                if( taskToRun == null )
                    throw new Exception( $"Task not found {task.TaskType?.Name}" );

                var options = new TaskOptions() { HasManualInteraction = false };
                var taskRunner = embyInterfaces._taskManager.Execute( taskToRun, options ).ConfigureAwait( false );
                await taskRunner;

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
    }
}
