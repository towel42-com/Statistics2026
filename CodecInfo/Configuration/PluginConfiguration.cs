using System.Collections.Generic;
using MediaBrowser.Model.Plugins;
using CodecInfo.Data;

namespace CodecInfo.Configuration
{
    public class CPluginConfiguration : BasePluginConfiguration
    {
        public CPluginConfiguration()
        {
        }

        public string BuildDate { get; set; }
        public string LastUpdated { get; set; }
        public string Version { get; set; }
        public string ServerId { get; set; }

        public bool showAllCodecs { get; set; } = false;
        public bool showUnknownDVProfiles { get; set; } = false;
        public bool showAllDVProfiles { get; set; } = false;
        public bool showAllResolutions { get; set; } = false;
    }
}

