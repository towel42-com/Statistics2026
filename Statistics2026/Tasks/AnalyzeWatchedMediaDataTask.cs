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
using Statistics2026.Api;
using Statistics2026.Configuration;
using Statistics2026.Data;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.ScheduledTasks
{
    public class AnalyzeWatchedMediaDataTask : IScheduledTask
    {
        private readonly EmbyInterfaces _embyInterfaces;

        public AnalyzeWatchedMediaDataTask(
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
                _logger = logManager.GetLogger( "Statistics2026 - AnalyzeWatchedMediaDataTask" ),
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

        string IScheduledTask.Name => "\u2022 Analyze User Watched Data Information";

        string IScheduledTask.Key => "Statistics2026_UserWatchedDataTask";

        string IScheduledTask.Description => "Task that will analyze the user watch data.";

        string IScheduledTask.Category => "Statistics 2026";

        Task IScheduledTask.Execute( CancellationToken cancellationToken, IProgress<double> progress )
        {
            if( Plugin.Instance != null && Plugin.Instance.IsStatistics2026TaskRunning( GetType() ) )
            {
                throw new Exception( "Statistics 2026 task is running" );
            }

            var taskName = "Analyze User Watch Data";
            _embyInterfaces._logger!.Info( $"Statistics 2026 : Starting Statistics 2026 {taskName} task" );

            var db = StatisticsDB.GetInstance( _embyInterfaces );
            db.Initialize( this, cancellationToken, progress );
            db.ClearLastUpdated( StatisticsDB.EAction.UserWatchDataAnalysis );

            long addUsers = 0;
            using( var timer = new AutoTimer( $"Adding User Watch Data", _embyInterfaces._logger ) )
            {
                db.AnalyzeWatchedMediaDataTaskImpl();
                addUsers = timer.ElapsedMilliseconds();
                cancellationToken.ThrowIfCancellationRequested();
            }

            _embyInterfaces._logger.Info( $"=======================================" );
            _embyInterfaces._logger.Info( $"User Watch Data : {addUsers} ms" );
            _embyInterfaces._logger.Info( $"=======================================" );
            _embyInterfaces._logger.Info( $"Statistics 2026: Finished Statistics 2026 {taskName} task" );

            db.UpdateLastUpdated( StatisticsDB.EAction.UserWatchDataAnalysis, addUsers );

            db.ResetCancellationToken();
            Plugin.Instance?.AddDBState( EDBState.eUserTablesCreated );

            return Task.CompletedTask;
        }

        IEnumerable<TaskTriggerInfo> IScheduledTask.GetDefaultTriggers()
        {
            return Array.Empty<TaskTriggerInfo>();
        }
    }
}