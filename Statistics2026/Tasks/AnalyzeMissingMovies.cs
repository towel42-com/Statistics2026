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
    public class AnalyzeMissingMoviesTask : IStatistics2026ScheduledTask
    {
        private readonly EmbyInterfaces _embyInterfaces;

        public AnalyzeMissingMoviesTask(
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
                _logger = logManager.GetLogger( "Statistics2026 - AnalyzeMissingMoviesTask" ),
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
            this.Name = TaskDef.GetTaskName( this.GetType() );
            this.Description = TaskDef.GetTaskDescription( this.GetType() );
        }

        public string Name { get; private set; } = "";
        public string Description { get; private set; } = "";
        public List<string> ReasonsForExecute { get; set; } = TaskDef.DefaultTaskReasons;

        string IScheduledTask.Key => "Statistics2026_MissingMoviesTask";

        string IScheduledTask.Category => "Statistics 2026";

        async Task IScheduledTask.Execute( CancellationToken cancellationToken, IProgress<double> progress )
        {
            if( TaskDef.IsStatistics2026TaskRunning( _embyInterfaces, GetType(), true ) )
            {
                throw new Exception( "Statistics 2026 task is running" );
            }

            var taskName = "Analyze Missing";
            _embyInterfaces.Logger().Info( $"Starting Statistics 2026 {taskName} task" );

            using var db = Statistics2026DB.GetInstance( _embyInterfaces );
            db.Initialize( this, cancellationToken, progress );
            db.ClearLastUpdated( Statistics2026DB.EAction.MissingMoviesAnalysis );

            long runtimeMS = 0;
            using( var timer = new AutoTimer( $"Analyzing Missing", _embyInterfaces.Logger() ) )
            {
                try
                {
                    await db.AnalyzeMissingMoviesTaskImplAsync().ConfigureAwait( false );
                    runtimeMS = timer.ElapsedMilliseconds();
                }
                catch( Exception ex )
                {
                    _embyInterfaces.Logger().ErrorException( $"Problem running '{( (IScheduledTask)this ).Name}", ex );
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            HelperFuncs.RunTimeSummary( runtimeMS, taskName, _embyInterfaces );
            db.SetLastUpdated( Statistics2026DB.EAction.MissingMoviesAnalysis, ReasonsForExecute, runtimeMS );

            db.ResetCancellationToken();
        }

        IEnumerable<TaskTriggerInfo> IScheduledTask.GetDefaultTriggers()
        {
            return Array.Empty<TaskTriggerInfo>();
        }
    }
}