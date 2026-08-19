using System.Collections.Generic;
using MediaBrowser.Model.Plugins;
using Statistics2026.Data;

namespace Statistics2026.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
        }

        public string BuildDate { get; set; }
        public string LastUpdated { get; set; }
        public string Version { get; set; }
        public string ServerId { get; set; }

        public bool hasConnectUserID { get; set; } = false;
        public int numMostActiveUsers { get; set; } = 5;
        public bool excludeAdmin { get; set; } = true;

        public bool showAllCodecs { get; set; } = false;
        public bool showUnknownDVProfiles { get; set; } = false;
        public bool showAllDVProfiles { get; set; } = false;
        public bool showAllResolutions { get; set; } = false;
    }
}

