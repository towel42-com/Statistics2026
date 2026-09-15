using MediaBrowser.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Statistics2026.Api;
using Statistics2026.ScheduledTasks;
using System.Collections.Generic;

namespace Statistics2026.Api
{
    public class EmbyInterfaces
    {
        public EmbyInterfaces(
            IFileSystem fileSystem,
            ILibraryManager libraryManager,
            ILogManager logManager,
            ILogger logger,
            IServerApplicationPaths serverApplicationPaths,
            IUserDataManager userDataManager,
            IUserManager userManager,
            IApplicationHost appHost,
            Statistics2026API apiService,
            IJsonSerializer jsonSerializer,
            IProviderManager providerManager,
            IServerConfigurationManager configManager,
            ITaskManager taskManager
        )
        {
            _fileSystem = fileSystem;
            _libraryManager = libraryManager;
            _logManager = logManager;
            _logger = logger;
            _serverApplicationPaths = serverApplicationPaths;
            _userDataManager = userDataManager;
            _userManager = userManager;
            _appHost = appHost;
            _apiService = apiService;
            _jsonSerializer = jsonSerializer;
            _providerManager = providerManager;
            _configManager = configManager;
            _taskManager = taskManager;
            if (Plugin.Instance != null)
                Plugin.Instance.ServerId = _appHost.SystemId;
        }

        public bool IsStatistics2026TaskRunning()
        {
            return IsStatistics2026TaskRunning(new List<System.Type>());
        }

        public bool IsStatistics2026TaskRunning(System.Type okIfRunning)
        {
            return IsStatistics2026TaskRunning(new List<System.Type>() { typeof(RunAllTasksTask), okIfRunning });
        }

        public bool IsStatistics2026TaskRunning(List<System.Type> okIfRunning)
        {
            if (_taskManager == null)
                return false;

            var knownTasks = Plugin.Instance?.GetKnownTasks(true);
            if (knownTasks == null)
                return false;

            var allTasks = _taskManager.ScheduledTasks;
            foreach (var task in allTasks)
            {
                if (task.State == MediaBrowser.Model.Tasks.TaskState.Idle)
                    continue;

                foreach (var currTask in knownTasks)
                {
                    bool okToBeRunning = false;
                    foreach (var okTask in okIfRunning)
                    {
                        if (okTask == task.ScheduledTask.GetType())
                        {
                            okToBeRunning = true;
                            break;
                        }
                    }

                    if (okToBeRunning)
                        continue;

                    if (currTask.TaskType == task.ScheduledTask.GetType())
                    {
                        return true;
                    }
                }

            }
            return false;
        }

        public readonly IFileSystem _fileSystem;
        public readonly ILibraryManager _libraryManager;
        public readonly ILogManager _logManager;
        public readonly ILogger _logger;
        public readonly IServerApplicationPaths _serverApplicationPaths;
        public readonly IUserDataManager _userDataManager;
        public readonly IUserManager _userManager;
        public IApplicationHost _appHost
        {
            set
            {
                field = value;
                if (Plugin.Instance != null && value != null)
                    Plugin.Instance.ServerId = value.SystemId;
            }
            get;
        }
        public readonly Statistics2026API _apiService;
        public readonly IJsonSerializer _jsonSerializer;
        public readonly IProviderManager _providerManager;
        public readonly IServerConfigurationManager _configManager;
        public readonly ITaskManager _taskManager;
        public ISessionManager? _sessionManager = null;
    }
}