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
    public class AnalyzeSeriesTask : IScheduledTask
    {
        private EmbyInterfaces _embyInterfaces;

        public AnalyzeSeriesTask(
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

        string IScheduledTask.Name => "• Analyze Series information";

        string IScheduledTask.Key => "Statistics2026CalculateSeriesTask";

        string IScheduledTask.Description => "Task that will analyze the library's series.";

        string IScheduledTask.Category => "Statistics 2026";

        Task IScheduledTask.Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (_embyInterfaces != null && _embyInterfaces.IsStatistics2026TaskRunning(this.GetType()))
            {
                throw new Exception("Statistics 2026 task is running");
            }

            var taskName = "Analyze Series";
            _embyInterfaces!._logger.Info($"Statistics 2026 : Starting Statistics 2026 {taskName} task");
            var db = StatisticsDB.GetInstance(_embyInterfaces);
            db.Initialize(cancellationToken, progress);

            try
            {
                db.ClearTable("Series"); // will throw an exception if the primary has not been run yet
            }
            catch (Exception /*ex*/)
            {
                throw new Exception("Please run the 'Calculate Media and User Information for all library media and users' task");
            }


            long addSeries = 0;
            using (var timer = new AutoTimer($"Adding All Series", _embyInterfaces._logger))
            {
                db.AddAllSeries();
                addSeries = timer.ElapsedMilliseconds();
            }
            cancellationToken.ThrowIfCancellationRequested();

            _embyInterfaces._logger.Info($"=======================================");
            _embyInterfaces._logger.Info($"         Series: {addSeries} ms");
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