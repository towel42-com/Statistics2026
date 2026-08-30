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
    public class CalculateWatchedShowsTask : IScheduledTask
    {
        private EmbyManagers _managers;

        public CalculateWatchedShowsTask(
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
            _managers = new EmbyManagers(fileSystem, libraryManager, logManager, logManager.GetLogger("Statistics2026 - CalculateWatchedShowsTask"), serverApplicationPaths, userDataManager, userManager, appHost, apiService, jsonSerializer, providerManager, config, taskManager);
        }

        string IScheduledTask.Name => "\u2022 Calculating Watched Shows";

        string IScheduledTask.Key => "Statistics2026CalculateWatchedShowsTask";

        string IScheduledTask.Description => "Task that will calculate the most (and least) watched shows.";

        string IScheduledTask.Category => "Statistics 2026";

        Task IScheduledTask.Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _managers._logger.Info("Statistics 2026 : Starting Statistics 2026 Weighted Watch Analysis");
            var db = StatisticsDB.GetInstance(_managers);
            db.SetCancellationToken(cancellationToken);
            try
            {
                db.ClearTable("CachedWatchedAnalysis"); // will throw an exception if the primary has not been run yet
            }
            catch (Exception /*ex*/)
            {
                throw new Exception("Please run the 'Calculate Media and User Information for all library media and users' task");
            }

            long computePercentWatchedCache = 0;
            using (var timer = new AutoTimer($"Computing Percent Watched Cached Stats", _managers._logger))
            {
                db.ComputePercentWatchedCache(cancellationToken, progress);
                computePercentWatchedCache = timer.ElapsedMilliseconds();
            }

            cancellationToken.ThrowIfCancellationRequested();

            _managers._logger.Info($"=======================================");
            _managers._logger.Info($"  Compute Percent Watched Cache: {computePercentWatchedCache} ms");
            _managers._logger.Info($"=======================================");
            _managers._logger.Info("Statistics 2026 : Finished Statistics 2026 Watched Show Analysis");

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