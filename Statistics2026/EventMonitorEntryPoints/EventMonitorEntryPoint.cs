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
using Statistics2026.Data;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Statistics2026.EventMonitorEntryPoints
{
    internal class EventMonitorEntryPoint : IServerEntryPoint
    {
        private readonly EmbyInterfaces? _embyInterfaces;

        private readonly object syncLock = new();

        public EventMonitorEntryPoint(
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
            // missing one
            _embyInterfaces = new EmbyInterfaces()
            {
                _logger = logManager.GetLogger( "Statistics2026 - EventMonitorEntryPoint" ),
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

        public void Dispose()
        {

        }

        private void CheckIsValid()
        {
            if( _embyInterfaces == null )
                throw new ArgumentNullException( "_embyInterfaces" );
            if( _embyInterfaces._sessionManager == null )
                throw new ArgumentNullException( "_sessionManager" );
        }

        public void Run()
        {
            CheckIsValid();

            _embyInterfaces!.Logger().Debug( "EventMonitorEntryPoint Running" );

            _embyInterfaces.SessionManager().PlaybackStart += _sessionManager_PlaybackStart;
            _embyInterfaces.SessionManager().PlaybackStopped += _sessionManager_PlaybackStop;

            // start playback monitor
            _ = Task.Run( () => PlaybackMonitoringTask() );
        }

        private void _sessionManager_PlaybackStart( object sender, PlaybackProgressEventArgs e )
        {
            CheckIsValid();
            _embyInterfaces!.Logger().Info( "_sessionManager_PlaybackStart : Entered" );
            lock( syncLock )
            {
                ProcessSessions();
            }
        }

        private void _sessionManager_PlaybackStop( object sender, PlaybackStopEventArgs e )
        {
            CheckIsValid();
            _embyInterfaces!.Logger().Info( "_sessionManager_PlaybackStop : Entered" );
            lock( syncLock )
            {
                ProcessSessions();
            }
        }

        public async Task PlaybackMonitoringTask()
        {
            CheckIsValid();
            _embyInterfaces!.Logger().Info( "PlaybackMonitoringTask : Started" );
            var currThreadSleep = 20;
            const int maxThreadSleep = 300;

            while( true )
            {
                try
                {
                    lock( syncLock )
                    {
                        ProcessSessions();
                    }

                    currThreadSleep = 20;
                }
                catch( Exception err )
                {
                    _embyInterfaces!.Logger().ErrorException( "PlaybackMonitoringTask Exception", err );

                    // try to throttle repeated exceptions up to a max of 5 min
                    if( currThreadSleep < maxThreadSleep )
                    {
                        currThreadSleep += 10;
                    }

                    _embyInterfaces!.Logger().Debug( "PlaybackMonitoringTask New Thread Sleep : " + currThreadSleep );
                }

                await Task.Delay( currThreadSleep * 1000 );
            }
        }

        private List<PlaybackInfo>? ActiveSessions = null;

        private void ProcessSessions()
        {
            CheckIsValid();

            if( Plugin.Instance == null || _embyInterfaces == null )
                return;

            if( TaskDef.IsStatistics2026TaskRunning( _embyInterfaces, false ) )
            {
                _embyInterfaces!.Logger().Warn( "Task is running" );
                return;
            }

            if( !Plugin.Instance.DBStateInitialized() )
            {
                _ = StatisticsDB.GetInstance( _embyInterfaces );
                if( !Plugin.Instance.DBStateInitialized() )
                {
                    _embyInterfaces!.Logger().Warn( "Databases have not been initialized" );
                    return;
                }
            }

            if( !Plugin.Instance.IsDBStateSet( EDBState.eSystemTablesCreated | EDBState.eUserTablesCreated | EDBState.eUserDataInitialized ) )
            {
                _embyInterfaces!.Logger().Warn( "Database state is invalid" );
                return;
            }

            _embyInterfaces!.Logger().Debug( "ProcessSessions Start" );
            ActiveSessions = [];

            foreach( var session in _embyInterfaces!.SessionManager().Sessions )
            {
                if( session.NowPlayingItem == null )
                {
                    _embyInterfaces!.Logger().Debug( $"user: {session.UserName} - No Media being played" );
                    // nothing playing so move on to next
                    continue;
                }

                var playbackInfo = PlaybackInfo.Create( session, _embyInterfaces );
                ActiveSessions.Add( playbackInfo );

                playbackInfo.UpdatePlaybackState( _embyInterfaces );
            }

            PlaybackInfo.RemoveInactivePlayinfo( ActiveSessions, _embyInterfaces );
            ActiveSessions.Clear();
            _embyInterfaces!.Logger().Debug( "ProcessSessions End" );
        }
    }
}
