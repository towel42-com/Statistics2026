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
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.ScheduledTasks
{
    public class CalculateDataTask : IScheduledTask
    {
        private EmbyManagers _managers;

        public CalculateDataTask(
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

        private static PluginConfiguration? PluginConfiguration => Plugin.Instance?.Configuration ?? null;
        string IScheduledTask.Name => "Calculate Media and User Information for all library media and users";

        string IScheduledTask.Key => "Statistics2026CalculateStatsTask";

        string IScheduledTask.Description => "Task that will calculate Statistics for all media in library.";

        string IScheduledTask.Category => "Statistics 2026";

        Task IScheduledTask.Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _managers._logger.Info("Statistics 2026 : Starting Statistics 2026 calculation task");
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
            long addUsers = 0;
            using (var timer = new AutoTimer($"Adding All Users", _managers._logger))
            {
                db.AddAllUsers(cancellationToken, progress);
                addUsers = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            long addCollections = 0;
            using (var timer = new AutoTimer($"Adding Collections", _managers._logger))
            {
                db.AddAllCollections(cancellationToken, progress);
                addCollections = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            long addMedia = 0;
            using (var timer = new AutoTimer($"Adding All Media", _managers._logger))
            {
                db.AddAllMedia(cancellationToken, progress);
                addMedia = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            long addSeries = 0;
            using (var timer = new AutoTimer($"Adding All Series", _managers._logger))
            {
                db.AddAllSeries(cancellationToken, progress);
                addSeries = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            long computeStats = 0;
            using (var timer = new AutoTimer($"Computing Cached Stats", _managers._logger))
            {
                db.ComputeCachedStats(cancellationToken, progress);
                computeStats = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            long computePercentWatchedCache = 0;
            if (_managers._taskManager != null)
            {
                var task = _managers._taskManager.ScheduledTasks.FirstOrDefault(task => task.Name == "Calculate Weighted Watched Shows Accounting");
                if (task != null)
                {
                    var options = new TaskOptions() { HasManualInteraction = false };
                    _managers._taskManager.Execute(task, options).GetAwaiter().GetResult();
                }
            }
            else
            {
                using (var timer = new AutoTimer($"Computing Percent Watched Cached Stats", _managers._logger))
                {
                    db.ComputePercentWatchedCache(cancellationToken, progress);
                    computePercentWatchedCache = timer.ElapsedMilliseconds();
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            db.UpdateLastUpdated(now, BuildDateInfo.GetBuildDate(), PluginConfiguration.Version);
            cancellationToken.ThrowIfCancellationRequested();

            var overall = overAllTimer.ElapsedMilliseconds();
            overAllTimer.Dispose();
            _managers._logger.Info($"=======================================");
            _managers._logger.Info($"Time to Add: {overall} ms");
            _managers._logger.Info($"          Users: {addUsers} ms");
            _managers._logger.Info($"    Collections: {addCollections} ms");
            _managers._logger.Info($"          Media: {addMedia} ms");
            _managers._logger.Info($"         Series: {addSeries} ms");
            _managers._logger.Info($"  Compute Cache: {computeStats} ms");
            _managers._logger.Info($"  Compute Percent Watched Cache: {computePercentWatchedCache} ms");
            _managers._logger.Info($"=======================================");
            _managers._logger.Info("Statistics 2026 : Finished Statistics 2026 calculation task");

            Plugin.Instance?.SaveConfiguration();

            db.SetCancellationToken(null);
            return Task.CompletedTask;
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

        private static PluginConfiguration? PluginConfiguration => Plugin.Instance?.Configuration ?? null;
        string IScheduledTask.Name => "Calculate Weighted Watched Shows Accounting";

        string IScheduledTask.Key => "Statistics2026CalculateWatchedShowsTask";

        string IScheduledTask.Description => "Task that will calculate the most (and least) watched shows.";

        string IScheduledTask.Category => "Statistics 2026";

        Task IScheduledTask.Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _managers._logger.Info("Statistics 2026 : Starting Statistics 2026 Weighted Watch Analysis");
            // purely for progress reporting
            var now = DateTime.Now;
            if (PluginConfiguration == null)
                throw new ArgumentNullException(nameof(PluginConfiguration));

            var db = StatisticsDB.GetInstance(_managers);
            db.SetCancellationToken(cancellationToken);
            try
            {
                db.ClearTable("CachedWatchedAnalysis"); // will throw an exception if the primary has not been run yet
            }
            catch (Exception /*ex*/)
            {
                return Task.CompletedTask;
            }

            long computePercentWatchedCache = 0;
            using (var timer = new AutoTimer($"Computing Percent Watched Cached Stats", _managers._logger))
            {
                db.ComputePercentWatchedCache(cancellationToken, progress);
                computePercentWatchedCache = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
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