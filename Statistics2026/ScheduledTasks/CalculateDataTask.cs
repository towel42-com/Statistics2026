using MediaBrowser.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Statistics2026.Api;
using Statistics2026.Configuration;
using Statistics2026.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.ScheduledTasks
{
    public class CalculateDataTask : IScheduledTask
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IServerApplicationPaths _serverApplicationPaths;
        private readonly IUserDataManager _userDataManager;
        private readonly IUserManager _userManager;
        private IApplicationHost _appHost;
        private Statistics2026API _apiService;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IProviderManager _providerManager;
        private readonly IServerConfigurationManager _appConfig;

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
            _logger = logger.GetLogger("Statistics2026 - CalculateDataTask");
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataManager = userDataManager;
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _serverApplicationPaths = serverApplicationPaths;
            _appHost = appHost;
            _providerManager = providerManager;
            _appConfig = config;
            _apiService = apiService;
        }

        private static PluginConfiguration? PluginConfiguration => Plugin.Instance?.Configuration ?? null;
        string IScheduledTask.Name => "Calculate Media and User Information for all library media and users";

        string IScheduledTask.Key => "Statistics2026CalculateStatsTask";

        string IScheduledTask.Description => "Task that will calculate Statistics for all media in library.";

        string IScheduledTask.Category => "Statistics 2026";

        Task IScheduledTask.Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.Info("Statistics 2026 : Starting Statistics 2026 calculation task");
            // purely for progress reporting
            var now = DateTime.Now;
            if (PluginConfiguration == null)
                throw new ArgumentNullException(nameof(PluginConfiguration));

            PluginConfiguration.LastUpdated = now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            PluginConfiguration.Version = Plugin.Instance?.Version.ToString(4) ?? "<UNKNOWN>";
            PluginConfiguration.BuildDate = BuildDateInfo.GetBuildDate().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            PluginConfiguration.ServerId = _appHost.SystemId;

            var db = StatisticsDB.GetInstance(_appConfig.ApplicationPaths.DataPath, _logger);
            db.SetCancellationToken(cancellationToken);
            db.Initialize();

            var overAllTimer = new AutoTimer($"Adding All Data", _logger, false);
            long addUsers = 0;
            using (var timer = new AutoTimer($"Adding All Users", _logger))
            {
                db.AddAllUsers(_userManager, _userDataManager, _libraryManager, cancellationToken, progress);
                addUsers = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            long addCollections = 0;
            using (var timer = new AutoTimer($"Adding Collections", _logger))
            {
                db.AddAllCollections(_libraryManager, cancellationToken, progress);
                addCollections = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            long addMedia = 0;
            using (var timer = new AutoTimer($"Adding All Media", _logger))
            {
                db.AddAllMedia(_libraryManager, _fileSystem, cancellationToken, progress);
                addMedia = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            long addSeries = 0;
            using (var timer = new AutoTimer($"Adding All Series", _logger))
            {
                db.AddAllSeries(_libraryManager, _fileSystem, cancellationToken, progress);
                addSeries = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            long computeStats = 0;
            using (var timer = new AutoTimer($"Computing Cached Stats", _logger))
            {
                db.ComputeCachedStats(_libraryManager, cancellationToken, progress);
                computeStats = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            long computePercentWatchedCache  = 0;
            using (var timer = new AutoTimer($"Computing Percent Watched Cached Stats", _logger))
            {
                db.ComputePercentWatchedCache(_libraryManager, cancellationToken, progress);
                computePercentWatchedCache = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            db.UpdateLastUpdated(now, BuildDateInfo.GetBuildDate(), PluginConfiguration.Version);
            cancellationToken.ThrowIfCancellationRequested();

            var overall = overAllTimer.ElapsedMilliseconds();
            overAllTimer.Dispose();
            _logger.Info($"=======================================");
            _logger.Info($"Time to Add: {overall} ms");
            _logger.Info($"          Users: {addUsers} ms");
            _logger.Info($"    Collections: {addCollections} ms");
            _logger.Info($"          Media: {addMedia} ms");
            _logger.Info($"         Series: {addSeries} ms");
            _logger.Info($"  Compute Cache: {computeStats} ms");
            _logger.Info($"  Compute Percent Watched Cache: {computePercentWatchedCache} ms");
            _logger.Info($"=======================================");
            _logger.Info("Statistics 2026 : Finished Statistics 2026 calculation task");

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
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IServerApplicationPaths _serverApplicationPaths;
        private readonly IUserDataManager _userDataManager;
        private readonly IUserManager _userManager;
        private IApplicationHost _appHost;
        private Statistics2026API _apiService;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IProviderManager _providerManager;
        private readonly IServerConfigurationManager _appConfig;

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
            _logger = logger.GetLogger("Statistics2026 - CalculateDataTask");
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataManager = userDataManager;
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _serverApplicationPaths = serverApplicationPaths;
            _appHost = appHost;
            _providerManager = providerManager;
            _appConfig = config;
            _apiService = apiService;
        }

        private static PluginConfiguration? PluginConfiguration => Plugin.Instance?.Configuration ?? null;
        string IScheduledTask.Name => "Calculate Weighted Watched Shows Accounting";

        string IScheduledTask.Key => "Statistics2026CalculateWatchedShowsTask";

        string IScheduledTask.Description => "Task that will calculate the most (and least) watched shows.";

        string IScheduledTask.Category => "Statistics 2026";

        Task IScheduledTask.Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.Info("Statistics 2026 : Starting Statistics 2026 Weighted Watch Analysis");
            // purely for progress reporting
            var now = DateTime.Now;
            if (PluginConfiguration == null)
                throw new ArgumentNullException(nameof(PluginConfiguration));

            var db = StatisticsDB.GetInstance(_appConfig.ApplicationPaths.DataPath, _logger);
            db.SetCancellationToken(cancellationToken);
            db.ClearTable("CachedWatchedAnalysis");

            long computePercentWatchedCache = 0;
            using (var timer = new AutoTimer($"Computing Percent Watched Cached Stats", _logger))
            {
                db.ComputePercentWatchedCache(_libraryManager, cancellationToken, progress);
                computePercentWatchedCache = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            cancellationToken.ThrowIfCancellationRequested();

            _logger.Info($"=======================================");
            _logger.Info($"  Compute Percent Watched Cache: {computePercentWatchedCache} ms");
            _logger.Info($"=======================================");
            _logger.Info("Statistics 2026 : Finished Statistics 2026 Watched Show Analysis");

            Plugin.Instance?.SaveConfiguration();

            db.SetCancellationToken(null);
            return Task.CompletedTask;
        }

        IEnumerable<TaskTriggerInfo> IScheduledTask.GetDefaultTriggers()
        {
            return null!;
            //{
            //    new TaskTriggerInfo
            //    {
            //        Type = TaskTriggerInfo.TriggerWeekly,
            //        DayOfWeek = DayOfWeek.Sunday,
            //        TimeOfDayTicks = TimeSpan.FromMinutes(30).Ticks
            //    }
            //};
        }
    }
}