using MediaBrowser.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Tasks;
using ServiceStack.Text;
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
        private EmbyManagers _managers;

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
            _managers = new EmbyManagers(fileSystem, libraryManager, logManager, logManager.GetLogger("Statistics2026 - CalculateDataTask"), serverApplicationPaths, userDataManager, userManager, appHost, apiService, jsonSerializer, providerManager, configManager, taskManager);
        }

        private static PluginConfiguration? PluginConfiguration => Plugin.Instance?.Configuration ?? null;
        string IScheduledTask.Name => "Calculate Media and User Information for all library media and users";

        string IScheduledTask.Key => "Statistics2026CalculateStatsTask";

        string IScheduledTask.Description => "Task that will calculate Statistics for all media in library.";

        string IScheduledTask.Category => "Statistics 2026";

        Task IScheduledTask.Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _managers._logger.Info("Statistics 2026 : Starting Statistics 2026 calculate all task");
            // purely for progress reporting
            var now = DateTime.Now;
            if (PluginConfiguration == null)
                throw new ArgumentNullException(nameof(PluginConfiguration));

            PluginConfiguration.LastUpdated = now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            PluginConfiguration.Version = Plugin.Instance?.Version.ToString(4) ?? "<UNKNOWN>";
            PluginConfiguration.BuildDate = BuildDateInfo.GetBuildDate().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            PluginConfiguration.ServerId = _managers._appHost.SystemId;

            var db = StatisticsDB.GetInstance(_managers);
            db.SetCancellationToken(cancellationToken);
            db.Initialize();

            var overAllTimer = new AutoTimer($"Adding All Data", _managers._logger, false);

            var tasks = new List<(string description, Type type, long runTime, string tableName)>
            {
                ($"Adding All Users", typeof(AnalyzeUsersTask), 0, "Users"),
                ($"Analyzing User Watch Data", typeof(AnalyzeUserWatchDataTask), 0, "User Watch Data"),
                ($"Adding All Media", typeof(AnalyzeMediaTask), 0, "Collections"),
                ($"Adding Collections", typeof(AnalyzeCollectionsTask), 0, "Media"),
                ($"Adding All Series", typeof(AnalyzeSeriesTask), 0, "Series"),
                ($"Computing Percent Watched Cached Stats", typeof(CalculateWatchedShowsTask), 0, "Compute Percent Watched")
            };

            for (int ii = 0; ii < tasks.Count; ii++)
            {
                progress.Report((100.0 * ii) / (1.0 * tasks.Count));
                var task = tasks[ii];
                task.runTime = launchSubTask(task.description, task.type, cancellationToken);
                tasks[ii] = task;
            }

            db.UpdateLastUpdated(now, BuildDateInfo.GetBuildDate(), PluginConfiguration.Version);
            cancellationToken.ThrowIfCancellationRequested();

            var overall = overAllTimer.ElapsedMilliseconds();
            overAllTimer.Dispose();
            _managers._logger.Info($"=======================================");
            _managers._logger.Info($"Time to Add: {overall} ms");
            int maxLen = 0;
            foreach (var task in tasks)
            {
                if (task.tableName.Length > maxLen)
                    maxLen = task.tableName.Length;
            }
            if (maxLen > 20)
                maxLen = 20;

            foreach (var task in tasks)
            {
                _managers._logger.Info($"{task.tableName.PadLeft(maxLen)}: {task.runTime} ms");
            }
            _managers._logger.Info($"=======================================");
            _managers._logger.Info("Statistics 2026 : Finished Statistics 2026 calculate all task");
            Plugin.Instance?.SaveConfiguration();

            db.SetCancellationToken(null);
            return Task.CompletedTask;
        }

        long launchSubTask(string description, Type taskType, CancellationToken cancellationToken)
        {
            long retVal = 0;
            using (var timer = new AutoTimer(description, _managers._logger))
            {
                var taskToRun = _managers._taskManager.ScheduledTasks.FirstOrDefault(taskToRun => taskToRun.ScheduledTask.GetType() == taskType);
                if (taskToRun == null)
                    throw new Exception($"Task not found {taskType.Name}");

                var options = new TaskOptions() { HasManualInteraction = false };
                _managers._taskManager.Execute(taskToRun, options).ConfigureAwait(false).GetAwaiter().GetResult();
                cancellationToken.ThrowIfCancellationRequested();

                var taskResult = taskToRun.LastExecutionResult;
                switch (taskResult.Status)
                {
                    case TaskCompletionStatus.Completed:
                        break;
                    case TaskCompletionStatus.Cancelled:
                        _managers._taskManager.CancelIfRunning<RunAllTasksTask>();
                        break;
                    case TaskCompletionStatus.Failed:
                    case TaskCompletionStatus.Aborted:
                    default:
                        throw new Exception($"{taskType.Name} failed to run successfully");
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
                    Type = TaskTriggerInfo.TriggerWeekly,
                    DayOfWeek = DayOfWeek.Sunday,
                    TimeOfDayTicks = TimeSpan.FromMinutes(30).Ticks
                }
            };
        }
    }
}