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
using Statistics2026.Utilities;
using Statistics2026.Responses;
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Statistics2026.Data.Statistics2026DB;

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

        public object Get( GetTotalMissingMovies request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                var groupData = db.TotalMissingMovies();
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetTotalMissingEpisodes request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                var groupData = db.TotalMissingEpisodes();
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetSeriesFilter request )
        {
            var retVal = HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                return db.GetSeriesFilter( true );
            } );
            return retVal;
        }

        public object Post( SetSeriesFilter request )
        {
            var retVal = HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

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

                return db.SaveSeriesFilter( seriesFilter, true );
            } );
            return retVal;
        }
    }
}
