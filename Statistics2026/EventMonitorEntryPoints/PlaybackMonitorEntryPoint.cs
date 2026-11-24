using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Statistics2026.Data;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Utilities;

namespace Statistics2026.EventMonitorEntryPoints
{
    internal class PlaybackMonitorEntryPoint : IServerEntryPoint, IHasEmbyInterface
    {
        public EmbyInterfaces _embyInterfaces { get; }

        private readonly TrackingSemaphoreSlim synclock = new TrackingSemaphoreSlim( 1, 1 );

        public PlaybackMonitorEntryPoint(
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
                _logger = logManager.GetLogger( "Statistics2026 - PlaybackMonitorEntryPoint" ),
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
        }

        public void Dispose()
        {

        }

        [MemberNotNull( nameof( _embyInterfaces ) )]
        private void CheckIsValid()
        {
            if( _embyInterfaces == null )
                throw new ArgumentNullException( "_embyInterfaces" );
        }

        public void Run()
        {
            CheckIsValid();

            _embyInterfaces.Logger().Debug( "EventMonitorEntryPoint Running" );

            _embyInterfaces.SessionManager().PlaybackStart += SessionManager_PlaybackStart;
            _embyInterfaces.SessionManager().PlaybackStopped += SessionManager_PlaybackStop;

            // start playback monitor
            _ = Task.Run( () => PlaybackMonitoringTaskAsync() );
        }

        [SuppressMessage( "Style", "IDE1006:Naming Styles", Justification = "Event handler cannot return Task" )]
        private async void SessionManager_PlaybackStart( object sender, PlaybackProgressEventArgs e )
        {
            try
            {
                CheckIsValid();
                _embyInterfaces.Logger().Info( "_sessionManager_PlaybackStart : Entered" );
                await ProcessSessionsAsync();
            }
            catch( Exception ex )
            {
                _embyInterfaces.Logger().ErrorException( "Error in SessionManager_PlaybackStart", ex );
            }
        }

        [SuppressMessage( "Style", "IDE1006:Naming Styles", Justification = "Event handler cannot return Task" )]
        private async void SessionManager_PlaybackStop( object sender, PlaybackStopEventArgs e )
        {
            try
            {
                CheckIsValid();
                _embyInterfaces.Logger().Info( "_sessionManager_PlaybackStop : Entered" );
                await ProcessSessionsAsync();
            }
            catch( Exception ex )
            {
                _embyInterfaces.Logger().ErrorException( "Error in SessionManager_PlaybackStart", ex );
            }
        }

        public async Task PlaybackMonitoringTaskAsync()
        {
            CheckIsValid();
            _embyInterfaces.Logger().Info( "PlaybackMonitoringTask : Started" );
            int currThreadSleep = 20;
            const int maxThreadSleep = 300;

            while( true )
            {
                try
                {
                    await ProcessSessionsAsync();

                    currThreadSleep = 20;
                }
                catch( Exception err )
                {
                    _embyInterfaces.Logger().ErrorException( "PlaybackMonitoringTask Exception", err );

                    // try to throttle repeated exceptions up to a max of 5 min
                    if( currThreadSleep < maxThreadSleep )
                    {
                        currThreadSleep += 10;
                    }

                    _embyInterfaces.Logger().Debug( "PlaybackMonitoringTask New Thread Sleep : " + currThreadSleep );
                }

                await Task.Delay( currThreadSleep * 1000 ).ConfigureAwait( false );
            }
        }

        private List<PlaybackInfo>? ActiveSessions = null;

        private async Task ProcessSessionsAsync()
        {
            await synclock.WaitAsync().ConfigureAwait( false );
            try
            {
                CheckIsValid();

                if( Plugin.Instance == null )
                    return;

                if( TaskDef.IsStatistics2026TaskRunning( _embyInterfaces, false ) )
                {
                    _embyInterfaces.Logger().Warn( "Task is running" );
                    return;
                }

                if( !Plugin.Instance.DBStateInitialized() )
                {
                    _ = Statistics2026DB.GetInstanceAsync( _embyInterfaces, this.GetType().ToString() );
                    if( !Plugin.Instance.DBStateInitialized() )
                    {
                        _embyInterfaces.Logger().Warn( "Databases have not been initialized" );
                        return;
                    }
                }

                if( !Plugin.Instance.IsDBStateSet( EDBState.eSystemTablesCreated | EDBState.eUserTablesCreated | EDBState.eUserDataInitialized ) )
                {
                    _embyInterfaces.Logger().Warn( "Database state is invalid" );
                    return;
                }

                _embyInterfaces.Logger().Debug( "ProcessSessions Start" );
                ActiveSessions = [];

                foreach( SessionInfo session in _embyInterfaces.SessionManager().Sessions )
                {
                    if( session.NowPlayingItem == null )
                    {
                        _embyInterfaces.Logger().Debug( $"user: {session.UserName} - No Media being played" );
                        // nothing playing so move on to next
                        continue;
                    }

                    PlaybackInfo playbackInfo = await PlaybackInfo.CreateAsync( session, _embyInterfaces );
                    ActiveSessions.Add( playbackInfo );

                    playbackInfo.UpdatePlaybackStateAsync( _embyInterfaces ).GetAwaiter().GetResult();
                }

                PlaybackInfo.RemoveInactivePlayinfo( ActiveSessions, _embyInterfaces );
                ActiveSessions.Clear();
                _embyInterfaces.Logger().Debug( "ProcessSessions End" );
            }
            finally
            {
                synclock.Release();
            }
        }
    }
}
