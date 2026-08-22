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

        private static PluginConfiguration PluginConfiguration => Plugin.Instance.Configuration;
        string IScheduledTask.Name => "Calculate Media and User Information for all library media and users";

        string IScheduledTask.Key => "Statistics2026CalculateStatsTask";

        string IScheduledTask.Description => "Task that will calculate Statistics for all media in library.";

        string IScheduledTask.Category => "Statistics 2026";

        Task IScheduledTask.Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.Info("Statistics 2026 : Starting Statistics 2026 calculation task");
            // purely for progress reporting
            var now = DateTime.Now;
            PluginConfiguration.LastUpdated = now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            PluginConfiguration.Version = Plugin.Instance.Version.ToString(4);
            PluginConfiguration.BuildDate = BuildDateInfo.GetBuildDate().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            PluginConfiguration.ServerId = _appHost.SystemId;

            var db = StatisticsDB.GetInstance(_appConfig.ApplicationPaths.DataPath, _logger);
            db.Initialize();

            Stopwatch stopWatch = Stopwatch.StartNew();
            progress.Report(0);
            db.AddUsers(_userManager, _userDataManager, _libraryManager, cancellationToken, progress);
            cancellationToken.ThrowIfCancellationRequested();
            stopWatch.Stop();
            _logger.Debug($"It took {stopWatch.ElapsedMilliseconds} ms to Add All Users");
            progress.Report(100);

            progress.Report(0);
            stopWatch.Restart();
            db.AddCollections(_libraryManager, cancellationToken, progress);
            cancellationToken.ThrowIfCancellationRequested();
            stopWatch.Stop();
            _logger.Debug($"It took {stopWatch.ElapsedMilliseconds} ms to Add Collections");
            progress.Report(100);

            progress.Report(0);
            stopWatch.Restart();
            db.AddAllMedia(_libraryManager, _fileSystem, cancellationToken, progress);
            stopWatch.Stop();
            _logger.Debug($"It took {stopWatch.ElapsedMilliseconds} ms to Add All Media");
            progress.Report(100);
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(0);
            stopWatch.Restart();
            db.AddAllSeries(_libraryManager, _fileSystem, cancellationToken, progress);
            stopWatch.Stop();
            _logger.Debug($"It took {stopWatch.ElapsedMilliseconds} ms to Add All Series");
            progress.Report(100);
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(0);
            stopWatch.Restart();
            db.UpdateLastUpdated(now, BuildDateInfo.GetBuildDate(), PluginConfiguration.Version);
            stopWatch.Stop();
            _logger.Debug($"It took {stopWatch.ElapsedMilliseconds} ms to UpdateLastUpdated");
            progress.Report(100);
            cancellationToken.ThrowIfCancellationRequested();

            Plugin.Instance.SaveConfiguration();
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
}