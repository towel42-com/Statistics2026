using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using ServiceStack;
using Statistics2026.Configuration;
using Statistics2026.ScheduledTasks;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace Statistics2026
{
    public class TaskDef
    {
        public TaskDef() { }
        public TaskDef(string description, Type? typeOf, long runTime, string tableName)
        {
            Description = description;
            TaskType = typeOf;
            RunTime = runTime;
            TableName = tableName;
        }

        public string Description { get; private set; } = string.Empty;
        public Type? TaskType { get; private set; } = null;
        public long RunTime { get; set; } = 0;
        public string TableName { get; private set; } = string.Empty;
    }

    public partial class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        public bool IsStatistics2026TaskRunning()
        {
            return IsStatistics2026TaskRunning(new List<System.Type>());
        }

        public bool IsStatistics2026TaskRunning(System.Type okIfRunning)
        {
            return IsStatistics2026TaskRunning(new List<System.Type>() { typeof(RunAllTasksTask), okIfRunning });
        }

        public bool IsStatistics2026TaskRunning(List<System.Type> okIfRunning)
        {
            if (_taskManager == null)
                return false;

            var knownTasks = Plugin.Instance?.GetKnownTasks(true);
            if (knownTasks == null)
                return false;

            var allTasks = _taskManager.ScheduledTasks;
            foreach (var task in allTasks)
            {
                if (task.State == MediaBrowser.Model.Tasks.TaskState.Idle)
                    continue;

                foreach (var currTask in knownTasks)
                {
                    bool okToBeRunning = false;
                    foreach (var okTask in okIfRunning)
                    {
                        if (okTask == task.ScheduledTask.GetType())
                        {
                            okToBeRunning = true;
                            break;
                        }
                    }

                    if (okToBeRunning)
                        continue;

                    if (currTask.TaskType == task.ScheduledTask.GetType())
                    {
                        return true;
                    }
                }

            }
            return false;
        }

        public List<TaskDef> GetKnownTasks(bool includeRunAll)
        {
            var tasks = new List<TaskDef>
            {
                new TaskDef($"Analyzing Users", typeof(AnalyzeUsersTask), 0, "Users"),
                new TaskDef($"Analyzing User Watch Data", typeof(AnalyzeUserWatchDataTask), 0, "UserWatchData"),
                new TaskDef($"Analyzing Media", typeof(AnalyzeMediaTask), 0, "Media"),
                new TaskDef($"Analyzing Collections", typeof(AnalyzeCollectionsTask), 0, "Collections"),
                new TaskDef($"Analyzing Series", typeof(AnalyzeSeriesTask), 0, "Series"),
            };
            if (includeRunAll)
            {
                tasks.Add(new TaskDef($"RunAll", typeof(RunAllTasksTask), 0, ""));
            }

            return tasks;
        }
    }
}
