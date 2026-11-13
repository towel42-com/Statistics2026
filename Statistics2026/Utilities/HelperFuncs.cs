#if DEBUG
#define DEBUG_PLUGIN
//#define DEBUG_PLUGIN_ASYNC
//#define DEBUG_DB
#endif

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using ServiceStack;
using SQLitePCL;
using SQLitePCL.pretty;
using Statistics2026.Secrets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Nodes;
using System.Threading;
using System.Web;
using System.Xml;
using TimeZoneNames;

using static Statistics2026.Utilities.StatGen;

namespace Statistics2026.Utilities
{
    public static class HelperFuncs
    {
        public static void AttachDebugger()
        {
#if DEBUG_PLUGIN
            // Check if a debugger is already attached
            if( !Debugger.IsAttached )
            {
                // This forces the server startup thread to halt and pop up a
                // "Choose Just-In-Time Debugger" window or wait for an attachment
                Debugger.Launch();
            }
            else
            {
                // If already attached, instantly trigger a breakpoint break
                Debugger.Break();
            }
#endif
        }

        public static void PreDebugRun( EmbyInterfaces embyInterfaces )
        {
#if DEBUG_PLUGIN
            if( !Debugger.IsAttached )
            {
                AttachDebugger();
            }

            EmbyHelperFuncs.MoveMediaLocations( embyInterfaces ).GetAwaiter().GetResult();
#endif
        }

        private static readonly string[] _datetimeFormats = new string[] {
            "THHmmssK",
            "THHmmK",
            "HH:mm:ss.FFFFFFFK",
            "HH:mm:ssK",
            "HH:mmK",
            "yyyy-MM-dd HH:mm:ss.FFFFFFFK", /* NOTE: UTC default (5). */
            "yyyy-MM-dd HH:mm:ssK",
            "yyyy-MM-dd HH:mmK",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
            "yyyy-MM-ddTHH:mmK",
            "yyyy-MM-ddTHH:mm:ssK",
            "yyyyMMddHHmmssK",
            "yyyyMMddHHmmK",
            "yyyyMMddTHHmmssFFFFFFFK",
            "THHmmss",
            "THHmm",
            "HH:mm:ss.FFFFFFF",
            "HH:mm:ss",
            "HH:mm",
            "yyyy-MM-dd HH:mm:ss.FFFFFFF", /* NOTE: Non-UTC default (19). */
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyyMMddHHmmss",
            "yyyyMMddHHmm",
            "yyyyMMddTHHmmssFFFFFFF",
            "yyyy-MM-dd",
            "yyyyMMdd",
            "yy-MM-dd",
            "o"
        };
        private static readonly string _datetimeFormatUtc = _datetimeFormats[ 5 ];
        private static readonly string _datetimeFormatLocal = _datetimeFormats[ 19 ];

        private static string GetDateTimeKindFormat( DateTimeKind kind )
        {
            return ( kind == DateTimeKind.Utc ) ? _datetimeFormatUtc : _datetimeFormatLocal;
        }

        public static DateTimeOffset? ReadDateTime( string dateText )
        {
            try
            {
                return DateTimeOffset.ParseExact(
                    dateText,
                    _datetimeFormats,
                    DateTimeFormatInfo.InvariantInfo,
                    DateTimeStyles.None ).ToUniversalTime();
            }
            catch
            {
                return null;
            }
        }

        public static string? ToDateTimeParamValue( DateTimeOffset? dateValue )
        {
            if( dateValue == null )
                return null;

            var tmp = dateValue.Value.DateTime.ToString( "o", CultureInfo.InvariantCulture );

            return dateValue.Value.UtcDateTime.ToString( GetDateTimeKindFormat( DateTimeKind.Utc ) );
        }

        public static string StreamToString( Stream stream )
        {
            if( stream.CanSeek )
            {
                stream.Position = 0;
            }

            using( var reader = new StreamReader( stream ) )
            {
                return reader.ReadToEnd();
            }
        }

        public static string ToTZShortString( this DateTimeOffset dto, bool local, bool dateOnly = false )
        {
            var tz = local ? TimeZoneInfo.Local : TimeZoneInfo.Utc;
            var abbreviations = TZNames.GetAbbreviationsForTimeZone( tz.Id, "en" );

            var isDaylight = tz.IsDaylightSavingTime( dto );
            var tzAbbreviation = isDaylight ? abbreviations.Daylight : abbreviations.Standard;

            var dt = local ? dto.LocalDateTime : dto.UtcDateTime;

            var retVal = dt.ToShortDateString();
            if( !dateOnly )
                retVal += " " + dt.ToString( "HH:mm" ) + $" {tzAbbreviation}";
            return retVal;
        }

