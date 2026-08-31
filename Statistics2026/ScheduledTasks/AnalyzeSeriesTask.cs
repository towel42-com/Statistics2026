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
        private EmbyManagers _managers;

        public AnalyzeSeriesTask(
            ILogManager logManager,
            IServerConfigurationManager config,
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
            _managers = new EmbyManagers(fileSystem, libraryManager, logManager, logManager.GetLogger("Statistics2026 - CalculateDataTask"), serverApplicationPaths, userDataManager, userManager, appHost, apiService, jsonSerializer, providerManager, config, taskManager);
        }

        string IScheduledTask.Name => "• Analyze Series information";

        string IScheduledTask.Key => "Statistics2026CalculateSeriesTask";

        string IScheduledTask.Description => "Task that will analyze the library's series.";

        string IScheduledTask.Category => "Statistics 2026";

        Task IScheduledTask.Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _managers._logger.Info("Statistics 2026 : Starting Statistics 2026 series task");
            var db = StatisticsDB.GetInstance(_managers);
            db.SetCancellationToken(cancellationToken);

            try
            {
                db.ClearTable("Series"); // will throw an exception if the primary has not been run yet
            }
            catch (Exception /*ex*/)
            {
                throw new Exception("Please run the 'Calculate Media and User Information for all library media and users' task");
            }


            long addSeries = 0;
            using (var timer = new AutoTimer($"Adding All Series", _managers._logger))
            {
                db.AddAllSeries(cancellationToken, progress);
                addSeries = timer.ElapsedMilliseconds();
            }
            cancellationToken.ThrowIfCancellationRequested();

            _managers._logger.Info($"=======================================");
            _managers._logger.Info($"         Series: {addSeries} ms");
            _managers._logger.Info($"=======================================");
            _managers._logger.Info("Statistics 2026 : Finished Statistics 2026 series task");

            Plugin.Instance?.SaveConfiguration();

            db.SetCancellationToken(null);
            return Task.CompletedTask;
        }

        IEnumerable<TaskTriggerInfo> IScheduledTask.GetDefaultTriggers()
        {
            return null!;
        }
    }
}