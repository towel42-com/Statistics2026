using MediaBrowser.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Tasks;
using Statistics2026.Api;
using Statistics2026.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
//using System.Diagnostics;
//using System.Linq;
//using System.IO;
//using MediaBrowser.Model.Session;

namespace Statistics2026
{
    class EventMonitorEntryPoint : IServerEntryPoint
    {
        private EmbyInterfaces? _embyInterfaces;

        private readonly object syncLock = new object();

        public EventMonitorEntryPoint(
            ISessionManager sessionManager,
            ILogManager logManager,
            IServerConfigurationManager configManager,
            IUserManager userManager,
            IUserDataManager userDataManager,
            ILibraryManager libraryManager,
            IFileSystem fileSystem,
            IJsonSerializer jsonSerializer,
            IServerApplicationPaths serverApplicationPaths,
            IApplicationHost appHost,
            IProviderManager providerManager,
            Statistics2026API apiService,
            ITaskManager taskManager
            )
        {

            _embyInterfaces = new EmbyInterfaces(fileSystem, libraryManager, logManager, logManager.GetLogger("Statistics2026 - EventMonitorEntryPoint"), serverApplicationPaths, userDataManager, userManager, appHost, apiService, jsonSerializer, providerManager, configManager, taskManager);
            _embyInterfaces._sessionManager = sessionManager;
        }

        public void Dispose()
        {

        }

        private void CheckIsValid()
        {
            if (_embyInterfaces == null)
                throw new ArgumentNullException("_embyInterfaces");
            if (_embyInterfaces._sessionManager == null)
                throw new ArgumentNullException("_sessionManager");
        }

        public void Run()
        {
            var db = StatisticsDB.GetInstance(_embyInterfaces);
            db.Initialize(null, null);

            CheckIsValid();

            _embyInterfaces!._logger!.Debug("EventMonitorEntryPoint Running");

            _embyInterfaces._sessionManager!.PlaybackStart += _sessionManager_PlaybackStart;
            _embyInterfaces._sessionManager!.PlaybackStopped += _sessionManager_PlaybackStop;

            // start playback monitor
            Task.Run(() => PlaybackMonitoringTask());
        }

        void _sessionManager_PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            CheckIsValid();
            _embyInterfaces!._logger!.Info("_sessionManager_PlaybackStart : Entered");
            lock (syncLock)
            {
                ProcessSessions();
            }
        }

        void _sessionManager_PlaybackStop(object sender, PlaybackStopEventArgs e)
        {
            CheckIsValid();
            _embyInterfaces!._logger!.Info("_sessionManager_PlaybackStop : Entered");
            lock (syncLock)
            {
                ProcessSessions();
            }
        }

        public async Task PlaybackMonitoringTask()
        {
            CheckIsValid();
            _embyInterfaces!._logger!.Info("PlaybackMonitoringTask : Started");
            int currThreadSleep = 20;
            const int maxThreadSleep = 300;

            while (true)
            {
                try
                {
                    lock (syncLock)
                    {
                        ProcessSessions();
                    }

                    currThreadSleep = 20;
                }
                catch (Exception err)
                {
                    _embyInterfaces!._logger!.ErrorException("PlaybackMonitoringTask Exception", err);

                    // try to throttle repeated exceptions up to a max of 5 min
                    if (currThreadSleep < maxThreadSleep)
                    {
                        currThreadSleep = currThreadSleep + 10;
                    }
                    _embyInterfaces!._logger!.Debug("PlaybackMonitoringTask New Thread Sleep : " + currThreadSleep);
                }

                await Task.Delay(currThreadSleep * 1000);
            }
        }

        private List<PlaybackInfo>? ActiveSessions = null;

        private void ProcessSessions()
        {
            CheckIsValid();
            _embyInterfaces!._logger!.Debug("PlaybackMonitoringTask : ProcessSessions Start");
            ActiveSessions = new List<PlaybackInfo>();

            foreach (SessionInfo session in _embyInterfaces!._sessionManager!.Sessions)
            {
                if (session.NowPlayingItem == null)
                {
                    _embyInterfaces!._logger!.Debug($"PlaybackMonitoringTask : user: {session.UserName} - No Media being played");
                    // nothing playing so move on to next
                    continue;
                }

                var playbackInfo = PlaybackInfo.Create(session, _embyInterfaces);
                ActiveSessions.Add(playbackInfo);

                playbackInfo.UpdatePlaybackState(_embyInterfaces);
            }

            PlaybackInfo.RemoveInactivePlayinfo(ActiveSessions,_embyInterfaces);
            ActiveSessions.Clear();
            _embyInterfaces!._logger!.Debug("PlaybackMonitoringTask : ProcessSessions End");
        }
    }
}
