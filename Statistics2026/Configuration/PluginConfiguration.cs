using MediaBrowser.Model.Plugins;
using System.Collections.Generic;

namespace Statistics2026.Configuration
{
    public class SortEntry
    {
        public string TableId { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public string AscDesc { get; set; } = string.Empty;
        public string ExtraFilter { get; set; } = string.Empty;
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {

        }

        public string BuildDate { get; set; } = string.Empty;
        public string LastUpdated { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        public bool hasConnectUserID { get; set; } = false;
        public int numMostActiveUsers { get; set; } = 5;
        public bool excludeAdmin { get; set; } = true;

        public int numWatchedToReport { get; set; } = 5;
        public int numTiedToReport { get; set; } = 5;

        public bool showUnknownDVProfiles { get; set; } = false;
        public bool showAllResolutions { get; set; } = false;
        public bool reportOnMissingSpecials { get; set; } = false;
        public string searchLocation { get; set; } = string.Empty;
        public int numDaysFutureForMissing { get; set; } = 0;

        public bool resetPlayCount { get; set; } = false;
        public bool resetDBState { get; set; } = false;
        public bool showDebugInfo { get; set; } = false;

        public bool dbStateOK { get; set; } = false;

        public List<SortEntry> SortEntries { get; set; } = [];
    }
}

