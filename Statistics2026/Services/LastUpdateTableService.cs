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
    [Route( "/Statistics2026/get_system_status", "GET", Summary = "Returns the system status" )]
    [Authenticated( Roles = "admin" )]
    public class GetSystemStatus : IReturn<object>
    {
    }

    [Route( "/Statistics2026/last_run_info", "GET", Summary = "Returns the last time the analysis for run was finished" )]
    [Authenticated]
    public class GetLastRunInfo : IReturn<object>
    {
        public string whichRuns { get; set; } = string.Empty; // comma separated list
    }

    public class LastUpdateTableService : BaseService
    {
        public LastUpdateTableService(
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

        public object Get( GetSystemStatus request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                return db.GetSystemStatus();
            } );
        }

        public object Get( GetLastRunInfo request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                return db.GetLastRunHtml( request.whichRuns );
            } );
        }

    }
}
