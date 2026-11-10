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

        public static List<string> JsonToStringArray( string json )
        {
            try
            {
                var node = JsonNode.Parse( json );
                if( node == null )
                    return [];

                var retVal = new List<string>();
                if( node is JsonArray jsonArray )
                {
                    foreach( var studio in jsonArray )
                    {
                        if( studio == null )
                            continue;
                        retVal.Add( studio.ToString() );
                    }
                }
                return retVal;
            }
            catch( Exception )
            {
                return json.Split( ',' ).ToList();
            }
        }

        static string? ServerCountryCode = null;

        public static string GetServerCountryCode( IHttpClient httpClient )
        {
            if( ServerCountryCode != null )
                return ServerCountryCode;

            RegionInfo currentRegion = RegionInfo.CurrentRegion;
            var defaultIsoCode = currentRegion.TwoLetterISORegionName;
            var isoCode = HelperFuncs.GetServerCountryCodeAsync( httpClient, defaultIsoCode ).GetAwaiter().GetResult();
            return isoCode;
        }

        public static async Task<string> GetServerCountryCodeAsync( IHttpClient httpClient, string defaultCountryCode = "US" )
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
                var options = new HttpRequestOptions
                {
                    Url = url,
                    CancellationToken = CancellationToken.None,
                    // Optional: Set cache timeout if you don't want to hit TMDB constantly
                    CacheLength = System.TimeSpan.FromDays( 1 ),
                    CacheMode = CacheMode.None
                };

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
            { 1, "https://vudu.com{encoded_title}" },
            { 2, "https://apple.com{encoded_title}" },
            { 3, "https://google.com{encoded_title}&c=movies" },
            { 7, "https://viki.com{encoded_title}" },
            { 8, "https://netflix.com{encoded_title}" },
            { 9, "https://amazon.com{encoded_title}&i=instant-video" },
            { 10, "https://amazon.com{encoded_title}&i=movies-tv" },
            { 11, "https://mubi.com{encoded_title}" },
            { 15, "https://hulu.com{encoded_title}" },
            { 26, "https://youtube.com{encoded_title}" },
            { 34, "https://mgmplus.com{encoded_title}" },
            { 35, "https://rakuten.tv{encoded_title}" },
            { 37, "https://paramountplus.com{encoded_title}" },
            { 43, "https://starz.com{encoded_title}" },
            { 68, "https://microsoft.com{encoded_title}&form=apps" },
            { 73, "https://tubitv.com/search/{encoded_title}" },
            { 76, "https://viaplay.com{encoded_title}" },
            { 84, "https://unext.jp{encoded_title}" },
            { 100, "https://bbc.co.uk{encoded_title}" },
            { 118, "https://www.max.com/search/{encoded_title}/" },
            { 119, "https://amazon.com{encoded_title}&i=instant-video" },
            { 124, "https://crave.ca{encoded_title}" },
            { 130, "https://sky.com{encoded_title}" },
            { 155, "https://cineplex.com{encoded_title}" },
            { 175, "https://netflix.com{encoded_title}" },
            { 177, "https://pantaflix.com{encoded_title}" },
            { 182, "https://hollywoodsuite.ca{encoded_title}" },
            { 190, "https://curiositystream.com/search/{encoded_title}" },
            { 192, "https://youtube.com{encoded_title}" },
            { 200, "https://www.film1.nl/zoeken/{encoded_title}" },
            { 207, "https://therokuchannel.roku.com/search/{encoded_title}" },
            { 209, "https://britbox.com{encoded_title}" },
            { 210, "https://amcplus.com{encoded_title}" },
            { 212, "https://hoopladigital.com{encoded_title}" },
            { 215, "https://kanopy.com{encoded_title}" },
            { 232, "https://www.max.com/search/{encoded_title}/" },
            { 234, "https://hotstar.com{encoded_title}" },
            { 243, "https://vudu.com{encoded_title}" },
            { 257, "https://fubo.tv{encoded_title}" },
            { 258, "https://criterionchannel.com{encoded_title}" },
            { 268, "https://www.dazn.com/en-US/search/{encoded_title}" },
            { 283, "https://crunchyroll.com{encoded_title}" },
            { 300, "https://pluto.tv/search/details/movies/{encoded_title}" },
            { 309, "https://sunnxt.com{encoded_title}" },
            { 315, "https://hoichoi.tv{encoded_title}" },
            { 329, "https://www.jiocinema.com/search/{encoded_title}" },
            { 337, "https://disneyplus.com{encoded_title}" },
            { 341, "https://zee5.com{encoded_title}" },
            { 350, "https://apple.com{encoded_title}" },
            { 352, "https://docplay.com{encoded_title}" },
            { 358, "https://www.dailymotion.com/search/{encoded_title}" },
            { 384, "https://www.max.com/search/{encoded_title}/" },
            { 386, "https://peacocktv.com{encoded_title}" },
            { 387, "https://peacocktv.com{encoded_title}" },
            { 393, "https://altt.co.in{encoded_title}" },
            { 396, "https://shoutfactorytv.com{encoded_title}" },
            { 424, "https://www.flixfling.com/search/{encoded_title}" },
            { 429, "https://cineverse.com{encoded_title}" },
            { 430, "https://hiyahtv.com{encoded_title}" },
            { 432, "https://flixole.com{encoded_title}" },
            { 436, "https://fetchtv.com.au{encoded_title}" },
            { 444, "https://topic.com{encoded_title}" },
            { 445, "https://paramountplus.com{encoded_title}" },
            { 454, "https://stan.com.au{encoded_title}" },
            { 464, "https://kinonow.com{encoded_title}" },
            { 475, "https://docsville.com{encoded_title}" },
            { 521, "https://erosnow.com{encoded_title}" },
            { 531, "https://paramountplus.com{encoded_title}" },
            { 546, "https://wowpresentsplus.com{encoded_title}" },
            { 551, "https://magellantv.com{encoded_title}" },
            { 554, "https://broadpix.com{encoded_title}" },
            { 559, "https://filmzie.com/search/{encoded_title}" },
            { 567, "https://truestory.film{encoded_title}" },
            { 569, "https://cultpix.com{encoded_title}" },
            { 606, "https://dekkoo.com{encoded_title}" },
            { 613, "https://redbox.com{encoded_title}" },
            { 632, "https://broadwayhd.com{encoded_title}" },
            { 633, "https://filmatique.com{encoded_title}" },
            { 634, "https://nightflightplus.com{encoded_title}" },
            { 677, "https://classixapp.com{encoded_title}" },
            { 692, "https://arrow-player.com{encoded_title}" },
            { 1773, "https://plex.tv{encoded_title}" },
            { 1794, "https://viendomovies.com{encoded_title}" },
            { 1796, "https://semillitas.tv{encoded_title}" },
            { 1853, "https://darkmatteritv.com{encoded_title}" },
            { 1960, "https://amazon.com{encoded_title}&i=freevee-tv" },
            { 1962, "https://retrocrush.tv{encoded_title}" },
            { 1967, "https://midnightpulp.com{encoded_title}" },
            { 1973, "https://asiancrush.com{encoded_title}" },
            { 1985, "https://amazon.co.uk{encoded_title}&i=freevee-tv" },
            { 2014, "https://cocoatv.com{encoded_title}" },
            { 2016, "https://reby.com{encoded_title}" },
            { 2065, "https://freemoviesplus.com{encoded_title}" },
            { 2186, "https://fandor.com{encoded_title}" },
            { 2187, "https://screambox.com{encoded_title}" },
            { 2188, "https://cinehouse.tv{encoded_title}" },
        };

        public static string GetProviderTemplateUrl( int providerId, string title )
        {
            string encodedTitle = System.Uri.EscapeDataString( title );

            if( ProviderTemplates.TryGetValue( providerId, out var template ) )
            {
                return template.Replace( "{encoded_title}", encodedTitle );
            }

            // Fallback default system lookup engine if provider id isn't mapped
            return $"https://google.com+{encodedTitle}";
        }


        public static string GetProviderTemplateUrl( int providerId )
        {
            if( ProviderTemplates.TryGetValue( providerId, out var template ) )
            {
                return template;
            }

            return string.Empty;
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

            retVal += addToHtml( depth++, "<ul>" );
            foreach( var curr in values )
            {
                retVal += addToHtml( depth, $"<li>{curr}</li>" );
            }
            retVal += addToHtml( --depth, "</ul>" );
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

        public static SemaphoreSlim CreateSemaphoreSlim()
        {
#if DEBUG_PLUGIN_ASYNC
            return new SemaphoreSlim( 1, 1 );
#else
            return new SemaphoreSlim( 10, 10 );
#endif
        }
    }
}
