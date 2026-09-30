using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using ServiceStack.Text;

namespace Statistics2026.Utilities
{
    using Emby.Media.Common.Extensions;
    using MediaBrowser.Common.Net;
    using MediaBrowser.Controller.Configuration;
    using MediaBrowser.Model.Serialization;
    using ServiceStack;
    using Statistics2026.Api;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;

    public class TmdbCollectionReader
    {
        private readonly EmbyInterfaces? _embyInterfaces = null;

        // Replace with your plugin's TMDB API Key configuration accessor
        //private const string ApiKey = "YOUR_TMDB_API_KEY";
        const string kApiKeyV3 = "7c58ff37c9fadd56c51dae3a97339378";
        const string kApiKeyV4 = "eyJhbGciOiJIUzI1NiJ9.eyJhdWQiOiI3YzU4ZmYzN2M5ZmFkZDU2YzUxZGFlM2E5NzMzOTM3OCIsInN1YiI6IjVmYTAzMzJiNjM1MDEzMDAzMTViZjg2NyIsInNjb3BlcyI6WyJhcGlfcmVhZCJdLCJ2ZXJzaW9uIjoxfQ.MBAzJIxvsRm54kgPKcfixxtfbg2bdNGDHKnEt15Nuac";


        public TmdbCollectionReader( EmbyInterfaces? embyInterfaces, CancellationToken? cancellationToken = null )
        {
            _embyInterfaces = embyInterfaces;

            if( ( _embyInterfaces == null ) || ( _embyInterfaces._httpClient == null ) || ( _embyInterfaces._jsonSerializer == null ) )
                return;

            login( cancellationToken );
        }

        private void login( CancellationToken? cancellationToken = null )
        {
            var builder = new UriBuilder();
            builder.Scheme = "https";
            builder.Host = "api.themoviedb.org";
            builder.Path = "/3/configuration";
            builder.Query = $"api_key={kApiKeyV3}";

            var url = builder.Uri.ToString();
            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken ?? CancellationToken.None,
                CacheLength = System.TimeSpan.FromDays( 1 ),
                CacheMode = CacheMode.None
            };

            var response = _embyInterfaces!._httpClient!.GetResponse( options ).ConfigureAwait( false );
        }


        public async Task<TmdbCollection?> GetRemoteCollectionMembersAsync( string tmdbId, CancellationToken? cancellationToken = null, string language = "en-US" )
        {
            if( string.IsNullOrEmpty( tmdbId ) )
                return null;

            if( ( _embyInterfaces == null ) || ( _embyInterfaces._httpClient == null ) || ( _embyInterfaces._jsonSerializer == null ) || ( _embyInterfaces._providerManager == null ) )
                return null;

            //string url = $"https://themoviedb.org/{tmdbCollectionId}?api_key={kApiKeyV3}&language={language}";
            var builder = new UriBuilder();
            builder.Scheme = "https";
            builder.Host = "api.themoviedb.org";
            builder.Path = $"/3/collection/{tmdbId}?language={language}";

            var query = HttpUtility.ParseQueryString( builder.Query );
            query[ "api_key" ] = kApiKeyV3;
            builder.Query = query.ToString();

            var url = builder.Uri.ToString();
            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken ?? CancellationToken.None,
                // Optional: Set cache timeout if you don't want to hit TMDB constantly
                CacheLength = System.TimeSpan.FromDays( 1 ),
                CacheMode = CacheMode.None
            };

