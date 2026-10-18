using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Tasks;
using ServiceStack.Text;
using Statistics2026.Configuration;
using Statistics2026.ScheduledTasks;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;


namespace Statistics2026.Utilities
{
    public class TaskDef
    {
        public TaskDef() { }
        public TaskDef( string description, Type? typeOf )
        {
            Description = description;
            TaskType = typeOf;
        }

        public static TaskResult EmptyTaskResult
        {
            get
            {
                var retVal = new TaskResult()
                {
                    StartTimeUtc = DateTimeOffset.UtcNow,
                    Status = TaskCompletionStatus.Completed
                };
                retVal.EndTimeUtc = retVal.StartTimeUtc; // make sure RunTimeMS is 0
                return retVal;
            }
        }

        public string Description { get; private set; } = string.Empty;
        public Type? TaskType { get; private set; } = null;
        public TaskResult Result { get; set; } = EmptyTaskResult;
        public long RunTimeMS
        {
            get
            {
                var difference = Result.EndTimeUtc - Result.StartTimeUtc;
                return (long)difference.TotalMilliseconds;
            }
        }

        public static bool IsStatistics2026TaskRunning( EmbyInterfaces embyInterfaces, bool runAllOkIfRunning )
        {
            return IsStatistics2026TaskRunning( embyInterfaces, [], runAllOkIfRunning );
        }

        public static bool IsStatistics2026TaskRunning( EmbyInterfaces embyInterfaces, MediaBrowser.Model.Tasks.IScheduledTask? runningTask, bool runAllOkIfRunning )
        {
            if( runningTask != null )
                return IsStatistics2026TaskRunning( embyInterfaces, [ runningTask.GetType() ], runAllOkIfRunning );
            else
                return IsStatistics2026TaskRunning( embyInterfaces, [], runAllOkIfRunning );
        }

        public static bool IsStatistics2026TaskRunning( EmbyInterfaces embyInterfaces, System.Type okIfRunning, bool runAllOkIfRunning )
        {
            return IsStatistics2026TaskRunning( embyInterfaces, [ okIfRunning ], runAllOkIfRunning );
        }

        public static bool IsStatistics2026TaskRunning( EmbyInterfaces embyInterfaces, List<System.Type> okIfRunning, bool runAllOkIfRunning )
        {
            if( embyInterfaces._taskManager == null )
                return false;

            if( runAllOkIfRunning )
            {
                okIfRunning.AddRange( [ typeof( PostMediaScanTask ), typeof( RunAllTasksTask ) ] );
            }
            var knownTasks = TaskDef.GetKnownTasks( true );

            var allTasks = embyInterfaces._taskManager.ScheduledTasks;
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
                tasks.Add( new TaskDef( $"PostMediaScan", typeof( PostMediaScanTask ) ) );
                tasks.Add( new TaskDef( $"RunAll", typeof( RunAllTasksTask ) ) );
            }

            return tasks;
        }

        public static async Task<List<TaskResult>> ExecuteSubTasks( EmbyInterfaces embyInterfaces, List<Type> taskTypes, CancellationToken cancellationToken, bool runAllOkIfRunning )
        {
            List<TaskDef?> taskDefs = [];
            foreach( var taskType in taskTypes )
            {
                var taskDef = GetTaskDef( taskType );
                taskDefs.Add( taskDef );
            }

            return await ExecuteSubTasks( embyInterfaces, taskDefs, cancellationToken, runAllOkIfRunning );
        }

        public static async Task<List<TaskResult>> ExecuteSubTasks( EmbyInterfaces embyInterfaces, List<TaskDef?> taskDefs, CancellationToken cancellationToken, bool runAllOkIfRunning )
        {
            List<TaskResult> retVal = [];
            var beenCanceled = false;
            foreach( var taskDef in taskDefs )
            {
                if( beenCanceled )
                {
                    retVal.Add( TaskDef.EmptyTaskResult );
                }
                else
                {
                    var taskResult = await ExecuteSubTask( embyInterfaces, taskDef, cancellationToken, runAllOkIfRunning );
                    beenCanceled = beenCanceled || ( taskResult.Status == TaskCompletionStatus.Cancelled );
                    retVal.Add( taskResult );
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            return retVal;
        }

        public static async Task<TaskResult> ExecuteSubTask( EmbyInterfaces embyInterfaces, Type taskType, CancellationToken cancellationToken, bool runAllOkIfRunning )
        {
            var taskDef = GetTaskDef( taskType );
            if( taskDef == null )
            {
                return TaskDef.EmptyTaskResult;
            }

            return await ExecuteSubTask( embyInterfaces, taskDef, cancellationToken, runAllOkIfRunning );
        }

        public static async Task<TaskResult> ExecuteSubTask( EmbyInterfaces embyInterfaces, TaskDef? task, CancellationToken cancellationToken, bool runAllOkIfRunning )
        {
            TaskResult retVal = TaskDef.EmptyTaskResult;
            if( task == null )
                return retVal;

            if( TaskDef.IsStatistics2026TaskRunning( embyInterfaces, runAllOkIfRunning ) )
            {
                var debouncer = new Debouncer();
                debouncer.RunLater( 5000, async ( cancellationToken ) =>
                {
                    if( TaskDef.IsStatistics2026TaskRunning( embyInterfaces, runAllOkIfRunning ) )
                        return true;

                    return false;
                } );
            }

            using( var timer = new AutoTimer( task.Description, embyInterfaces._logger ) )
            {
                var taskToRun = embyInterfaces._taskManager!.ScheduledTasks.FirstOrDefault( taskToRun => taskToRun.ScheduledTask.GetType() == task.TaskType );
                if( taskToRun == null )
                    throw new Exception( $"Task not found {task.TaskType?.Name}" );

                var options = new TaskOptions() { HasManualInteraction = false };
                await embyInterfaces._taskManager.Execute( taskToRun, options ).ConfigureAwait( false );

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

                retVal = taskResult;
                cancellationToken.ThrowIfCancellationRequested();
            }

            return retVal;
        }
    }
}
