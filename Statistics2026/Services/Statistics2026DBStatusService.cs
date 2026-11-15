using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
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
using Statistics2026.Responses;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statistics2026.Services
{
    [Route( "/Statistics2026/database_status", "GET" )]
    [Authenticated]
    public class GetDatabaseStatus : IReturn<GetDatabaseStatusReponse>
    {
    }

    public class Statistics2026DBStatusService : Statistics2026BaseService
    {
        public Statistics2026DBStatusService(
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

        public object Get( GetDatabaseStatus request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                if( Plugin.Instance == null )
                    return new GetDatabaseStatusReponse();

                return new GetDatabaseStatusReponse( Plugin.Instance.Configuration );
            } );
        }
    }
}
