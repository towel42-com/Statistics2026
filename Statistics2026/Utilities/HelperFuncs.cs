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
            if ( !dateOnly )
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
        public static async Task<string> GetServerCountryCodeAsync( IHttpClient httpClient, string defaultCountryCode="US" )
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
                    // Appending /country/ directly to the URI allows the API to return the raw text code string instantly
                    //string rawResponse = await httpClient.GetStringAsync( "https://ipapi.co/json" ).ConfigureAwait( false );
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
    }
}
