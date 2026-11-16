using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Plugins;
using ServiceStack;
using ServiceStack.Text;
using Statistics2026.Utilities;
using System.Collections.Generic;
using System.Reflection;

namespace Statistics2026.Configuration
{
    public class SortEntry
    {
        public string TableId { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public string AscDesc { get; set; } = string.Empty;
        public string ExtraFilter { get; set; } = string.Empty;
    }

    public interface IDataForConfig
    {
        string BuildDate { get; set; }
        string Version { get; set; }

        bool hasConnectUserID { get; set; }
        int numMostActiveUsers { get; set; }
        bool excludeAdmin { get; set; }

        int numWatchedToReport { get; set; }
        int numTiedToReport { get; set; }

        bool showUnknownDVProfiles { get; set; }
        bool showAllResolutions { get; set; }

        bool alwaysShowJustWatch { get; set; }
        int maxNumStreamingProviders { get; set; }

        bool reportOnMissingSpecials { get; set; }
        bool showFilteredMissingSummaries { get; set; }
        List<string> searchLocations { get; set; }
        bool showSearchTheMovieDB { get; set; }
        int numDaysFutureForMissing { get; set; }

        // a list of regular expressions, if the Collection name matches, the movie is NOT listed as missing
        // note, the db is complete just what is shown is changed
        List<string> CollectionFiltersForMissingMovies { get; set; }

        bool resetPlayCount { get; set; }
        bool resetDBState { get; set; }
        bool showDebugInfo { get; set; }
        bool showLastRunInfo { get; set; }

        bool dbStateOK { get; set; }
    }

    public static class DataForConfigExtensions
    {
        public static bool HasCustomSearches( this IDataForConfig target )
        {
            var retVal = false;
            for( var ii = 0; !retVal && ( ii < target.searchLocations.Count ); ++ii )
            {
                retVal = !string.IsNullOrWhiteSpace( target.searchLocations[ ii ] );
            }
            return retVal;
        }

        public static bool CanSearch( this IDataForConfig target )
        {
            var retVal = target.showSearchTheMovieDB;
            for( var ii = 0; !retVal && ( ii < target.searchLocations.Count ); ++ii )
            {
                retVal = !string.IsNullOrWhiteSpace( target.searchLocations[ ii ] );
            }
            return retVal;
        }

        public static void CopyData( this IDataForConfig target, IDataForConfig dataSource )
        {
            if( target == null || dataSource == null )
            {
                return;
            }

            target.BuildDate = dataSource.BuildDate;
            target.Version = dataSource.Version;

            target.hasConnectUserID = dataSource.hasConnectUserID;
            target.numMostActiveUsers = dataSource.numMostActiveUsers;
            target.excludeAdmin = dataSource.excludeAdmin;

            target.numWatchedToReport = dataSource.numWatchedToReport;
            target.numTiedToReport = dataSource.numTiedToReport;

            target.showUnknownDVProfiles = dataSource.showUnknownDVProfiles;
            target.showAllResolutions = dataSource.showAllResolutions;

            target.alwaysShowJustWatch = dataSource.alwaysShowJustWatch;
            target.maxNumStreamingProviders = dataSource.maxNumStreamingProviders;

            target.reportOnMissingSpecials = dataSource.reportOnMissingSpecials;
            target.showFilteredMissingSummaries = dataSource.showFilteredMissingSummaries;

            target.searchLocations = [];
            if( dataSource.searchLocations != null )
                target.searchLocations.AddRange( dataSource.searchLocations );

            target.showSearchTheMovieDB = dataSource.showSearchTheMovieDB;
            target.numDaysFutureForMissing = dataSource.numDaysFutureForMissing;

            target.CollectionFiltersForMissingMovies = new List<string>();
            if( dataSource.CollectionFiltersForMissingMovies != null )
                target.CollectionFiltersForMissingMovies.AddRange( dataSource.CollectionFiltersForMissingMovies );

            target.resetPlayCount = dataSource.resetPlayCount;
            target.resetDBState = dataSource.resetDBState;
            target.showDebugInfo = dataSource.showDebugInfo;
            target.showLastRunInfo = dataSource.showLastRunInfo;

            target.dbStateOK = dataSource.dbStateOK;
        }
    }

    public class PluginConfiguration : BasePluginConfiguration, IDataForConfig
    {
        public PluginConfiguration() { }

