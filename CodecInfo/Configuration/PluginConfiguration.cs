using System.Collections.Generic;
using MediaBrowser.Model.Plugins;
using CodecInfo.API;

namespace CodecInfo.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            MediaInfoList = new List<MediaInfo>();
        }

        public string BuildDate { get; set; }
        public string LastUpdated { get; set; }
        public string Version { get; set; }
        public string ServerId { get; set; }

        public bool showUnknownDVProfileCount { get; set; } = true;
 
        public List<MediaInfo> MediaInfoList { get; set; }

        // user for the summary tables
        public ValueGroup MediaResolutions { get; set; }
        public ValueGroup MediaCodecs { get; set; }
        public ValueGroup DolbyVisionProfiles { get; set; }
        public ValueGroup DolbyVisionProfilesWithUnknown { get; set; }

        // user for the icon/text list pages
        public MediaItemCollection MovieCodecItems { get; set; }
        public MediaItemCollection EpisodeCodecItems { get; set; }

        public static bool IsUnknownDolbyProfile(string profile)
        {
            if (string.IsNullOrEmpty(profile))
                return true;
            var unknownProfiles = new List<string>
            {
                Constants.MissingVideoStream,
                Constants.UnknownDolbyProfile,
                Constants.NonDolbyVisionCompatibleCodec,
                Constants.NoDolbyProfile
            };
            return unknownProfiles.Contains(profile);
        }
    }
}

