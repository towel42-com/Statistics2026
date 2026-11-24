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
    [Route( "/Statistics2026/tv_series_progress/{User}", "GET", Summary = "Gets the tv series progress for the user" )]
    [Authenticated]
    public class GetTVSeriesProgress : IReturn<List<GetTVSeriesProgressResponse>>
    {
        public string user { get; set; } = string.Empty;
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
            return HelperFuncs.HandleRequestAsync<object>( request.GetType().Name, _embyInterfaces, async (timer) =>
            {
                await using Statistics2026DB db = await Statistics2026DB.GetInstanceAsync( _embyInterfaces, request.GetType().Name );
                string userName = request.user;
                User? user = EmbyHelperFuncs.GetUserByName( userName, _embyInterfaces );
                if( user == null )
                    return new List<GetTVSeriesProgressResponse>();

                return await db.GetTVSeriesProgressAsync( user );
            } );
        }
    }
}