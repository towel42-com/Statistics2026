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
using static Statistics2026.Data.Statistics2026DB;

namespace Statistics2026.Services
{
    [Route( "/Statistics2026/episode_list", "GET", Summary = "Gets Episodes" )]
    [Authenticated( Roles = "admin" )]
    public class GetEpisodeList : IReturn<List<MediaItemResponse>>
    {
        public bool showOnServer { get; set; } = false;
        public bool showMissing { get; set; } = false;
    }

    [Route( "/Statistics2026/movie_list", "GET", Summary = "Gets Movies" )]
    [Authenticated( Roles = "admin" )]
    public class GetMovieList : IReturn<List<MediaItemResponse>>
    {
        public bool showOnServer { get; set; } = false;
        public bool showMissing { get; set; } = false;
    }

    [Route( "/Statistics2026/series_list", "GET", Summary = "Gets Series" )]
    [Authenticated( Roles = "admin" )]
    public class GetSeriesList : IReturn<List<MediaItemResponse>>
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

        public async Task<object> Get( GetSeriesList request )
        {
            var retVal = HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                var series = db.getSeriesListResponseAsync();

                return series ?? new object();
            } );
            return retVal;
        }

        public async Task<object> Get( GetEpisodeList request )
        {
            var retVal = HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );
                var showOnServer = request.showOnServer;
                var showMissing = request.showMissing;

                if( !showOnServer && !showMissing )
                {
                    showOnServer = true;
                    showMissing = true;
                }

                var whichMedia = EWhichMediaList.eEpisodes;
                if( showOnServer )
                    whichMedia = whichMedia | EWhichMediaList.eOnServer;
                if( showMissing )
                    whichMedia = whichMedia | EWhichMediaList.eMissing;

                var episodes = db.getMediaListResponseAsync( whichMedia );

                return episodes ?? new object();
            } );
            return retVal;
        }

        public async Task<object> Get( GetMovieList request )
        {
            var retVal = HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );
                var showOnServer = request.showOnServer;
                var showMissing = request.showMissing;

                if( !showOnServer && !showMissing )
                {
                    showOnServer = true;
                    showMissing = true;
                }

                var whichMedia = EWhichMediaList.eMovies;
                if( showOnServer )
                    whichMedia = whichMedia | EWhichMediaList.eOnServer;
                if( showMissing )
                    whichMedia = whichMedia | EWhichMediaList.eMissing;

                var movies = db.getMediaListResponseAsync( whichMedia );
                return movies ?? new object();
            } );
            return retVal;
        }
    }
}
