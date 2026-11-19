using Emby.Notifications;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
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

            var debouncer = new Debouncer();
            debouncer.RunLaterAsync( 500, async ( cancellationToken ) => {
                HelperFuncs.CheckForOOD( _embyInterfaces, true, ["Startup"], GetType(), true );
                return false;
            } );

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
                    Name = "Stats2026_Summary",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Summary.html",
                    EnableInMainMenu = true
                },
                new() {
                    Name = "Stats2026_Summary.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Summary.js"
                },
                new() {
                    Name = "Stats2026_EpisodeSummary",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.EpisodeSummary.html",
                },
                new() {
                    Name = "Stats2026_EpisodeSummary.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.EpisodeSummary.js"
                },
                new() {
                    Name = "Stats2026_MovieSummary",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.MovieSummary.html",
                },
                new() {
                    Name = "Stats2026_MovieSummary.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.MovieSummary.js"
                },
                new() {
                    Name = "Stats2026_UserStats",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.UserStats.html"
                },
                new() {
                    Name = "Stats2026_UserStats.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.UserStats.js"
                },
                new() {
                    Name = "Stats2026_TVSeriesProgress",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.TVSeriesProgress.html",
                },
                new() {
                    Name = "Stats2026_TVSeriesProgress.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.TVSeriesProgress.js"
                },
                new() {
                    Name = "Stats2026_Episodes",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Episodes.html",
                },
                new() {
                    Name = "Stats2026_Episodes.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Episodes.js"
                },
                new() {
                    Name = "Stats2026_Movies",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Movies.html",
                },
                new() {
                    Name = "Stats2026_Movies.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Movies.js"
                },
                new() {
                    Name = "Stats2026_Settings",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Settings.html",
                },
                new() {
                    Name = "Stats2026_Settings.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Settings.js"
                },
                new() {
                    Name = "Stats2026_SystemStatus",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.SystemStatus.html",
                },
                new() {
                    Name = "Stats2026_SystemStatus.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.SystemStatus.js"
                },
                new() {
                    Name = "Stats2026_AdminHelpers.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.AdminHelpers.js"
                },
                new() {
                    Name = "Stats2026_UserStats_UserPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.UserStats.html",
                    EnableInUserMenu = true,
                    MenuSection = "User Statistics",
                    DisplayName = "User Statistics",
                    FeatureId = Feature.StaticId
                },
                new() {
                    Name = "Stats2026_UserStats_UserPage.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.UserStats.js"
                },
                new() {
                    Name = "Stats2026_TVSeriesProgress_UserPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.TVSeriesProgress.html",
                    //EnableInUserMenu = true,
                    MenuSection = "User Statistics",
                    DisplayName = "TV Progress",
                    FeatureId = Feature.StaticId
                },
                new() {
                    Name = "Stats2026_TVSeriesProgress_UserPage.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.TVSeriesProgress.js",
                    FeatureId = Feature.StaticId
                },
                new() {
                    Name = "Stats2026_UserPageHelpers.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.UserHelpers.js"
                },

            };

            foreach( var page in retVal )
            {
                var path = page.EmbeddedResourcePath;
                if( GetType().Assembly.GetManifestResourceInfo( path ) == null )
                {
                    throw new Exception( "Could not find embedded resource: " + path );
                }
            }
            return retVal;
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
            var prevNumFutureDays = Configuration.numDaysFutureForMissing;
            var prevReportOnMissing = Configuration.reportOnMissingSpecials;
            var prevNumDaysStreaming = Configuration.numDaysForMovieStreaming;

            base.UpdateConfiguration( configuration );
            var numFutureDaysChanged = ( Configuration.numDaysFutureForMissing != prevNumFutureDays );
            var reportMissingChanged = ( Configuration.reportOnMissingSpecials != prevReportOnMissing );
            var numDaysStreamingChanged = ( Configuration.numDaysForMovieStreaming != prevNumDaysStreaming );

            var needsRun = numFutureDaysChanged || reportMissingChanged || numDaysStreamingChanged;
            if( configuration != null && needsRun )
            {
                if( _embyInterfaces == null )
                    return;

                Debouncer debouncer = new Debouncer();
                debouncer.RunLaterAsync( 5000, async ( cancellationToken ) =>
                {
                    if( TaskDef.IsStatistics2026TaskRunning( _embyInterfaces, false ) )
                        return true;

                    List<Type> taskTypes = [];
                    if( numFutureDaysChanged || reportMissingChanged )
                    {
                        taskTypes.Add( typeof( AnalyzeMissingEpisodesTask ) );
                    }

                    if( numDaysStreamingChanged || reportMissingChanged )
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
