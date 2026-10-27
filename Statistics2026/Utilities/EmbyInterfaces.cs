using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
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
using ServiceStack;
using Statistics2026.Api;
using System;
using System.Diagnostics.CodeAnalysis;


namespace Statistics2026.Utilities
{
    public class EmbyInterfaces
    {
        public EmbyInterfaces()
        {
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

        public ILogger? _logger = null;
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
        public required IAuthenticationRepository? _authenticationRepository = null!;
        public required IFileSystem? _fileSystem = null;
        public required IHttpClient? _httpClient = null;
        public required IJsonSerializer? _jsonSerializer = null;
        public required ILibraryManager? _libraryManager = null;
        public required ILogManager? _logManager = null;
        public required IProviderManager? _providerManager = null;
        public required IServerApplicationPaths? _serverApplicationPaths = null;
        public required IServerConfigurationManager? _configManager = null;
        public required ISessionManager? _sessionManager = null;
        public required ITaskManager? _taskManager = null;
        public required IUserDataManager? _userDataManager = null;
        public required IUserManager? _userManager = null;
        public required Statistics2026API? _apiService = null;

        [MemberNotNullWhen( true, nameof( _authenticationRepository ) )]
        public IAuthenticationRepository AuthenticationRepository()
        {
            CheckIsValidWithThrow();
            return _authenticationRepository!;
        }

        [MemberNotNullWhen( true, nameof( _fileSystem ) )]
        public IFileSystem FileSystem()
        {
            CheckIsValidWithThrow();
            return _fileSystem!;
        }

        [MemberNotNullWhen( true, nameof( _logger ) )]
        public ILogger Logger()
        {
            CheckIsValidWithThrow();
            return _logger!;
        }

        [MemberNotNullWhen( true, nameof( _appHost ) )]
        public IApplicationHost AppHost()
        {
            CheckIsValidWithThrow();
            return _appHost!;
        }

        [MemberNotNullWhen( true, nameof( _httpClient ) )]
        public IHttpClient HttpClient()
        {
            CheckIsValidWithThrow();
            return _httpClient!;
        }

        [MemberNotNullWhen( true, nameof( _jsonSerializer ) )]
        public IJsonSerializer JsonSerializer()
        {
            CheckIsValidWithThrow();
            return _jsonSerializer!;
        }

        [MemberNotNullWhen( true, nameof( _libraryManager ) )]
        public ILibraryManager LibraryManager()
        {
            CheckIsValidWithThrow();
            return _libraryManager!;
        }

        [MemberNotNullWhen( true, nameof( _logManager ) )]
        public ILogManager LogManager()
        {
            CheckIsValidWithThrow();
            return _logManager!;
        }

        [MemberNotNullWhen( true, nameof( _providerManager ) )]
        public IProviderManager ProviderManager()
        {
            CheckIsValidWithThrow();
            return _providerManager!;
        }

        [MemberNotNullWhen( true, nameof( _serverApplicationPaths ) )]
        public IServerApplicationPaths ServerApplicationPaths()
        {
            CheckIsValidWithThrow();
            return _serverApplicationPaths!;
        }

        [MemberNotNullWhen( true, nameof( _configManager ) )]
        public IServerConfigurationManager ConfigManager()
        {
            CheckIsValidWithThrow();
            return _configManager!;
        }

        [MemberNotNullWhen( true, nameof( _sessionManager ) )]
        public ISessionManager SessionManager()
        {
            CheckIsValidWithThrow();
            return _sessionManager!;
        }

        [MemberNotNullWhen( true, nameof( _taskManager ) )]
        public ITaskManager TaskManager()
        {
            CheckIsValidWithThrow();
            return _taskManager!;
        }

        [MemberNotNullWhen( true, nameof( _userDataManager ) )]
        public IUserDataManager UserDataManager()
        {
            CheckIsValidWithThrow();
            return _userDataManager!;
        }

        [MemberNotNullWhen( true, nameof( _userManager ), nameof( _apiService ) )]
        public IUserManager UserManager()
        {
            CheckIsValidWithThrow();
            return _userManager!;
        }

        [MemberNotNullWhen( true, nameof( _apiService ) )]
        public Statistics2026API _ApiService()
        {
            CheckIsValidWithThrow();
            return _apiService!;
        }

        [MemberNotNullWhen( true, nameof( _logger ), nameof( _appHost ), nameof( _authenticationRepository ), nameof( _fileSystem ), nameof( _httpClient ), nameof( _jsonSerializer ), nameof( _libraryManager ), nameof( _logManager ), nameof( _providerManager ), nameof( _serverApplicationPaths ), nameof( _configManager ), nameof( _sessionManager ), nameof( _taskManager ), nameof( _userDataManager ), nameof( _userManager ), nameof( _apiService ) )]
        public bool CheckIsValid( ref List<string> msgs )
        {
            if( _logger == null )
            {
                msgs.Add( "_embyInterfaces.Logger() is null" );
            }
            if( _appHost == null )
            {
                msgs.Add( "_embyInterfaces._appHost is null" );
            }
            if( _authenticationRepository == null )
            {
                msgs.Add( "_embyInterfaces._authenticationRepository is null" );
            }
            if( _fileSystem == null )
            {
                msgs.Add( "_embyInterfaces._fileSystem is null" );
            }
            if( _httpClient == null )
            {
                msgs.Add( "_embyInterfaces._httpClient is null" );
            }
            if( _jsonSerializer == null )
            {
                msgs.Add( "_embyInterfaces._jsonSerializer is null" );
            }
            if( _libraryManager == null )
            {
                msgs.Add( "_embyInterfaces._libraryManager is null" );
            }
            if( _logManager == null )
            {
                msgs.Add( "_embyInterfaces._logManager is null" );
            }
            if( _providerManager == null )
            {
                msgs.Add( "_embyInterfaces._providerManager is null" );
            }
            if( _serverApplicationPaths == null )
            {
                msgs.Add( "_embyInterfaces._serverApplicationPaths is null" );
            }
            if( _configManager == null )
            {
                msgs.Add( "_embyInterfaces._configManager is null" );
            }
            if( _sessionManager == null )
            {
                msgs.Add( "_embyInterfaces._sessionManager is null" );
            }
            if( _taskManager == null )
            {
                msgs.Add( "_embyInterfaces._taskManager is null" );
            }
            if( _userDataManager == null )
            {
                msgs.Add( "_embyInterfaces._userDataManager is null" );
            }
            if( _userManager == null )
            {
                msgs.Add( "_embyInterfaces._userManager is null" );
            }
            if( _apiService == null )
            {
                msgs.Add( "_embyInterfaces._apiService is null" );
            }
            return msgs.Count == 0;
        }

        public bool CheckIsValid()
        {
            var msgs = new List<string>();
            if( !CheckIsValid( ref msgs ) )
            {
                if( _logger != null )
                {
                    foreach( var msg in msgs )
                    {
                        _logger!.Error( msg );
                    }
                }
                return false;
            }
            return true;
        }

        public bool CheckIsValidWithThrow()
        {
            var msgs = new List<string>();
            if( CheckIsValid( ref msgs ) )
            {
                throw new ArgumentNullException( msgs.Join( "\n" ) );
            }
            return true;
        }
    }
}