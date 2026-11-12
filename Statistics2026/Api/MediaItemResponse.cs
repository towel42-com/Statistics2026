using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using ServiceStack;
using SQLitePCL;
using SQLitePCL.pretty;
using Statistics2026;
using Statistics2026.Api;
using Statistics2026.Configuration;
using Statistics2026.Data;
using Statistics2026.Utilities;
using System.Globalization;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static Statistics2026.Data.Statistics2026DB;

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
        public SortByText StreamingProviders { get; set; } = new();
        public SortByText ServerLocation { get; set; } = new();
        public SortByText SearchLocation { get; set; } = new();
        public SortByText ParentName { get; set; } = new();
        public string ParentUrl { get; set; } = string.Empty;
        public List<string> ParentIds { get; set; } = [];

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

        public static List<(string id, string name, string sortName, string displayText, string url)>? getParentInfo( string parentJson, Func<string, string, string, bool> isFiltered )
        {
            List<(string id, string name, string sortName, string displayText, string url)> retVal = [];

            JsonArray? parentArray = null;
            try
            {
                var jsonNode = JsonNode.Parse( parentJson );
                parentArray = jsonNode?.AsArray();
            }
            catch( Exception )
            {
                return retVal;
            }

            if( parentArray == null )
                return retVal;

            for( int ii = 0; ii < parentArray.Count(); ++ii )
            {
                var parentNode = parentArray[ ii ];
                if( parentNode == null )
                    continue;

                var name = parentNode[ "name" ]?.GetValue<string>() ?? string.Empty;
                var sortName = parentNode[ "sort_name" ]?.GetValue<string>() ?? string.Empty;
                var id = parentNode[ "id" ]?.GetValue<string>() ?? string.Empty;
                var url = parentNode[ "url" ]?.GetValue<string>() ?? string.Empty;
                if( name.IsNullOrEmpty() || sortName.IsNullOrEmpty() )
                    continue;

                if( isFiltered( id, name, sortName ) )
                    return null;

                string parentUrl = string.Empty;
                var displayText = name;

                var urlName = name;
                if( ii != 0 )
                    urlName = $", {urlName}";
                var currParentUrl = ItemImageUrl.ItemUrlFromItemId( id, url, urlName ) ?? string.Empty;

                if( currParentUrl.IsNullOrEmpty() )
                {
                    currParentUrl = string.Empty;
                }
                else
                {
                    displayText = currParentUrl;
                }

                retVal.Add( (id, name, sortName, displayText, currParentUrl) );

            }
            return retVal;
        }

        public void setParentInfo( List<(string id, string name, string sortName, string displayText, string url)> parentInfo )
        {
            ParentIds = [];

            var parentNamesText = new List<string>();
            var parentNamesSortBy = new List<string>();
            var parentUrls = new List<string>();

            foreach( var currParentInfo in parentInfo )
            {
                ParentIds.Add( currParentInfo.id );
                parentNamesText.Add( currParentInfo.displayText );
                parentNamesSortBy.Add( currParentInfo.sortName );

                if( !currParentInfo.url.IsEmpty() )
                    parentUrls.Add( currParentInfo.url );
            }

            ParentName = new() { SortBy = parentNamesSortBy.Join( ", " ), Text = parentNamesText.Join( ", " ) };
            ParentUrl = parentUrls.Join( "\n" );
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

        private static MediaItemResponse? SetMissingData( MediaItemResponse retVal, IResultSet row, ref int col, DateTimeOffset? premiereDate, bool isEpisode, Func<string, int, bool> isSeriesFiltered, Func<string, bool> isCollectionFiltered, Func<string, SortByText?> getCollectionName )
        {
            if( col != (int)Statistics2026DB.EMediaListResponseColumns.ePosterPath )
                throw new Exception( "SetMissingData called with the wrong column" );

            var tmdbPosterPath = row.GetString( col++ ); // blank for onServer
            var seasonNum = row.GetInt( col++ );
            var episodeNum = row.GetInt( col++ );
            var parentId = row.GetString( col++ );  // blank for onServer

            if( !isEpisode )
            {
                List<SortByText> collectionNames = [];
                foreach( var currParentId in retVal.ParentIds )
                {
                    var currCollectionNames = getCollectionName( currParentId );
                    if( currCollectionNames != null )
                    {
                        collectionNames.Add( currCollectionNames );
                    }
                }

                retVal.ParentName = new();
                foreach( var curr in collectionNames )
                {
                    retVal.ParentName += curr;
                }
            }


            var futureDate = InvalidDateForMissingAnalysis( premiereDate, true );
            if( !tmdbPosterPath.IsNullOrEmpty() )
            {
                var tmp = string.Empty;
                (retVal.ItemUrl, retVal.DisplayName.Text, tmp) = HelperFuncs.GetTMDBUrl( tmdbPosterPath, retVal.DisplayName.Text );
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
                var premiereYear = premiereDate?.Year ?? 0;
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
                    var daysTo = Math.Ceiling( ( premiereDate.Value - DateTimeOffset.Now ).TotalDays );
                    if( daysTo == 0 && premiereDate.Value.Date != DateTimeOffset.Now.Date )
                        daysTo = 1;
                    var msg = $"Available in {daysTo} day";
                    if( daysTo != 1 )
                        msg += "s";
                    retVal.SearchLocation = new SortByText( msg );
                    retVal.PremiereDate = premiereDate.Value.ToString( "dddd" ) + $" {retVal.PremiereDate}";
                }
            }
            else if( !string.IsNullOrEmpty( Plugin.Instance!.Configuration.searchLocation ) )
            {
                if( premiereDate != null )
                {
                    if( premiereDate.Value.LocalDateTime.Date == DateTimeOffset.Now.Date )
                    {
                        retVal.PremiereDate = "Today" + " " + retVal.PremiereDate;
                    }
                    else if( premiereDate.Value.LocalDateTime.Date == DateTimeOffset.Now.Date.AddDays( -1 ) )
                    {
                        retVal.PremiereDate = "Yesterday" + " " + retVal.PremiereDate;
                    }
                }
                var searchUrl = Plugin.Instance!.Configuration.searchLocation;
                if( !searchUrl.EndsWith( "=" ) )
                    searchUrl += "?q=";
                searchUrl += searchKey;

                if( !subKey.IsEmpty() )
                    searchUrl += $"{subKey}";

                var sortBy = searchKey;
                if( !subKey.IsEmpty() )
                    sortBy += $" - {subKey}";

                retVal.SearchLocation.SortBy = sortBy;

                var displayText = $"Click to Search for '{sortBy}'";
                var searchLocation = $"<a is=\"emby-linkbutton\" href=\"{searchUrl}\" target=\"_blank\" rel=\"noopener noreferrer\" title=\"Search for {sortBy}\">{displayText}</a>";
                retVal.SearchLocation.Text = searchLocation;
            }
            return retVal;
        }

        private static MediaItemResponse? AddStreamingProviders( MediaItemResponse retVal, string isoCode, string streamingProvidersString, EmbyInterfaces embyInterfaces, Statistics2026DB db )
        {
            List<int>? streamingProviders = null;

            var allStreamingProviders = embyInterfaces.JsonSerializer().DeserializeFromString<Dictionary<string, List<int>>>( streamingProvidersString );
            if( allStreamingProviders != null )
            {
                if( allStreamingProviders.TryGetValue( isoCode, out var streamingProvidersList ) )
                {
                    streamingProviders = streamingProvidersList;
                }
            }

            if( streamingProviders == null )
                streamingProviders = [];

            PluginConfiguration? config = Plugin.Instance?.Configuration ?? null;

            var providerInfos = db.GetStreamingProviders( streamingProviders, ( config?.alwaysShowJustWatch ?? true ) );

            var depth = 0;
            var html = HelperFuncs.addToHtml( depth++, "<ul>" );
            List<string> providerNames = [];
            int maxNumProviders = config?.maxNumStreamingProviders ?? 3;
            for( int ii = 0; ( ( maxNumProviders == -1 ) || ( ii < maxNumProviders ) ) && ii < providerInfos.Count(); ++ii )
            {
                var provider = providerInfos[ ii ];
                var providerUrl = provider.TargetUrl( retVal.DisplayName.SortBy, isoCode );

                providerNames.Add( provider.ProviderName );

                var htmlTemplate =
                    "<a is=\"emby-linkbutton\" target=\"_blank\" referrerpolicy=\"no-referrer\" rel=\"noopener noreferrer\" href=\"<TARGET_URL>\" class=\"emby-button emby-button-focusscale\">" +
                    "   <TARGET_TEXT>" +
                    "</a>";
                var (imageUrl, itemText, itemUrl) = provider.ImageUrl( targetUrl: providerUrl, targetHtml: htmlTemplate, imageHeight: "45px" );
                html += HelperFuncs.addToHtml( depth++, "<li>" );
                html += HelperFuncs.addToHtml( depth, itemUrl );
                html += HelperFuncs.addToHtml( --depth, "</li>" );
            }
            html += HelperFuncs.addToHtml( --depth, "</ul>" );

            retVal.StreamingProviders = new SortByText( providerNames.Join( ", " ), html );
            return retVal;
        }

        public static MediaItemResponse? CreateFromStatementRow( IResultSet row, bool isEpisode, string isoCode, Func<string, int, bool> isSeriesFiltered, Func<string, bool> isCollectionFiltered, Func<string, SortByText?> getCollectionName, EmbyInterfaces? embyInterfaces, Statistics2026DB db )
        {
            if( embyInterfaces == null )
                return null;

            var col = 0;
            var isMissing = row.GetBoolean( col++ );
            var title = CleanSortName( row.GetString( col++ ) );
            var sortName = row.GetString( col++ );

            var premiereDate = HelperFuncs.ReadDateTime( row.GetString( col++ ) ) ?? DateTimeOffset.MinValue;
            var studioNames = HelperFuncs.JsonToStringArray( row.GetString( col++ ) ).Join( ", " );
            var streamingProvidersString = row.GetString( col++ );

            var resolutionDetail = row.GetString( col++ ); // blank for missing
            var codec = row.GetString( col++ ); // blank for missing
            var dolbyVisionProfile = row.GetString( col++ ); // blank for missing
            var serverLocation = row.GetString( col++ ); // blank for missing
            var itemId = row.GetString( col++ ); // blank for missing
            var itemImageUrl = row.GetString( col++ ); // blank for missing

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
                retVal.ItemUrl = ItemImageUrl.ItemUrlFromItemId( itemId, itemImageUrl, title );


            if( isMissing )
            {
                var parentInfoJSON = row.GetString( col++ );
                var seasonNum = row.GetInt( (int)EMediaListResponseColumns.eSeasonNum );

                var parentInfo = getParentInfo( parentInfoJSON, 
                    ( string id, string name, string sortName ) =>
                    {
                        if( isEpisode )
                        {
                            return isSeriesFiltered( id, seasonNum );
                        }
                        else
                        {
                            return isCollectionFiltered( name ) || isCollectionFiltered( sortName );
                        }
                    }
                );

                if( parentInfo == null )
                    return null;

                retVal.setParentInfo( parentInfo );

                retVal = SetMissingData( retVal, row, ref col, premiereDate, isEpisode, isSeriesFiltered, isCollectionFiltered, getCollectionName );
                if( retVal == null )
                    return null;
            }

            retVal = AddStreamingProviders( retVal, isoCode, streamingProvidersString, embyInterfaces, db );

            return retVal;
        }
    }
}
