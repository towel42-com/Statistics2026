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

        private bool IsFilteredParent( string parentName, bool episodes )
        {
            if( Plugin.Instance == null )
                return false;

            if( Plugin.Instance.Configuration == null )
                return false;

            if( parentName.IsNullOrEmpty() )
                return false;

            if( !episodes )
            {
                var regexes = Plugin.Instance.Configuration.CollectionFiltersForMissingMovies;

                foreach( var regexPattern in regexes )
                {
                    var regex = new Regex( regexPattern, RegexOptions.IgnoreCase );

                    if( regex.IsMatch( parentName ) )
                    {
                        return true;
                    }
                }
            }
            return false;
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

            retVal.AddLine( numMissing.ToString(), true );
            retVal.AddLine( subTitle, true );
            retVal.AddLine( numCollections.ToString(), true );
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
                if( IsFilteredParent( row.GetString( 2 ), episodes ) )
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

        public void UpdateSeriesFilter()
        {
            if( ( Plugin.Instance == null ) || ( Plugin.Instance.Configuration == null ) )
                return;

            CheckIsValid( ECheckType.eReport );

            var config = Plugin.Instance!.Configuration;

            var currFilterMap = config.SeriesFiltersForMissingEpisodes.ToDictionary( p => $"{p.SortName}_{p.PremiereYear}" );

            var sql = "SELECT " +
                "  Name" +
                ", SortName" +
                ", PremiereDate" +
                ", NumSeasons" +
                ", HasSpecials" +
                " FROM Series";

            var cmd = new SQLCmdDef( sql );
            _dbHelper.ExecuteCommand( cmd, statement =>
            {
                var row = statement.Current;
                var col = 0;
                var name = row.GetString( col++ );
                var sortName = row.GetString( col++ );
                var premiereDate = HelperFuncs.ReadDateTime( row.GetString( col++ ) );
                var numSeasons = row.GetInt( col++ );
                var hasSpecials = row.GetBoolean( col++ );

                var key = $"{sortName}";
                var premiereYear = 0;
                if( premiereDate != null )
                {
                    premiereYear = premiereDate.Value.LocalDateTime.Year;
                    key += "_" + premiereYear.ToString();
                }

                if ( !currFilterMap.ContainsKey( key ))
                {
                    var newFilter = new SeriesFilter()
                    {
                        Enabled = true,
                        Name = name,
                        SortName = sortName,
                        PremiereYear = premiereYear,
                        IncludeSpecials = false,
                        MinSeason = -1,
                        MaxSeason = -1,
                        NumSeasons = numSeasons,
                        HasSpecials = hasSpecials
                    };
                    currFilterMap[ key ] = newFilter;
                }
                return true;
            } );

            config.SeriesFiltersForMissingEpisodes = currFilterMap.Values.ToList();

            Plugin.Instance?.UpdateConfiguration( config );
        }
    }
}
