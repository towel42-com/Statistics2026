using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Statistics2026.Configuration;
using Statistics2026.Data;
using Statistics2026.Utilities;

namespace Statistics2026.ScheduledTasks
{
    public class RunAllTasksTask : IStatistics2026ScheduledTask
    {
        public EmbyInterfaces _embyInterfaces { get; }

        public RunAllTasksTask(
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
            )
        {
            _embyInterfaces = new EmbyInterfaces()
            {
                _logger = logManager.GetLogger( "Statistics2026 - RunAllTasksTask" ),
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
            };
            this.Name = TaskDef.GetTaskName( this.GetType() );
            this.Description = TaskDef.GetTaskDescription( this.GetType() );
        }

        public string Name { get; private set; } = "";
        public string Description { get; private set; } = "";
        public List<string> ReasonsForExecute { get; set; } = TaskDef.DefaultTaskReasons;

        private static PluginConfiguration? PluginConfiguration => Plugin.Instance?.Configuration ?? null;

        string IScheduledTask.Key => "Statistics2026CalculateStatsTask";

        string IScheduledTask.Category => "Statistics 2026";

        async Task IScheduledTask.Execute( CancellationToken cancellationToken, IProgress<double> progress )
        {
            HelperFuncs.MemoryUsage memoryReport = await HelperFuncs.RunAndReportMemoryAsync( true, async () =>
            {
                if( TaskDef.IsStatistics2026TaskRunning( _embyInterfaces, GetType(), false ) )
                {
                    _embyInterfaces?.Logger().Info( "Analysis is currently running" );
                    return;
                }
                if( PluginConfiguration == null )
                    throw new ArgumentNullException( nameof( PluginConfiguration ) );

                Statistics2026DB db = await Statistics2026DB.GetInstanceAsync( _embyInterfaces, GetType().Name ).ConfigureAwait( false );
                await using( db.ConfigureAwait( false ) )
                {
                    await db.InitializeAsync( this, cancellationToken, progress, PluginConfiguration.systemSettings.resetPlayCount ).ConfigureAwait( false );
                    cancellationToken.ThrowIfCancellationRequested();

                    string taskName = "Analyze All";
                    _embyInterfaces.Logger().Info( $"Starting Statistics 2026 {taskName} task" );

                    await db.ClearLastUpdatedAsync( Statistics2026DB.EAction.System ).ConfigureAwait( false );

                    var overAllTimer = new AutoTimer( $"Adding All Data", _embyInterfaces.Logger(), false );
                    List<TaskDef>? tasks = TaskDef.GetKnownTasks( false );

                    for( int ii = 0; ii < tasks?.Count; ii++ )
                    {
                        progress.Report( 100.0 * ii / ( 1.0 * tasks.Count ) );
                        TaskDef task = tasks[ ii ];
                        if( task != null )
                        {
                            task.Result = await TaskDef.ExecuteSubTaskAsync( _embyInterfaces, task, ReasonsForExecute, cancellationToken, true ).ConfigureAwait( false );
                            tasks[ ii ] = task;
                        }
                    }

                    long overall = overAllTimer.ElapsedMilliseconds();
                    await db.SetLastUpdatedAsync( Statistics2026DB.EAction.System, ReasonsForExecute, overall ).ConfigureAwait( false );
                    await EmbyHelperFuncs.UpdateLastAddedAsync( _embyInterfaces, ReasonsForExecute, db ).ConfigureAwait( false );

                    overAllTimer.Dispose();
                    _embyInterfaces.Logger().Info( $"=======================================" );
                    _embyInterfaces.Logger().Info( $"Time to Analyze: {overall} ms" );
                    if( tasks != null )
                    {
                        int maxLen = 0;
                        foreach( TaskDef task in tasks )
                        {
                            if( task == null )
                                continue;

                            if( task.Description.Length > maxLen )
                                maxLen = task.Description.Length;
                        }

                        foreach( TaskDef task in tasks )
                        {
                            if( task == null )
                                continue;
                            _embyInterfaces.Logger().Info( $"{task.Description.PadLeft( maxLen )}: {task.RunTimeMS} ms" );
                        }
                    }

                    _embyInterfaces.Logger().Info( $"=======================================" );
                    _embyInterfaces.Logger().Info( $"Finished Statistics 2026 {taskName} task" );

                    Plugin.Instance?.AddDBState( EDBState.eFullyInitialized );
                    Plugin.Instance?.SaveConfiguration();
                    db.ResetCancellationToken();

                    GetItemImageUrl.ClearCache();
                    HelperFuncs.ClearCache();
                    MediaInfo.ClearCache();
                    TmdbInterface.ClearCache();
                }
            } ).ConfigureAwait( false );
            memoryReport.Report( _embyInterfaces.Logger(), GetType().Name, LogSeverity.Info );
        }

        IEnumerable<TaskTriggerInfo> IScheduledTask.GetDefaultTriggers()
        {
            return new[] {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromMinutes(30).Ticks
                }
            };
        }
    }
}