using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using CodecInfo.Configuration;

namespace CodecInfo
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
                    Name = "CodecInfoConfigPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.CodecInfoConfigPage.html",
                    EnableInMainMenu = true
                },
                new PluginPageInfo
                {
                    Name = "CodecInfoConfigPageJS",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.CodecInfoConfigPage.js"
                },
                new PluginPageInfo
                {
                    Name = "AllCodecEpisodeInformationPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.SubPages.AllCodecEpisodeInformation.html",
                },
                new PluginPageInfo
                {
                    Name = "AllCodecEpisodeInformationPageJS",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.SubPages.AllCodecEpisodeInformation.js"
                },
                new PluginPageInfo
                {
                    Name = "AllCodecMovieInformationPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.SubPages.AllCodecMovieInformation.html",
                },
                new PluginPageInfo
                {
                    Name = "AllCodecMovieInformationPageJS",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.SubPages.AllCodecMovieInformation.js"
                },
                new PluginPageInfo
                {
                    Name = "CodecInfoEpisodeCodecPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.SubPages.episodeCodecPage.html"
                },
                new PluginPageInfo
                {
                    Name = "CodecInfoEpisodeCodecPageJS",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.SubPages.episodeCodecPage.js"
                },
                new PluginPageInfo
                {
                    Name = "CodecInfoEpisodeCodecTextPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.SubPages.episodeCodecTextPage.html"
                },
                new PluginPageInfo
                {
                    Name = "CodecInfoEpisodeCodecTextPageJS",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.SubPages.episodeCodecTextPage.js"
                },
               new PluginPageInfo
                {
                    Name = "CodecInfoEpisodeDVProfileTextPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.SubPages.episodeDVProfileTextPage.html"
                },
                new PluginPageInfo
                {
                    Name = "CodecInfoEpisodeDVProfileTextPageJS",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.SubPages.episodeDVProfileTextPage.js"
                },
                new PluginPageInfo
                {
                    Name = "CodecInfoMovieCodecPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.SubPages.movieCodecPage.html"
                },
                new PluginPageInfo
                {
                    Name = "CodecInfoMovieCodecPageJS",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.SubPages.movieCodecPage.js"
                },
                new PluginPageInfo
                {
                    Name = "CodecInfoMovieCodecTextPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.SubPages.movieCodecTextPage.html"
                },
                new PluginPageInfo
                {
                    Name = "CodecInfoMovieCodecTextPageJS",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.SubPages.movieCodecTextPage.js"
                },
                new PluginPageInfo
                {
                    Name = "CodecInfoMovieDVProfileTextPage",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.SubPages.movieDVProfileTextPage.html"
                },
                new PluginPageInfo
                {
                    Name = "CodecInfoMovieDVProfileTextPageJS",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.SubPages.movieDVProfileTextPage.js"
                }
            };
        }

        public override Guid Id => new Guid("4BFE2894-AEA3-4D3C-A429-503B56D61711");

        public static Plugin Instance { get; private set; }

        public override string Name => "Codec Information";

        public override string Description => "Get Codec Information from your collection";

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
