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
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.ScheduledTasks
{
    public class CalculateDataTask : IScheduledTask
    {
        private EmbyManagers _providers;

        public CalculateDataTask(
            ILogManager logger,
            IServerConfigurationManager config,
            IUserManager userManager,
            IUserDataManager userDataManager,
            ILibraryManager libraryManager,
            IFileSystem fileSystem,
            IJsonSerializer jsonSerializer,
            IServerApplicationPaths serverApplicationPaths,
            IApplicationHost appHost,
            IProviderManager providerManager,
            Statistics2026API apiService
            )
        {
            _providers = new EmbyManagers(fileSystem, libraryManager, logger.GetLogger("Statistics2026 - CalculateDataTask"), serverApplicationPaths, userDataManager, userManager, appHost, apiService, jsonSerializer, providerManager, config);
        }

        private static PluginConfiguration? PluginConfiguration => Plugin.Instance?.Configuration ?? null;
        string IScheduledTask.Name => "Calculate Media and User Information for all library media and users";

        string IScheduledTask.Key => "Statistics2026CalculateStatsTask";

        string IScheduledTask.Description => "Task that will calculate Statistics for all media in library.";

        string IScheduledTask.Category => "Statistics 2026";

        Task IScheduledTask.Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _providers._logger.Info("Statistics 2026 : Starting Statistics 2026 calculation task");
            // purely for progress reporting
            var now = DateTime.Now;
            if (PluginConfiguration == null)
                throw new ArgumentNullException(nameof(PluginConfiguration));

            PluginConfiguration.LastUpdated = now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            PluginConfiguration.Version = Plugin.Instance?.Version.ToString(4) ?? "<UNKNOWN>";
            PluginConfiguration.BuildDate = BuildDateInfo.GetBuildDate().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            PluginConfiguration.ServerId = _providers._appHost.SystemId;

            var db = StatisticsDB.GetInstance(_providers);
            db.SetCancellationToken(cancellationToken);
            db.Initialize();

            var overAllTimer = new AutoTimer($"Adding All Data", _providers._logger, false);
            long addUsers = 0;
            using (var timer = new AutoTimer($"Adding All Users", _providers._logger))
            {
                db.AddAllUsers(cancellationToken, progress);
                addUsers = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            long addCollections = 0;
            using (var timer = new AutoTimer($"Adding Collections", _providers._logger))
            {
                db.AddAllCollections(cancellationToken, progress);
                addCollections = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            long addMedia = 0;
            using (var timer = new AutoTimer($"Adding All Media", _providers._logger))
            {
                db.AddAllMedia(cancellationToken, progress);
                addMedia = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            long addSeries = 0;
            using (var timer = new AutoTimer($"Adding All Series", _providers._logger))
            {
                db.AddAllSeries(cancellationToken, progress);
                addSeries = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            long computeStats = 0;
            using (var timer = new AutoTimer($"Computing Cached Stats", _providers._logger))
            {
                db.ComputeCachedStats(cancellationToken, progress);
                computeStats = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            long computePercentWatchedCache = 0;
            using (var timer = new AutoTimer($"Computing Percent Watched Cached Stats", _providers._logger))
            {
                db.ComputePercentWatchedCache(cancellationToken, progress);
                computePercentWatchedCache = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            db.UpdateLastUpdated(now, BuildDateInfo.GetBuildDate(), PluginConfiguration.Version);
            cancellationToken.ThrowIfCancellationRequested();

            var overall = overAllTimer.ElapsedMilliseconds();
            overAllTimer.Dispose();
            _providers._logger.Info($"=======================================");
            _providers._logger.Info($"Time to Add: {overall} ms");
            _providers._logger.Info($"          Users: {addUsers} ms");
            _providers._logger.Info($"    Collections: {addCollections} ms");
            _providers._logger.Info($"          Media: {addMedia} ms");
            _providers._logger.Info($"         Series: {addSeries} ms");
            _providers._logger.Info($"  Compute Cache: {computeStats} ms");
            _providers._logger.Info($"  Compute Percent Watched Cache: {computePercentWatchedCache} ms");
            _providers._logger.Info($"=======================================");
            _providers._logger.Info("Statistics 2026 : Finished Statistics 2026 calculation task");

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
        private EmbyManagers _providers;

        public CalculateWatchedShowsTask(
            ILogManager logger,
            IServerConfigurationManager config,
            IUserManager userManager,
            IUserDataManager userDataManager,
            ILibraryManager libraryManager,
            IFileSystem fileSystem,
            IJsonSerializer jsonSerializer,
            IServerApplicationPaths serverApplicationPaths,
            IApplicationHost appHost,
            IProviderManager providerManager,
            Statistics2026API apiService
            )
        {
            _providers = new EmbyManagers(fileSystem, libraryManager, logger.GetLogger("Statistics2026 - CalculateWatchedShowsTask"), serverApplicationPaths, userDataManager, userManager, appHost, apiService, jsonSerializer, providerManager, config);
        }

        private static PluginConfiguration? PluginConfiguration => Plugin.Instance?.Configuration ?? null;
        string IScheduledTask.Name => "Calculate Weighted Watched Shows Accounting";

        string IScheduledTask.Key => "Statistics2026CalculateWatchedShowsTask";

        string IScheduledTask.Description => "Task that will calculate the most (and least) watched shows.";

        string IScheduledTask.Category => "Statistics 2026";

        Task IScheduledTask.Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _providers._logger.Info("Statistics 2026 : Starting Statistics 2026 Weighted Watch Analysis");
            // purely for progress reporting
            var now = DateTime.Now;
            if (PluginConfiguration == null)
                throw new ArgumentNullException(nameof(PluginConfiguration));

            var db = StatisticsDB.GetInstance(_providers);
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
            using (var timer = new AutoTimer($"Computing Percent Watched Cached Stats", _providers._logger))
            {
                db.ComputePercentWatchedCache(cancellationToken, progress);
                computePercentWatchedCache = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            cancellationToken.ThrowIfCancellationRequested();

            _providers._logger.Info($"=======================================");
            _providers._logger.Info($"  Compute Percent Watched Cache: {computePercentWatchedCache} ms");
            _providers._logger.Info($"=======================================");
            _providers._logger.Info("Statistics 2026 : Finished Statistics 2026 Watched Show Analysis");

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