using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using Statistics2026.Configuration;
using Statistics2026.ScheduledTasks;
using System;
using System.Collections.Generic;

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

        public bool IsStatistics2026TaskRunning( System.Type okIfRunning )
        {
            return IsStatistics2026TaskRunning( [ typeof( RunAllTasksTask ), okIfRunning ] );
        }

        public bool IsStatistics2026TaskRunning( List<System.Type> okIfRunning )
        {
            if( _taskManager == null )
                return false;

            var knownTasks = Plugin.Instance?.GetKnownTasks( true );
            if( knownTasks == null )
                return false;

            var allTasks = _taskManager.ScheduledTasks;
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

        public List<TaskDef> GetKnownTasks( bool includeRunAll )
        {
            var tasks = new List<TaskDef>
            {
                new($"Analyzing Users", typeof(AnalyzeUsersTask)),
                new($"Analyzing Watched Media Data", typeof(AnalyzeWatchedMediaDataTask)),
                new($"Analyzing Media", typeof(AnalyzeMediaTask)),
                new($"Analyzing Collections", typeof(AnalyzeCollectionsTask)),
                new($"Analyzing Series", typeof(AnalyzeSeriesTask)),
            };
            if( includeRunAll )
            {
                tasks.Add( new TaskDef( $"RunAll", typeof( RunAllTasksTask ) ) );
            }

            return tasks;
        }
    }
}
