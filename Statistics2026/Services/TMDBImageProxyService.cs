using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Tasks;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.Api
{
    [Route( "/Statistics2026/tmdbimage/{size}/{path*}", "GET", Summary = "Get Tmdb Image" )]
    [Unauthenticated]
    public class GetTmdbImage : IReturn<Stream>
    {
        public string size { get; set; } = string.Empty;
        public string path { get; set; } = string.Empty;
    }

    public class TMDBImageProxyService : IService, IRequiresRequest
    {
        private readonly IHttpResultFactory _httpResultFactory;
        private readonly EmbyInterfaces _embyInterfaces;
        public IRequest Request { get; set; } = null!;

        public TMDBImageProxyService(
            IFileSystem fileSystem,
            ILibraryManager libraryManager,
            ILogManager logManager,
            IServerApplicationPaths serverApplicationPaths,
            IUserDataManager userDataManager,
            IUserManager userManager,
            IApplicationHost appHost,
            IJsonSerializer jsonSerializer,
            IProviderManager providerManager,
            IServerConfigurationManager configManager,
            ITaskManager taskManager,
            ISessionManager sessionManager,
            IHttpClient httpClient,
            IAuthenticationRepository authenticationRepository,
            INotificationManager notificationManager,
            IHttpResultFactory resultFactory
        )
        {
            _embyInterfaces = new EmbyInterfaces()
            {
                _logger = logManager.GetLogger( "Statistics2026 - TMDBImageProxyService" ),
                _appHost = appHost,
                _authenticationRepository = authenticationRepository,
                _fileSystem = fileSystem,
                _httpClient = httpClient,
                _jsonSerializer = jsonSerializer,
                _libraryManager = libraryManager,
                _logManager = logManager,
                _notificationManager = notificationManager,
                _providerManager = providerManager,
                _serverApplicationPaths = serverApplicationPaths,
                _configManager = configManager,
                _sessionManager = sessionManager,
                _taskManager = taskManager,
                _userDataManager = userDataManager,
                _userManager = userManager,
            };
            _httpResultFactory = resultFactory;
        }

        public async Task<object> Get( GetTmdbImage request )
        {
            _embyInterfaces.Logger().Info( $"Request for TMDB image: {request.path} - {request.size}" );
            var options = new HttpRequestOptions
            {
                Url = HelperFuncs.GetTMDBUrl( request.path, request.size, false ),
                CancellationToken = CancellationToken.None,

                CacheMode = CacheMode.Unconditional,
                CacheLength = TimeSpan.FromDays( 30 ),

                UserAgent = "Mozilla/5.0"
            };

            try
            {
                HttpResponseInfo response = await _embyInterfaces.HttpClient().GetResponse( options ).ConfigureAwait( false );
                if( response.StatusCode != HttpStatusCode.OK )
                {
                    response.Content?.Dispose();
                    throw new ResourceNotFoundException( "Remote image not found." );
                }

                string contentType = string.IsNullOrWhiteSpace( response.ContentType ) ? "image/jpeg" : response.ContentType;

                _embyInterfaces.Logger().Info( $"Returned: {contentType} - {response.ContentLength}" );

                // 3. Pass Request as the 1st argument and response.Content (Stream) as the 2nd
                return _httpResultFactory.GetResult( Request, response.Content, contentType );
            }
            catch( HttpException ex ) when( ex.StatusCode == HttpStatusCode.NotFound )
            {
                throw new ResourceNotFoundException( "Remote image not found." );
            }
        }
    }
}