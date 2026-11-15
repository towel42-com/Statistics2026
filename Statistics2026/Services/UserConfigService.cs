using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
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
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Tasks;
using Statistics2026.Configuration;
using Statistics2026.Utilities;
using System.Net;

namespace Statistics2026.Services
{
    [Route( "/Statistics2026/get_user_config", "GET", Summary = "Get the pluging config for the current user" )]
    [Authenticated]
    public class GetUserConfig : IReturn<UserConfig>
    {
    }

    [Route( "/Statistics2026/set_user_config", "POST", Summary = "Set the pluging config for the current user" )]
    [Authenticated]
    public class SetUserConfig : UserConfig, IReturnVoid
    {
    }

    public class UserConfigService : BaseService
    {
        private readonly ISessionContext _sessionContext;

        public UserConfigService(
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
            INotificationManager notificationManager,
            ISessionContext sessionContext
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
            _sessionContext = sessionContext;
        }

        private string GetCurrentUserId()
        {
            if( Request == null )
            {
                throw new InvalidOperationException( "Request context is unavailable." );
            }

            var user = _sessionContext.GetUser( Request );
            if( user == null )
            {
                throw new UnauthorizedAccessException( "User is not authenticated." );
            }

            return user.Id.ToString();
        }

        public object Get( GetUserConfig request )
        {
            var userId = GetCurrentUserId();
            var config = Plugin.Instance!.Configuration;

            var userConfig = config.UserConfigs.FirstOrDefault( u => u.UserId == userId );
            return userConfig ?? new UserConfig { UserId = userId };
        }

        public void Post( SetUserConfig request )
        {
            var userId = GetCurrentUserId();
            var config = Plugin.Instance!.Configuration;

            var existing = config.UserConfigs.FirstOrDefault( u => u.UserId == userId );
            if( existing == null )
            {
                existing = new UserConfig { UserId = userId };
                config.UserConfigs.Add( existing );
            }

            existing.TableSortEntries = request.TableSortEntries;
            Plugin.Instance.SaveConfiguration();
        }
    }
}