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

namespace Statistics2026.Services
{
    [Route( "/Statistics2026/tmdbimage/{size}/{path*}", "GET", Summary = "Get Tmdb Image" )]
    [Authenticated]
    public class GetTmdbImage : IReturn<Stream>
    {
        public string size { get; set; } = string.Empty;
        public string path { get; set; } = string.Empty;
    }

    public class TMDBImageProxyService : Statistics2026BaseService
    {
        private readonly IHttpResultFactory _httpResultFactory;

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
        ) : base(
                fileSystem,
                libraryManager,
                logManager,
                serverApplicationPaths,
                userDataManager,
                userManager,
                appHost,
                jsonSerializer,
                providerManager,
                configManager,
                taskManager,
                sessionManager,
                httpClient,
                authenticationRepository,
                notificationManager
                )
        {
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