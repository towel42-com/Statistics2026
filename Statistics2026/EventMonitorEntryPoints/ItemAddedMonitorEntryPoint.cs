using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
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
using System.Timers;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.EventMonitorEntryPoints
{
    public class ItemAddedMonitorEntryPoint : IServerEntryPoint
    {
        private readonly EmbyInterfaces _embyInterfaces;
        private readonly System.Timers.Timer _debounceTimer;

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
            IAuthenticationRepository authenticationRepository
            )
        {
            _embyInterfaces = new EmbyInterfaces()
            {
                _logger = logManager.GetLogger( "Statistics2026 - PostMediaScanTask" ),
                _appHost = appHost,
                _authenticationRepository = authenticationRepository,
                _fileSystem = fileSystem,
                _httpClient = httpClient,
                _jsonSerializer = jsonSerializer,
                _libraryManager = libraryManager,
                _logManager = logManager,
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
        }

        public void Dispose()
        {
            _embyInterfaces.LibraryManager().ItemAdded -= OnItemAdded;
            _debounceTimer.Dispose();
        }

        private void OnItemAdded( object sender, ItemChangeEventArgs e )
        {
            if( e.Item == null )
                return;
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void OnTimerElapsed( object sender, ElapsedEventArgs e )
        {
            try
            {
                if( _embyInterfaces == null || TaskDef.IsStatistics2026TaskRunning( _embyInterfaces, GetType(), false ) )
                    return;

                _debounceTimer.Stop();
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );
                db.Initialize( null, null, null, false );

                var currLogger = _embyInterfaces.Logger();
                var (runFullAnalysis, runMissingAnalysis) = EmbyHelperFuncs.AnalysisOutOfDate( _embyInterfaces, db );
                _embyInterfaces._logger = currLogger;
                _embyInterfaces.Logger().Info( $"runFullAnalysis=${runFullAnalysis} runMissingAnalysis=${runMissingAnalysis}" );

                List<Type> tasks = [];
                if( !runFullAnalysis && runMissingAnalysis )
                {
                    tasks.AddRange( [ typeof( AnalyzeMissingEpisodesTask ), typeof( AnalyzeMissingMoviesTask ) ] );
                }
                else if( runFullAnalysis )
                {
                    tasks.Add( typeof( RunAllTasksTask ) );
                }

                _ = TaskDef.ExecuteSubTasksAsync( _embyInterfaces, tasks, null, false );
            }
            catch( Exception )
            {
                // Always wrap timer callbacks in try-catch blocks to keep the server stable
            }
        }
    }
}