using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Statistics2026.Configuration;

namespace Statistics2026
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new PluginPageInfo[]
            {
                new PluginPageInfo
                {
                    Name = "Summary",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.Summary.html",
                    EnableInMainMenu = true
                },
                new PluginPageInfo
                {
                    Name = "Summary.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.Summary.js"
                },
                new PluginPageInfo
                {
                    Name = "UserStats",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserStats.html"
                },
                new PluginPageInfo
                {
                    Name = "UserStats.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserStats.js"
                },
                new PluginPageInfo
                {
                    Name = "TVSeriesProgress",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.TVSeriesProgress.html",
                },
                new PluginPageInfo
                {
                    Name = "TVSeriesProgress.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.TVSeriesProgress.js"
                },
                new PluginPageInfo
                {
                    Name = "Episodes",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.Episodes.html",
                },
                new PluginPageInfo
                {
                    Name = "Episodes.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.Episodes.js"
                },
                new PluginPageInfo
                {
                    Name = "Movies",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.Movies.html",
                },
                new PluginPageInfo
                {
                    Name = "Movies.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.Movies.js"
                },
                new PluginPageInfo
                {
                    Name = "Settings",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.Settings.html",
                },
                new PluginPageInfo
                {
                    Name = "Settings.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.Settings.js"
                },
                new PluginPageInfo
                {
                    Name = "Helpers.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.Helpers.js"
                },
                new PluginPageInfo
                {
                    Name = "style.css",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.style.css"
                },
                new PluginPageInfo
                {
                    Name = "UserStats_UserPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserStats_UserPage.html",
                    EnableInUserMenu = true,
                    MenuSection = "User Statistics",
                    DisplayName = "User Statistics",
                    FeatureId = Feature.StaticId
                },
                new PluginPageInfo
                {
                    Name = "UserStats_UserPage.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.UserStats_UserPage.js"
                },
                new PluginPageInfo
                {
                    Name = "TVSeriesProgress_UserPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.TVSeriesProgress_UserPage.html",
                    //EnableInUserMenu = true,
                    MenuSection = "User Statistics",
                    DisplayName = "TV Progress",
                    FeatureId = Feature.StaticId
                },
                new PluginPageInfo
                {
                    Name = "TVSeriesProgress_UserPage.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.TVSeriesProgress_UserPage.js",
                    FeatureId = Feature.StaticId
                },
                new PluginPageInfo
                {
                    Name = "Helpers_UserPage.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.Helpers_UserPage.js"
                },

            };
        }

        public override Guid Id => new Guid("23ADB024-F759-438F-B9A7-D5912A75596C");

        public static Plugin? Instance { get; private set; } = null;

        public static string StaticName = "Statistics 2026";

        public override string Name
        {
            get { return StaticName; }
        }

        public override string Description => "Get the statistics for your media collection";

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".Images.plugin-thumb.png");
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
