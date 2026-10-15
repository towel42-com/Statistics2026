using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using RestSharp;
using ServiceStack;
using ServiceStack.Text;
using Statistics2026.Api;
using Statistics2026.Configuration;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static ServiceStack.Diagnostics;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        private const string kSQLAddToMissing =
            "INSERT INTO Missing" +
            "(" +
                "  Key" +
                ", TmdbId" +
                ", Title" +
                ", OriginalTitle" +
                ", ReleaseDate" +
                ", Overview" +
                ", PosterPath" +
                ", ParentId" +
                ", IsEpisode" +
                ", SeasonNum" +
                ", EpisodeNum" +
            ")" +
            " VALUES " +
            "(" +
                "  @Key" +
                ", @TmdbId" +
                ", @Title" +
                ", @OriginalTitle" +
                ", @ReleaseDate" +
                ", @Overview" +
                ", @PosterPath" +
                ", @ParentId" +
                ", @IsEpisode" +
                ", @SeasonNum" +
                ", @EpisodeNum" +
            ")" +
            " ON CONFLICT(Key) " +
            " DO UPDATE " +
            " SET " +
                "  TmdbId=@TmdbId" +
                ", Title=@Title" +
                ", OriginalTitle=@OriginalTitle" +
                ", ReleaseDate=@ReleaseDate" +
                ", Overview=@Overview" +
                ", PosterPath=@PosterPath" +
                ", ParentId=@ParentId" +
                ", IsEpisode=@IsEpisode" +
                ", SeasonNum=@SeasonNum" +
                ", EpisodeNum=@EpisodeNum"
                ;

        private bool IsFilteredCollection( string collectionName )
        {
            if( Plugin.Instance == null )
                return false;

            if( Plugin.Instance.Configuration == null )
                return false;

            if( collectionName.IsNullOrEmpty() )
                return false;

            var regexes = Plugin.Instance.Configuration.CollectionFiltersForMissingMovies;

            foreach( var regexPattern in regexes )
            {
                var regex = new Regex( regexPattern, RegexOptions.IgnoreCase );

                if( regex.IsMatch( collectionName ) )
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsFilteredSeries( string seriesId, int seasonNum )
        {
            if( Plugin.Instance == null )
                return false;

            if( Plugin.Instance.Configuration == null )
                return false;

            if( seriesId.IsNullOrEmpty() )
                return false;

            var sql = "SELECT " +
            "    TrackSeries" +
            "  , TrackSpecials" +
            "  , MinSeason" +
            "  , MaxSeason" +
            " FROM SeriesFilterList " +
            " WHERE SeriesId=@SeriesId";

            var cmd = new SQLCmdDef( sql,
                     [
                        ("@SeriesId", seriesId ),
                         ] );

            var isSpecial = ( seasonNum == 0 );
            bool filtered = false;
            _dbHelper.ExecuteCommand( cmd, statement =>
            {
                var row = statement.Current;
                var col = 0;
                var trackSeries = row.GetBoolean( col++ );
                if( !trackSeries )
                {
                    filtered = true;
                    return false;
                }

                var trackSpecials = row.GetBoolean( col++ );
                if( isSpecial && !trackSpecials )
                {
                    filtered = true;
                    return false;
                }

                var minSeason = row.GetInt( col++ );
                if( ( minSeason > 0 ) && ( seasonNum < minSeason ) )
                {
                    filtered = true;
                    return false;
                }

                var maxSeason = row.GetInt( col++ );
                if( ( maxSeason > 0 ) && ( seasonNum > maxSeason ) )
                {
                    filtered = true;
                    return false;
                }

                filtered = false;
                return true;
            } );
            return filtered;
        }

        private bool InvalidDateForMissingAnalysis( DateTimeOffset? dto, bool ignoreConfig = false )
        {
            if( dto == null )
                return true;

            var numDaysToAdd = ignoreConfig ? 0 : Plugin.Instance!.Configuration.numDaysFutureForMissing;
            var maxDate = DateTimeOffset.Now.Date.AddDays( numDaysToAdd );
            if( dto.Value.Date > maxDate )
                return true;

            if( dto == DateTimeOffset.MinValue )
                return true;

            return false;
        }

        private StatCard TotalMissingMedia( bool episodes )
        {
            CheckIsValid( ECheckType.eReport );

            var whereClause = episodes ? "IsEpisode" : "NOT IsEpisode";

            var sql = $"SELECT COUNT( * ) FROM Missing WHERE {whereClause}";
            var value = GetSingleValueFromSQL( sql );
            var numMissing = value.ToInt64();

            sql = $"SELECT COUNT( DISTINCT ParentId ) FROM Missing WHERE {whereClause}";
            value = GetSingleValueFromSQL( sql );
            var numCollections = value.ToInt64();

            var title = episodes ? Constants.MissingEpisodes : Constants.MissingMovies;
            var subTitle = episodes ? Constants.MissingEpisodesSubTitle : Constants.MissingMoviesSubTitle;
            var help = episodes ? Constants.MissingEpisodesHelp : Constants.MissingMoviesHelp;

            var retVal = new TextBasedStatCard( title, help, EStatCardStyle.eCompact );

            retVal.AddLine( numMissing.ToString(), false );
            retVal.AddLine( subTitle, true );
            retVal.AddLine( numCollections.ToString(), false );
            return retVal;
        }

        private List<MediaItemResponse> getMediaListResponseMissing( EWhichMediaList whichMedia )
        {
            var episodes = ( whichMedia & EWhichMediaList.eEpisodes ) != 0;

            var sql = "SELECT " +
                "  Missing.Title AS ListDisplayName" +
                ", Missing.ReleaseDate"
                ;
            if( episodes )
            {
                sql += ", Series.Name";
            }
            else
            {
                sql += ", Collections.Name";
            }

            sql +=
                ", Missing.PosterPath" +
                ", Missing.SeasonNum" +
                ", Missing.EpisodeNum" +
                ", Missing.ParentId" +
                " FROM " +
                "   Missing ";

            if( episodes )
            {
                sql += " LEFT JOIN Series ON Series.ItemId=Missing.ParentId ";
                sql += " WHERE Missing.IsEpisode ";
            }
            else
            {
                sql += " LEFT JOIN Collections ON Collections.ItemId=Missing.ParentId ";
                sql += " WHERE NOT Missing.IsEpisode ";
            }

            sql += " ORDER BY ListDisplayName ASC";
            if( episodes )
                sql += ", Missing.SeasonNum ASC, Missing.EpisodeNum ASC ";

            var retVal = new List<MediaItemResponse>();
            _dbHelper.ExecuteCommand( new SQLCmdDef( sql ), statement =>
            {
                var row = statement.Current;
                if( !episodes && IsFilteredCollection( row.GetString( 2 ) ) )
                    return true;

                var premiereDate = HelperFuncs.ReadDateTime( row.GetString( 1 ) ) ?? DateTimeOffset.MinValue;
                var premiereYear = premiereDate == DateTimeOffset.MinValue ? 0 : premiereDate.Year;

                var curr = new MediaItemResponse()
                {
                    DisplayName = new SortByText( CleanSortName( row.GetString( 0 ) ), row.GetString( 0 ) ),
                    PremiereDate = premiereDate.ToTZShortString( true, true ) ?? string.Empty,
                    ResolutionDetail = string.Empty,
                    Codec = string.Empty,
                    DolbyVisionProfile = string.Empty,
                };
                var col = 2;
                curr.ParentName = new SortByText( row.GetString( col++ ), string.Empty );

                var posterPath = row.GetString( col++ );
                var seasonNum = row.GetInt( col++ );
                var episodeNum = row.GetInt( col++ );
                var parentId = row.GetString( col++ );

                if( episodes && IsFilteredSeries( parentId, seasonNum ) )
                    return true;

                if( !episodes )
                {
                    var collections = GetCollectionName( parentId );
                    curr.ParentName = collections ?? new();
                }

                var futureDate = InvalidDateForMissingAnalysis( premiereDate, true );
                if( posterPath != null && !posterPath.StartsWith( "/" ) )
                {
                    posterPath = "/" + posterPath;
                    curr.ItemUrl = "https://image.tmdb.org/t/p/w185" + posterPath;
                }

                if( !futureDate && curr.ItemUrl != null && curr.ItemUrl != string.Empty )
                {
                    curr.DisplayName.Text = $"<a is=\"emby-linkbutton\" href=\"{curr.ItemUrl}\"><img loading=\"lazy\" src=\"{curr.ItemUrl}\" height=\"105px\"/>{curr.DisplayName.SortBy}</a>";
                }

                var searchKey = curr.DisplayName.SortBy;
                var subKey = string.Empty;
                if( episodes )
                {
                    searchKey = curr.ParentName.SortBy;
                    if( seasonNum != 0 )
                        subKey += $"S{seasonNum:D2}";
                    if( episodeNum != 0 )
                        subKey += $"E{episodeNum:D2}";
                }
                else
                {
                    if( premiereYear != 0 )
                    {
                        subKey = $"{premiereYear}";
                    }
                }

                if( futureDate )
                {
                    curr.SearchLocation = new SortByText( WebUtility.HtmlEncode( "<FUTURE RELEASE>" ) );
                    if( premiereDate != null )
                    {
                        var daysTo = Math.Ceiling( ( premiereDate - DateTimeOffset.Now ).TotalDays );
                        if( daysTo == 0 && premiereDate.Date != DateTimeOffset.Now.Date )
                            daysTo = 1;
                        var msg = $"Available in {daysTo} day";
                        if( daysTo != 1 )
                            msg += "s";
                        curr.SearchLocation = new SortByText( msg );
                        curr.PremiereDate = premiereDate.ToString( "dddd" ) + $" {curr.PremiereDate}";
                    }
                }
                else if( !string.IsNullOrEmpty( Plugin.Instance!.Configuration.searchLocation ) )
                {
                    if( premiereDate.LocalDateTime.Date == DateTimeOffset.Now.Date )
                    {
                        curr.PremiereDate = "Today" + " " + curr.PremiereDate;
                    }
                    else if( premiereDate.LocalDateTime.Date == DateTimeOffset.Now.Date.AddDays( -1 ) )
                    {
                        curr.PremiereDate = "Yesterday" + " " + curr.PremiereDate;
                    }

                    var searchUrl = Plugin.Instance!.Configuration.searchLocation;
                    if( !searchUrl.EndsWith( "?q=" ) )
                        searchUrl += "?q=";
                    searchUrl += searchKey;

                    if( !subKey.IsEmpty() )
                        searchUrl += $" {subKey}";

                    var sortBy = searchKey;
                    if( !subKey.IsEmpty() )
                        sortBy += $" - {subKey}";

                    curr.SearchLocation.SortBy = sortBy;

                    var displayText = $"Click to Search for '{sortBy}'";
                    var searchLocation = $"<a is=\"emby-linkbutton\" href=\"{searchUrl}\" target=\"_blank\" rel=\"noopener noreferrer\" title=\"Search for {sortBy}\">{displayText}</a>";
                    curr.SearchLocation.Text = searchLocation;
                }

                retVal.Add( curr );
                return true;
            } );
            return retVal;
        }

        public StatCard TotalMissingMovies()
        {
            return TotalMissingMedia( false );
        }

        public StatCard TotalMissingEpisodes()
        {
            return TotalMissingMedia( true );
        }

        public List<SeriesFilterResponse> GetSeriesFilter( bool updateIfEmpty )
        {
            CheckIsValid( ECheckType.eReport );

            var sql = "SELECT" +
                "  SeriesFilterList.SeriesId" +
                ", SeriesFilterList.TrackSeries" +
                ", SeriesFilterList.TrackSpecials" +
                ", Series.Name" +
                ", Series.SortName" +
                ", Series.PremiereDate" +
                ", SeriesFilterList.MinSeason" +
                ", SeriesFilterList.MaxSeason" +
                ", Series.HasSpecials" +
                ", Series.NumSeasons" +
                " FROM SeriesFilterList" +
                " JOIN Series ON Series.ItemId=SeriesFilterList.SeriesId";

            List<SeriesFilterResponse> retVal = [];
            bool found = false;
            _dbHelper.ExecuteCommand( new SQLCmdDef( sql ), statement =>
            {
                found = true;
                var row = statement.Current;

                var col = 0;
                var currFilter = new SeriesFilterResponse()
                {
                    SeriesId = row.GetString( col++ ),
                    TrackSeries = row.GetBoolean( col++ ),
                    TrackSpecials = row.GetBoolean( col++ ),
                    Name = row.GetString( col++ ),
                    SortName = row.GetString( col++ ),
                    PremiereYear = HelperFuncs.ReadDateTime( row.GetString( col++ ) )?.Year ?? -1,
                    MinSeason = row.GetInt( col++ ),
                    MaxSeason = row.GetInt( col++ ),
                    HasSpecials = row.GetBoolean( col++ ),
                    NumSeasons = row.GetInt( col++ )
                };
                retVal.Add( currFilter );
                return true;
            } );
            if( !found && updateIfEmpty )
            {
                var cmds = UpdateSeriesFilter( true );
                _dbHelper.ExecuteCommands( cmds );
                return GetSeriesFilter( false );
            }
            return retVal;
        }

        public SQLCmdDef SaveSeriesFilter( SeriesFilterResponse seriesFilter, bool andExecute )
        {
            var sql = "INSERT INTO SeriesFilterList" +
                "(" +
                "    SeriesId" +
                "  , TrackSeries" +
                "  , TrackSpecials" +
                "  , MinSeason" +
                "  , MaxSeason" +
                ")" +
                " VALUES " +
                "(" +
                "  @SeriesId" +
                ", @TrackSeries" +
                ", @TrackSpecials" +
                ", @MinSeason" +
                ", @MaxSeason" +
                ")" +
                " ON CONFLICT(SeriesId) " +
                " DO UPDATE" +
                " SET" +
                "  TrackSeries=@TrackSeries" +
                ", TrackSpecials=@TrackSpecials" +
                ", MinSeason=@MinSeason" +
                ", MaxSeason=@MaxSeason"
                ;

            var cmd = new SQLCmdDef( sql,
                         [
                            ("@SeriesId", seriesFilter.SeriesId ),
                            ("@TrackSeries", seriesFilter.TrackSeries ),
                            ("@TrackSpecials", seriesFilter.TrackSpecials ),
                            ("@MinSeason", seriesFilter.MinSeason ),
                            ("@MaxSeason", seriesFilter.MaxSeason ),
                        ] );

            if( andExecute )
            {
                _dbHelper.ExecuteCommand( cmd );
            }
            return cmd;
        }

        public List<SQLCmdDef> UpdateSeriesFilter( bool nested = false )
        {
            bool filterChanged( ref string msg, ref SeriesFilterResponse lhs, int numSeasons, bool hasSpecials )
            {
                bool retVal = false;
                if( lhs.HasSpecials != hasSpecials )
                {
                    msg += $" - HasSpecials : old: {lhs.HasSpecials} new: {hasSpecials}";
                    lhs.HasSpecials = lhs.HasSpecials || hasSpecials;
                    retVal = true;
                }
                var newNumSeasons = ( numSeasons > lhs.NumSeasons ) ? numSeasons : lhs.NumSeasons;
                if( newNumSeasons != lhs.NumSeasons )
                {
                    msg += $" - NumSeasons : old: {lhs.NumSeasons} new: {newNumSeasons}";
                    lhs.NumSeasons = newNumSeasons;
                    retVal = true;
                }
                return retVal;
            }

            if( !nested )
                CheckIsValid( ECheckType.eUpdate );

            if( _embyInterfaces == null || _embyInterfaces._libraryManager == null )
                throw new ArgumentNullException( "_embyInterfaces or libraryManager is null" );

            var seriesFilterList = GetSeriesFilter( false );
            Dictionary<string, List<SeriesFilterResponse>> oldFilterMap = [];
            foreach( var series in seriesFilterList )
            {
                var key = series.key();
                if ( !oldFilterMap.TryGetValue( key, out var list ) )
                {
                    list = [];
                    oldFilterMap[ key ] = list;
                }
                list.Add( series );
            }

            var currFilterMap = GetSeriesFilter( false ).ToDictionary( p => p.SeriesId );

            var changedFilterMap = new List<SeriesFilterResponse>();

            var seriesList = EmbyHelperFuncs.GetLibraryItems<Series>( null, _embyInterfaces?._libraryManager );

            foreach( var series in seriesList )
            {
                var key = series.Id.ToString();
                var oldKey = $"{series.SortName}";
                int premiereYear = series.PremiereDate?.Year ?? -1;
                if( premiereYear != -1 )
                {
                    oldKey += $"_{premiereYear}";
                }

                var (numSeasons, hasSpecials) = GetSeasonCount( series );

                if( currFilterMap.TryGetValue( key, out var currFilter ) )
                {
                    var msg = $"Updating filter for Series: {series.Name}";
                    var changed = filterChanged( ref msg, ref currFilter, numSeasons, hasSpecials ); 
                    if( changed )
                    {
                        currFilterMap[ key ] = currFilter;
                        changedFilterMap.Add( currFilter );
                        _embyInterfaces?._logger?.Debug( msg );
                    }
                }
                else if ( oldFilterMap.TryGetValue( oldKey, out var oldFilterList ) )
                {
                    bool found = false;
                    SeriesFilterResponse? reference = null;
                    foreach( var tmp in oldFilterList )
                    {
                        reference = reference ?? tmp;

                        if ( series.Id.ToString() == tmp.SeriesId )
                        {
                            var msg = $"Updating filter for Series: {series.Name}";
                            var changed = filterChanged( ref msg, ref currFilter, numSeasons, hasSpecials );
                            if( changed )
                            {
                                currFilterMap[ key ] = currFilter;
                                changedFilterMap.Add( currFilter );
                                _embyInterfaces?._logger?.Debug( msg );
                            }
                            found = true;
                            break;
                        }
                    }

                    if( !found )
                    {
                        var newFilter = new SeriesFilterResponse()
                        {
                            SeriesId = series.Id.ToString(),
                            TrackSeries = true,
                            TrackSpecials = false,
                            Name = series.Name,
                            SortName = series.SortName,
                            PremiereYear = premiereYear,
                            MinSeason = -1,
                            MaxSeason = -1,
                            HasSpecials = hasSpecials,
                            NumSeasons = numSeasons,
                        };
                        if( reference != null )
                        {
                            newFilter.TrackSeries = reference.TrackSeries;
                            newFilter.TrackSpecials = reference.TrackSpecials;
                            newFilter.MinSeason = reference.MinSeason;
                            newFilter.MaxSeason = reference.MaxSeason;
                        }
                        changedFilterMap.Add( newFilter );
                    }
                }
                else
                {
                    var newFilter = new SeriesFilterResponse()
                    {
                        SeriesId = series.Id.ToString(),
                        TrackSeries = true,
                        Name = series.Name,
                        SortName = series.SortName,
                        PremiereYear = premiereYear,
                        TrackSpecials = false,
                        MinSeason = -1,
                        MaxSeason = -1,
                        HasSpecials = hasSpecials,
                        NumSeasons = numSeasons,
                    };
                    currFilterMap[ key ] = newFilter;
                    _embyInterfaces?._logger?.Debug( $"Adding default filter for Series: {series.Name} - HasSpecials: {hasSpecials} - NumSeasons: {numSeasons}" );
                    changedFilterMap.Add( newFilter );
                }
            }

            List<SQLCmdDef> sqlCmds = [];
            foreach( var entry in changedFilterMap )
            {
                sqlCmds.Add( SaveSeriesFilter( entry, false ) );
            }
            return sqlCmds;
        }
    }
}
