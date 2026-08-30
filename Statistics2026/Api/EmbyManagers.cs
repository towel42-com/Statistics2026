using MediaBrowser.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using Statistics2026.Api;

namespace Statistics2026.Api
{
    public class EmbyManagers
    {
        public EmbyManagers(
            IFileSystem fileSystem,
            ILibraryManager libraryManager,
            ILogger logger,
            IServerApplicationPaths serverApplicationPaths,
            IUserDataManager userDataManager,
            IUserManager userManager,
            IApplicationHost appHost,
            Statistics2026API apiService,
            IJsonSerializer jsonSerializer,
            IProviderManager providerManager,
            IServerConfigurationManager appConfig
        )
        {
            _fileSystem = fileSystem;
            _libraryManager = libraryManager;
            _logger = logger;
            _serverApplicationPaths = serverApplicationPaths;
            _userDataManager = userDataManager;
            _userManager = userManager;
            _appHost = appHost;
            _apiService = apiService;
            _jsonSerializer = jsonSerializer;
            _providerManager = providerManager;
            _appConfig = appConfig;
        }

        public readonly IFileSystem _fileSystem;
        public readonly ILibraryManager _libraryManager;
        public readonly ILogger _logger;
        public readonly IServerApplicationPaths _serverApplicationPaths;
        public readonly IUserDataManager _userDataManager;
        public readonly IUserManager _userManager;
        public IApplicationHost _appHost;
        public Statistics2026API _apiService;
        public readonly IJsonSerializer _jsonSerializer;
        public readonly IProviderManager _providerManager;
        public readonly IServerConfigurationManager _appConfig;
    }
}