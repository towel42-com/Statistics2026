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
using Statistics2026.Data;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.ScheduledTasks
{
    public class AnalyzeMediaTask : IScheduledTask
    {
        private readonly EmbyInterfaces _embyInterfaces;

        public AnalyzeMediaTask(
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
                _logger = logManager.GetLogger( "Statistics2026 - AnalyzeMediaTask" ),
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

        string IScheduledTask.Name => "\u2022 Analyze Media information";

        string IScheduledTask.Key => "Statistics2026_MediaTask";

        string IScheduledTask.Description => "Task that will analyze the library's media.";

        string IScheduledTask.Category => "Statistics 2026";

        async Task IScheduledTask.Execute( CancellationToken cancellationToken, IProgress<double> progress )
        {
            if( TaskDef.IsStatistics2026TaskRunning( _embyInterfaces, GetType(), true ) )
            {
                throw new Exception( "Statistics 2026 task is running" );
            }

            var taskName = "Analyze Media";
            _embyInterfaces.Logger().Info( $"Starting Statistics 2026 {taskName} task" );

            var db = StatisticsDB.GetInstance( _embyInterfaces );
            db.Initialize( this, cancellationToken, progress );
            db.ClearLastUpdated( StatisticsDB.EAction.MediaAnalysis );

            long addMedia = 0;
            using( var timer = new AutoTimer( $"Adding All Media", _embyInterfaces.Logger() ) )
            {
                await db.AddAllMediaTaskImpl();
                addMedia = timer.ElapsedMilliseconds();
            }

            cancellationToken.ThrowIfCancellationRequested();

            _embyInterfaces.Logger().Info( $"=======================================" );
            _embyInterfaces.Logger().Info( $"          Media: {addMedia} ms" );
            _embyInterfaces.Logger().Info( $"=======================================" );
            _embyInterfaces.Logger().Info( $"Finished Statistics 2026 {taskName} task" );

            db.UpdateLastUpdated( StatisticsDB.EAction.MediaAnalysis, addMedia );
            
            db.ResetCancellationToken();
        }

        IEnumerable<TaskTriggerInfo> IScheduledTask.GetDefaultTriggers()
        {
            return Array.Empty<TaskTriggerInfo>();
        }
    }
}