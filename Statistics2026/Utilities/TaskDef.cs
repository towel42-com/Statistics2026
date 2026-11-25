using MediaBrowser.Model.Tasks;
using Statistics2026.EventMonitorEntryPoints;
using Statistics2026.ScheduledTasks;


namespace Statistics2026.Utilities
{
    public enum ETaskGroups
    {
        eNone         = 0x00,
        eMissing      = 0x01,
        eAllButRunAll = 0x02,
        eRunAll       = 0x04,
        eAllOrMissing = 0x03,
        eAll          = 0x06,
    };

    public class TaskDef
    {
        public static List<string> DefaultTaskReasons = new List<string> { "Task" };

        public TaskDef() { }
        public TaskDef( int pos, string name, string description, Type typeOf )
        {
            Pos = pos;
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

        public int Pos { get; private set; } = -1;
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public Type? TaskType { get; private set; } = null;
        public TaskResult Result { get; set; } = EmptyTaskResult;
        public long RunTimeMS
        {
            get
            {
                TimeSpan difference = Result.EndTimeUtc - Result.StartTimeUtc;
                return (long)difference.TotalMilliseconds;
            }
        }

        public static bool IsTaskRunning( EmbyInterfaces embyInterfaces, MediaBrowser.Model.Tasks.IScheduledTask runningTask )
        {
            return IsTaskRunning( embyInterfaces, runningTask.GetType() );
        }

        public static bool IsTaskRunning( EmbyInterfaces embyInterfaces, System.Type? runningTask )
        {
            if( runningTask == null )
                return false;

            if( embyInterfaces._taskManager == null )
                return false;

            IScheduledTaskWorker[] allTasks = embyInterfaces.TaskManager().ScheduledTasks;
            foreach( IScheduledTaskWorker? task in allTasks )
            {
                if( task.State == MediaBrowser.Model.Tasks.TaskState.Idle )
                    continue;

                if( runningTask == task.ScheduledTask.GetType() )
                {
                    return true;
                }
            }
            return false;
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
            List<TaskDef> knownTasks = TaskDef.GetKnownTasks( ETaskGroups.eAll );

            IScheduledTaskWorker[] allTasks = embyInterfaces.TaskManager().ScheduledTasks;
            foreach( IScheduledTaskWorker? task in allTasks )
            {
                if( task.State == MediaBrowser.Model.Tasks.TaskState.Idle )
                    continue;

                foreach( TaskDef currTask in knownTasks )
                {
                    if( currTask == null )
                        continue;

                    bool okToBeRunning = false;
                    foreach( Type okTask in okIfRunning )
                    {
                        if( okTask == task.ScheduledTask.GetType() )
                        {
                            okToBeRunning = true;
                            break;
                        }
                    }

                    if( okToBeRunning )
                        break;

                    if( currTask.TaskType == task.ScheduledTask.GetType() )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static string GetTaskName( Type taskType )
        {
            TaskDef task = GetTaskDef( taskType );

            if( ( taskType != typeof( RunAllTasksTask ) ) && ( task.Pos != -1 ) )
                return $"\u2022 {task.Pos} - {task.Name}";
            return task.Name;
        }

        public static string GetTaskDescription( Type taskType )
        {
            TaskDef task = GetTaskDef( taskType );
            return task.Description;
        }

        private static Dictionary<Type, (ETaskGroups taskGroup, Func<TaskDef> factory) > sTaskDefMap = new()
            {
                { typeof( AnalyzeUsersTask ),              (ETaskGroups.eAllButRunAll, () => { return new TaskDef( 1, $"Analyze User Information", "Analyze the users on the server for use in the analysis.",                     typeof( AnalyzeUsersTask ) ); } ) },
                { typeof( AnalyzeMediaTask ),              (ETaskGroups.eAllOrMissing, () => { return new TaskDef( 2, $"Analyze Media Information", "Analyze the video media on the server for use in the analysis.",              typeof( AnalyzeMediaTask ) ); } ) },
                { typeof( AnalyzePeopleTask ),             (ETaskGroups.eAllButRunAll, () => { return new TaskDef( 3, $"Analyze Actor Information", "Analyze the actors associated with media on the server use in the analysis.", typeof( AnalyzePeopleTask ) ); } ) },
                { typeof( AnalyzeWatchedMediaDataTask ),   (ETaskGroups.eAllButRunAll, () => { return new TaskDef( 4, $"Analyze User Watched Data Information", "Analyze the users watched/played information.",                   typeof( AnalyzeWatchedMediaDataTask ) ); } ) },
                { typeof( AnalyzeSeriesTask ),             (ETaskGroups.eAllOrMissing, () => { return new TaskDef( 5, $"Analyze Series Information", "Analyze the library's series.",                                              typeof( AnalyzeSeriesTask ) ); } ) },
                { typeof( AnalyzeMissingEpisodesTask ),    (ETaskGroups.eAllOrMissing, () => { return new TaskDef( 6, $"Analyze Missing Episodes", "Analyze the library for missing episodes from existing series.",               typeof( AnalyzeMissingEpisodesTask ) ); } ) },
                { typeof( AnalyzeCollectionsTask ),        (ETaskGroups.eAllOrMissing, () => { return new TaskDef( 7, $"Analyze Collection Information", "Analyze the library's collections.",                                     typeof( AnalyzeCollectionsTask ) ); } ) },
                { typeof( AnalyzeMissingMoviesTask ),      (ETaskGroups.eAllOrMissing, () => { return new TaskDef( 8, $"Analyze Missing Movies", "Analyze the library for missing movies from existing collections.",              typeof( AnalyzeMissingMoviesTask ) ); } ) },
                { typeof( AnalyzeStreamingProvidersTask ), (ETaskGroups.eAllOrMissing, () => { return new TaskDef( 9, $"Analyze Streaming Providers for Media", "Find streaming providers for the media in the library.",          typeof( AnalyzeStreamingProvidersTask ) ); } ) },
                { typeof( RunAllTasksTask ),               (ETaskGroups.eRunAll,       () => { return new TaskDef(-1, $"Run all Tasks", "Run All Tasks.",                                                                          typeof( RunAllTasksTask ) ); } ) },
            };

        private static TaskDef GetTaskDef( Type taskType )
        {
            if( !sTaskDefMap.TryGetValue( taskType, out (ETaskGroups taskGroup, Func<TaskDef> factory) value ) )
            {
                throw new Exception( "Invalid task type" );
            }
            return value.factory.Invoke();
        }

        public static List<TaskDef> GetKnownTasks( ETaskGroups taskGroup )
         {
            var retVal = sTaskDefMap
                .Where( entry => ( entry.Value.taskGroup & taskGroup ) != 0 )
                .Select( entry => entry.Value.factory.Invoke() )
                .OrderBy( taskDef => taskDef.Pos )
                .ToList();
            return retVal;
        }

        public static Task<List<TaskResult>> ExecuteSubTasksAsync( EmbyInterfaces embyInterfaces, List<Type> taskTypes, List<string> reasons, CancellationToken? cancellationToken, bool runAllOkIfRunning )
        {
            List<TaskDef?> taskDefs = [];
            foreach( Type taskType in taskTypes )
            {
                TaskDef? taskDef = GetTaskDef( taskType );
                taskDefs.Add( taskDef );
            }

            return ExecuteSubTasksAsync( embyInterfaces, taskDefs, reasons, cancellationToken, runAllOkIfRunning );
        }

        public static async Task<List<TaskResult>> ExecuteSubTasksAsync( EmbyInterfaces embyInterfaces, List<TaskDef?> taskDefs, List<string> reasons, CancellationToken? cancellationToken, bool runAllOkIfRunning )
        {
            List<TaskResult> retVal = [];
            bool beenCanceled = false;
            foreach( TaskDef? taskDef in taskDefs )
            {
                if( beenCanceled )
                {
                    retVal.Add( TaskDef.EmptyTaskResult );
                }
                else
                {
                    TaskResult taskResult = await ExecuteSubTaskAsync( embyInterfaces, taskDef, reasons, cancellationToken, runAllOkIfRunning ).ConfigureAwait( false );
                    beenCanceled = beenCanceled || ( taskResult.Status == TaskCompletionStatus.Cancelled );
                    retVal.Add( taskResult );
                    cancellationToken?.ThrowIfCancellationRequested();
                }
            }
            return retVal;
        }

        public static async Task<TaskResult> ExecuteSubTaskAsync( EmbyInterfaces embyInterfaces, Type taskType, List<string> reasons, CancellationToken? cancellationToken, bool runAllOkIfRunning )
        {
            TaskDef? taskDef = GetTaskDef( taskType );
            if( taskDef == null )
            {
                return TaskDef.EmptyTaskResult;
            }

            return await ExecuteSubTaskAsync( embyInterfaces, taskDef, reasons, cancellationToken, runAllOkIfRunning ).ConfigureAwait( false );
        }

        public static async Task<TaskResult> ExecuteSubTaskAsync( EmbyInterfaces embyInterfaces, TaskDef? task, List<string> reasons, CancellationToken? cancellationToken, bool runAllOkIfRunning )
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

            if( reasons.Count() == 0 )
            {
                reasons = DefaultTaskReasons;
            }

            using( var timer = new AutoTimer( task.Description, embyInterfaces.Logger() ) )
            {
                IScheduledTaskWorker taskToRun = embyInterfaces.TaskManager().ScheduledTasks.FirstOrDefault( currTask => currTask.ScheduledTask.GetType() == task.TaskType );
                if( taskToRun == null )
                    throw new Exception( $"Task not found {task.TaskType?.Name}" );

                var options = new TaskOptions() { HasManualInteraction = false };
                if( taskToRun.ScheduledTask is IStatistics2026ScheduledTask )
                {
                    ( taskToRun.ScheduledTask as IStatistics2026ScheduledTask )!.ReasonsForExecute = reasons;
                }
                await embyInterfaces.TaskManager().Execute( taskToRun, options ).ConfigureAwait( false );

                cancellationToken?.ThrowIfCancellationRequested();

                TaskResult taskResult = taskToRun.LastExecutionResult;
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

                if( taskToRun.ScheduledTask is IStatistics2026ScheduledTask )
                {
                    ( taskToRun.ScheduledTask as IStatistics2026ScheduledTask )!.ReasonsForExecute = DefaultTaskReasons;
                }

                retVal = taskResult;
                cancellationToken?.ThrowIfCancellationRequested();
            }

            return retVal;
        }
    }
}
