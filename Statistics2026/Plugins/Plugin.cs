#if DEBUG
#define DEBUG_PLUGIN
#endif
using Emby.Notifications;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Statistics2026.Configuration;
using Statistics2026.Data;
using Statistics2026.Features;
using Statistics2026.ScheduledTasks;
using Statistics2026.Utilities;
using System.Text;

namespace Statistics2026
{
    public partial class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage, IHasEmbyInterface
    {
        public EmbyInterfaces _embyInterfaces { get; }
        public readonly TableDefs _tableDefs;

        public class NotificationTypeFactory : INotificationTypeFactory
        {
            public List<NotificationTypeInfo> GetNotificationTypes( string locale )
            {
                return new List<NotificationTypeInfo>
                {
                    new NotificationTypeInfo
                    {
                        Id = "Statistics20206.EpisodeNowAvailable",
                        Name = "Episode Now Available",
                        CategoryId = "Statistics20206.Statistics2026",
                        CategoryName = "Statistics 2026",
                    }
                };
            }
        }

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
            IJsonSerializer jsonSerializer,
            IProviderManager providerManager,
            IServerConfigurationManager configManager,
            ITaskManager taskManager,
            ISessionManager sessionManager,
            IHttpClient httpClient,
            IAuthenticationRepository authenticationRepository,
            INotificationManager notificationManager
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
                _notificationManager = notificationManager,
                _providerManager = providerManager,
                _serverApplicationPaths = serverApplicationPaths,
                _configManager = configManager,
                _sessionManager = sessionManager,
                _taskManager = taskManager,
                _userDataManager = userDataManager,
                _userManager = userManager,
            };
            _tableDefs = new TableDefs( _embyInterfaces );

            HelperFuncs.PreDebugRun( _embyInterfaces );
#if !DEBUG_PLUGIN
            var debouncer = new Debouncer();

