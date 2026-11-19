#if DEBUG
#define DEBUG_PLUGIN
//#define DEBUG_PLUGIN_ASYNC
//#define DEBUG_DB
#endif

using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using ServiceStack;
using Statistics2026.Data;
using Statistics2026.ScheduledTasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json.Nodes;
using TimeZoneNames;

namespace Statistics2026.Utilities
{
    public static class HelperFuncs
    {
        [DllImport( "ole32.dll" )]
        private static extern int GetRunningObjectTable( int reserved, out IRunningObjectTable prot );

        [DllImport( "ole32.dll" )]
        private static extern int CreateBindCtx( int reserved, out IBindCtx ppbc );

        private static object? GetActiveVisualStudioInstance()
        {
            GetRunningObjectTable( 0, out IRunningObjectTable rot );
            rot.EnumRunning( out IEnumMoniker enumMoniker );

            IMoniker[] monikers = new IMoniker[ 1 ];
            while( enumMoniker.Next( 1, monikers, IntPtr.Zero ) == 0 )
            {
                CreateBindCtx( 0, out IBindCtx bindCtx );
                monikers[ 0 ].GetDisplayName( bindCtx, null, out string displayName );

                // Matches Visual Studio DTE monikers (e.g., !VisualStudio.DTE.17.0:12345)
                if( displayName.StartsWith( "!VisualStudio.DTE" ) )
                {
                    rot.GetObject( monikers[ 0 ], out object dte );
                    return dte;
                }
            }
            return null;
        }

        public static void AttachDebugger()
        {
#if DEBUG_PLUGIN
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

            // Check if a debugger is already attached
            if( !Debugger.IsAttached )
            {
                int currentProcessId = Process.GetCurrentProcess().Id;

                // Note: Requires reference to EnvDTE / EnvDTE80 (or dynamic COM interop)
                dynamic? dte = GetActiveVisualStudioInstance();
                if( dte != null )
                {
                    foreach( dynamic proc in dte.Debugger.LocalProcesses )
                    {
                        if( proc.ProcessID == currentProcessId )
                        {
                            proc.Attach();
                            return;
                        }
                    }
                }

                // Fallback to standard prompt if no active VS instance is found
                Debugger.Launch();
            }
#endif
        }

