namespace CodecInfo
{
    internal static class Constants
    {
        internal const string Name = "Codec Analysis";
        internal const string MediaResolutions = "Media Resolutions";
        internal const string MediaCodecs = "Media Codecs";
        internal const string DolbyVisionProfiles = "Dolby Vision Profiles";

        internal const string HelpMediaResolutions = "Entries with Resolution Not Available can be located in the log file after debug logging has been enabled by searching CalculateMediaResolutions.";
        internal const string HelpMediaCodecs = "Entries with Unknown can be located in the log file after debug logging has been enabled by searching CalculateMediaCodecs";
        internal const string HelpDolbyVisionProfile = "Videos with hevc or av1 codecs will also have their Dolby Vision Profile tracked.";

        internal const string MissingVideoStream = "Missing Video Stream";
        internal const string UnknownDolbyProfile = "Unknown Dolby Profile";
        internal const string NonDolbyVisionCompatibleCodec = "Non Dolby Vision Compatible Codec";
        internal const string NoDolbyProfile = "No Dolby Profile";

        internal const string NoResolution = "Resolution Not Available";
        internal const string HD = "1080p";
        internal const string _4k = "4K";
        internal const string _8k = "8K";
        internal const string _720p = "720p";
        internal const string SD = "SD";
    }
}