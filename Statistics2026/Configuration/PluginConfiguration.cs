using MediaBrowser.Model.Plugins;
using System;

namespace Statistics2026.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
        }

        public string BuildDate { get; set; } = String.Empty;
        public string LastUpdated { get; set; } = String.Empty;
        public string Version { get; set; } = String.Empty;
        public string ServerId { get; set; } = String.Empty;

        public bool hasConnectUserID { get; set; } = false;
        public int numMostActiveUsers { get; set; } = 5;
        public bool excludeAdmin { get; set; } = true;

        public int numWatchedShows { get; set; } = 5;

        public bool showUnknownDVProfiles { get; set; } = false;
        public bool showAllResolutions { get; set; } = false;
    }
}

