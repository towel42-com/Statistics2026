using System;
using System.Collections.Generic;

namespace Statistics20
{
    internal static class Constants
    {
        internal const string Name = "Codec And User Analysis";
        internal const string MediaResolutions = "Media Resolutions";
        internal const string MediaCodecs = "Media Codecs";
        internal const string DolbyVisionProfiles = "Dolby Vision Profiles";
        internal const string HelpMediaResolutions = "Entries with Resolution Not Available can be located in the log file after debug logging has been enabled by searching CalculateMediaResolutions.";
        internal const string HelpMediaCodecs = "Entries with Unknown can be located in the log file after debug logging has been enabled by searching CalculateMediaCodecs";
        internal const string HelpDolbyVisionProfile = "Videos with hevc or av1 codecs will also have their Dolby Vision Profile tracked.";

        // user summary constants
        internal const string TotalUsers = "Total Users";
        internal const string MostActiveUsers = "Most Active Users";
        internal const string HelpMostActiveUsers = "Top 5 users that are the most active on the Emby server. This includes viewing movies and episodes.";


        internal const string MissingVideoStream = "Missing Video Stream";
        internal const string UnknownDolbyProfile = "Unknown Dolby Profile";
        internal const string NonDolbyVisionCompatibleCodec = "Non Dolby Vision Compatible Codec";
        internal const string NoDolbyProfile = "No Dolby Profile";
        public static readonly string[] UnknownDolbyProfiles =
        {
            MissingVideoStream,
            UnknownDolbyProfile,
            NonDolbyVisionCompatibleCodec,
            NoDolbyProfile
        };

        public static bool IsUnknownDolbyProfile(string profile)
        {
            if (string.IsNullOrEmpty(profile))
                return true;
            return Array.Exists(UnknownDolbyProfiles, f => f.Equals(profile, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsDolbyVision50(string profile)
        {
            return profile == "Profile 5.0";
        }

        internal const string NoResolution = "Resolution Not Available";
        internal const string HD = "1080p";
        internal const string _4k = "4K";
        internal const string _8k = "8K";
        internal const string _720p = "720p";
        internal const string SD = "SD";

        internal const string HEVC = "HEVC";
        internal const string AV1 = "AV1";
    }
}