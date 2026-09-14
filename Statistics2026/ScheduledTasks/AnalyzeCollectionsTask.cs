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
    public class AnalyzeCollectionsTask : IScheduledTask
    {
        private EmbyInterfaces _embyInterfaces;

        public AnalyzeCollectionsTask(
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

        string IScheduledTask.Name => "\u2022 Analyze Collection information";

        string IScheduledTask.Key => "Statistics2026CalculateAllCollections";

        string IScheduledTask.Description => "Task that will analyze the library's collections.";

        string IScheduledTask.Category => "Statistics 2026";

        Task IScheduledTask.Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var taskName = "Analyze Collections";
            _embyInterfaces._logger.Info($"Statistics 2026 : Starting Statistics 2026 {taskName} task");
            // purely for progress reporting

            var db = StatisticsDB.GetInstance(_embyInterfaces);
            db.Initialize(cancellationToken, progress);
            try
            {
                db.ClearTable("Collections"); // will throw an exception if the primary has not been run yet
            }
            catch (Exception /*ex*/)
            {
                throw new Exception("Please run the 'Calculate Media and User Information for all library media and users' task");
            }

            long addCollections = 0;
            using (var timer = new AutoTimer($"Adding Collections", _embyInterfaces._logger))
            {
                db.AddAllCollections(cancellationToken, progress);
                addCollections = timer.ElapsedMilliseconds();
            }
            cancellationToken.ThrowIfCancellationRequested();


            _embyInterfaces._logger.Info($"=======================================");
            _embyInterfaces._logger.Info($"    Collections: {addCollections} ms");
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