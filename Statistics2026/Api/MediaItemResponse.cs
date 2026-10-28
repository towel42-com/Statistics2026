using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Logging;
using ServiceStack;
using SQLitePCL;
using SQLitePCL.pretty;
using Statistics2026;
using Statistics2026.Api;
using Statistics2026.Configuration;
using Statistics2026.Utilities;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Statistics2026.Api
{
    public class MediaItemResponse
    {
        public SortByText DisplayName { get; set; } = new();
        public string PremiereDate { get; set; } = string.Empty;
        public string ResolutionDetail { get; set; } = string.Empty;
        public string Codec { get; set; } = string.Empty;
        public string DolbyVisionProfile { get; set; } = string.Empty;

        public SortByText StudioNames { get; set; } = new();
        public SortByText ServerLocation { get; set; } = new();
        public SortByText SearchLocation { get; set; } = new();
        public SortByText ParentName { get; set; } = new();
        public string ParentUrl { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;

        public string ItemUrl
        {
            get;
            set
            {
                field = value ?? string.Empty;
                if( value != null && value != string.Empty )
                    DisplayName.Text = value;
                else
                    DisplayName.Text = DisplayName.SortBy;
            }
        } = string.Empty;
        public string ItemId { get; set; } = string.Empty;

        public void setParentInfo( string parentJson, ref List<string> parentNames )
        {
            ParentUrl = string.Empty;
            ParentName = new();
            try
            {
                var jsonNode = JsonNode.Parse( parentJson );

                var parentArray = jsonNode?.AsArray();
                if( parentArray == null )
                    return;

                List<SortByText> sortByTexts = [];
                List<(string id, string url)> urls = [];
                foreach( var parentNode in parentArray )
                {
                    if( parentNode == null )
                        continue;

                    var name = parentNode[ "name" ]?.GetValue<string>() ?? string.Empty;
                    var sortName = parentNode[ "sort_name" ]?.GetValue<string>() ?? string.Empty;
                    var id = parentNode[ "id" ]?.GetValue<string>() ?? string.Empty;
                    var url = parentNode[ "url" ]?.GetValue<string>() ?? string.Empty;
                    if( name.IsNullOrEmpty() || sortName.IsNullOrEmpty() )
                        continue;


                    if( ParentName.SortBy != string.Empty )
                        ParentName.SortBy += ", ";
                    ParentName.SortBy += sortName;
                    parentNames.Add( sortName );

                    ParentId = id;
                    var currParentUrl = ItemImageUrl.ItemUrl( id, url, name );
                    if( currParentUrl != null && currParentUrl != string.Empty )
                    {
                        if( ParentUrl != string.Empty )
                            ParentUrl += "\n";
                        ParentUrl += currParentUrl;
                        if( !ParentName.Text.IsEmpty() )
                            ParentName.Text += "\n";
                        ParentName.Text += currParentUrl;
                    }
                    else
                    {
                        if( ParentName.Text != string.Empty )
                            ParentName.Text += ", ";
                        ParentName.Text += name;
                    }
                }

            }
            catch( Exception )
            {
            }
        }

        private static bool InvalidDateForMissingAnalysis( DateTimeOffset? dto, bool ignoreConfig = false )
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


        private static string CleanSortName( string sortName )
        {
            var retVal = Regex.Replace( sortName, @"^[^a-zA-Z0-9]+", "" );
            return retVal;
        }

        public static MediaItemResponse? CreateFromStatementRow( IResultSet row, bool isEpisode, Func<string, int, bool> isSeriesFiltered, Func<string, bool> isCollectionFiltered, Func<string, SortByText?> getCollectionName )
        {
            var col = 0;
            var isMissing = row.GetBoolean( col++ );
            var title = CleanSortName( row.GetString( col++ ) );
            var sortName = row.GetString( col++ );

            var premiereDate = HelperFuncs.ReadDateTime( row.GetString( col++ ) ) ?? DateTimeOffset.MinValue;
            var premiereYear = premiereDate == DateTimeOffset.MinValue ? 0 : premiereDate.Year;
            var studioNames = HelperFuncs.JsonToStringArray( row.GetString( col++ ) ).Join( ", " );
            var resolutionDetail = row.GetString( col++ ); // blank for missing
            var codec = row.GetString( col++ ); // blank for missing
            var dolbyVisionProfile = row.GetString( col++ ); // blank for missing
            var serverLocation = row.GetString( col++ ); // blank for missing
            var itemId = row.GetString( col++ ); // blank for missing
            var itemImageUrl = row.GetString( col++ ); // blank for missing
            var parentInfo = row.GetString( col++ );
            var posterPath = row.GetString( col++ ); // blank for onServer 
            var seasonNum = row.GetInt( col++ );
            var episodeNum = row.GetInt( col++ );
            var parentId = row.GetString( col++ );  // blank for onServer

            var retVal = new MediaItemResponse();
            retVal.DisplayName = new SortByText( title );
            retVal.PremiereDate = ( premiereDate == DateTimeOffset.MinValue ) ? string.Empty : premiereDate.ToTZShortString( true, true );
            retVal.StudioNames = new SortByText( studioNames );
            retVal.ResolutionDetail = resolutionDetail;
            retVal.Codec = codec;
            retVal.DolbyVisionProfile = dolbyVisionProfile;
            if( retVal.Codec != "hevc" && retVal.Codec != "av1" )
                retVal.DolbyVisionProfile = string.Empty;
            retVal.ServerLocation = new SortByText( serverLocation );
            retVal.ItemId = itemId;
            if( itemId != null && !itemId.IsEmpty() )
                retVal.ItemUrl = ItemImageUrl.ItemUrl( itemId, itemImageUrl, title );
            var parentNames = new List<string>();
            retVal.setParentInfo( parentInfo, ref parentNames );

            if( isMissing )
            {
                if( !isEpisode )
                {
                    foreach( var parentName in parentNames )
                    {
                        if( isCollectionFiltered( parentName ) )
                            return null;
                    }
                    var collections = getCollectionName( retVal.ParentId );
                    retVal.ParentName = collections ?? new();
                }
                else
                {
                    if( isSeriesFiltered( retVal.ParentId, seasonNum ) )
                    {
                        return null;
                    }
                }

                var futureDate = InvalidDateForMissingAnalysis( premiereDate, true );
                if( posterPath != null && !futureDate )
                {
                    if( !posterPath.StartsWith( "/" ) )
                        posterPath = "/" + posterPath;

                    retVal.ItemUrl = "https://image.tmdb.org/t/p/w185" + posterPath;
                    retVal.DisplayName.Text = $"<img loading=\"lazy\" src=\"{retVal.ItemUrl}\" height=\"105px\"/>{retVal.DisplayName.SortBy}";
                }

                var searchKey = retVal.DisplayName.SortBy;
                var subKey = string.Empty;
                if( isEpisode )
                {
                    searchKey = retVal.ParentName.SortBy;
                    if( seasonNum != 0 )
                        subKey += $"S{seasonNum:D2}";
                    if( episodeNum != 0 )
                        subKey += $"E{episodeNum:D2}";
                }
                else
                {
                    if( premiereYear > 0 )
                    {
                        subKey = $"{premiereYear}";
                    }
                }

                if( futureDate )
                {
                    retVal.SearchLocation = new SortByText( WebUtility.HtmlEncode( "<FUTURE RELEASE>" ) );
                    if( premiereDate != null )
                    {
                        var daysTo = Math.Ceiling( ( premiereDate - DateTimeOffset.Now ).TotalDays );
                        if( daysTo == 0 && premiereDate.Date != DateTimeOffset.Now.Date )
                            daysTo = 1;
                        var msg = $"Available in {daysTo} day";
                        if( daysTo != 1 )
                            msg += "s";
                        retVal.SearchLocation = new SortByText( msg );
                        retVal.PremiereDate = premiereDate.ToString( "dddd" ) + $" {retVal.PremiereDate}";
                    }
                }
                else if( !string.IsNullOrEmpty( Plugin.Instance!.Configuration.searchLocation ) )
                {
                    if( premiereDate.LocalDateTime.Date == DateTimeOffset.Now.Date )
                    {
                        retVal.PremiereDate = "Today" + " " + retVal.PremiereDate;
                    }
                    else if( premiereDate.LocalDateTime.Date == DateTimeOffset.Now.Date.AddDays( -1 ) )
                    {
                        retVal.PremiereDate = "Yesterday" + " " + retVal.PremiereDate;
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

                    retVal.SearchLocation.SortBy = sortBy;

                    var displayText = $"Click to Search for '{sortBy}'";
                    var searchLocation = $"<a is=\"emby-linkbutton\" href=\"{searchUrl}\" target=\"_blank\" rel=\"noopener noreferrer\" title=\"Search for {sortBy}\">{displayText}</a>";
                    retVal.SearchLocation.Text = searchLocation;
                }
            }

            return retVal;
        }
    }
}
