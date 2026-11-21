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
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Statistics2026.Data.Statistics2026DB;

namespace Statistics2026.Services
{
    [Route( "/Statistics2026/user_count", "GET", Summary = "Gets the total User Count" )]
    [Authenticated( Roles = "admin" )]
    public class GetUserCount : IReturn<object>
    {
    }

    [Route( "/Statistics2026/most_active_users", "GET", Summary = "Gets the top 5 most active users" )]
    [Authenticated( Roles = "admin" )]
    public class GetMostActiveUsers : IReturn<object>
    {
    }

    [Route( "/Statistics2026/least_watched_movies", "GET", Summary = "Get the List of Least Watched Movies" )]
    [Authenticated]
    public class GetLeastWatchedMovies : IReturn<object>
    {
    }

    [Route( "/Statistics2026/most_watched_movies/{User}", "GET", Summary = "Get the List of Most Watched Movies" )]
    [Authenticated]
    public class GetMostWatchedMovies : IReturn<object>
    {
        [ApiMember( Name = "user", Description = "The user for whom to retrieve statistics", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET" )]
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/most_watched_movies", "GET", Summary = "Get the List of Most Watched Movies" )]
    [Authenticated]
    public class GetMostWatchedMoviesNoUser : IReturn<object>
    {
    }

    [Route( "/Statistics2026/least_watched_shows", "GET", Summary = "Get the List of Least Watched Shows" )]
    [Authenticated]
    public class GetLeastWatchedShows : IReturn<object>
    {
    }

    [Route( "/Statistics2026/total_time_watched/{User}", "GET", Summary = "Get the Total Time Watched for User" )]
    [Authenticated]
    public class GetTotalTimeWatched : IReturn<object>
    {
        public string user { get; set; } = string.Empty;

        [ApiMember( Name = "episodes", Description = "Episodes", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET" )]
        public string episodes { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/total_watchable_time/{User}", "GET", Summary = "Get the Total Time Watched for User" )]
    [Authenticated]
    public class GetTotalWatchableTime : IReturn<object>
    {
        public string user { get; set; } = string.Empty;

        [ApiMember( Name = "episodes", Description = "Episodes", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET" )]
        public string episodes { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/last_seen/{User}", "GET", Summary = "Get the Favorite Movie Years" )]
    [Authenticated]
    public class GetLastSeen : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
        public bool episodes { get; set; } = false;
    }

    public class UserService : BaseService
    {
        public UserService(
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

        public Task<object> Get( GetUserCount request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, async (timer) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstance( _embyInterfaces, request.GetType().Name );

                StatCard groupData = await db.UserCount();
                if( groupData == null )
                    return new StatCardResponse();

                object vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public Task<object> Get( GetMostActiveUsers request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, async (timer) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstance( _embyInterfaces, request.GetType().Name );

                StatCard groupData = await db.MostActiveUsers();
                if( groupData == null )
                    return new StatCardResponse();
                groupData.SortByKey = false;

                object vgReponse = groupData.createStat();

                return vgReponse;
            } );
        }

        public Task<object> Get( GetLeastWatchedMovies request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, async (timer) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstance( _embyInterfaces, request.GetType().Name );
                StatCard groupData = await db.WatchedMedia( null, true, EMediaType.eMovie );
                if( groupData == null )
                    return new StatCardResponse();

                object vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public Task<object> Get( GetMostWatchedMovies request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, async (timer) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstance( _embyInterfaces, request.GetType().Name );

                string userName = request.user;
                User? user = EmbyHelperFuncs.GetUserByName( userName, _embyInterfaces );
                if( user == null )
                    return new StatCardResponse();

                StatCard groupData = await db.WatchedMedia( user, false, EMediaType.eMovie );
                if( groupData == null )
                    return new StatCardResponse();

                object vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public Task<object> Get( GetMostWatchedMoviesNoUser request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, async (timer) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstance( _embyInterfaces, request.GetType().Name );

                StatCard groupData = await db.WatchedMedia( null, false, EMediaType.eMovie );
                if( groupData == null )
                    return new StatCardResponse();

                object vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public Task<object> Get( GetLeastWatchedShows request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, async (timer) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstance( _embyInterfaces, request.GetType().Name );

                StatCard groupData = await db.WatchedMedia( null, true, EMediaType.eSeries );
                if( groupData == null )
                    return new StatCardResponse();

                object vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public Task<object> Get( GetTotalTimeWatched request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, async (timer) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstance( _embyInterfaces, request.GetType().Name );
                string userName = request.user;
                User? user = EmbyHelperFuncs.GetUserByName( userName, _embyInterfaces );
                if( user == null )
                    return new StatCardResponse();

                bool? showEpisodes = null;
                if( request.episodes == "true" )
                    showEpisodes = true;
                else if( request.episodes == "all" )
                    showEpisodes = null;
                else
                    showEpisodes = false;

                StatCard groupData = await db.TotalTimeWatched( user, showEpisodes );
                if( groupData == null )
                    return new StatCardResponse();

                object vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public Task<object> Get( GetTotalWatchableTime request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, async (timer) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstance( _embyInterfaces, request.GetType().Name );
                string userName = request.user;
                User? user = EmbyHelperFuncs.GetUserByName( userName, _embyInterfaces );
                if( user == null )
                    return new StatCardResponse();

                bool? showEpisodes = null;
                if( request.episodes == "true" )
                    showEpisodes = true;
                else if( request.episodes == "all" )
                    showEpisodes = null;
                else
                    showEpisodes = false;

                StatCard groupData = await db.TotalWatchableTime( user, showEpisodes );
                if( groupData == null )
                    return new StatCardResponse();

                object vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public Task<object> Get( GetLastSeen request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, async (timer) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstance( _embyInterfaces, request.GetType().Name );
                string userName = request.user;
                User? user = EmbyHelperFuncs.GetUserByName( userName, _embyInterfaces );
                if( user == null )
                    return new StatCardResponse();

                bool episodes = request.episodes;

                StatCard groupData = await db.LastSeen( user, !episodes );
                if( groupData == null )
                    return new StatCardResponse();

                object vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }
    }
}
