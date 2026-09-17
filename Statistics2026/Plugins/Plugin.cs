using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using ServiceStack;
using Statistics2026.Configuration;
using Statistics2026.ScheduledTasks;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace Statistics2026
{
    public partial class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        public readonly ITaskManager _taskManager;
        public readonly ILogger _logger;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ITaskManager taskManager, ILogManager logManager)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            _taskManager = taskManager;
            _logger = logManager.GetLogger("Statistics2026 - Plugin");
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new PluginPageInfo[]
            {
                new PluginPageInfo
                {
                    Name = "style.css",
                    EmbeddedResourcePath = GetType().Namespace + ".style.css"
                },
                new PluginPageInfo
                {
                    Name = "Helpers.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.Helpers.js"
                },
                new PluginPageInfo
                {
                    Name = "Summary",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Summary.html",
                    EnableInMainMenu = true
                },
                new PluginPageInfo
                {
                    Name = "Summary.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Summary.js"
                },
                new PluginPageInfo
                {
                    Name = "UserStats",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.UserStats.html"
                },
                new PluginPageInfo
                {
                    Name = "UserStats.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.UserStats.js"
                },
                new PluginPageInfo
                {
                    Name = "TVSeriesProgress",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.TVSeriesProgress.html",
                },
                new PluginPageInfo
                {
                    Name = "TVSeriesProgress.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.TVSeriesProgress.js"
                },
                new PluginPageInfo
                {
                    Name = "Episodes",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Episodes.html",
                },
                new PluginPageInfo
                {
                    Name = "Episodes.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Episodes.js"
                },
                new PluginPageInfo
                {
                    Name = "Movies",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Movies.html",
                },
                new PluginPageInfo
                {
                    Name = "Movies.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Movies.js"
                },
                new PluginPageInfo
                {
                    Name = "Settings",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Settings.html",
                },
                new PluginPageInfo
                {
                    Name = "Settings.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.Settings.js"
                },
                new PluginPageInfo
                {
                    Name = "AdminHelpers.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.AdminPages.AdminHelpers.js"
                },
                new PluginPageInfo
                {
                    Name = "UserStats_UserPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.UserStats.html",
                    EnableInUserMenu = true,
                    MenuSection = "User Statistics",
                    DisplayName = "User Statistics",
                    FeatureId = Feature.StaticId
                },
                new PluginPageInfo
                {
                    Name = "UserStats_UserPage.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.UserStats.js"
                },
                new PluginPageInfo
                {
                    Name = "TVSeriesProgress_UserPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.TVSeriesProgress.html",
                    //EnableInUserMenu = true,
                    MenuSection = "User Statistics",
                    DisplayName = "TV Progress",
                    FeatureId = Feature.StaticId
                },
                new PluginPageInfo
                {
                    Name = "TVSeriesProgress_UserPage.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.TVSeriesProgress.js",
                    FeatureId = Feature.StaticId
                },
                new PluginPageInfo
                {
                    Name = "UserPageHelpers.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserPages.Helpers.js"
                },

            };
        }

        public override Guid Id => new Guid("23ADB024-F759-438F-B9A7-D5912A75596C");

        public static Plugin? Instance { get; private set; } = null;
        public static string StaticName = "Statistics 2026";

        private string? _serverId { get; set; } = null;

        private static readonly object _padlock = new object();

        public string? ServerId
        {
            get
            {
                return _serverId;
            }
            set
            {
                if (_serverId != null)
                    return;
                _serverId = value;
            }
        } // set when a embyInterfaces is constructed

        public override string Name
        {
            get { return StaticName; }
        }

        public override string Description => "Get the statistics for your media collection";

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".plugin.png");
        }

        public ImageFormat ThumbImageFormat
        {
            get
            {
                return ImageFormat.Png;
            }
        }
    }
}
