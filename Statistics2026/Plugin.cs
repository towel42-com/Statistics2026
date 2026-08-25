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
                }

            };
        }

        public override Guid Id => new Guid("4BFE2894-AEA3-4D3C-A429-503B56D61711");

        public static Plugin? Instance { get; private set; }

        public override string Name => "Statistics 2026";

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