        public static void PreDebugRun( EmbyInterfaces embyInterfaces )
        {
            if( !Debugger.IsAttached )
            {
                AttachDebugger();
            }

#if DEBUG_PLUGIN_IMAGES
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

        public static string GetCountryCode( IHttpClient httpClient, CancellationToken? _cancellationToken = null )
        {
            RegionInfo currentRegion = RegionInfo.CurrentRegion;
            var defaultIsoCode = currentRegion.TwoLetterISORegionName;
            var retVal = HelperFuncs.GetIPInfoAsync( "", httpClient, defaultIsoCode, _cancellationToken ).ConfigureAwait( false ).GetAwaiter().GetResult();

            return retVal.countryCode;
        }

        public static string GetCountryCode( string ipAddress, IHttpClient httpClient, CancellationToken? _cancellationToken = null )
        {
            RegionInfo currentRegion = RegionInfo.CurrentRegion;
            var defaultIsoCode = currentRegion.TwoLetterISORegionName;
            var retVal = HelperFuncs.GetIPInfoAsync( ipAddress, httpClient, defaultIsoCode, _cancellationToken ).ConfigureAwait( false ).GetAwaiter().GetResult();

            return retVal.countryCode;
        }

        public static async Task<string> GetCountryCodeAsync( IHttpClient httpClient, string defaultCountryCode = "US", CancellationToken? _cancellationToken = null )
        {
            var retVal = await GetIPInfoAsync( "", httpClient, defaultCountryCode, _cancellationToken );
            return retVal.countryCode;
        }


        private static ConcurrentDictionary<string, (string countryCode, string tzOffset)> sIpLookup = [];
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim( 1, 1 );

        public static void ClearCache()
        {
            sIpLookup.Clear();
        }

        private static async Task<(string countryCode, string tzOffset)> GetIPInfoAsync( string ipAddress, IHttpClient httpClient, string defaultCountryCode = "US", CancellationToken? _cancellationToken = null )
        {
            await _semaphore.WaitAsync( _cancellationToken ?? CancellationToken.None );
            try
            {
                if( sIpLookup.TryGetValue( ipAddress, out var value ) )
                    return value;

                (string countryCode, string tzOffset) retVal = (defaultCountryCode, "-0500");
                if( httpClient == null )
                {
                    return retVal;
                }

                try
                {
                    var path = string.IsNullOrWhiteSpace( ipAddress ) ? "" : "/{ipAddress}";
                    path += "/json";
                    var builder = new UriBuilder
                    {
                        Scheme = "https",
                        Host = "ipapi.co",
                        Path = path
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
                            if( obj != null )
                            {
                                retVal.countryCode = obj[ "country_code" ]?.GetValue<string>() ?? defaultCountryCode;
                                retVal.tzOffset = obj[ "utc_offset " ]?.GetValue<string>() ?? "-0500";
                            }
                        }
                    }
                }
                catch( System.Exception )
                {
                }

                sIpLookup.TryAdd( ipAddress, retVal );
                return retVal;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public static string JoinWhereClauses( List<string> clauses )
        {
            if( clauses.IsNullOrEmpty() )
                return string.Empty;

            for( var ii = 0; ii < clauses.Count; ++ii )
            {
                clauses[ ii ] = $"( {clauses[ ii ]} )";
            }

            return $" WHERE {( string.Join( " AND ", clauses ) )} ";
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

        public static string MakeTableHtml( List<(string key, string value)> rows, bool inDiv = true, int depth = 0 )
        {
            if( rows.IsNullOrEmpty() )
                return string.Empty;

            var retVal = string.Empty;
            if( inDiv )
                retVal += addToHtml( depth++, "<div>" );

            retVal += addToHtml( depth++, "<ul class=\"dynamic-list\">" );
            foreach( var curr in rows )
            {
                retVal += addToHtml( depth++, "<li>" );
                retVal += addToHtml( depth++, "<div class=\"row\">" );
                retVal += addToHtml( depth, $"<span class=\"label\">{curr.key}</span>" );
                if( !curr.value.IsNullOrEmpty() )
                    retVal += addToHtml( depth, $"<span class=\"value\">{curr.value}</span>" );
                retVal += addToHtml( --depth, "</div>" );
                retVal += addToHtml( --depth, "</li>" );
            }
            retVal += addToHtml( --depth, "</ul>" );
            if( inDiv )
                retVal += addToHtml( --depth, "</div>" );
            return retVal;
        }

        public static string MakeTableHtml( List<string> values, bool inDiv = true, int depth = 0 )
        {
            List<(string key, string value)> rows = [];
            foreach( var curr in values )
            {
                rows.Add( (curr, "") );
            }
            return MakeTableHtml( rows, inDiv, depth );
        }

        public static string MakeTableHtml( string title, List<(string key, string value)> rows )
        {
            if( rows.IsNullOrEmpty() )
                return string.Empty;

            var depth = 0;
            var retVal = addToHtml( depth++, "<div>" );
            if( !title.IsEmpty() )
            {
                retVal += addToHtml( depth, $"<h3>{title}</h3>" );
            }

            retVal += MakeTableHtml( rows, false, depth );

            retVal += addToHtml( --depth, "</div>" );
            return retVal;
        }


        public static string MakeTableHtml( string title, List<string> values )
        {
            List<(string key, string value)> rows = [];
            foreach( var curr in values )
            {
                rows.Add( (curr, "") );
            }
            return MakeTableHtml( title, rows );
        }

        public static string PrettyPrintSize( this long bytes )
        {
            string[] suffixes = { "b", "Kb", "Mb", "Gb", "Tb", "Pb", "Eb" };

            if( bytes == 0 )
                return "0 b";

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

        static HashSet<string> AllowedTMDBSizes = new HashSet<string>( StringComparer.OrdinalIgnoreCase )
            {
                "w45", "w92", "w154", "w185", "w300", "w342", "w500", "w780", "h632", "original"
            };
        public static string GetTMDBUrl( string posterPath, string posterSize, bool proxy )
        {
            if( posterPath.IsNullOrEmpty() )
                return string.Empty;

            if( posterSize.IsNullOrEmpty() )
            {
                posterSize = "original";
            }

            if( !AllowedTMDBSizes.Contains( posterSize ) )
            {
                throw new ResourceNotFoundException( "Invalid image size." );
            }


            posterPath = posterPath.TrimStart( '/' ).TrimEnd( '/' );
            posterSize = posterSize.TrimStart( '/' ).TrimEnd( '/' );

            var urlBase = string.Empty;
            if( proxy )
            {
                urlBase = $"/emby/Statistics2026/tmdbimage";
            }
            else
            {
                urlBase = $"https://image.tmdb.org/t/p";
            }
            var retVal = $"{urlBase}/{posterSize}/{posterPath}";
            return retVal;
        }

        public static (string imageUrl, string itemText, string itemUrl) GetTMDBLink( string posterPath, string text, (string targetUrl, string targetHtml)? targetInfo = null, string posterSize = "w300", string? imageHeight = "105px", string? imageWidth = null )
        {
            string imageUrl = GetTMDBUrl( posterPath, posterSize, true );
            string itemText = string.Empty;
            if( !imageUrl.IsNullOrEmpty() )
            {
                itemText = $"<img loading=\"lazy\" class=\"image-proxy\" data-src=\"{imageUrl}\" ";
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
                CacheMode = CacheMode.Unconditional,
                UserAgent = "Mozilla/5.0"
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

        static public void CheckForOOD( EmbyInterfaces embyInterfaces, bool initDB, List<string> reasons, System.Type okIfRunning, bool allowRunAll )
        {
            try
            {
                if( embyInterfaces == null )
                    return;
                if( TaskDef.IsStatistics2026TaskRunning( embyInterfaces, okIfRunning, false ) )
                {
                    embyInterfaces.Logger().Debug( $"System currently running" );
                    return;
                }

                embyInterfaces.Logger().Debug( $"System currently NOT running" );
                using var db = Statistics2026DB.GetInstance( embyInterfaces );
                db.Initialize( null, null, null, false );

                var currLogger = embyInterfaces.Logger();
                var (runFullAnalysis, runMissingAnalysis, oodReasons) = EmbyHelperFuncs.AnalysisOutOfDate( embyInterfaces, db );

                embyInterfaces._logger = currLogger;
                reasons.AddRange( oodReasons );
                embyInterfaces.Logger().Debug( $"runFullAnalysis=${runFullAnalysis} runMissingAnalysis=${runMissingAnalysis} reasons={string.Join( ", ", reasons )}" );

                List<Type> tasks = [];
                if( ( !runFullAnalysis || !allowRunAll ) && runMissingAnalysis )
                {
                    tasks.AddRange( [ typeof( AnalyzeMediaTask ), typeof( AnalyzeSeriesTask ), typeof( AnalyzeCollectionsTask ), typeof( AnalyzeMissingEpisodesTask ), typeof( AnalyzeMissingMoviesTask ) ] );
                }
                else if( runFullAnalysis && allowRunAll )
                {
                    tasks.Add( typeof( RunAllTasksTask ) );
                }

                _ = TaskDef.ExecuteSubTasksAsync( embyInterfaces, tasks, reasons, null, false );
            }
            catch( Exception )
            {
                // Always wrap timer callbacks in try-catch blocks to keep the server stable
            }
        }

        public static object HandleRequest( string requestName, EmbyInterfaces embyInterfaces, Func<AutoTimer, object> requestFunc )
        {
            using( var timer = new AutoTimer( $"Request: {requestName}", embyInterfaces.Logger() ) )
            {
                try
                {
                    return requestFunc( timer );
                }
                catch( Exception )
                {
                    throw;
                }
            }
        }

        public static bool InvalidDateForMissingAnalysis( DateTimeOffset? dto, bool ignoreConfig = false )
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

        public class MemoryUsage
        {
            public long _managedBefore { get; set; } = 0;
            public long _managedPeak { get; set; } = 0;
            public long _managedAfter { get; set; } = 0;
            public long _processBefore { get; set; } = 0;
            public long _processAfter { get; set; } = 0;

            public void Report( ILogger logger, string prefix, LogSeverity severity=LogSeverity.Debug )
            {
                StringBuilder builder = new();

                builder.AppendLine( $"{prefix}: --- Managed Heap Memory ---" );
                builder.AppendLine( $"{prefix}: Before: {PrettyPrintSize( _managedBefore )}" );
                builder.AppendLine( $"{prefix}: Peak:   {PrettyPrintSize( _managedPeak )}" );
                builder.AppendLine( $"{prefix}: After:  {PrettyPrintSize( _managedAfter )}" );
                builder.AppendLine( $"{prefix}: --- Total OS Process Memory (Task Manager) ---" );
                builder.AppendLine( $"{prefix}: Before: {PrettyPrintSize( _processBefore )}" );
                builder.AppendLine( $"{prefix}: After:  {PrettyPrintSize( _processAfter )}" );

                logger?.LogMultiline( "Memory Usage Report", severity, builder );
            }
        };

        public static async Task<MemoryUsage> RunAndReportMemory( bool debugLoggingEnabled, Func<Task> workload )
        {
            if( !debugLoggingEnabled )
            {
                await workload();
                return new();
            }
            // Optional: Start with a clean baseline before measuring
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var retVal = new MemoryUsage();

            // 1. Measure BEFORE
            retVal._managedBefore = GC.GetTotalMemory( forceFullCollection: false );
            retVal._processBefore = GetProcessMemory();

            // 2. Run the heavy function
            await workload();

            // Measure PEAK (Optional: see how much it grew before cleanup)
            retVal._managedPeak = GC.GetTotalMemory( forceFullCollection: false );

            // 3. Force cleanup
            GC.Collect( 2, GCCollectionMode.Forced, blocking: true );
            GC.WaitForPendingFinalizers();
            GC.Collect( 2, GCCollectionMode.Forced, blocking: true );

            // 4. Measure AFTER
            retVal._managedAfter = GC.GetTotalMemory( forceFullCollection: false );
            retVal._processAfter = GetProcessMemory();

            return retVal;
        }

        private static void ForceFullCleanup()
        {
            GC.Collect( 2, GCCollectionMode.Forced, blocking: true );
            GC.WaitForPendingFinalizers();
            GC.Collect( 2, GCCollectionMode.Forced, blocking: true );
        }

        private static long GetProcessMemory()
        {
            using var currentProcess = Process.GetCurrentProcess();
            currentProcess.Refresh();
            return currentProcess.WorkingSet64;
        }
    }
}
