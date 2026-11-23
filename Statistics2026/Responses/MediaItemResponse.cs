using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Configuration;
using Statistics2026.Data;
using Statistics2026.Utilities;
using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static Statistics2026.Data.Statistics2026DB;

namespace Statistics2026.Responses
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
        public string Extra { get; set; } = string.Empty;
        public string Extra2 { get; set; } = string.Empty;

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

        public static async Task<List<(string id, string name, string sortName, string displayText, string url)>?> getParentInfo( string parentJson, Func<string, string, string, Task<bool>> isFiltered )
        {
            List<(string id, string name, string sortName, string displayText, string url)> retVal = [];

            if( parentJson.IsNullOrEmpty() || parentJson.IsWhiteSpace() )
                return retVal;

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
                JsonNode? parentNode = parentArray[ ii ];
                if( parentNode == null )
                    continue;

                string name = parentNode[ "name" ]?.GetValue<string>() ?? string.Empty;
                string sortName = parentNode[ "sort_name" ]?.GetValue<string>() ?? string.Empty;
                string id = parentNode[ "id" ]?.GetValue<string>() ?? string.Empty;
                string url = parentNode[ "url" ]?.GetValue<string>() ?? string.Empty;
                if( name.IsNullOrEmpty() || sortName.IsNullOrEmpty() )
                    continue;

                if( await isFiltered( id, name, sortName ) )
                    return null;

                string parentUrl = string.Empty;
                string displayText = name;

                string urlName = name;
                if( ii != 0 )
                    urlName = $", {urlName}";
                string currParentUrl = ItemImageUrl.ItemUrlFromItemId( id, url, urlName ) ?? string.Empty;

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

            foreach( (string id, string name, string sortName, string displayText, string url) currParentInfo in parentInfo )
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

        private static string CleanSortName( string sortName )
        {
            string retVal = Regex.Replace( sortName, @"^[^a-zA-Z0-9]+", "" );
            return retVal;
        }

        private static SortByText getSearchData( string searchLink, string searchLocationName, string searchKey, string subKey )
        {
            string searchUrl = searchLink;
            if( !searchUrl.EndsWith( "=" ) )
                searchUrl += "?q=";
            searchUrl += searchKey;

            if( !subKey.IsEmpty() )
                searchUrl += $" {subKey}";

            string sortBy = searchKey;
            if( !subKey.IsEmpty() )
                sortBy += $" - {subKey}";

            if( searchLocationName.IsEmpty() )
            {
                if( !string.IsNullOrWhiteSpace( searchLink ) )
                {
                    var uri = new System.Uri( searchLink );
                    searchLocationName = uri.Host;
                }
            }

            string displayText = $"{( string.IsNullOrWhiteSpace( searchLocationName ) ? "" : $"{searchLocationName} - " )}Click to Search for '{sortBy}'";
            string searchLocation = $"<a is=\"emby-linkbutton\" href=\"{searchUrl}\" target=\"_blank\" rel=\"noopener noreferrer\" title=\"Search for {sortBy}\">{displayText}</a>";

            var retVal = new SortByText()
            {
                SortBy = sortBy,
                Text = searchLocation
            };

            return retVal;
        }

        private static MediaItemResponse? SetMissingData( MediaItemResponse retVal, IResultSet row, ref int col, DateTimeOffset? premiereDate, bool isEpisode, Func<string, int, Task<bool>> isSeriesFiltered, Func<string, bool> isCollectionFiltered, Func<string, Task<SortByText?>> getCollectionName )
        {
            if( col != (int)Statistics2026DB.EMediaListResponseColumns.ePosterPath )
                throw new Exception( "SetMissingData called with the wrong column" );

            string tmdbPosterPath = row.GetString( col++ ); // blank for onServer
            int seasonNum = row.GetInt( col++ );
            int episodeNum = row.GetInt( col++ );
            string parentId = row.GetString( col++ );  // blank for onServer

            bool futureDate = HelperFuncs.InvalidDateForMissingAnalysis( premiereDate, null );
            if( !tmdbPosterPath.IsNullOrEmpty() )
            {
                string tmp = string.Empty;
                (retVal.ItemUrl, retVal.DisplayName.Text, tmp) = HelperFuncs.GetTMDBLink( tmdbPosterPath, retVal.DisplayName.Text );
            }

            string searchKey = retVal.DisplayName.SortBy;
            string subKey = string.Empty;
            if( isEpisode )
            {
                if( retVal.ParentName.SortBy.IsNullOrEmpty() )
                {
                    return null;
                }

                searchKey = retVal.ParentName.SortBy;
                if( seasonNum != 0 )
                    subKey += $"S{seasonNum:D2}";
                if( episodeNum != 0 )
                    subKey += $"E{episodeNum:D2}";
            }
            else
            {
                int premiereYear = premiereDate?.Year ?? 0;
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
                    double daysTo = Math.Ceiling( ( premiereDate.Value - DateTimeOffset.Now ).TotalDays );
                    if( daysTo == 0 && premiereDate.Value.Date != DateTimeOffset.Now.Date )
                        daysTo = 1;
                    string msg = $"Available in {daysTo} day";
                    if( daysTo != 1 )
                        msg += "s";
                    retVal.SearchLocation = new SortByText( msg );
                    retVal.PremiereDate = premiereDate.Value.ToString( "dddd" ) + $" {retVal.PremiereDate}";
                }
            }
            else
            {
                bool canSearch = Plugin.Instance!.Configuration.CanSearch( isEpisode );
                if( canSearch )
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
                }

                List<SortByText> searchData = [];
                if( Plugin.Instance!.Configuration.ShowSearchTheMovieDB( isEpisode ) )
                {
                    searchData.Add( getSearchData( "https://www.themoviedb.org/search?query=", "The MovieDB", searchKey, string.Empty ) );
                }

                if( Plugin.Instance!.Configuration.HasCustomSearches( isEpisode ) )
                {
                    foreach( string curr in Plugin.Instance!.Configuration.CustomSearchLocations( isEpisode ) )
                    {
                        if( string.IsNullOrWhiteSpace( curr ) )
                        {
                            continue;
                        }
                        searchData.Add( getSearchData( curr, string.Empty, searchKey, subKey ) );
                    }
                }

                retVal.SearchLocation = new();
                if( searchData.Count == 1 )
                {
                    retVal.SearchLocation = searchData[ 0 ];
                }
                else if( searchData.Count > 1 )
                {
                    int depth = 0;
                    retVal.SearchLocation.SortBy = searchData[ 0 ].SortBy;
                    retVal.SearchLocation.Text = HelperFuncs.addToHtml( depth++, "<ul>" );

                    foreach( SortByText curr in searchData )
                    {
                        retVal.SearchLocation.Text += HelperFuncs.addToHtml( depth++, "<li>" );
                        retVal.SearchLocation.Text += HelperFuncs.addToHtml( depth, curr.Text );
                        retVal.SearchLocation.Text += HelperFuncs.addToHtml( --depth, "</li>" );
                    }
                    retVal.SearchLocation.Text += HelperFuncs.addToHtml( --depth, "</ul>" );
                }
            }

            return retVal;
        }

        private static async Task<MediaItemResponse?> AddStreamingProviders( MediaItemResponse retVal, bool? isEpisode, string isoCode, string streamingProvidersString, EmbyInterfaces embyInterfaces, Statistics2026DB db, bool locked )
        {
            List<int>? streamingProviders = null;

            Dictionary<string, List<int>> allStreamingProviders = embyInterfaces.JsonSerializer().DeserializeFromString<Dictionary<string, List<int>>>( streamingProvidersString );
            if( allStreamingProviders != null )
            {
                if( allStreamingProviders.TryGetValue( isoCode, out List<int>? streamingProvidersList ) )
                {
                    streamingProviders = streamingProvidersList;
                }
            }

            if( streamingProviders == null )
                streamingProviders = [];

            PluginConfiguration? config = Plugin.Instance?.Configuration ?? null;

            List<StreamingProvider> providerInfos = await db.GetStreamingProviders( streamingProviders, ( config?.AlwaysShowJustWatch( isEpisode ?? true ) ?? true ), locked );

            int depth = 0;
            string html = HelperFuncs.addToHtml( depth++, "<ul>" );
            List<string> providerNames = [];
            int maxNumProviders = config?.MaxNumStreamingProviders( isEpisode ?? true ) ?? 3;
            for( int ii = 0; ( ( maxNumProviders == -1 ) || ( ii < maxNumProviders ) ) && ii < providerInfos.Count(); ++ii )
            {
                StreamingProvider provider = providerInfos[ ii ];
                string providerUrl = provider.TargetUrl( retVal.DisplayName.SortBy, isoCode );

                providerNames.Add( provider.ProviderName );

                string htmlTemplate =
                    "<a is=\"emby-linkbutton\" target=\"_blank\" referrerpolicy=\"no-referrer\" rel=\"noopener noreferrer\" href=\"<TARGET_URL>\" class=\"emby-button emby-button-focusscale\">" +
                    "   <TARGET_TEXT>" +
                    "</a>";
                (string? imageUrl, string? itemText, string? itemUrl) = provider.ImageUrl( targetUrl: providerUrl, targetHtml: htmlTemplate, imageHeight: "45px" );
                html += HelperFuncs.addToHtml( depth++, "<li>" );
                html += HelperFuncs.addToHtml( depth, itemUrl );
                html += HelperFuncs.addToHtml( --depth, "</li>" );
            }
            html += HelperFuncs.addToHtml( --depth, "</ul>" );

            retVal.StreamingProviders = new SortByText( providerNames.Join( ", " ), html );
            return retVal;
        }

        // if isEpisode is null its a series item, true its an episode item, false movie
        public static async Task<MediaItemResponse?> CreateFromStatementRow( IResultSet row, bool? isEpisode, string isoCode, Func<string, int, Task<bool>>? isSeriesFiltered, Func<string, bool>? isCollectionFiltered, Func<string, Task<SortByText?>>? getCollectionName, EmbyInterfaces? embyInterfaces, Statistics2026DB db )
        {
            if( embyInterfaces == null )
                return null;

            int col = 0;
            bool isMissing = row.GetBoolean( col++ );
            string title = CleanSortName( row.GetString( col++ ) );
            string sortName = row.GetString( col++ );

            DateTimeOffset premiereDate = HelperFuncs.ReadDateTime( row.GetString( col++ ) ) ?? DateTimeOffset.MinValue;
            string studioNames = HelperFuncs.JsonToStringArray( row.GetString( col++ ) ).Join( ", " );
            string streamingProvidersString = row.GetString( col++ );

            string resolutionDetail = row.GetString( col++ ); // blank for missing
            string codec = row.GetString( col++ ); // blank for missing
            string dolbyVisionProfile = row.GetString( col++ ); // blank for missing
            string serverLocation = row.GetString( col++ ); // blank for missing
            string itemId = row.GetString( col++ ); // blank for missing
            string itemImageUrl = row.GetString( col++ ); // blank for missing

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


            string parentInfoJSON = row.GetString( col++ );
            int seasonNum = row.GetInt( (int)EMediaListResponseColumns.eSeasonNum );

            List<(string id, string name, string sortName, string displayText, string url)>? parentInfo = await getParentInfo( parentInfoJSON,
                async ( string id, string name, string sortName ) =>
                {
                    if( isMissing && ( isEpisode != null ) )
                    {
                        if( isEpisode.Value && ( isSeriesFiltered != null ) )
                        {
                            return await isSeriesFiltered( id, seasonNum );
                        }
                        else if( isCollectionFiltered != null )
                        {
                            return isCollectionFiltered( name ) || ( ( sortName != name ) && isCollectionFiltered( sortName ) ) || isCollectionFiltered( title );
                        }
                    }
                    return false;
                }
            );

            if( parentInfo == null )
            {
                if( isEpisode != null )
                    return null;
            }
            if( isEpisode != null )
                retVal.setParentInfo( parentInfo! );

            if( isMissing && isEpisode != null )
            {
                retVal = SetMissingData( retVal, row, ref col, premiereDate, isEpisode.Value, isSeriesFiltered!, isCollectionFiltered!, getCollectionName! );
                if( retVal == null )
                    return null;
            }
            retVal = await AddStreamingProviders( retVal, isEpisode, isoCode, streamingProvidersString, embyInterfaces, db, true );

            return retVal;
        }
    }
}
