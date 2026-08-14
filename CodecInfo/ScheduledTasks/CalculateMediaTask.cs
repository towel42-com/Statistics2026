using CodecInfo.Configuration;
using CodecInfo.Data;
using MediaBrowser.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodecInfo.ScheduledTasks
{
    public class CCalculateMediaTask : IScheduledTask
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IServerApplicationPaths _serverApplicationPaths;
        private readonly IUserDataManager _userDataManager;
        private readonly IUserManager _userManager;
        private IApplicationHost _appHost;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IProviderManager _providerManager;
        private readonly IServerConfigurationManager _appConfig;

        public CCalculateMediaTask(
            ILogManager logger,
            IServerConfigurationManager config,
            IUserManager userManager,
            IUserDataManager userDataManager,
            ILibraryManager libraryManager,
            IFileSystem fileSystem,
            IJsonSerializer jsonSerializer,
            IServerApplicationPaths serverApplicationPaths,
            IApplicationHost appHost,
            IProviderManager providerManager)
        {
            _logger = logger.GetLogger("CodecInfo");
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataManager = userDataManager;
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _serverApplicationPaths = serverApplicationPaths;
            _appHost = appHost;
            _providerManager = providerManager;
            _appConfig = config;
        }

        private static CPluginConfiguration PluginConfiguration => CPlugin.Instance.Configuration;
        string IScheduledTask.Name => "Calculate Codec Information for all library media";

        string IScheduledTask.Key => "CodecInfoCalculateStatsTask";

        string IScheduledTask.Description => "Task that will calculate Codec Information of all media in library. (Ideal for weekly/non-daily schedule)";

        string IScheduledTask.Category => "Media Codec Information";

        Task IScheduledTask.Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.Info("CodecInfo : Starting CodecInfo calculation task");
            // purely for progress reporting
            var now = DateTime.Now;
            PluginConfiguration.LastUpdated = now.ToString("g");
            PluginConfiguration.Version = CPlugin.Instance.Version.ToString(4);
            PluginConfiguration.BuildDate = CBuildDateInfo.GetBuildDate().ToString();
            PluginConfiguration.ServerId = _appHost.SystemId;

            var db = CConfigInfoDB.GetInstance(_appConfig.ApplicationPaths.DataPath, _logger);
            db.Initialize();

            progress.Report(0);
            db.UpdateLastUpdated(now, CBuildDateInfo.GetBuildDate(), PluginConfiguration.Version);
            progress.Report(100);

            progress.Report(0);
            db.ClearMediaInfo();
            progress.Report(100);

            progress.Report(0);
            db.CalculateMediaInfo(_libraryManager, progress);
            progress.Report(100);

            CPlugin.Instance.SaveConfiguration();
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