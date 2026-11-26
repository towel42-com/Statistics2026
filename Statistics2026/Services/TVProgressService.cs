using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Tasks;
using Statistics2026.Responses;
using Statistics2026.Utilities;

namespace Statistics2026.Services
{
    [Route( "/Statistics2026/tv_series_progress/{User}", "GET", Summary = "Gets the tv series progress for the user" )]
    [Authenticated]
    public class GetTVSeriesProgress : IReturn<QueryResult<GetTVSeriesProgressResponse>>
    {
        public string user { get; set; } = string.Empty;
        public int StartIndex { get; set; } = 0;
        public int Limit { get; set; } = 50;
        public string SortBy { get; set; } = string.Empty;
        public string SortOrder { get; set; } = string.Empty;
    }

    public class TVProgressService : BaseService
    {
        public TVProgressService(
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
                   INotificationManager notificationManager
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
        }

        public Task<object> Get( GetTVSeriesProgress request )
        {
            return HelperFuncs.HandleRequestAsync<object>( request.GetType().Name, _embyInterfaces, async ( timer, db ) =>
            {
                string userName = request.user;
                User? user = EmbyHelperFuncs.GetUserByName( userName, _embyInterfaces );
                if( user == null )
                    return new List<GetTVSeriesProgressResponse>();

                int startIndex = Math.Max( 0, request.StartIndex );
                int limit = ( request.Limit > 0 ) ? request.Limit : 50;

                return await db.GetTVSeriesProgressAsync(
                    user,
                    startIndex,
                    limit,
                    request.SortBy,
                    request.SortOrder
                ).ConfigureAwait( false );
            } );
        }
    }
}