            try
            {
                using( var response = await _embyInterfaces._httpClient.GetResponse( options ).ConfigureAwait( false ) )
                {
                    var collections = _embyInterfaces._jsonSerializer.DeserializeFromStream<TmdbCollection>( response.Content );
                    return collections ?? null;
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<TmdbSeries?> GetRemoteSeriesAsync( string tmdbId, CancellationToken? cancellationToken = null, string language = "en-US" )
        {
            TmdbSeries? retVal = null;
            if( string.IsNullOrEmpty( tmdbId ) )
                return retVal;

            if( ( _embyInterfaces == null ) || ( _embyInterfaces._httpClient == null ) || ( _embyInterfaces._jsonSerializer == null ) || ( _embyInterfaces._providerManager == null ) )
                return retVal;

            //string url = $"https://themoviedb.org/{tmdbCollectionId}?api_key={kApiKeyV3}&language={language}";
            var builder = new UriBuilder();
            builder.Scheme = "https";
            builder.Host = "api.themoviedb.org";
            builder.Path = $"/3/tv/{tmdbId}?language={language}";

            var query = HttpUtility.ParseQueryString( builder.Query );
            query[ "api_key" ] = kApiKeyV3;
            builder.Query = query.ToString();

            var url = builder.Uri.ToString();
            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken ?? CancellationToken.None,
                // Optional: Set cache timeout if you don't want to hit TMDB constantly
                CacheLength = System.TimeSpan.FromDays( 1 ),
                CacheMode = CacheMode.None
            };
            try
            {

                using( var response = await _embyInterfaces._httpClient.GetResponse( options ).ConfigureAwait( false ) )
                {
                    var text = DBHelper.StreamToString( response.Content );
                    //var seriesList = TmdbSeriesResponse.FromJson( text, _embyInterfaces._jsonSerializer );
                    var series = _embyInterfaces._jsonSerializer.DeserializeFromString<TmdbSeries>( text );
                    if( series == null )
                        return retVal;
                    if( series.Seasons != null && series.Seasons.Count > 0 )
                    {
                        List<TmdbSeason> seasons = [];
                        foreach( var season in series.Seasons )
                        {
                            if( season == null )
                                continue;
                            seasons.Add( season );
                            if( seasons.Count == 20 )
                            {
                                retVal = await GetRemoteEpisodesAsync( retVal, seasons, tmdbId, cancellationToken ).ConfigureAwait( false );
                                seasons.Clear();
                            }
                        }
                        if( seasons.Count > 0 )
                        {
                            retVal = await GetRemoteEpisodesAsync( retVal, seasons, tmdbId, cancellationToken ).ConfigureAwait( false );
                        }
                    }
                    return retVal;
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<TmdbSeries?> GetRemoteEpisodesAsync( TmdbSeries? retVal, List<TmdbSeason> seasons, string tmdbId, CancellationToken? cancellationToken = null, string language = "en-US" )
        {
            if( seasons.Count == 0 )
                return retVal;

            if( seasons.Count > 20 )
            {
                throw new ArgumentOutOfRangeException( "Can only get up to 20 seasons at a time" );
            }

            if( ( _embyInterfaces == null ) || ( _embyInterfaces._httpClient == null ) || ( _embyInterfaces._jsonSerializer == null ) || ( _embyInterfaces._providerManager == null ) )
                return retVal;

            //string url = $"https://themoviedb.org/{tmdbCollectionId}?api_key={kApiKeyV3}&language={language}";

            var builder = new UriBuilder();
            builder.Scheme = "https";
            builder.Host = "api.themoviedb.org";
            builder.Path = $"/3/tv/{tmdbId}?language={language}";

            var query = HttpUtility.ParseQueryString( builder.Query );
            query[ "api_key" ] = kApiKeyV3;
            List<string> seasonStrings = [];
            foreach( var season in seasons )
            {
                seasonStrings.Add( $"season/{season.SeasonNumber.ToString()}" );
            }
            query[ "append_to_response" ] = String.Join( ",", seasonStrings );
            builder.Query = query.ToString();

            var url = builder.Uri.ToString();
            var options = new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken ?? CancellationToken.None,
                // Optional: Set cache timeout if you don't want to hit TMDB constantly
                CacheLength = System.TimeSpan.FromDays( 1 ),
                CacheMode = CacheMode.None
            };

            using( var response = await _embyInterfaces._httpClient.Get( options ).ConfigureAwait( false ) )
            {
                var text = DBHelper.StreamToString( response );
                retVal = TmdbSeries.FromJson( retVal, text, _embyInterfaces._jsonSerializer );
                return retVal;
            }
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

        public DateTime ReleaseDate()
        {
            if( string.IsNullOrEmpty( ReleaseDateText ) )
                return DateTime.MinValue;

            return DBHelper.ReadDateTime( ReleaseDateText );
        }
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
            var jsonNode = JsonObject.Parse( jsonString );
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
        public DateTime AirDate()
        {
            if( string.IsNullOrEmpty( AirDateStr ) )
                return DateTime.MinValue;

            return DBHelper.ReadDateTime( AirDateStr );
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
        public DateTime AirDate()
        {
            if( string.IsNullOrEmpty( AirDateStr ) )
                return DateTime.MinValue;

            return DBHelper.ReadDateTime( AirDateStr );
        }
    }

}