        public static List<string> JsonToStringArray( string json, char splitter = ',' )
        {
            List<string> retVal = [];
            try
            {
                var node = JsonNode.Parse( json );
                if( node != null && ( node is JsonArray jsonArray ) )
                {
                    foreach( var studio in jsonArray )
                    {
                        if( studio == null )
                            continue;
                        retVal.Add( studio.ToString() );
                    }
                }
            }
            catch( Exception )
            {
                if( !json.IsNullOrEmpty() )
                    retVal = json.Split( splitter ).ToList();
            }

            retVal = retVal.Where( s => !string.IsNullOrWhiteSpace( s ) ).Select( s => s.Trim() ).ToList();
            return retVal;
        }

        static string? ServerCountryCode = null;

        public static string GetServerCountryCode( IHttpClient httpClient, CancellationToken? _cancellationToken = null )
        {
            if( ServerCountryCode != null )
                return ServerCountryCode;

            RegionInfo currentRegion = RegionInfo.CurrentRegion;
            var defaultIsoCode = currentRegion.TwoLetterISORegionName;
            var isoCode = HelperFuncs.GetServerCountryCodeAsync( httpClient, defaultIsoCode, _cancellationToken ).GetAwaiter().GetResult();
            return isoCode;
        }

        public static async Task<string> GetServerCountryCodeAsync( IHttpClient httpClient, string defaultCountryCode = "US", CancellationToken? _cancellationToken = null )
        {
            // Provide a default fallback region code in case the network request is rate-limited or fails

            if( ServerCountryCode != null )
                return ServerCountryCode;

            ServerCountryCode = defaultCountryCode;
            if( httpClient == null )
            {
                return ServerCountryCode;
            }

            try
            {
                var builder = new UriBuilder
                {
                    Scheme = "https",
                    Host = "ipapi.co",
                    Path = "/json"
                };

                var url = builder.Uri.ToString();
                var options = getRequestOptions( url, _cancellationToken );

                using( var response = await httpClient.Get( options ).ConfigureAwait( false ) )
                {
                    var rawResponse = HelperFuncs.StreamToString( response );
                    if( !string.IsNullOrWhiteSpace( rawResponse ) )
                    {
                        var jsonNode = JsonNode.Parse( rawResponse );
                        var obj = jsonNode?.AsObject();
                        if( obj == null )
                            return defaultCountryCode;

                        ServerCountryCode = obj[ "country_code" ]?.GetValue<string>() ?? defaultCountryCode;
                    }
                }
            }
            catch( System.Exception )
            {
            }

            return ServerCountryCode;
        }

        public static string JoinClauses( List<string> clauses )
        {
            if( clauses.IsNullOrEmpty() )
                return string.Empty;

            for( var ii = 0; ii < clauses.Count; ++ii )
            {
                clauses[ ii ] = $"( {clauses[ ii ]} )";
            }

            return " WHERE " + string.Join( " AND ", clauses ) + " ";
        }

