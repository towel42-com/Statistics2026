using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using ServiceStack;
using ServiceStack.Text.Common;
using Statistics2026.Secrets;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Collections.Concurrent;

namespace Statistics2026.Utilities
{
    public class TmdbInterface
    {
        private readonly EmbyInterfaces? _embyInterfaces = null;
        public readonly CancellationToken? _cancellationToken = null;

        public TmdbInterface()
        {
            if( Plugin.Instance == null )
                throw new ArgumentNullException( "Plugin.Instance can not be null" );
            _embyInterfaces = Plugin.Instance._embyInterfaces;
        }

        public TmdbInterface( EmbyInterfaces? embyInterfaces, CancellationToken? cancellationToken = null )
        {
            _embyInterfaces = embyInterfaces;
            _cancellationToken = cancellationToken;

            CheckIsValidWithThrow();

            try
            {
                login().GetAwaiter().GetResult();
            }
            catch( Exception ex )
            {
                throw ex;
            }
        }

        [MemberNotNullWhen( true, nameof( _embyInterfaces ) )]
        private bool _CheckIsValid( ref List<string> msgs )
        {
            if( _embyInterfaces == null )
            {
                msgs.Add( "_embyInterfaces is null" );
            }
            else
            {
                _embyInterfaces.CheckIsValid( ref msgs );
            }

            if( Plugin.Instance == null )
            {
                msgs.Add( "Plugin.Instance is null" );
            }

            return msgs.Count == 0;
        }

        [MemberNotNullWhen( true, nameof( _embyInterfaces ) )]
        private bool CheckIsValid()
        {
            List<string> msgs = [];
            if( !_CheckIsValid( ref msgs ) )
            {
                foreach( var msg in msgs )
                {
                    _embyInterfaces?.Logger().Error( msg );
                }
                return false;
            }
            return true;
        }

        [MemberNotNull( nameof( _embyInterfaces ) )]
        private bool CheckIsValidWithThrow()
        {
            List<string> msgs = [];
            if( !_CheckIsValid( ref msgs ) )
            {
                throw new ArgumentNullException( msgs.Join( "\n" ) );
            }
            return true;
        }

        private async Task login()
        {
            if( !CheckIsValid() )
                return;

            var builder = new UriBuilder
            {
                Scheme = "https",
                Host = "api.themoviedb.org",
                Path = "/3/configuration",
                Query = $"api_key={TheMovieDB.getApiKeyV3()}"
            };

            var url = builder.Uri.ToString();
            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = _cancellationToken ?? CancellationToken.None,
                CacheLength = System.TimeSpan.FromDays( 1 ),
                CacheMode = CacheMode.None
            };

            try
            {
                _ = await _embyInterfaces.HttpClient().GetResponse( options ).ConfigureAwait( false );
            }
            catch( Exception ex )
            {
                throw ex;
            }
            _cancellationToken?.ThrowIfCancellationRequested();
        }

        public async Task<TmdbCollection?> GetRemoteCollectionMembersAsync( string tmdbId, string language = "en-US" )
        {
            if( string.IsNullOrEmpty( tmdbId ) )
                return null;

            if( !CheckIsValid() )
                return null;

            //string url = $"https://themoviedb.org/{tmdbCollectionId}?api_key={kApiKeyV3}&language={language}";
            var builder = new UriBuilder
            {
                Scheme = "https",
                Host = "api.themoviedb.org",
                Path = $"/3/collection/{tmdbId}"
            };

            var query = HttpUtility.ParseQueryString( builder.Query );
            query[ "api_key" ] = TheMovieDB.getApiKeyV3();
            query[ "language" ] = language;
            builder.Query = query.ToString();

            var url = builder.Uri.ToString();
            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = _cancellationToken ?? CancellationToken.None,
                // Optional: Set cache timeout if you don't want to hit TMDB constantly
                CacheLength = System.TimeSpan.FromDays( 1 ),
                CacheMode = CacheMode.None
            };

