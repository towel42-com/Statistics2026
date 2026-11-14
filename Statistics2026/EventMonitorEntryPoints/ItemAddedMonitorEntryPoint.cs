using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Statistics2026.Api;
using Statistics2026.Configuration;
using Statistics2026.Data;
using Statistics2026.ScheduledTasks;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Statistics2026.EventMonitorEntryPoints
{
    public class ItemAddedMonitorEntryPoint : IServerEntryPoint
    {
        private readonly EmbyInterfaces _embyInterfaces;
        private readonly System.Timers.Timer _debounceTimer;
        private List<string> Reasons = [];

        public ItemAddedMonitorEntryPoint(
            IFileSystem fileSystem,
            ILibraryManager libraryManager,
            ILogManager logManager,
            IServerApplicationPaths serverApplicationPaths,
            IUserDataManager userDataManager,
            IUserManager userManager,
            IApplicationHost appHost,
            Statistics2026API apiService,
            IJsonSerializer jsonSerializer,
            IProviderManager providerManager,
            IServerConfigurationManager configManager,
            ITaskManager taskManager,
            ISessionManager sessionManager,
            IHttpClient httpClient,
            IAuthenticationRepository authenticationRepository,
            INotificationManager notificationManager
            )
        {
            _embyInterfaces = new EmbyInterfaces()
            {
                _logger = logManager.GetLogger( "Statistics2026 - MediaOrUserAddMonitorEntryPoint" ),
                _appHost = appHost,
                _authenticationRepository = authenticationRepository,
                _fileSystem = fileSystem,
                _httpClient = httpClient,
                _jsonSerializer = jsonSerializer,
                _libraryManager = libraryManager,
                _logManager = logManager,
                _notificationManager = notificationManager,
                _providerManager = providerManager,
                _serverApplicationPaths = serverApplicationPaths,
                _configManager = configManager,
                _sessionManager = sessionManager,
                _taskManager = taskManager,
                _userDataManager = userDataManager,
                _userManager = userManager,
                _apiService = apiService,
            };
            _debounceTimer = new System.Timers.Timer( 30000 );
            _debounceTimer.AutoReset = false;
            _debounceTimer.Elapsed += OnTimerElapsed;
        }

        private static PluginConfiguration? PluginConfiguration => Plugin.Instance?.Configuration ?? null;

        public void Run()
        {
            _embyInterfaces.LibraryManager().ItemAdded += OnItemAdded;
            _embyInterfaces.UserManager().UserCreated += OnUserCreated;
        }

        public void Dispose()
        {
            _embyInterfaces.LibraryManager().ItemAdded -= OnItemAdded;
            _embyInterfaces.UserManager().UserCreated -= OnUserCreated;
            _debounceTimer.Dispose();
        }

        private void OnItemAdded( object sender, ItemChangeEventArgs e )
        {
            if( e.Item == null )
                return;
            var enabledStr = _debounceTimer.Enabled ? "Enabled" : "NOT Enabled";
            _embyInterfaces.Logger().Debug( $"Notification of item added: Timer currently {enabledStr}" );
            Reasons.Add( "Item Added" );
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void OnUserCreated( object sender, GenericEventArgs<User> e )
        {
            if( e.Argument == null )
                return;

            var enabledStr = _debounceTimer.Enabled ? "Enabled" : "NOT Enabled";
            _embyInterfaces.Logger().Debug( $"Notification of user added: Timer currently {enabledStr}" );
            Reasons.Add( "User Added" );
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void OnTimerElapsed( object sender, ElapsedEventArgs e )
        {
            CheckForOOD();
        }

        private void CheckForOOD()
        {
            _embyInterfaces.Logger().Debug( $"Timer Expired" );
            HelperFuncs.CheckForOOD( _embyInterfaces, false, Reasons, GetType() );
            Reasons.Clear();
            _debounceTimer.Stop();
        }
    }
}