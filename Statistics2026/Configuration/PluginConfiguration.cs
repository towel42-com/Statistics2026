using Emby.Media.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Plugins;
using ServiceStack;
using ServiceStack.Text;
using Statistics2026.Utilities;
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

    public class SystemSettings
    {
        public bool showDebugInfo { get; set; } = false;
        public bool showDiagnosticInfo { get; set; } = false;
        public bool showLastRunInfo { get; set; } = false;
        public bool resetPlayCount { get; set; } = false;
        public bool resetDBState { get; set; } = false;
        public bool showUnknownDVProfiles { get; set; } = false;
        public bool showAllResolutions { get; set; } = false;
        public int numMostActiveUsers { get; set; } = 5;

        public void CopyData( SystemSettings dataSource )
        {
            resetPlayCount = dataSource.resetPlayCount;
            resetDBState = dataSource.resetDBState;
            showDebugInfo = dataSource.showDebugInfo;
            showDiagnosticInfo = dataSource.showDiagnosticInfo;
            showLastRunInfo = dataSource.showLastRunInfo;
            showUnknownDVProfiles = dataSource.showUnknownDVProfiles;
            showAllResolutions = dataSource.showAllResolutions;
            numMostActiveUsers = dataSource.numMostActiveUsers;
        }
    }

    public class MovieSettings : IEquatable<MovieSettings>
    {
        public int numDaysForMovieStreaming { get; set; } = 90;
        public bool showSearchTheMovieDB { get; set; } = true;

        public List<string> searchLocations { get; set; } = [];

        public bool showFilteredMissingSummaries { get; set; } = true;
        public bool alwaysShowJustWatch { get; set; } = true;
        public int maxNumStreamingProviders { get; set; } = 3;

        // a list of regular expressions, if the Collection name matches, the movie is NOT listed as missing
        // note, the db is complete just what is shown is changed
        public List<string> CollectionFiltersForMissingMovies { get; set; } = [];

        public bool showImages { get; set; } = true;
        public int numWatchedToReport { get; set; } = 5;
        public int numTiedToReport { get; set; } = 5;
        public int numGenresToReport { get; set; } = 5;
        public int numActorsToReport { get; set; } = 5;

        public static bool operator ==( MovieSettings? left, MovieSettings? right )
        {
            if( ReferenceEquals( left, right ) )
                return true;
            if( left is null || right is null )
                return false;
            return left.Equals( right );
        }
        public static bool operator !=( MovieSettings? left, MovieSettings? right ) => !( left == right );

        public override bool Equals( object? obj ) => Equals( obj as MovieSettings );
        public bool Equals( MovieSettings? other )
        {
            if( other is null )
                return false;

            if( ReferenceEquals( this, other ) )
                return true;

            bool searchLocationsEqual = ReferenceEquals( searchLocations, other.searchLocations )
                || ( searchLocations is not null
                     && other.searchLocations is not null
                     && searchLocations.SequenceEqual( other.searchLocations ) );

            bool collectionFiltersEqual = ReferenceEquals( CollectionFiltersForMissingMovies, other.CollectionFiltersForMissingMovies )
                || ( CollectionFiltersForMissingMovies is not null
                     && other.CollectionFiltersForMissingMovies is not null
                     && CollectionFiltersForMissingMovies.SequenceEqual( other.CollectionFiltersForMissingMovies ) );

            return AnalysisEquals( other )
                && showSearchTheMovieDB == other.showSearchTheMovieDB
                && searchLocationsEqual
                && showFilteredMissingSummaries == other.showFilteredMissingSummaries
                && alwaysShowJustWatch == other.alwaysShowJustWatch
                && maxNumStreamingProviders == other.maxNumStreamingProviders
                && collectionFiltersEqual
                && showImages == other.showImages
                && numWatchedToReport == other.numWatchedToReport
                && numTiedToReport == other.numTiedToReport
                && numGenresToReport == other.numGenresToReport
                && numActorsToReport == other.numActorsToReport
                ;
        }

        public bool AnalysisEquals( MovieSettings? other )
        {
            if( other is null )
                return false;

            return ( numDaysForMovieStreaming == other.numDaysForMovieStreaming );
        }

        public override int GetHashCode()
        {
            int hash = 17;

            hash = ( hash * 23 ) + numDaysForMovieStreaming.GetHashCode();
            hash = ( hash * 23 ) + showSearchTheMovieDB.GetHashCode();
            if( searchLocations is not null )
            {
                foreach( string location in searchLocations )
                {
                    hash = ( hash * 23 ) + location.GetHashCode();
                }
            }

            hash = ( hash * 23 ) + showFilteredMissingSummaries.GetHashCode();
            hash = ( hash * 23 ) + alwaysShowJustWatch.GetHashCode();
            hash = ( hash * 23 ) + maxNumStreamingProviders.GetHashCode();
            if( CollectionFiltersForMissingMovies is not null )
            {
                foreach( string location in CollectionFiltersForMissingMovies )
                {
                    hash = ( hash * 23 ) + location.GetHashCode();
                }
            }
            hash = ( hash * 23 ) + showImages.GetHashCode();
            hash = ( hash * 23 ) + numWatchedToReport.GetHashCode();
            hash = ( hash * 23 ) + numTiedToReport.GetHashCode();
            hash = ( hash * 23 ) + numGenresToReport.GetHashCode();
            hash = ( hash * 23 ) + numActorsToReport.GetHashCode();

            return hash;
        }

        public void CopyData( MovieSettings dataSource )
        {
            numDaysForMovieStreaming = dataSource.numDaysForMovieStreaming;
            showSearchTheMovieDB = dataSource.showSearchTheMovieDB;

            searchLocations = [];
            if( dataSource.searchLocations != null )
                searchLocations.AddRange( dataSource.searchLocations );

            showFilteredMissingSummaries = dataSource.showFilteredMissingSummaries;
            alwaysShowJustWatch = dataSource.alwaysShowJustWatch;
            maxNumStreamingProviders = dataSource.maxNumStreamingProviders;

            CollectionFiltersForMissingMovies = [];
            if( dataSource.CollectionFiltersForMissingMovies != null )
                CollectionFiltersForMissingMovies.AddRange( dataSource.CollectionFiltersForMissingMovies );

            showImages = dataSource.showImages;
            numWatchedToReport = dataSource.numWatchedToReport;
            numTiedToReport = dataSource.numTiedToReport;
            numGenresToReport = dataSource.numGenresToReport;
            numActorsToReport = dataSource.numActorsToReport;
        }
    }

    public class TVSettings : IEquatable<TVSettings>
    {
        public int numDaysFutureForMissing { get; set; } = 0;
        public bool showSearchTheMovieDB { get; set; } = true;
        public List<string> searchLocations { get; set; } = [];
        public bool showFilteredMissingSummaries { get; set; } = true;
        public bool reportOnMissingSpecials { get; set; } = false;
        public bool alwaysShowJustWatch { get; set; } = true;
        public int maxNumStreamingProviders { get; set; } = 3;
        public bool showImages { get; set; } = true;
        public int numWatchedToReport { get; set; } = 5;
        public int numTiedToReport { get; set; } = 5;
        public int numGenresToReport { get; set; } = 5;
        public int numActorsToReport { get; set; } = 5;

        public static bool operator ==( TVSettings? left, TVSettings? right )
        {
            if( ReferenceEquals( left, right ) )
                return true;
            if( left is null || right is null )
                return false;
            return left.Equals( right );
        }

        public static bool operator !=( TVSettings? left, TVSettings? right ) => !( left == right );

        public override bool Equals( object? obj ) => Equals( obj as TVSettings );
        public bool Equals( TVSettings? other )
        {
            if( other is null )
                return false;
            if( ReferenceEquals( this, other ) )
                return true;

            bool searchLocationsEqual = ReferenceEquals( searchLocations, other.searchLocations )
                || ( searchLocations is not null
                     && other.searchLocations is not null
                     && searchLocations.SequenceEqual( other.searchLocations ) );

            return AnalysisEquals( other )
                && showSearchTheMovieDB == other.showSearchTheMovieDB
                && searchLocationsEqual
                && showFilteredMissingSummaries == other.showFilteredMissingSummaries
                && alwaysShowJustWatch == other.alwaysShowJustWatch
                && maxNumStreamingProviders == other.maxNumStreamingProviders
                && showImages == other.showImages
                && numWatchedToReport == other.numWatchedToReport
                && numTiedToReport == other.numTiedToReport
                && numGenresToReport == other.numGenresToReport
                && numActorsToReport == other.numActorsToReport
                ;
        }

        public bool AnalysisEquals( TVSettings? other )
        {
            if( other is null )
                return false;

            return ( numDaysFutureForMissing == other.numDaysFutureForMissing )
                && ( reportOnMissingSpecials == other.reportOnMissingSpecials );
        }

        public override int GetHashCode()
        {
            int hash = 17;

            hash = ( hash * 23 ) + numDaysFutureForMissing.GetHashCode();
            hash = ( hash * 23 ) + showSearchTheMovieDB.GetHashCode();

            if( searchLocations is not null )
            {
                foreach( string location in searchLocations )
                {
                    hash = ( hash * 23 ) + location.GetHashCode();
                }
            }

            hash = ( hash * 23 ) + showFilteredMissingSummaries.GetHashCode();
            hash = ( hash * 23 ) + reportOnMissingSpecials.GetHashCode();
            hash = ( hash * 23 ) + alwaysShowJustWatch.GetHashCode();
            hash = ( hash * 23 ) + maxNumStreamingProviders.GetHashCode();
            hash = ( hash * 23 ) + showImages.GetHashCode();
            hash = ( hash * 23 ) + numWatchedToReport.GetHashCode();
            hash = ( hash * 23 ) + numTiedToReport.GetHashCode();
            hash = ( hash * 23 ) + numGenresToReport.GetHashCode();
            hash = ( hash * 23 ) + numActorsToReport.GetHashCode();

            return hash;
        }

        public void CopyData( TVSettings dataSource )
        {
            numDaysFutureForMissing = dataSource.numDaysFutureForMissing;
            showSearchTheMovieDB = dataSource.showSearchTheMovieDB;

            searchLocations = [];
            if( dataSource.searchLocations != null )
                searchLocations.AddRange( dataSource.searchLocations );

            showFilteredMissingSummaries = dataSource.showFilteredMissingSummaries;
            reportOnMissingSpecials = dataSource.reportOnMissingSpecials;
            alwaysShowJustWatch = dataSource.alwaysShowJustWatch;
            maxNumStreamingProviders = dataSource.maxNumStreamingProviders;

            // series/tv filters are stored in the DB

            showImages = dataSource.showImages;
            numWatchedToReport = dataSource.numWatchedToReport;
            numTiedToReport = dataSource.numTiedToReport;
            numGenresToReport = dataSource.numGenresToReport;
            numActorsToReport = dataSource.numActorsToReport;
        }
    }
    public class UserReportSettings
    {
        public bool excludeAdmin { get; set; } = true;
        public bool hasConnectUserID { get; set; } = false;
        public bool showImages { get; set; } = true;
        public int numFavoriteYears { get; set; } = 5;
        public int numGenresToReport { get; set; } = 5;
        public int numLastSeenToReport { get; set; } = 5;
        public int numWatchedToReport { get; set; } = 5;
        public int numTiedToReport { get; set; } = 5;


        public void CopyData( UserReportSettings dataSource )
        {
            hasConnectUserID = dataSource.hasConnectUserID;
            excludeAdmin = dataSource.excludeAdmin;
            showImages = dataSource.showImages;
            numFavoriteYears = dataSource.numFavoriteYears;
            numGenresToReport = dataSource.numGenresToReport;
            numLastSeenToReport = dataSource.numLastSeenToReport;
            numWatchedToReport = dataSource.numWatchedToReport;
            numTiedToReport = dataSource.numTiedToReport;
        }
    }

    public interface IDataForConfig
    {
        string BuildDate { get; set; }
        string Version { get; set; }

        SystemSettings systemSettings { get; set; }
        MovieSettings movieSettings { get; set; }
        TVSettings tvSettings { get; set; }
        UserReportSettings userReportSettings { get; set; }
    }

    public enum EReportType
    {
        eTVReport,
        eMovieReport,
        eUserReport
    };

    public static class DataForConfigExtensions
    {
        public static int GetNumLastSeenToReport( this IDataForConfig target, User? user )
        {
            if( user != null )
            {
                target = Plugin.Instance!.Configuration.ConfigForUser( user );
            }
            return target.userReportSettings.numLastSeenToReport;
        }

        public static int GetNumFavoriteYears( this IDataForConfig target, User? user )
        {
            if( user != null )
            {
                target = Plugin.Instance!.Configuration.ConfigForUser( user );
            }
            return target.userReportSettings.numFavoriteYears;
        }

        public static bool ShowImages( this IDataForConfig target, User? user, EReportType reportType )
        {
            if( user != null )
            {
                target = Plugin.Instance!.Configuration.ConfigForUser( user );
            }
            switch( reportType )
            {
                case EReportType.eTVReport:
                    return target.tvSettings.showImages;
                case EReportType.eMovieReport:
                    return target.movieSettings.showImages;
                case EReportType.eUserReport:
                    return target.userReportSettings.showImages;
            }
            return true;
        }

        public static List<string> CustomSearchLocations( this IDataForConfig target, EReportType reportType )
        {
            switch( reportType )
            {
                case EReportType.eTVReport:
                    return target.tvSettings.searchLocations;
                case EReportType.eMovieReport:
                    return target.movieSettings.searchLocations;
            }
            return [];
        }

        public static bool HasCustomSearches( this IDataForConfig target, EReportType reportType )
        {
            bool retVal = false;
            List<string> searchLocations = target.CustomSearchLocations( reportType );
            for( int ii = 0; !retVal && ( ii < searchLocations.Count ); ++ii )
            {
                retVal = !string.IsNullOrWhiteSpace( searchLocations[ ii ] );
            }
            return retVal;
        }

        public static bool ShowSearchTheMovieDB( this IDataForConfig target, EReportType reportType )
        {
            switch( reportType )
            {
                case EReportType.eTVReport:
                    return target.tvSettings.showSearchTheMovieDB;
                case EReportType.eMovieReport:
                    return target.movieSettings.showSearchTheMovieDB;
            }
            return true;
        }

        public static bool AlwaysShowJustWatch( this IDataForConfig target, EReportType reportType )
        {
            switch( reportType )
            {
                case EReportType.eTVReport:
                    return target.tvSettings.alwaysShowJustWatch;
                case EReportType.eMovieReport:
                    return target.movieSettings.alwaysShowJustWatch;
            }
            return true;
        }

        public static int MaxNumStreamingProviders( this IDataForConfig target, EReportType reportType )
        {
            switch( reportType )
            {
                case EReportType.eTVReport:
                    return target.tvSettings.maxNumStreamingProviders;
                case EReportType.eMovieReport:
                    return target.movieSettings.maxNumStreamingProviders;
            }
            return 0;
        }

        public static int NumGenresToReport( this IDataForConfig target, User? user, EReportType reportType )
        {
            if( user != null )
            {
                target = Plugin.Instance!.Configuration.ConfigForUser( user );
            }

            switch( reportType )
            {
                case EReportType.eTVReport:
                    return target.tvSettings.numGenresToReport;
                case EReportType.eMovieReport:
                    return target.movieSettings.numGenresToReport;
                case EReportType.eUserReport:
                    return target.userReportSettings.numGenresToReport;
            }
            return 0;
        }

        public static int NumActorsToReport( this IDataForConfig target, EReportType reportType )
        {
            switch( reportType )
            {
                case EReportType.eTVReport:
                    return target.tvSettings.numActorsToReport;
                case EReportType.eMovieReport:
                    return target.movieSettings.numActorsToReport;
            }
            return 0;
        }

        public static int NumWatchedToReport( this IDataForConfig target, User? user, EReportType reportType )
        {
            if( user != null )
            {
                target = Plugin.Instance!.Configuration.ConfigForUser( user );
            }

            switch( reportType )
            {
                case EReportType.eTVReport:
                    return target.tvSettings.numWatchedToReport;
                case EReportType.eMovieReport:
                    return target.movieSettings.numWatchedToReport;
                case EReportType.eUserReport:
                    return target.userReportSettings.numWatchedToReport;
            }
            return 0;
        }

        public static int NumTiedToReport( this IDataForConfig target, User? user, EReportType reportType )
        {
            if( user != null )
            {
                target = Plugin.Instance!.Configuration.ConfigForUser( user );
            }
            switch( reportType )
            {
                case EReportType.eTVReport:
                    return target.tvSettings.numTiedToReport;
                case EReportType.eMovieReport:
                    return target.movieSettings.numTiedToReport;
                case EReportType.eUserReport:
                    return target.userReportSettings.numTiedToReport;

            }
            return 0;
        }

        public static bool CanSearch( this IDataForConfig target, EReportType reportType )
        {
            bool retVal = false;
            switch( reportType )
            {
                case EReportType.eTVReport:
                    retVal = target.tvSettings.showSearchTheMovieDB;
                    break;
                case EReportType.eMovieReport:
                    retVal = target.movieSettings.showSearchTheMovieDB;
                    break;
            }

            List<string> searchLocations = CustomSearchLocations( target, reportType );
            for( int ii = 0; !retVal && ( ii < searchLocations.Count ); ++ii )
            {
                retVal = !string.IsNullOrWhiteSpace( searchLocations[ ii ] );
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

            target.systemSettings.CopyData( dataSource.systemSettings );
            target.movieSettings.CopyData( dataSource.movieSettings );
            target.tvSettings.CopyData( dataSource.tvSettings );
            target.userReportSettings.CopyData( dataSource.userReportSettings );
        }
    }

    public class PluginConfiguration : BasePluginConfiguration, IDataForConfig
    {
        public PluginConfiguration() { }

        public void UpdateBuildDateAndVersionInfo()
        {
            string gitHash = Assembly.GetExecutingAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split( '+' ).LastOrDefault() ?? String.Empty;
            gitHash = gitHash.Length >= 8 ? gitHash[ ..8 ] : gitHash;
            if( !gitHash.IsEmpty() )
                gitHash = "+" + gitHash;

            Version = Plugin.Instance?.Version.ToString( 4 ) + gitHash ?? "<UNKNOWN>";
            BuildDate = HelperFuncs.ToDateTimeParamValue( BuildDateInfo.GetBuildDate() )!;
            Plugin.Instance?.UpdateConfiguration( this );
        }

        public string BuildDate { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        public SystemSettings systemSettings { get; set; } = new();
        public MovieSettings movieSettings { get; set; } = new();
        public TVSettings tvSettings { get; set; } = new();
        public UserReportSettings userReportSettings { get; set; } = new();

        public List<UserConfig> UserConfigs { get; set; } = [];

        public UserConfig ConfigForUser( User user )
        {
            if( user == null )
                throw new UnauthorizedAccessException( "User likely is not authenticated." );

            UserConfig retVal = UserConfigs.FirstOrDefault( u => u.UserId == user.Id.ToString() );
            if( retVal == null )
            {
                retVal = new UserConfig { UserId = user.Id.ToString() };
                UserConfigs.Add( retVal );
                retVal.CopyData( this );
            }

            return retVal;
        }

        public void SetConfigForUser( User user, UserConfig config )
        {
            if( user == null )
                throw new UnauthorizedAccessException( "User likely is not authenticated." );

            bool found = false;
            for( int ii = 0; ii < UserConfigs.Count; ++ii )
            {
                if ( UserConfigs[ ii ].UserId == user.Id.ToString() )
                {
                    found = true;
                    UserConfigs[ ii ] = config;
                    break;
                }
            }

            if( !found )
            {
                UserConfigs.Add( config );
            }
        }

        public bool ShowDebugInfo( EmbyInterfaces embyInterfaces )
        {
            return systemSettings.showDebugInfo && ( embyInterfaces.ConfigManager().CommonConfiguration?.EnableDebugLevelLogging ?? false );
        }

        public bool ShowDiagnosticInfo( EmbyInterfaces embyInterfaces )
        {
            return systemSettings.showDiagnosticInfo && ShowDebugInfo( embyInterfaces ) && ( embyInterfaces.ConfigManager().CommonConfiguration?.EnableDebugLevelLogging ?? false );
        }
    }

    public class UserConfig : IDataForConfig
    {
        public string UserId { get; set; } = string.Empty;
        public List<SortEntry> TableSortEntries { get; set; } = [];

        public string BuildDate { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;


        public SystemSettings systemSettings { get; set; } = new();
        public MovieSettings movieSettings { get; set; } = new();
        public TVSettings tvSettings { get; set; } = new();
        public UserReportSettings userReportSettings { get; set; } = new();
    }
}

