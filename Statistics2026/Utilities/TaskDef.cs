using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Tasks;
using ServiceStack.Text;
using Statistics2026.Configuration;
using Statistics2026.EventMonitorEntryPoints;
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
        public TaskDef( string name, string description, Type typeOf )
        {
            Name = name;
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

        public string Name { get; private set; } = string.Empty;
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
                okIfRunning.AddRange( [ typeof( ItemAddedMonitorEntryPoint ), typeof( RunAllTasksTask ) ] );
            }
            var knownTasks = TaskDef.GetKnownTasks( true );

            var allTasks = embyInterfaces.TaskManager().ScheduledTasks;
            foreach( var task in allTasks )
            {
                if( task.State == MediaBrowser.Model.Tasks.TaskState.Idle )
                    continue;

                foreach( var currTask in knownTasks )
                {
                    if( currTask == null )
                        continue;

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

        public static TaskDef? GetTaskDef( Type taskType, ref int pos )
        {
            pos = -1;
            var tasks = GetKnownTasks( true );
            for( int ii = 0; ii < tasks.Count(); ++ii )
            {
                var task = tasks[ ii ];
                if( task.TaskType == taskType )
                {
                    pos = ii;
                    return task;
                }
            }
            return null;
        }

        public static TaskDef? GetTaskDef( Type taskType )
        {
            int pos = -1;
            return GetTaskDef( taskType, ref pos );
        }

        public static string GetTaskName( Type taskType )
        {
            int pos = -1;
            var task = GetTaskDef( taskType, ref pos );
            if( task == null )
                throw new Exception( $"Could not find task defintion for type '{taskType}'" );

            if ( taskType != typeof( RunAllTasksTask ) )
                return $"\u2022 {pos + 1} - {task.Name}";
            return task.Name;
        }

        public static string GetTaskDescription( Type taskType )
        {
            var task = GetTaskDef( taskType );
            if( task == null )
                throw new Exception( $"Could not find task defintion for type '{taskType}'" );

            return task.Description;
        }

        public static List<TaskDef> GetKnownTasks( bool includeRunAll )
        {
            var tasks = new List<TaskDef>
            {
                new($"Analyze User Information", "Analyze the users on the server for use in the analysis.", typeof(AnalyzeUsersTask)),
                new($"Analyze Media Information", "Analyze the video media on the server use in the analysis.", typeof(AnalyzeMediaTask)),
                new($"Analyze User Watched Data Information", "Analyze the users watched/played information.", typeof(AnalyzeWatchedMediaDataTask)),
                new($"Analyze Collection Information", "Analyze the library's collections.", typeof(AnalyzeCollectionsTask)),
                new($"Analyzing Series Information", "Analyze the library's series.", typeof(AnalyzeSeriesTask)),
                new($"Analyze Streaming Providers for Media", "Find streaming providers for the media in the library.", typeof(AnalyzeStreamingProvidersTask)),
                new($"Analyze Missing Episodes", "Analyze the library for missing episodes from existing series.", typeof(AnalyzeMissingEpisodesTask)),
                new($"Analyze Missing Movies", "Analyze the library for missing movies from existing collections.", typeof(AnalyzeMissingMoviesTask)),
            };
            if( includeRunAll )
            {
                tasks.Add( new TaskDef( $"Run All Tasks", "Run all the tasks for Statistical Analysis", typeof( RunAllTasksTask ) ) );
            }

            return tasks;
        }

        public static async Task<List<TaskResult>> ExecuteSubTasksAsync( EmbyInterfaces embyInterfaces, List<Type> taskTypes, CancellationToken? cancellationToken, bool runAllOkIfRunning )
        {
            List<TaskDef?> taskDefs = [];
            foreach( var taskType in taskTypes )
            {
                var taskDef = GetTaskDef( taskType );
                taskDefs.Add( taskDef );
            }

            return await ExecuteSubTasksAsync( embyInterfaces, taskDefs, cancellationToken, runAllOkIfRunning ).ConfigureAwait( false );
        }

        public static async Task<List<TaskResult>> ExecuteSubTasksAsync( EmbyInterfaces embyInterfaces, List<TaskDef?> taskDefs, CancellationToken? cancellationToken, bool runAllOkIfRunning )
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
                    var taskResult = await ExecuteSubTaskAsync( embyInterfaces, taskDef, cancellationToken, runAllOkIfRunning ).ConfigureAwait( false );
                    beenCanceled = beenCanceled || ( taskResult.Status == TaskCompletionStatus.Cancelled );
                    retVal.Add( taskResult );
                    cancellationToken?.ThrowIfCancellationRequested();
                }
            }
            return retVal;
        }

        public static async Task<TaskResult> ExecuteSubTaskAsync( EmbyInterfaces embyInterfaces, Type taskType, CancellationToken? cancellationToken, bool runAllOkIfRunning )
        {
            var taskDef = GetTaskDef( taskType );
            if( taskDef == null )
            {
                return TaskDef.EmptyTaskResult;
            }

            return await ExecuteSubTaskAsync( embyInterfaces, taskDef, cancellationToken, runAllOkIfRunning ).ConfigureAwait( false );
        }

        public static async Task<TaskResult> ExecuteSubTaskAsync( EmbyInterfaces embyInterfaces, TaskDef? task, CancellationToken? cancellationToken, bool runAllOkIfRunning )
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

            using( var timer = new AutoTimer( task.Description, embyInterfaces.Logger() ) )
            {
                var taskToRun = embyInterfaces.TaskManager().ScheduledTasks.FirstOrDefault( taskToRun => taskToRun.ScheduledTask.GetType() == task.TaskType );
                if( taskToRun == null )
                    throw new Exception( $"Task not found {task.TaskType?.Name}" );

                var options = new TaskOptions() { HasManualInteraction = false };
                await embyInterfaces.TaskManager().Execute( taskToRun, options ).ConfigureAwait( false );

                cancellationToken?.ThrowIfCancellationRequested();

                var taskResult = taskToRun.LastExecutionResult;
                switch( taskResult.Status )
                {
                    case TaskCompletionStatus.Completed:
                        break;
                    case TaskCompletionStatus.Cancelled:
                        _ = embyInterfaces.TaskManager().CancelIfRunning<RunAllTasksTask>();
                        break;
                    case TaskCompletionStatus.Failed:
                    case TaskCompletionStatus.Aborted:
                    default:
                        throw new Exception( $"{task.TaskType?.Name} failed to run successfully" );
                }

                retVal = taskResult;
                cancellationToken?.ThrowIfCancellationRequested();
            }

            return retVal;
        }
    }
}
