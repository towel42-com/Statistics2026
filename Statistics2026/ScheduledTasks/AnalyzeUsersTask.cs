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
using Statistics2026.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.ScheduledTasks
{
    public class AnalyzeUsersTask : IScheduledTask
    {
        private EmbyInterfaces _embyInterfaces;

        public AnalyzeUsersTask(
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
            _embyInterfaces = new EmbyInterfaces(fileSystem, libraryManager, logManager, logManager.GetLogger("Statistics2026 - CalculateDataTask"), serverApplicationPaths, userDataManager, userManager, appHost, apiService, jsonSerializer, providerManager, configManager, taskManager);
        }

        string IScheduledTask.Name => "\u2022 Analyze User Information";

        string IScheduledTask.Key => "Statistics2026CalculateAllUsers";

        string IScheduledTask.Description => "Task that will analyze the user data.";

        string IScheduledTask.Category => "Statistics 2026";

        Task IScheduledTask.Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (Plugin.Instance != null && Plugin.Instance.IsStatistics2026TaskRunning(this.GetType()))
            {
                throw new Exception("Statistics 2026 task is running");
            }

            var taskName = "Analyze All Users";
            _embyInterfaces!._logger.Info($"Statistics 2026 : Starting Statistics 2026 {taskName} task");
            // purely for progress reporting
            var now = DateTime.Now;

            var db = StatisticsDB.GetInstance(_embyInterfaces);
            db.Initialize(cancellationToken, progress);

            long addUsers = 0;
            using (var timer = new AutoTimer($"Adding All Users", _embyInterfaces._logger))
            {
                db.AddAllUsers();
                addUsers = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            _embyInterfaces._logger.Info($"=======================================");
            _embyInterfaces._logger.Info($"Analyze all users");
            _embyInterfaces._logger.Info($"=======================================");
            _embyInterfaces._logger.Info($"          Users: {addUsers} ms");
            _embyInterfaces._logger.Info($"=======================================");
            _embyInterfaces._logger.Info($"Statistics 2026 : Finished Statistics 2026 {taskName} task");

            db.ResetCancellationToken();
            return Task.CompletedTask;
        }

        IEnumerable<TaskTriggerInfo> IScheduledTask.GetDefaultTriggers()
        {
            return Array.Empty<TaskTriggerInfo>();
        }
    }
}