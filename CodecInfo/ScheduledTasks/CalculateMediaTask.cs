using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using CodecInfo;
using CodecInfo.Configuration;
using CodecInfo.Models.Configuration;
using CodecInfo.Helpers;

namespace CodecInfo.ScheduledTasks
{
    public class CalculateMediaTask : IScheduledTask
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

        public CalculateMediaTask(ILogManager logger,
            IUserManager userManager,
            IUserDataManager userDataManager,
            ILibraryManager libraryManager, IFileSystem fileSystem, IJsonSerializer jsonSerializer,
            IServerApplicationPaths serverApplicationPaths, IApplicationHost appHost, IProviderManager providerManager)
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
        }

        private static PluginConfiguration PluginConfiguration => Plugin.Instance.Configuration;
        string IScheduledTask.Name => "Calculate Codec Information for all library media";

        string IScheduledTask.Key => "CodecInfoCalculateStatsTask";

        string IScheduledTask.Description => "Task that will calculate Codec Information of all media in library. (Ideal for weekly/non-daily schedule)";

        string IScheduledTask.Category => "Media Codec Information";

        Task IScheduledTask.Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            // purely for progress reporting
            PluginConfiguration.LastUpdated = DateTime.Now.ToString("g");
            PluginConfiguration.Version = Plugin.Instance.Version.ToString( 4 );
            PluginConfiguration.BuildDate = BuildDateInfo.GetBuildDate().ToString();
            PluginConfiguration.ServerId = _appHost.SystemId;

            var numSteps = 4;
            var currStep = 0;
            progress.Report(currStep/numSteps);


            var calculator = new Calculator(_userManager, _libraryManager, _userDataManager, _fileSystem, _logger, _providerManager, cancellationToken);
            using (calculator)
            {
                PluginConfiguration.MediaInfoList = calculator.CalculateMediaInfo();
                progress.Report((++currStep) / numSteps);

                PluginConfiguration.MediaResolutions = calculator.CalculateMediaResolutions();
                progress.Report((++currStep)/numSteps);

                PluginConfiguration.MediaCodecs = calculator.CalculateMediaCodecs();
                progress.Report((++currStep) / numSteps);

                PluginConfiguration.DolbyVisionProfiles = calculator.CalculateDVProfileInfo( PluginConfiguration.showUnknownDVProfileCount );
                progress.Report((++currStep) / numSteps);
            }

            progress.Report(100);

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