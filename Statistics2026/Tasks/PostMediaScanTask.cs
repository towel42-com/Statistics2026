using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Statistics2026;
using Statistics2026.Api;
using Statistics2026.Configuration;
using Statistics2026.Data;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.ScheduledTasks
{
    public class PostMediaScanTask : ILibraryPostScanTask
    {
        private readonly EmbyInterfaces _embyInterfaces;

        public PostMediaScanTask(
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
        }

        private static PluginConfiguration? PluginConfiguration => Plugin.Instance?.Configuration ?? null;

        Task ILibraryPostScanTask.Run( IProgress<double> progress, CancellationToken cancellationToken )
        {
            if( _embyInterfaces == null || TaskDef.IsStatistics2026TaskRunning( _embyInterfaces, GetType(), false ) )
                return Task.CompletedTask;

            var db = StatisticsDB.GetInstance( _embyInterfaces );
            db.Initialize( null, cancellationToken, progress, false );

            var currLogger = _embyInterfaces.Logger();
            var (runFullAnalysis, runMissingAnalysis) = EmbyHelperFuncs.AnalysisOutOfDate( _embyInterfaces, db );
            _embyInterfaces._logger = currLogger;
            _embyInterfaces.Logger().Info( $"runFullAnalysis=${runFullAnalysis} runMissingAnalysis=${runMissingAnalysis}");

            List<Type> tasks = [];
            if( !runFullAnalysis && runMissingAnalysis )
            {
                tasks.AddRange( [ typeof( AnalyzeMissingEpisodesTask ), typeof( AnalyzeMissingMoviesTask ) ] );
            }
            else if( runFullAnalysis )
            {
                tasks.Add( typeof( RunAllTasksTask ) );
            }

            _ = TaskDef.ExecuteSubTasks( _embyInterfaces, tasks, cancellationToken, false );
            return Task.CompletedTask;
        }
    }
}