//using MediaBrowser.Common;
//using MediaBrowser.Common.Net;
//using MediaBrowser.Controller;
//using MediaBrowser.Controller.Configuration;
//using MediaBrowser.Controller.Library;
//using MediaBrowser.Controller.Providers;
//using MediaBrowser.Controller.Security;
//using MediaBrowser.Controller.Session;
//using MediaBrowser.Model.IO;
//using MediaBrowser.Model.Logging;
//using MediaBrowser.Model.Serialization;
//using MediaBrowser.Model.Tasks;
//using Statistics2026;
//using Statistics2026.Api;
//using Statistics2026.Configuration;
//using Statistics2026.Data;
//using Statistics2026.Utilities;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Globalization;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Statistics2026.ScheduledTasks
//{
//    public class PostMediaScanTask : ILibraryPostScanTask
//    {
//        private readonly EmbyInterfaces _embyInterfaces;

//        public PostMediaScanTask(
//            IFileSystem fileSystem,
//            ILibraryManager libraryManager,
//            ILogManager logManager,
//            IServerApplicationPaths serverApplicationPaths,
//            IUserDataManager userDataManager,
//            IUserManager userManager,
//            IApplicationHost appHost,
//            Statistics2026API apiService,
//            IJsonSerializer jsonSerializer,
//            IProviderManager providerManager,
//            IServerConfigurationManager configManager,
//            ITaskManager taskManager,
//            ISessionManager sessionManager,
//            IHttpClient httpClient,
//            IAuthenticationRepository authenticationRepository
//            )
//        {
//            _embyInterfaces = new EmbyInterfaces( appHost )
//            {
//                _fileSystem = fileSystem,
//                _libraryManager = libraryManager,
//                _logManager = logManager,
//                _logger = logManager.GetLogger( "Statistics2026 - RunAllTasksTask" ),
//                _serverApplicationPaths = serverApplicationPaths,
//                _userDataManager = userDataManager,
//                _userManager = userManager,
//                _appHost = appHost,
//                _apiService = apiService,
//                _jsonSerializer = jsonSerializer,
//                _providerManager = providerManager,
//                _configManager = configManager,
//                _taskManager = taskManager,
//            };
//        }

//        private static PluginConfiguration? PluginConfiguration => Plugin.Instance?.Configuration ?? null;

//        Task ILibraryPostScanTask.Run( IProgress<double> progress, CancellationToken cancellationToken )
//        {
//            return RunAllTasksTask.RunAllTasks( this, _embyInterfaces, progress, cancellationToken );
//        }
//    }
//}