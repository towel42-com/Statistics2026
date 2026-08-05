using System.Collections.Generic;
using MediaBrowser.Model.Plugins;
using CodecInfoPlugin.Models;
using CodecInfoPlugin.Models.Configuration;

namespace CodecInfoPlugin.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            MediaInfoList = new List<MediaInfo>();
            showUnknownDVProfileCount = true;
        }

        public string BuildDate { get; set; }
        public string LastUpdated { get; set; }
        public string Version { get; set; }
        public string ServerId { get; set; }

        public bool showUnknownDVProfileCount { get; set; }
 
        public List<MediaInfo> MediaInfoList { get; set; }

        // user for the summary tables
        public ValueGroup MediaResolutions { get; set; }
        public ValueGroup MediaCodecs { get; set; }
        public ValueGroup DolbyVisionProfiles { get; set; }

        // user for the icon/text list pages
        public MediaItemCollection MovieCodecItems { get; set; }
        public MediaItemCollection EpisodeCodecItems { get; set; }
        public MediaItemCollection MovieDVProfileItems { get; set; }
        public MediaItemCollection EpisodeDVProfileItems { get; set; }

        public static bool IsUnknownDolbyProfile(string profile)
        {
            if (string.IsNullOrEmpty(profile))
                return true;
            var unknownProfiles = new List<string>
            {
                "Unknown Media",
                "Unknown Dolby Profile",
                "Non Dolby Vision Compatible Codec",
                "No Dolby Profile"
            };
            return unknownProfiles.Contains(profile);
        }
    }
}

