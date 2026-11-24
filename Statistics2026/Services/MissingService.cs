using Emby.Media.Common.Extensions;
using MediaBrowser.Common;
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
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Tasks;
using Statistics2026.Data;
using Statistics2026.Responses;
using Statistics2026.Utilities;

namespace Statistics2026.Services
{
    [Route( "/Statistics2026/total_missing_movies", "GET", Summary = "Get the total number of movies missing from Collections" )]
    [Authenticated( Roles = "admin" )]
    public class GetTotalMissingMovies : IReturn<object>
    {
    }

    [Route( "/Statistics2026/total_missing_episodes", "GET", Summary = "Get the total number of episodes missing from Series" )]
    [Authenticated( Roles = "admin" )]
    public class GetTotalMissingEpisodes : IReturn<object>
    {
    }

    [Route( "/Statistics2026/series_filters", "GET", Summary = "Gets Series Filter" )]
    [Authenticated( Roles = "admin" )]
    public class GetSeriesFilter : IReturn<List<SeriesFilterResponse>>
    {
    }

    [Route( "/Statistics2026/set_series_filter", "POST", Summary = "Sets a Series Filter" )]
    [Authenticated( Roles = "admin" )]
    public class SetSeriesFilter : IReturn<object>
    {
        public string series_id { get; set; } = string.Empty;
        public bool track_series { get; set; } = false;
        public bool track_specials { get; set; } = false;
        public int min_season { get; set; } = -1;
        public int max_season { get; set; } = -1;
    }

    public class MissingService : BaseService
    {
        public MissingService(
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

        public Task<object> Get( GetTotalMissingMovies request )
        {
            return HelperFuncs.HandleRequestAsync( request.GetType().Name, _embyInterfaces, async ( timer ) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstanceAsync( _embyInterfaces, request.GetType().Name );

                StatCard groupData = await db.TotalMissingMoviesAsync();
                if( groupData == null )
                    return new StatCardResponse();

                object vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public Task<object> Get( GetTotalMissingEpisodes request )
        {
            return HelperFuncs.HandleRequestAsync( request.GetType().Name, _embyInterfaces, async ( timer ) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstanceAsync( _embyInterfaces, request.GetType().Name );

                StatCard groupData = await db.TotalMissingEpisodesAsync();
                if( groupData == null )
                    return new StatCardResponse();

                object vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public Task<object> Get( GetSeriesFilter request )
        {
            return HelperFuncs.HandleRequestAsync<object>( request.GetType().Name, _embyInterfaces, async ( timer ) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstanceAsync( _embyInterfaces, request.GetType().Name );
                return await db.GetSeriesFilterAsync( true );
            } );
        }

        public async Task<object> Post( SetSeriesFilter request )
        {
            object retVal = await HelperFuncs.HandleRequestAsync( request.GetType().Name, _embyInterfaces, async ( timer ) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstanceAsync( _embyInterfaces, request.GetType().Name );

                var seriesFilter = new SeriesFilterResponse()
                {
                    SeriesId = request.series_id,
                    TrackSeries = request.track_series,
                    TrackSpecials = request.track_specials,
                    MinSeason = request.min_season,
                    MaxSeason = request.max_season
                };

                if( seriesFilter.SeriesId.IsNullOrEmpty() )
                    return new object();

                return await db.SaveSeriesFilterAsync( seriesFilter, true );
            } );
            return retVal;
        }
    }
}
