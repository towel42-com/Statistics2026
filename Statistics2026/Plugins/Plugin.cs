using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Statistics2026.Api;
using Statistics2026.Configuration;
using Statistics2026.Features;
using Statistics2026.ScheduledTasks;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026
{
    public partial class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        private readonly EmbyInterfaces _embyInterfaces;

        public Plugin(
            IApplicationPaths applicationPaths,
            IXmlSerializer xmlSerializer,
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
            : base( applicationPaths, xmlSerializer )
        {
            Instance = this;
            _embyInterfaces = new EmbyInterfaces( appHost )
            {
                _fileSystem = fileSystem,
                _libraryManager = libraryManager,
                _logManager = logManager,
                _logger = logManager.GetLogger( "Statistics2026 - Plugin" ),
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

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new PluginPageInfo[]
            {
                new() {
                    Name = "style.css",
                    EmbeddedResourcePath = GetType().Namespace + ".style.css"
                },
                new() {
                    Name = "Helpers.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.Helpers.js"
                },
                new() {
                    Name = "Summary",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Summary.html",
                    EnableInMainMenu = true
                },
                new() {
                    Name = "Summary.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Summary.js"
                },
                new() {
                    Name = "UserStats",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.UserStats.html"
                },
                new() {
                    Name = "UserStats.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.UserStats.js"
                },
                new() {
                    Name = "TVSeriesProgress",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.TVSeriesProgress.html",
                },
                new() {
                    Name = "TVSeriesProgress.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.TVSeriesProgress.js"
                },
                new() {
                    Name = "Episodes",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Episodes.html",
                },
                new() {
                    Name = "Episodes.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Episodes.js"
                },
                new() {
                    Name = "Movies",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Movies.html",
                },
                new() {
                    Name = "Movies.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Movies.js"
                },
                new() {
                    Name = "Settings",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Settings.html",
                },
                new() {
                    Name = "Settings.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Settings.js"
                },
                new() {
                    Name = "AdminHelpers.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.AdminHelpers.js"
                },
                new() {
                    Name = "UserStats_UserPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.UserStats.html",
                    EnableInUserMenu = true,
                    MenuSection = "User Statistics",
                    DisplayName = "User Statistics",
                    FeatureId = Feature.StaticId
                },
                new() {
                    Name = "UserStats_UserPage.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.UserStats.js"
                },
                new() {
                    Name = "TVSeriesProgress_UserPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.TVSeriesProgress.html",
                    //EnableInUserMenu = true,
                    MenuSection = "User Statistics",
                    DisplayName = "TV Progress",
                    FeatureId = Feature.StaticId
                },
                new() {
                    Name = "TVSeriesProgress_UserPage.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.TVSeriesProgress.js",
                    FeatureId = Feature.StaticId
                },
                new() {
                    Name = "UserPageHelpers.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.Helpers.js"
                },

            };
        }

        public override Guid Id => new( "23ADB024-F759-438F-B9A7-D5912A75596C" );

        public static Plugin? Instance { get; private set; } = null;
        public static string StaticName = "Statistics 2026";

        private string? _serverId { get; set; } = null;

        private static readonly object _padlock = new();

        public string? ServerId
        {
            get => _serverId;
            set
            {
                if( _serverId != null )
                    return;
                _serverId = value;
            }
        } // set when a embyInterfaces is constructed

        public override string Name => StaticName;

        public override string Description => "Get the statistics for your media collection";

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream( type.Namespace + ".plugin.png" );
        }

        public ImageFormat ThumbImageFormat => ImageFormat.Png;

        public class Debouncer
        {
            public CancellationTokenSource? CancellationToken = null;
            private readonly object _lock = new object();

            public void RunLater( int ms, Func<CancellationToken, Task<bool>> func )
            {
                lock( _lock )
                {
                    // Cancel the previous scheduled execution
                    CancellationToken?.Cancel();
                    CancellationToken?.Dispose();

                    // Create a new token for the current execution
                    CancellationToken = new CancellationTokenSource();
                    var token = CancellationToken.Token;

                    // Start the delay and execution task
                    Task.Run( async () =>
                    {
                        try
                        {
                            while( true )
                            {
                                // Wait for 5 seconds
                                await Task.Delay( ms, token );

                                // Execute the action if not canceled
                                var runAgain = await func( token );
                                if( !runAgain )
                                    break;
                            }
                        }
                        catch( OperationCanceledException )
                        {
                            // Intentionally left blank: The task was canceled because a new call came in
                        }
                    }, token );
                }
            }
        }

        public override void UpdateConfiguration( BasePluginConfiguration configuration )
        {
            var prevNumDays = Configuration.numDaysFutureForMissing;
            var prevReportOnMissing = Configuration.reportOnMissingSpecials;

            base.UpdateConfiguration( configuration );
            var numDaysChanged = ( Configuration.numDaysFutureForMissing != prevNumDays );
            var reportMissingChanged = ( Configuration.reportOnMissingSpecials != prevReportOnMissing );
            var needsRun = numDaysChanged || reportMissingChanged;
            if( configuration != null && needsRun )
            {
                if( _embyInterfaces == null )
                    return;

                Debouncer debouncer = new Debouncer();
                debouncer.RunLater( 5000, async ( cancellationToken ) =>
                {
                    if( IsStatistics2026TaskRunning() )
                        return true;
                    if( numDaysChanged || reportMissingChanged )
                    {
                        var task = launchSubTask( _embyInterfaces, typeof( AnalyzeMissingEpisodesTask ), cancellationToken );
                        await task;
                    }
                    if( numDaysChanged )
                    {
                        var task = launchSubTask( _embyInterfaces, typeof( AnalyzeMissingMoviesTask ), cancellationToken );
                        await task;
                    }
                    return false;
                } );
            }
        }

    }
}