            using( var response = await _embyInterfaces.HttpClient().GetResponse( options ).ConfigureAwait( false ) )
            {
                var text = HelperFuncs.StreamToString( response.Content );

                var collection = _embyInterfaces.JsonSerializer().DeserializeFromString<TmdbCollection>( text );
                if( collection == null )
                    return collection;

                for( int ii = 0; ii < collection.Movies.Count; ++ii )
                {
                    var movie = collection.Movies[ ii ];
                    if( movie == null )
                        continue;

                    collection.Movies[ ii ] = await GetMovieStudiosAsync( movie ).ConfigureAwait( false );
                    _cancellationToken?.ThrowIfCancellationRequested();
                }
                _cancellationToken?.ThrowIfCancellationRequested();
                return collection ?? null;
            }
        }

        public async Task<TmdbMovie> GetMovieStudiosAsync( TmdbMovie movie, string language = "en-US" )
        {
            if( !CheckIsValid() )
                return movie;

            var builder = new UriBuilder
            {
                Scheme = "https",
                Host = "api.themoviedb.org",
                Path = $"/3/movie/{movie.Id}"
            };

            var query = HttpUtility.ParseQueryString( builder.Query );
            query[ "api_key" ] = TheMovieDB.getApiKeyV3();
            query[ "language" ] = language;
            builder.Query = query.ToString();

            var url = builder.Uri.ToString();
            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = _cancellationToken ?? CancellationToken.None,
                // Optional: Set cache timeout if you don't want to hit TMDB constantly
                CacheLength = System.TimeSpan.FromDays( 1 ),
                CacheMode = CacheMode.None
            };

            using( var response = await _embyInterfaces.HttpClient().GetResponse( options ).ConfigureAwait( false ) )
            {
                var text = HelperFuncs.StreamToString( response.Content );

                var retVal = _embyInterfaces.JsonSerializer().DeserializeFromString<TmdbMovie>( text );
                _cancellationToken?.ThrowIfCancellationRequested();
                if( retVal == null )
                    return movie;
                movie = retVal;
            }
            return movie;
        }

        public async Task<TmdbSeries?> GetRemoteSeriesAsync( string tmdbId, string language = "en-US" )
        {
            TmdbSeries? retVal = null;
            if( string.IsNullOrEmpty( tmdbId ) )
                return retVal;

            if( !CheckIsValid() )
                return null;

            //string url = $"https://themoviedb.org/{tmdbCollectionId}?api_key={kApiKeyV3}&language={language}";
            var builder = new UriBuilder
            {
                Scheme = "https",
                Host = "api.themoviedb.org",
                Path = $"/3/tv/{tmdbId}"
            };

            var query = HttpUtility.ParseQueryString( builder.Query );
            query[ "api_key" ] = TheMovieDB.getApiKeyV3();
            query[ "language" ] = language;
            builder.Query = query.ToString();

            var url = builder.Uri.ToString();
            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = _cancellationToken ?? CancellationToken.None,
                // Optional: Set cache timeout if you don't want to hit TMDB constantly
                CacheLength = System.TimeSpan.FromDays( 1 ),
                CacheMode = CacheMode.None
            };

            using( var response = await _embyInterfaces.HttpClient().GetResponse( options ).ConfigureAwait( false ) )
            {
                var text = HelperFuncs.StreamToString( response.Content );
                var series = _embyInterfaces.JsonSerializer().DeserializeFromString<TmdbSeries>( text );
                if( series == null )
                    return retVal;
                if( series.Seasons != null && series.Seasons.Count > 0 )
                {
                    List<TmdbSeason> seasons = [];
                    foreach( var season in series.Seasons )
                    {
                        _cancellationToken?.ThrowIfCancellationRequested();
                        if( season == null )
                            continue;
                        seasons.Add( season );
                        if( seasons.Count == 20 )
                        {
                            retVal = await GetRemoteEpisodesAsync( retVal, seasons, tmdbId ).ConfigureAwait( false );
                            seasons.Clear();
                        }
                    }

                    if( seasons.Count > 0 )
                    {
                        _cancellationToken?.ThrowIfCancellationRequested();
                        retVal = await GetRemoteEpisodesAsync( retVal, seasons, tmdbId ).ConfigureAwait( false );
                    }
                }

            }
            return retVal;
        }

        public async Task<TmdbSeries?> GetRemoteEpisodesAsync( TmdbSeries? retVal, List<TmdbSeason> seasons, string tmdbId, string language = "en-US" )
        {
            if( !CheckIsValid() )
                return retVal;

            if( seasons.Count == 0 )
                return retVal;

            if( seasons.Count > 20 )
            {
                throw new ArgumentOutOfRangeException( "Can only get up to 20 seasons at a time" );
            }

            //string url = $"https://themoviedb.org/{tmdbCollectionId}?api_key={kApiKeyV3}&language={language}";

            var builder = new UriBuilder
            {
                Scheme = "https",
                Host = "api.themoviedb.org",
                Path = $"/3/tv/{tmdbId}"
            };

            var query = HttpUtility.ParseQueryString( builder.Query );
            query[ "api_key" ] = TheMovieDB.getApiKeyV3();
            query[ "language" ] = language;
            List<string> seasonStrings = [];
            foreach( var season in seasons )
            {
                _cancellationToken?.ThrowIfCancellationRequested();
                seasonStrings.Add( $"season/{season.SeasonNumber}" );
            }

            query[ "append_to_response" ] = string.Join( ",", seasonStrings );
            builder.Query = query.ToString();

            var url = builder.Uri.ToString();
            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = _cancellationToken ?? CancellationToken.None,
                // Optional: Set cache timeout if you don't want to hit TMDB constantly
                CacheLength = System.TimeSpan.FromDays( 1 ),
                CacheMode = CacheMode.None
            };

            using( var response = await _embyInterfaces.HttpClient().Get( options ).ConfigureAwait( false ) )
            {
                _cancellationToken?.ThrowIfCancellationRequested();
                var text = HelperFuncs.StreamToString( response );
                retVal = TmdbSeries.FromJson( retVal, text, _embyInterfaces.JsonSerializer() );
                return retVal;
            }
        }

        private static readonly ConcurrentDictionary<string, TmdbStreamingProvidersResponse> _ProviderCache = new ConcurrentDictionary<string, TmdbStreamingProvidersResponse>();

        public async Task<List<TmdbStreamingProvider>> GetStreamingServicesAsync( ILogger logger, Video? video, string language = "en-US" )
        {
            if( !CheckIsValid() )
                return [];

            if( video == null )
                return [];

            var tmdbId = string.Empty;
            if( video is Episode )
            {
                tmdbId = ( video as Episode )!.Series?.GetProviderId( MetadataProviders.Tmdb );
            }
            else
            {
                tmdbId = video.GetProviderId( MetadataProviders.Tmdb );
            }

            if( ( tmdbId == null ) || tmdbId.IsEmpty() || !long.TryParse( tmdbId, out _ ) )
            {
                return [];
            }

            if( !_ProviderCache.TryGetValue( tmdbId, out var providerResponse ) )
            {
                try
                {
                    //string url = https://themoviedb.org/3/movie|tv|series/{tmdbid}/watch/providers?api_key={kApiKeyV3}&language={language}";
                    var videoType = ( video is Episode ) ? "tv" : "movie";
                    var builder = new UriBuilder
                    {
                        Scheme = "https",
                        Host = "api.themoviedb.org",
                        Path = $"/3/{videoType}/{tmdbId}/watch/providers"
                    };

                    var query = HttpUtility.ParseQueryString( builder.Query );
                    query[ "api_key" ] = TheMovieDB.getApiKeyV3();
                    query[ "language" ] = language;
                    builder.Query = query.ToString();

                    var url = builder.Uri.ToString();
                    var options = new HttpRequestOptions
                    {
                        Url = url,
                        CancellationToken = _cancellationToken ?? CancellationToken.None,
                        // Optional: Set cache timeout if you don't want to hit TMDB constantly
                        CacheLength = System.TimeSpan.FromDays( 1 ),
                        CacheMode = CacheMode.None
                    };

                    using( var response = await _embyInterfaces.HttpClient().Get( options ).ConfigureAwait( false ) )
                    {
                        _cancellationToken?.ThrowIfCancellationRequested();
                        var text = HelperFuncs.StreamToString( response );

                        providerResponse = _embyInterfaces.JsonSerializer().DeserializeFromString<TmdbStreamingProvidersResponse>( text );
                        if( providerResponse == null )
                            return [];

                        providerResponse.UpdateProviderTypes();
                        _ProviderCache[ tmdbId ] = providerResponse;
                    }
                }
                catch( Exception )
                {
                    logger.Error( $"Could not get streaming services for video: '{video.Name}'" );
                    return [];
                }
            }
            if( providerResponse == null )
                return [];

            RegionInfo currentRegion = RegionInfo.CurrentRegion;
            var defaultIsoCode = currentRegion.TwoLetterISORegionName;
            var isoCode = await HelperFuncs.GetServerCountryCodeAsync( _embyInterfaces.HttpClient(), defaultIsoCode );

            if( providerResponse.Results.TryGetValue( isoCode, out var region ) )
            {
                return region.GetAllProviders();
            }

            return [];
        }
    }


    /// <summary>
    /// Root response wrapper returned by the /watch/providers endpoints.
    /// </summary>
    [DataContract]
    public class TmdbStreamingProvidersResponse
    {
        [DataMember( Name = "id" )]
        public int Id { get; set; } = -1; // -1 indicates an uninitialized or missing ID

        /// <summary>
        /// Keyed by ISO 3166-1 country code strings (e.g., "US", "CA", "GB").
        /// </summary>
        [DataMember( Name = "results" )]
        public Dictionary<string, TmdbStreamingProviderRegion> Results { get; set; } = new Dictionary<string, TmdbStreamingProviderRegion>();

        [OnDeserializing]
        private void InitializeDefaults( StreamingContext context )
        {
            Id = -1;
            Results = new Dictionary<string, TmdbStreamingProviderRegion>();
        }

        public void UpdateProviderTypes()
        {
            foreach( var regionKeyValuePair in Results )
            {
                if( regionKeyValuePair.Value != null )
                {
                    regionKeyValuePair.Value.UpdateProviderTypes();
                }
            }
        }
    }

    /// <summary>
    /// Represents the regional breakdown of availability options.
    /// </summary>
    [DataContract]
    public class TmdbStreamingProviderRegion
    {
        [DataMember( Name = "link" )]
        public string DeepLinkUrl { get; set; } = string.Empty;

        /// <summary>
        /// Streaming services included in a flat-rate subscription package (e.g., Netflix, Hulu).
        /// </summary>
        [DataMember( Name = "flatrate" )]
        public List<TmdbStreamingProvider> FlatRate { get; set; } = new List<TmdbStreamingProvider>();

        /// <summary>
        /// Services where the title can be rented on a transactional basis (e.g., Apple TV, Vudu).
        /// </summary>
        [DataMember( Name = "rent" )]
        public List<TmdbStreamingProvider> Rent { get; set; } = new List<TmdbStreamingProvider>();

        /// <summary>
        /// Services where the title can be purchased digitally (e.g., Amazon, Google Play).
        /// </summary>
        [DataMember( Name = "buy" )]
        public List<TmdbStreamingProvider> Buy { get; set; } = new List<TmdbStreamingProvider>();

        /// <summary>
        /// Ad-supported channels streaming the title for free (e.g., Tubi, Pluto TV).
        /// </summary>
        [DataMember( Name = "ads" )]
        public List<TmdbStreamingProvider> Ads { get; set; } = new List<TmdbStreamingProvider>();

        /// <summary>
        /// Premium add-on channels within another host provider (e.g., Paramount+ via Prime Video).
        /// </summary>
        [DataMember( Name = "addons" )]
        public List<TmdbStreamingProvider> AddOns { get; set; } = new List<TmdbStreamingProvider>();

        /// <summary>
        /// Platforms hosting the title completely free with no account requirements.
        /// </summary>
        [DataMember( Name = "free" )]
        public List<TmdbStreamingProvider> Free { get; set; } = new List<TmdbStreamingProvider>();

        [OnDeserializing]
        private void InitializeDefaults( StreamingContext context )
        {
            DeepLinkUrl = string.Empty;
            FlatRate = new List<TmdbStreamingProvider>();
            Rent = new List<TmdbStreamingProvider>();
            Buy = new List<TmdbStreamingProvider>();
            Ads = new List<TmdbStreamingProvider>();
            AddOns = new List<TmdbStreamingProvider>();
            Free = new List<TmdbStreamingProvider>();
        }

        public void UpdateProviderTypes()
        {
            UpdateProviderType( FlatRate, "flatrate" );
            UpdateProviderType( Rent, "rent" );
            UpdateProviderType( Buy, "buy" );
            UpdateProviderType( Ads, "ads" );
            UpdateProviderType( AddOns, "addons" );
            UpdateProviderType( Free, "free" );
        }

        public List<TmdbStreamingProvider> GetAllProviders()
        {
            var retVal = new List<TmdbStreamingProvider>();

            retVal.AddRange( FlatRate );
            retVal.AddRange( Rent );
            retVal.AddRange( Buy );
            retVal.AddRange( Ads );
            retVal.AddRange( AddOns );
            retVal.AddRange( Free );

            retVal.Sort( ( lhs, rhs ) => lhs.DisplayPriority.CompareTo( rhs.DisplayPriority ) );
            return retVal;
        }


        private void UpdateProviderType( List<TmdbStreamingProvider> providersList, string typeName )
        {
            if( providersList == null )
                return;

            for( int ii = 0; ii < providersList.Count; ii++ )
            {
                if( providersList[ ii ] != null )
                {
                    providersList[ ii ].ProviderType = typeName;
                }
            }
        }
    }

    /// <summary>
    /// Structural definition of a single streaming platform metadata asset.
    /// </summary>
    [DataContract]
    public class TmdbStreamingProvider
    {
        [DataMember( Name = "provider_id" )]
        public int ProviderId { get; set; } = -1;

        [DataMember( Name = "provider_name" )]
        public string ProviderName { get; set; } = string.Empty;

        [DataMember( Name = "logo_path" )]
        public string LogoPath { get; set; } = string.Empty;

        [DataMember( Name = "display_priority" )]
        public int DisplayPriority { get; set; } = int.MaxValue; // Pushes uninitialized items to the end of custom sorting layouts

        [OnDeserializing]
        private void InitializeDefaults( StreamingContext context )
        {
            ProviderId = -1;
            ProviderName = string.Empty;
            LogoPath = string.Empty;
            DisplayPriority = int.MaxValue;
        }

        public string ProviderType { get; set; } = string.Empty;
        public string ProviderUrl()
        {
            if( LogoPath.IsNullOrEmpty() )
                return string.Empty;
            var path = LogoPath.TrimStart( '/' );
            return $"https://tmdb.org/{path}";
        }
    }

    // Data contracts mapped exactly to TMDB's native collection response footprint
    [DataContract]
    public class TmdbCollection
    {
        [DataMember( Name = "id" )]
        public string Id { get; set; } = string.Empty;

        [DataMember( Name = "name" )]
        public string Name { get; set; } = string.Empty;

        [DataMember( Name = "overview" )]
        public string Overview { get; set; } = string.Empty;

        [DataMember( Name = "parts" )]
        public List<TmdbMovie> Movies { get; set; } = [];
    }

    [DataContract]
    public class TmdbMovie
    {
        [DataMember( Name = "id" )]
        public string Id { get; set; } = string.Empty; // The TMDB Movie ID (Crucial for Emby matching)

        [DataMember( Name = "title" )]
        public string Title { get; set; } = string.Empty;

        [DataMember( Name = "original_title" )]
        public string OriginalTitle { get; set; } = string.Empty;

        [DataMember( Name = "release_date" )]
        public string ReleaseDateText { get; set; } = string.Empty;

        [DataMember( Name = "overview" )]
        public string Overview { get; set; } = string.Empty;

        [DataMember( Name = "poster_path" )]
        public string PosterPath { get; set; } = string.Empty;

        [DataMember( Name = "production_companies" )]
        public List<TmdbStudio> Studios { get; set; } = [];

        public DateTimeOffset? ReleaseDate()
        {
            if( string.IsNullOrEmpty( ReleaseDateText ) )
                return null;

            return HelperFuncs.ReadDateTime( ReleaseDateText );
        }

        public string StudioNames()
        {
            List<string> names = [];
            foreach( var studio in Studios )
            {
                names.Add( studio.Name );
            }
            return JsonSerializer.Serialize( names );
        }
    }

    public class TmdbStudio
    {
        [DataMember( Name = "id" )]
        public string Id { get; set; } = string.Empty; // The TMDB Movie ID (Crucial for Emby matching)

        [DataMember( Name = "name" )]
        public string Name { get; set; } = string.Empty;
    }

    [DataContract]
    public class TmdbSeries
    {
        [DataMember( Name = "id" )]
        public string Id { get; set; } = string.Empty;

        [DataMember( Name = "name" )]
        public string Name { get; set; } = string.Empty;

        [DataMember( Name = "overview" )]
        public string Overview { get; set; } = string.Empty;

        [DataMember( Name = "seasons" )]
        public List<TmdbSeason> Seasons { get; set; } = [];

        public List<int> listOfSeasons()
        {
            List<int> retVal = [];
            foreach( var season in Seasons )
            {
                if( season == null )
                    continue;

                retVal.Add( season.SeasonNumber );
            }

            return retVal;
        }

        public static TmdbSeries? FromJson( TmdbSeries? series, string jsonString, IJsonSerializer jsonSerializer )
        {
            TmdbSeries? retVal = series;
            if( retVal == null )
                retVal = jsonSerializer.DeserializeFromString<TmdbSeries>( jsonString );

            if( retVal == null )
                return null;

            var seasons = retVal.listOfSeasons();
            var jsonNode = System.Text.Json.Nodes.JsonObject.Parse( jsonString );
            if( jsonNode == null )
                return null;

            var jsonObject = jsonNode.AsObject();

            foreach( var seasonNum in seasons )
            {
                var keyName = $"season/{seasonNum}";
                if( !jsonObject.ContainsKey( keyName ) )
                {
                    continue;
                }

                var detailedSeasonObj = jsonObject[ keyName ]!.AsObject();
                if( detailedSeasonObj == null )
                    continue;

                var detailedSeasonString = detailedSeasonObj.ToJsonString();
                var detailedSeason = jsonSerializer.DeserializeFromString<TmdbSeason>( detailedSeasonString );
                if( detailedSeason == null )
                    continue;

                var existingSeason = retVal.Seasons.FirstOrDefault( s => ( s != null ) && ( s.SeasonNumber == detailedSeason.SeasonNumber ) );
                if( existingSeason != null )
                {
                    // Merge the full episode array into your main dataset
                    existingSeason.Episodes = detailedSeason.Episodes;
                }
                else
                {
                    // Fallback just in case TMDB returns a season not present in the array
                    retVal.Seasons.Add( detailedSeason );
                }
            }

            return retVal;
        }
    }

    [DataContract]
    public class TmdbSeason
    {
        [DataMember( Name = "id" )]
        public string Id { get; set; } = string.Empty;

        [DataMember( Name = "name" )]
        public string Name { get; set; } = string.Empty;

        [DataMember( Name = "season_number" )]
        public int SeasonNumber { get; set; } = 0;

        [DataMember( Name = "episode_count" )]
        public int EpisodeCount { get; set; } = 0;

        [DataMember( Name = "air_date" )]
        public string AirDateStr { get; set; } = string.Empty;

        [DataMember( Name = "overview" )]
        public string Overview { get; set; } = string.Empty;

        [DataMember( Name = "poster_path" )]
        public string PosterPath { get; set; } = string.Empty;

        [DataMember( Name = "episodes" )]
        public List<TmdbEpisode> Episodes { get; set; } = [];

        // Helper method for safely parsing the date string
        public DateTimeOffset? AirDate()
        {
            if( string.IsNullOrEmpty( AirDateStr ) )
                return null;

            return HelperFuncs.ReadDateTime( AirDateStr );
        }
    }

    [DataContract]
    public class TmdbEpisode
    {
        [DataMember( Name = "id" )]
        public string Id { get; set; } = string.Empty;

        [DataMember( Name = "name" )]
        public string Name { get; set; } = string.Empty;

        [DataMember( Name = "air_date" )]
        public string AirDateStr { get; set; } = string.Empty;

        [DataMember( Name = "overview" )]
        public string Overview { get; set; } = string.Empty;

        [DataMember( Name = "still_path" )]
        public string StillPath { get; set; } = string.Empty;

        [DataMember( Name = "episode_number" )]
        public int EpisodeNumber { get; set; } = 0;

        // Helper method for safely parsing the date string
        public DateTimeOffset? AirDate()
        {
            if( string.IsNullOrEmpty( AirDateStr ) )
                return null;

            return HelperFuncs.ReadDateTime( AirDateStr );
        }
    }
}
