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
using System.Runtime.InteropServices.ComTypes;

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
            string prefix = GetType().Namespace;

            HashSet<string> knownPagePrefixes = new HashSet<string>()
            {
                $"{prefix}.Resources",
                $"{prefix}.Pages",
                $"tabulator"
            };

            HashSet<string> mainMenuPages = new HashSet<string>() { $"{prefix}_Pages_AdminPages_System_SystemSummary" };

            Dictionary<string, (string menuSection, string displayName)> userPages = new Dictionary<string, (string menuSection, string displayName)>()
            {
                { $"{prefix}_Pages_UserPages_User_UserSummary", ("User Statistics", "Summary") },
                { $"{prefix}_Pages_UserPages_User_MovieSummary", ("User Statistics", "Movies") },
                { $"{prefix}_Pages_UserPages_User_TVSummary", ("User Statistics", "TV") },
                { $"{prefix}_Pages_UserPages_User_TVSeriesProgress", ("User Statistics", "TV Progress") }
            };

            HashSet<string> userMenuPages = new HashSet<string>()
            {
                $"{prefix}_Pages_UserPages_User_UserSummary"
            };

            List<PluginPageInfo> generatedPages = EmbyHelperFuncs.GeneratePages( this, knownPagePrefixes, mainMenuPages, userPages, userMenuPages );

            if( !_beenValidated )
            {
                EmbyHelperFuncs.validatePluginResourcePages( generatedPages, this, GetType().Namespace, _embyInterfaces );
                _beenValidated = true;
            }

            return generatedPages;
        }

        public override Guid Id => new( "23ADB024-F759-438F-B9A7-D5912A75596C" );

        private static bool _beenValidated { get; set; } = false;
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