        static private readonly Dictionary<int, string> ProviderTemplates = new()
        {
            {  0, "https://justwatch.com/{isoCode}/search?q={searchInfo}" },
            {  1, "https://vudu.com" },
            {  2, "https://tv.apple.com/{isoCode}/search?term={searchInfo}" },
            {  3, "https://tv.google" },
            {  7, "https://viki.com" },
            {  8, "https://netflix.com/browse?q={searchInfo}" },
            {  9, "https://www.amazon.com/gp/video/storefront?k={searchInfo}" },
            { 10, "https://www.amazon.com/gp/video/storefront?k={searchInfo}" },
            { 11, "https://mubi.com" },
            { 15, "https://hulu.com" },
            { 26, "https://tv.youtube.com" },
            { 34, "https://mgmplus.com" },
            { 35, "https://rakuten.tv" },
            { 37, "https://paramountplus.com" },
            { 43, "https://starz.com" },
            { 68, "https://microsoft.com" },
            { 73, "https://tubitv.com" },
            { 76, "https://viaplay.com" },
            { 84, "https://unext.jp" },
            { 100, "https://bbc.co.uk" },
            { 118, "https://www.hbomax.com/search?q={searchInfo}" },
            { 119, "https://www.amazon.com/gp/video/storefront?k={searchInfo}" },
            { 124, "https://crave.ca" },
            { 130, "https://sky.com" },
            { 155, "https://cineplex.com" },
            { 175, "https://netflix.com/browse?q={searchInfo}" },
            { 177, "https://pantaflix.com" },
            { 182, "https://hollywoodsuite.ca" },
            { 190, "https://curiositystream.com/search" },
            { 192, "https://tv.youtube.com" },
            { 200, "https://www.film1.nl/zoeken" },
            { 207, "https://therokuchannel.roku.com/search" },
            { 209, "https://britbox.com" },
            { 210, "https://amcplus.com" },
            { 212, "https://hoopladigital.com" },
            { 215, "https://kanopy.com" },
            { 232, "https://www.hbomax.com/search?q={searchInfo}" },
            { 234, "https://hotstar.com" },
            { 243, "https://vudu.com" },
            { 257, "https://fubo.tv" },
            { 258, "https://criterionchannel.com" },
            { 268, "https://www.dazn.com/en-US/search" },
            { 283, "https://crunchyroll.com" },
            { 300, "https://pluto.tv/search/details/movies" },
            { 309, "https://sunnxt.com" },
            { 315, "https://hoichoi.tv" },
            { 329, "https://www.jiocinema.com/search" },
            { 337, "https://disneyplus.com" },
            { 341, "https://zee5.com" },
            { 350, "https://tv.apple.com/{isoCode}/search?term={searchInfo}" },
            { 352, "https://docplay.com" },
            { 358, "https://www.dailymotion.com/search" },
            { 384, "https://www.hbomax.com/search?q={searchInfo}" },
            { 386, "https://peacocktv.com" },
            { 387, "https://peacocktv.com" },
            { 393, "https://altt.co.in" },
            { 396, "https://shoutfactorytv.com" },
            { 424, "https://www.flixfling.com/search" },
            { 429, "https://cineverse.com" },
            { 430, "https://hiyahtv.com" },
            { 432, "https://flixole.com" },
            { 436, "https://fetchtv.com.au" },
            { 444, "https://topic.com" },
            { 445, "https://paramountplus.com" },
            { 454, "https://stan.com.au" },
            { 464, "https://kinonow.com" },
            { 475, "https://docsville.com" },
            { 521, "https://erosnow.com" },
            { 531, "https://paramountplus.com" },
            { 546, "https://wowpresentsplus.com" },
            { 551, "https://magellantv.com" },
            { 554, "https://broadpix.com" },
            { 559, "https://filmzie.com/search" },
            { 567, "https://truestory.film" },
            { 569, "https://cultpix.com" },
            { 606, "https://dekkoo.com" },
            { 613, "https://redbox.com" },
            { 632, "https://broadwayhd.com" },
            { 633, "https://filmatique.com" },
            { 634, "https://nightflightplus.com" },
            { 677, "https://classixapp.com" },
            { 692, "https://arrow-player.com" },
            { 1773, "https://plex.tv" },
            { 1794, "https://viendomovies.com" },
            { 1796, "https://semillitas.tv" },
            { 1853, "https://darkmatteritv.com" },
            { 1960, "https://www.amazon.com/gp/video/storefront?k={searchInfo}" },
            { 1962, "https://retrocrush.tv" },
            { 1967, "https://midnightpulp.com" },
            { 1973, "https://asiancrush.com" },
            { 1985, "https://www.amazon.co.uk/gp/video/storefront?k={searchInfo}" },
            { 2014, "https://cocoatv.com" },
            { 2016, "https://reby.com" },
            { 2065, "https://freemoviesplus.com" },
            { 2186, "https://fandor.com" },
            { 2187, "https://screambox.com" },
            { 2188, "https://cinehouse.tv" },
        };

        public static string GetProviderSearchUrl( int providerId, string isoCode, string title )
        {
            string encodedTitle = System.Uri.EscapeDataString( title );

            if( ProviderTemplates.TryGetValue( providerId, out var template ) )
            {
                return template.Replace( "{searchInfo}", encodedTitle ).Replace( "{isoCode}", isoCode );
            }

            encodedTitle = System.Uri.EscapeDataString( title );
            return $"https://justwatch.com/{isoCode}/search?q={encodedTitle}";
        }


        public static string GetProviderTemplateUrl( int providerId )
        {
            if( ProviderTemplates.TryGetValue( providerId, out var template ) )
            {
                return template;
            }

            return "https://justwatch.com/{isoCode}/search?q={searchInfo}";
        }

