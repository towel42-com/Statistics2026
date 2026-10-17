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
            _embyInterfaces = new EmbyInterfaces( appHost )
            {
                _fileSystem = fileSystem,
                _libraryManager = libraryManager,
                _logManager = logManager,
                _logger = logManager.GetLogger( "Statistics2026 - PostMediaScanTask" ),
                _serverApplicationPaths = serverApplicationPaths,
                _userDataManager = userDataManager,
                _userManager = userManager,
                _appHost = appHost,
                _apiService = apiService,
                _jsonSerializer = jsonSerializer,
                _providerManager = providerManager,
                _configManager = configManager,
                _taskManager = taskManager,
            };
        }

        private static PluginConfiguration? PluginConfiguration => Plugin.Instance?.Configuration ?? null;

        async Task ILibraryPostScanTask.Run( IProgress<double> progress, CancellationToken cancellationToken )
        {
            if( _embyInterfaces == null || _embyInterfaces._taskManager == null )
                return;

            var db = StatisticsDB.GetInstance( _embyInterfaces );
            db.Initialize( null, cancellationToken, progress, false );

            var (runFullAnalysis, runMissingAnalysis) = EmbyHelperFuncs.AnalysisOutOfDate( _embyInterfaces, db );

            if( !runFullAnalysis && runMissingAnalysis )
            {
                var taskWorker = _embyInterfaces._taskManager.ScheduledTasks.FirstOrDefault( taskWorker => taskWorker.ScheduledTask.GetType() == typeof( AnalyzeMissingEpisodesTask ) );
                if( taskWorker == null || taskWorker.ScheduledTask == null )
                    throw new Exception( $"AnalyzeMissingEpisodesTask not found Run" );

                var options = new TaskOptions() { HasManualInteraction = false };
                await _embyInterfaces._taskManager.Execute( taskWorker, options );
                
                taskWorker = _embyInterfaces._taskManager.ScheduledTasks.FirstOrDefault( taskWorker => taskWorker.ScheduledTask.GetType() == typeof( AnalyzeMissingMoviesTask ) );
                if( taskWorker == null || taskWorker.ScheduledTask == null )
                    throw new Exception( $"AnalyzeMissingEpisodesTask not found Run" );

                options = new TaskOptions() { HasManualInteraction = false };
                await _embyInterfaces._taskManager.Execute( taskWorker, options );
            }
            else if ( runFullAnalysis )
            {
                var taskWorker = _embyInterfaces._taskManager.ScheduledTasks.FirstOrDefault( taskWorker => taskWorker.ScheduledTask.GetType() == typeof( RunAllTasksTask ) );
                if( taskWorker == null || taskWorker.ScheduledTask == null )
                    throw new Exception( $"RunAllTasksTask not found Run" );

                var options = new TaskOptions() { HasManualInteraction = false };
                await _embyInterfaces._taskManager.Execute( taskWorker, options );
            }
        }
    }
}