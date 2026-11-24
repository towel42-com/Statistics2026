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
    [Route( "/Statistics2026/total_collection_count", "GET", Summary = "Get the total Collection Count" )]
    [Authenticated( Roles = "admin" )]
    public class GetTotalCollectionCount : IReturn<object>
    {
    }

    [Route( "/Statistics2026/multi_collection_movies", "GET", Summary = "Get the List of Multi-Collection Movies" )]
    [Authenticated]
    public class GetMultiCollectionMovies : IReturn<object>
    {
    }

    public class CollectionsService : BaseService
    {
        public CollectionsService(
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

        public Task<object> Get( GetTotalCollectionCount request )
        {
            return HelperFuncs.HandleRequestAsync( request.GetType().Name, _embyInterfaces, async (timer) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstanceAsync( _embyInterfaces, request.GetType().Name );

                StatCard groupData = await db.TotalCollectionCountAsync();
                if( groupData == null )
                    return new StatCardResponse();

                object vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public Task<object> Get( GetMultiCollectionMovies request )
        {
            return HelperFuncs.HandleRequestAsync( request.GetType().Name, _embyInterfaces, async (timer) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstanceAsync( _embyInterfaces, request.GetType().Name );

                StatCard groupData = await db.MultiCollectionMoviesAsync();
                if( groupData == null )
                    return new StatCardResponse();

                object vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }
    }
}
