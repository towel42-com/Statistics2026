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

namespace Statistics2026
{
    public partial class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        public readonly EmbyInterfaces _embyInterfaces;

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
            Instance.ServerId = appHost.SystemId;

            _embyInterfaces = new EmbyInterfaces()
            {
                _logger = logManager.GetLogger( "Statistics2026 - Plugin" ),
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
                _apiService = apiService
            };
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new PluginPageInfo[]
            {
                new() {
                    Name = "style.css",
                    EmbeddedResourcePath = GetType().Namespace + ".Resources.style.css"
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
            return type.Assembly.GetManifestResourceStream( type.Namespace + ".Resources.thumb.png" );
        }

        public ImageFormat ThumbImageFormat => ImageFormat.Png;

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
                    if( TaskDef.IsStatistics2026TaskRunning( _embyInterfaces, false ) )
                        return true;

                    List<Type> taskTypes = [];
                    if( numDaysChanged || reportMissingChanged )
                    {
                        taskTypes.Add( typeof( AnalyzeMissingEpisodesTask ) );
                    }
                    if( numDaysChanged )
                    {
                        taskTypes.Add( typeof( AnalyzeMissingMoviesTask ) );
                    }
                    await TaskDef.ExecuteSubTasks( _embyInterfaces, taskTypes, cancellationToken, false );
                    return false;
                } );
            }
        }

    }
}
