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
    [Route( "/Statistics2026/total_series_finished/{User}", "GET", Summary = "Get the total Movie Count" )]
    [Authenticated]
    public class GetTotalSeriesFinished : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/user_watch_media_issues", "GET", Summary = "Reports user media data that is causing the db status to not be valid" )]
    [Authenticated( Roles = "admin" )]
    public class GetUserWatchMediaIssues : IReturn<object>
    {
    }

    [Route( "/Statistics2026/played_user_media", "GET", Summary = "Reports Media played by users (debug only)" )]
    [Authenticated( Roles = "admin" )]
    public class GetPlayedUserMedia : IReturn<object>
    {
    }

    [Route( "/Statistics2026/most_watched_shows/{User}", "GET", Summary = "Get the List of Most Watched Shows" )]
    [Authenticated]
    public class GetMostWatchedShows : IReturn<object>
    {
        [ApiMember( Name = "User", Description = "The user for whom to retrieve statistics", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET" )]
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/most_watched_shows", "GET", Summary = "Get the List of Most Watched Shows" )]
    [Authenticated]
    public class GetMostWatchedShowsNoUser : IReturn<object>
    {
    }

    public class WatchedMediaDataService : BaseService
    {
        public WatchedMediaDataService(
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

        public Task<object> Get( GetTotalSeriesFinished request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, async (timer) => 
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstance( _embyInterfaces, request.GetType().Name );
                string userName = request.user;
                User? user = EmbyHelperFuncs.GetUserByName( userName, _embyInterfaces );
                if( user == null )
                    return new StatCardResponse();

                StatCard groupData = await db.TotalFinishedSeries( user );
                if( groupData == null )
                    return new StatCardResponse();

                object vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public Task<object> Get( GetUserWatchMediaIssues request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, async (timer) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstance( _embyInterfaces, request.GetType().Name );

                StatCard? groupData = await db.UserWatchMediaIssues();
                if( groupData == null )
                    return new StatCardResponse();

                object vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public Task<object> Get( GetPlayedUserMedia request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, async (timer) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstance( _embyInterfaces, request.GetType().Name );

                StatCard groupData = await db.PlayedUserMedia();
                if( groupData == null )
                    return new StatCardResponse();

                object vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public Task<object> Get( GetMostWatchedShows request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, async (timer) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstance( _embyInterfaces, request.GetType().Name );
                string userName = request.user;
                User? user = EmbyHelperFuncs.GetUserByName( userName, _embyInterfaces );
                if( user == null )
                    return new StatCardResponse();

                StatCard groupData = await db.WatchedMedia( user, false, EMediaType.eSeries );
                if( groupData == null )
                    return new StatCardResponse();

                object vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public Task<object> Get( GetMostWatchedShowsNoUser request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, async (timer) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstance( _embyInterfaces, request.GetType().Name );

                StatCard groupData = await db.WatchedMedia( null, false, EMediaType.eSeries );
                if( groupData == null )
                    return new StatCardResponse();

                object vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }
    }
}