        public void UpdateBuildDateAndVersionInfo()
        {
            var gitHash = Assembly.GetExecutingAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split( '+' ).LastOrDefault() ?? String.Empty;
            gitHash = gitHash.Length >= 8 ? gitHash[ ..8 ] : gitHash;
            if( !gitHash.IsEmpty() )
                gitHash = "+" + gitHash;

            Version = Plugin.Instance?.Version.ToString( 4 ) + gitHash ?? "<UNKNOWN>";
            BuildDate = HelperFuncs.ToDateTimeParamValue( BuildDateInfo.GetBuildDate() )!;
            Plugin.Instance?.UpdateConfiguration( this );
        }

        public string BuildDate { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        public bool hasConnectUserID { get; set; } = false;
        public int numMostActiveUsers { get; set; } = 5;
        public bool excludeAdmin { get; set; } = true;

        public int numWatchedToReport { get; set; } = 5;
        public int numTiedToReport { get; set; } = 5;

        public bool showUnknownDVProfiles { get; set; } = false;
        public bool showAllResolutions { get; set; } = false;

        public bool alwaysShowJustWatch { get; set; } = true;
        public int maxNumStreamingProviders { get; set; } = 3;

        public bool reportOnMissingSpecials { get; set; } = false;
        public bool showFilteredMissingSummaries { get; set; } = true;
        public List<string> searchLocations { get; set; } = [];
        public bool showSearchTheMovieDB { get; set; } = true;
        public int numDaysFutureForMissing { get; set; } = 0;

        // a list of regular expressions, if the Collection name matches, the movie is NOT listed as missing
        // note, the db is complete just what is shown is changed
        public List<string> CollectionFiltersForMissingMovies { get; set; } = [];

        public bool resetPlayCount { get; set; } = false;
        public bool resetDBState { get; set; } = false;
        public bool showDebugInfo { get; set; } = false;
        public bool showLastRunInfo { get; set; } = false;

        public bool dbStateOK { get; set; } = false;

        public List<UserConfig> UserConfigs { get; set; } = [];

        public UserConfig ConfigForUser( User user )
        {
            if( user == null )
                throw new UnauthorizedAccessException( "User likely is not authenticated." );

            var retVal = UserConfigs.FirstOrDefault( u => u.UserId == user.Id.ToString() );
            if( retVal == null )
            {
                retVal = new UserConfig { UserId = user.Id.ToString() };
                UserConfigs.Add( retVal );
            }
            retVal.CopyData( this ); // the user does NOT have the ability to customize the global config data

            return retVal;
        }

        public bool ShowDebugInfo( EmbyInterfaces embyInterfaces )
        {
            return showDebugInfo || ( embyInterfaces.ConfigManager().CommonConfiguration?.EnableDebugLevelLogging ?? false );
        }
    }

    public class UserConfig : IDataForConfig
    {
        public string UserId { get; set; } = string.Empty;
        public List<SortEntry> TableSortEntries { get; set; } = [];

        public string BuildDate { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        public bool hasConnectUserID { get; set; } = false;
        public int numMostActiveUsers { get; set; } = 5;
        public bool excludeAdmin { get; set; } = true;

        public int numWatchedToReport { get; set; } = 5;
        public int numTiedToReport { get; set; } = 5;

        public bool showUnknownDVProfiles { get; set; } = false;
        public bool showAllResolutions { get; set; } = false;

        public bool alwaysShowJustWatch { get; set; } = true;
        public int maxNumStreamingProviders { get; set; } = 3;

        public bool reportOnMissingSpecials { get; set; } = false;
        public bool showFilteredMissingSummaries { get; set; } = true;
        public List<string> searchLocations { get; set; } = [];
        public bool showSearchTheMovieDB { get; set; } = true;
        public int numDaysFutureForMissing { get; set; } = 0;

        // a list of regular expressions, if the Collection name matches, the movie is NOT listed as missing
        // note, the db is complete just what is shown is changed
        public List<string> CollectionFiltersForMissingMovies { get; set; } = [];

        public bool resetPlayCount { get; set; } = false;
        public bool resetDBState { get; set; } = false;
        public bool showDebugInfo { get; set; } = false;
        public bool showLastRunInfo { get; set; } = false;

        public bool dbStateOK { get; set; } = false;
    }
}

