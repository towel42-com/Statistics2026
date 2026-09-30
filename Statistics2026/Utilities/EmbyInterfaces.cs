using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using System;

namespace Statistics2026.Api
{
    public class EmbyInterfaces
    {
        public EmbyInterfaces(
            IApplicationHost appHost
        )
        {
            _appHost = appHost;
            Plugin.Instance?.ServerId = _appHost.SystemId;
        }

        public User? GetUserByName( string userName )
        {
            if( _userManager == null )
                throw new ArgumentNullException( "_userManager is null." );

            return _userManager!.GetUserByName( userName );
        }

        public User? GetUserById( Guid id )
        {
            if( _userManager == null )
                throw new ArgumentNullException( "_userManager is null." );

            return _userManager!.GetUserById( id );
        }

        public IFileSystem? _fileSystem = null;
        public ILibraryManager? _libraryManager = null;
        public ILogManager? _logManager = null;
        public ILogger? _logger = null;
        public IServerApplicationPaths? _serverApplicationPaths = null;
        public IUserDataManager? _userDataManager = null;
        public IUserManager? _userManager = null;
        public IApplicationHost? _appHost
        {
            set
            {
                field = value;
                if( Plugin.Instance != null && value != null )
                    Plugin.Instance.ServerId = value.SystemId;
            }
            get;
        }
        public Statistics2026API? _apiService = null;
        public IJsonSerializer? _jsonSerializer = null;
        public IProviderManager? _providerManager = null;
        public IServerConfigurationManager? _configManager = null;
        public ITaskManager? _taskManager = null;
        public ISessionManager? _sessionManager = null;
        public IHttpClient? _httpClient = null;
        public IAuthenticationRepository? _authenticationRepository = null;
    }
}