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
    public class AnalyzeMediaTask : IScheduledTask
    {
        private EmbyInterfaces _embyInterfaces;

        public AnalyzeMediaTask(
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

        string IScheduledTask.Name => "\u2022 Analyze Media information";

        string IScheduledTask.Key => "Statistics2026CalculateAllMediaTask";

        string IScheduledTask.Description => "Task that will analyze the library's media.";

        string IScheduledTask.Category => "Statistics 2026";

        Task IScheduledTask.Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (_embyInterfaces != null && _embyInterfaces.IsStatistics2026TaskRunning(this.GetType()))
            {
                throw new Exception("Statistics 2026 task is running");
            }

            var taskName = "Analyze Media";
            _embyInterfaces!._logger.Info($"Statistics 2026 : Starting Statistics 2026 {taskName} task");
            // purely for progress reporting

            var db = StatisticsDB.GetInstance(_embyInterfaces);
            db.Initialize(cancellationToken, progress);

            long addMedia = 0;
            using (var timer = new AutoTimer($"Adding All Media", _embyInterfaces._logger))
            {
                db.AddAllMedia();
                addMedia = timer.ElapsedMilliseconds();
            }

            cancellationToken.ThrowIfCancellationRequested();

            _embyInterfaces._logger.Info($"=======================================");
            _embyInterfaces._logger.Info($"          Media: {addMedia} ms");
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