            debouncer.RunLater( 500, async ( cancellationToken ) => {
                await HelperFuncs.CheckForOODAsync( _embyInterfaces, [ "Startup" ], GetType(), true ).ConfigureAwait( false );
                return false;
            } );
#endif

        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            var retVal = new PluginPageInfo[]
            {
                new() {
                    Name = "Stats2026_style.css",
                    EmbeddedResourcePath = GetType().Namespace + ".Resources.style.css"
                },
                new() {
                    Name = "Stats2026_Helpers.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.Helpers.js"
                },
                new() {
                    Name = "Stats2026_AdminHelpers.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.AdminHelpers.js"
                },
                new() {
                    Name = "Stats2026_SystemSummary",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.System.SystemSummary.html",
                    EnableInMainMenu = true
                },
                new() {
                    Name = "Stats2026_SystemSummary.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.System.SystemSummary.js"
                },
                new() {
                    Name = "Stats2026_SystemStatus",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.System.SystemStatus.html",
                },
                new() {
                    Name = "Stats2026_SystemStatus.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.System.SystemStatus.js"
                },
                new() {
                    Name = "Stats2026_SystemSettings",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.System.SystemSettings.html",
                },
                new() {
                    Name = "Stats2026_SystemSettings.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.System.SystemSettings.js"
                },
                new() {
                    Name = "Stats2026_MovieSummary",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Movies.MovieSummary.html",
                },
                new() {
                    Name = "Stats2026_MovieSummary.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Movies.MovieSummary.js"
                },
                new() {
                    Name = "Stats2026_CollectionsTable",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Movies.CollectionsTable.html",
                },
                new() {
                    Name = "Stats2026_CollectionsTable.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Movies.CollectionsTable.js"
                },
                new() {
                    Name = "Stats2026_MovieTable",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Movies.MovieTable.html",
                },
                new() {
                    Name = "Stats2026_MovieTable.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Movies.MovieTable.js"
                },
                new() {
                    Name = "Stats2026_MovieSettings",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Movies.MovieSettings.html",
                },
                new() {
                    Name = "Stats2026_MovieSettings.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Movies.MovieSettings.js"
                },
                new() {
                    Name = "Stats2026_SeriesSummary",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.TV.SeriesSummary.html",
                },
                new() {
                    Name = "Stats2026_SeriesSummary.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.TV.SeriesSummary.js"
                },
                new() {
                    Name = "Stats2026_EpisodeSummary",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.TV.EpisodeSummary.html",
                },
                new() {
                    Name = "Stats2026_EpisodeSummary.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.TV.EpisodeSummary.js"
                },
                new() {
                    Name = "Stats2026_SeriesTable",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.TV.SeriesTable.html",
                },
                new() {
                    Name = "Stats2026_SeriesTable.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.TV.SeriesTable.js"
                },
                new() {
                    Name = "Stats2026_EpisodeTable",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.TV.EpisodeTable.html",
                },
                new() {
                    Name = "Stats2026_EpisodeTable.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.TV.EpisodeTable.js"
                },
                new() {
                    Name = "Stats2026_TVSettings",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.TV.TVSettings.html",
                },
                new() {
                    Name = "Stats2026_TVSettings.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.TV.TVSettings.js"
                },
                new() {
                    Name = "Stats2026_UserStats",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.User.UserStats.html"
                },
                new() {
                    Name = "Stats2026_UserStats.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.User.UserStats.js"
                },
                new() {
                    Name = "Stats2026_TVSeriesProgress",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.User.TVSeriesProgress.html",
                },
                new() {
                    Name = "Stats2026_TVSeriesProgress.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.User.TVSeriesProgress.js"
                },
                new() {
                    Name = "Stats2026_UserReportSettings",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.User.UserReportSettings.html",
                },
                new() {
                    Name = "Stats2026_UserReportSettings.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.User.UserReportSettings.js"
                },
                new() {
                    Name = "Stats2026_UserPageHelpers.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.UserHelpers.js"
                },
                new() {
                    Name = "Stats2026_UserStats_UserPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.User.UserStats.html",
                    EnableInUserMenu = true,
                    MenuSection = "User Statistics",
                    DisplayName = "User Statistics",
                    FeatureId = Feature.StaticId
                },
                new() {
                    Name = "Stats2026_UserStats_UserPage.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.User.UserStats.js"
                },
                new() {
                    Name = "Stats2026_TVSeriesProgress_UserPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.User.TVSeriesProgress.html",
                    MenuSection = "User Statistics",
                    DisplayName = "TV Progress",
                    FeatureId = Feature.StaticId
                },
                new() {
                    Name = "Stats2026_TVSeriesProgress_UserPage.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.User.TVSeriesProgress.js",
                    FeatureId = Feature.StaticId
                },
            };

            if( !_beenValidated )
            {
                EmbyHelperFuncs.validatePluginResourcePages( retVal, this, _embyInterfaces );
                _beenValidated = true;
            }

            return retVal;
        }

        public override Guid Id => new( "23ADB024-F759-438F-B9A7-D5912A75596C" );

        private static bool _beenValidated{ get; set; } = false;
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
            Type type = GetType();
            return type.Assembly.GetManifestResourceStream( type.Namespace + ".Resources.thumb.png" );
        }

        public ImageFormat ThumbImageFormat => ImageFormat.Png;

        public override void UpdateConfiguration( BasePluginConfiguration configuration )
        {
            MovieSettings prevMovieSettings = Configuration.movieSettings;
            TVSettings prevTVSettings = Configuration.tvSettings;

            base.UpdateConfiguration( configuration );

            bool movieSettingsChanged = ( !Configuration.movieSettings.AnalysisEquals( prevMovieSettings ) );
            bool tvSettingsChanged = ( !Configuration.tvSettings.AnalysisEquals( prevTVSettings ) );

            bool needsRun = tvSettingsChanged || movieSettingsChanged;
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
                    if( tvSettingsChanged )
                    {
                        taskTypes.Add( typeof( AnalyzeMissingEpisodesTask ) );
                    }

                    if( movieSettingsChanged )
                    {
                        taskTypes.Add( typeof( AnalyzeMissingMoviesTask ) );
                    }

                    await TaskDef.ExecuteSubTasksAsync( _embyInterfaces, taskTypes, [ "ConfigUpdated" ], cancellationToken, false ).ConfigureAwait( false );
                    return false;
                } );
            }
        }

    }
}
