using System.Collections.Generic;
using MediaBrowser.Model.Plugins;
using CodecInfo.API;

namespace CodecInfo.Configuration
{
    public class CPluginConfiguration : BasePluginConfiguration
    {
        public CPluginConfiguration()
        {
            MediaInfoList = new List<CMediaInfo>();
        }

        public string BuildDate { get; set; }
        public string LastUpdated { get; set; }
        public string Version { get; set; }
        public string ServerId { get; set; }

        public bool showUnknownDVProfileCount { get; set; } = true;
 
        public List<CMediaInfo> MediaInfoList { get; set; }

        // user for the summary tables
        public CValueGroup MediaResolutions { get; set; }
        public CValueGroup MediaCodecs { get; set; }
        public CValueGroup DolbyVisionProfiles { get; set; }
        public CValueGroup DolbyVisionProfilesWithUnknown { get; set; }

        // user for the icon/text list pages
        public CMediaItemCollection MovieCodecItems { get; set; }
        public CMediaItemCollection EpisodeCodecItems { get; set; }

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

