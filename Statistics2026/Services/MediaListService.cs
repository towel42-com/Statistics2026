using Emby.Media.Common.Extensions;
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
using Statistics2026.Data;
using Statistics2026.Responses;
using Statistics2026.Utilities;
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using static Statistics2026.Data.Statistics2026DB;

namespace Statistics2026.Services
{
    [Route( "/Statistics2026/episode_list", "GET", Summary = "Gets Episodes" )]
    [Authenticated]
    public class GetEpisodeList : TabulatorTableOptions, IReturn<QueryResult<MediaItemResponse>>
    {
        public bool ShowOnServer { get; set; } = false;
        public bool ShowMissing { get; set; } = false;
    }

    [Route( "/Statistics2026/movie_list", "GET", Summary = "Gets Movies" )]
    [Authenticated]
    public class GetMovieList : TabulatorTableOptions, IReturn<QueryResult<MediaItemResponse>>
    {
        public bool ShowOnServer { get; set; } = false;
        public bool ShowMissing { get; set; } = false;
    }

    [Route( "/Statistics2026/series_list", "GET", Summary = "Gets Series" )]
    [Authenticated]
    public class GetSeriesList : TabulatorTableOptions, IReturn<QueryResult<MediaItemResponse>>
    {
    }

    [Route( "/Statistics2026/collection_list", "GET", Summary = "Gets Collections" )]
    [Authenticated]
    public class GetCollectionList : TabulatorTableOptions, IReturn<QueryResult<MediaItemResponse>>
    {
    }

    public class MediaListService : BaseService
    {
        public MediaListService(
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

        public Task<object> Get( GetSeriesList request )
        {
            return HelperFuncs.HandleRequestAsync<object>( request.GetType().Name, _embyInterfaces, async (timer, db) =>
            {
                return await db.GetSeriesListResponseAsync( null, request ).ConfigureAwait( false );
            } );
        }

        public Task<object> Get( GetCollectionList request )
        {
            return HelperFuncs.HandleRequestAsync<object>( request.GetType().Name, _embyInterfaces, async (timer, db) =>
            {
                int startIndex = Math.Max( 0, request.StartIndex );
                int limit = ( request.Limit > 0 ) ? request.Limit : 50;

                return await db.GetCollectionListResponseAsync( null, request ).ConfigureAwait( false );
            } );
        }

        public Task<object> Get( GetEpisodeList request )
        {
            return HelperFuncs.HandleRequestAsync<object>( request.GetType().Name, _embyInterfaces, async (timer, db) =>
            {
                bool showOnServer = request.ShowOnServer;
                bool showMissing = request.ShowMissing;

                if( !showOnServer && !showMissing )
                {
                    showOnServer = true;
                    showMissing = true;
                }

                EWhichMediaList whichMedia = EWhichMediaList.eEpisodes;
                if( showOnServer )
                    whichMedia = whichMedia | EWhichMediaList.eOnServer;
                if( showMissing )
                    whichMedia = whichMedia | EWhichMediaList.eMissing;

                int startIndex = Math.Max( 0, request.StartIndex );
                int limit = ( request.Limit > 0 ) ? request.Limit : 50;

                return await db.GetMediaListResponseAsync(
                    null,
                    whichMedia,
                    request
                ).ConfigureAwait( false );
            } );
        }

        public Task<object> Get( GetMovieList request )
        {
            return HelperFuncs.HandleRequestAsync<object>( request.GetType().Name, _embyInterfaces, async (timer, db) =>
            {
                bool showOnServer = request.ShowOnServer;
                bool showMissing = request.ShowMissing;

                if( !showOnServer && !showMissing )
                {
                    showOnServer = true;
                    showMissing = true;
                }

                EWhichMediaList whichMedia = EWhichMediaList.eMovies;
                if( showOnServer )
                    whichMedia |= EWhichMediaList.eOnServer;
                if( showMissing )
                    whichMedia |= EWhichMediaList.eMissing;

                int startIndex = Math.Max( 0, request.StartIndex );
                int limit = ( request.Limit > 0 ) ? request.Limit : 50;

                return await db.GetMediaListResponseAsync(
                    null, 
                    whichMedia,
                    request
                ).ConfigureAwait( false );
            } );
        }

    }
}