        public static string addToHtml( int depth, string _html )
        {
            if( _html.IsNullOrEmpty() )
                return string.Empty;

            var retVal = string.Empty;
            if( depth > 0 )
                retVal += new string( ' ', 4 * depth );
            retVal += _html;
            if( !retVal.EndsWith( "\n" ) )
                retVal += "\n";
            return retVal;
        }

        public static string MakeTableHtml( List<string> values, bool inDiv = true, int depth = 0 )
        {
            if( values.IsNullOrEmpty() )
                return string.Empty;

            var retVal = string.Empty;
            if( inDiv )
                retVal += addToHtml( depth++, "<div>" );

            retVal += addToHtml( depth++, "<ul>" );
            foreach( var curr in values )
            {
                retVal += addToHtml( depth, $"<li>{curr}</li>" );
            }
            retVal += addToHtml( --depth, "</ul>" );
            if( inDiv )
                retVal += addToHtml( --depth, "</div>" );
            return retVal;
        }

        public static string MakeTableHtml( string title, List<string> values )
        {
            if( values.IsNullOrEmpty() )
                return string.Empty;

            var depth = 0;
            var retVal = addToHtml( depth++, "</br>\n<div>" );
            if( !title.IsEmpty() )
            {
                retVal += addToHtml( depth, $"<h3>{title}</h3>" );
            }

            retVal += MakeTableHtml( values, false, depth );

            retVal += addToHtml( --depth, "</div>" );
            return retVal;
        }


        public static string PrettyPrintFileSize( this long bytes )
        {
            string[] suffixes = { "bytes", "Kb", "Mb", "Gb", "Tb", "Pb", "Eb" };

            if( bytes == 0 )
                return "0 B";

            long absBytes = Math.Abs( bytes );
            // Calculate the index in the suffixes array
            int place = Convert.ToInt32( Math.Floor( Math.Log( absBytes, 1024 ) ) );
            // Adjust the magnitude
            double num = Math.Round( absBytes / Math.Pow( 1024, place ), 1 );

            // Retain the original sign for negative byte configurations, if applicable
            return $"{Math.Sign( bytes ) * num} {suffixes[ place ]}";
        }

        public static SemaphoreSlim CreateSemaphoreSlim( bool forceDebug = false )
        {
            int numThreads = forceDebug ? 1 : 10;
#if DEBUG_PLUGIN_ASYNC
            numThreads = 1;
#endif

            return new SemaphoreSlim( numThreads, numThreads );
        }

        public static (string imageUrl, string itemText, string itemUrl) GetTMDBUrl( string posterPath, string text, (string targetUrl, string targetHtml)? targetInfo = null, string posterSize = "w300", string? imageHeight = "105px", string? imageWidth = null )
        {
            string imageUrl = string.Empty;
            if( !posterPath.IsNullOrEmpty() && posterPath != "/" )
            {
                if( !posterPath.StartsWith( "/" ) )
                    posterPath = "/" + posterPath;

                imageUrl = $"https://image.tmdb.org/t/p/{posterSize}" + posterPath;
            }
            string itemText = string.Empty;
            if( !imageUrl.IsNullOrEmpty() )
            {
                itemText = $"<img referrerpolicy=\"no-referrer\" loading=\"lazy\" src=\"{imageUrl}\" ";
                if( imageHeight != null )
                {
                    itemText += $"height=\"{imageHeight}\" ";
                }
                if( imageWidth != null )
                {
                    itemText += "width=\"{imageWidth}\" ";
                }
                itemText += "/>";
            }
            itemText += text;

            string itemUrl = string.Empty;
            if( targetInfo != null )
            {
                itemUrl = targetInfo.Value.targetHtml;
                itemUrl = itemUrl.Replace( "<TARGET_URL>", targetInfo.Value.targetUrl );
                itemUrl = itemUrl.Replace( "<TARGET_TEXT>", itemText );
            }
            return (imageUrl, itemText, itemUrl);
        }

        public static HttpRequestOptions getRequestOptions( string url, CancellationToken? _cancellationToken )
        {
            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = _cancellationToken ?? CancellationToken.None,
                CacheLength = System.TimeSpan.FromDays( 30 ),
                CacheMode = CacheMode.Unconditional
            };
            return options;
        }

        public static void RunTimeSummary( long runtimeMS, string taskName, EmbyInterfaces embyInterfaces )
        {
            embyInterfaces.Logger().Info( $"=======================================" );
            embyInterfaces.Logger().Info( $"           {runtimeMS} ms" );
            embyInterfaces.Logger().Info( $"=======================================" );
            embyInterfaces.Logger().Info( $"Finished Statistics 2026 {taskName} task" );
        }
    }
}
