using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;
using System.Collections.Generic;

namespace Statistics2026.Api
{
    [Route( "/Statistics2026/GetItemImageUrl/{ItemId}", "GET" )]
    [Authenticated( Roles = "admin" )]
    public class GetItemImageUrl : IReturn<GetItemImageUrlResponse>
    {
        public string ItemId { get; set; } = string.Empty;
    }

    // http://localhost:8096/emby/Statistics2026/episode_list
    [Route( "/Statistics2026/episode_list", "GET", Summary = "Gets Codec Info for Episodes" )]
    [Authenticated( Roles = "admin" )]
    public class GetEpisodeList : IReturn<List<MediaItemResponse>>
    {
    }

    // http://localhost:8096/emby/Statistics2026/movie_list
    [Route( "/Statistics2026/movie_list", "GET", Summary = "Gets Codec Info for Movies" )]
    [Authenticated( Roles = "admin" )]
    public class GetMovieList : IReturn<List<MediaItemResponse>>
    {
    }

    // http://localhost:8096/emby/Statistics2026/tv_series_progress/{User}
    [Route( "/Statistics2026/tv_series_progress/{User}", "GET", Summary = "Gets Codec Info for Movies" )]
    public class GetTVSeriesProgress : IReturn<List<GetTVSeriesProgressResponse>>
    {
        public string user { get; set; } = string.Empty;

    }

    [Route( "/Statistics2026/codec_summary", "GET", Summary = "Gets Codec Summary for Library" )]
    [Authenticated( Roles = "admin" )]
    public class GetCodecSummary : IReturn<object>
    {
        [ApiMember( Name = "rootDivName", Description = "Root Division Name", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET" )]
        public string rootDivName { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/resolution_summary", "GET", Summary = "Gets Resolution Summary for Library" )]
    [Authenticated( Roles = "admin" )]
    public class GetResolutionSummary : IReturn<object>
    {
        [ApiMember( Name = "rootDivName", Description = "Root Division Name", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET" )]
        public string rootDivName { get; set; } = string.Empty;

    }

    [Route( "/Statistics2026/dvprofile_summary", "GET", Summary = "Gets Dolby Vision Profile Summary for Library" )]
    [Authenticated( Roles = "admin" )]
    public class GetDVProfileSummary : IReturn<object>
    {
        [ApiMember( Name = "rootDivName", Description = "Root Division Name", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET" )]
        public string rootDivName { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/user_count", "GET", Summary = "Gets the total User Count" )]
    [Authenticated( Roles = "admin" )]
    public class GetUserCount : IReturn<object>
    {
    }

    [Route( "/Statistics2026/most_active_users", "GET", Summary = "Gets the top 5 most active users" )]
    [Authenticated( Roles = "admin" )]
    public class GetMostActiveUsers : IReturn<object>
    {
    }

    [Route( "/Statistics2026/total_movie_count/{User}", "GET", Summary = "Get the total Movie Count" )]
    public class GetTotalMovieCount : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/total_movie_count", "GET", Summary = "Get the total Movie Count" )]
    [Authenticated( Roles = "admin" )]
    public class GetTotalMovieCountNoUser : IReturn<object>
    {
    }

    [Route( "/Statistics2026/total_movies_watched/{User}", "GET", Summary = "Get the total Movie Count" )]
    public class GetTotalMoviesWatched : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/total_tv_watched/{User}", "GET", Summary = "Get the total Movie Count" )]
    public class GetTotalTVWatched : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/total_series_finished/{User}", "GET", Summary = "Get the total Movie Count" )]
    public class GetTotalSeriesFinished : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/movie_favorite_years/{User}", "GET", Summary = "Get the Favorite Movie Years" )]
    public class GetMovieFavoriteYears : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/movie_favorite_genres/{User}", "GET", Summary = "Get the Favorite Movie Genres" )]
    public class GetMovieFavoriteGenres : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/tv_favorite_genres/{User}", "GET", Summary = "Get the Favorite Movie Genres" )]
    public class GetTVFavoriteGenres : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/total_collection_count", "GET", Summary = "Get the total Collection Count" )]
    [Authenticated( Roles = "admin" )]
    public class GetTotalCollectionCount : IReturn<object>
    {
    }

    [Route( "/Statistics2026/total_movie_studio_count", "GET", Summary = "Get the total Movie Studio Count" )]
    [Authenticated( Roles = "admin" )]
    public class GetTotalMovieStudioCount : IReturn<object>
    {
    }

    [Route( "/Statistics2026/total_tv_studio_count", "GET", Summary = "Get the total TV Studio Count" )]
    [Authenticated( Roles = "admin" )]
    public class GetTotalTVStudioCount : IReturn<object>
    {
    }

    [Route( "/Statistics2026/total_tv_count", "GET", Summary = "Get the total TV Count" )]
    [Authenticated( Roles = "admin" )]
    public class GetTotalTVCountNoUser : IReturn<object>
    {
    }

    [Route( "/Statistics2026/total_tv_count/{User}", "GET", Summary = "Get the total TV Count" )]
    public class GetTotalTVCount : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/get_movie/{WhichStatistic}", "GET", Summary = "Get the movie statistic in from the database" )]
    [Authenticated( Roles = "admin" )]
    public class GetMovie : IReturn<object>
    {
        public StatGen.EStatisticType whichStatistic { get; set; }
    }

    [Route( "/Statistics2026/get_series/{WhichStatistic}", "GET", Summary = "Get the series statistic information from the database" )]
    [Authenticated( Roles = "admin" )]
    public class GetSeries : IReturn<object>
    {
        public StatGen.EStatisticType whichStatistic { get; set; }
    }

    [Route( "/Statistics2026/get_episode/{WhichStatistic}", "GET", Summary = "Get the episode statistic information from the database" )]
    [Authenticated( Roles = "admin" )]
    public class GetEpisode : IReturn<object>
    {
        public StatGen.EStatisticType whichStatistic { get; set; }
    }

    [Route( "/Statistics2026/least_watched_shows", "GET", Summary = "Get the List of Least Watched Shows" )]
    public class GetLeastWatchedShows : IReturn<object>
    {
    }

    [Route( "/Statistics2026/most_watched_shows/{User}", "GET", Summary = "Get the List of Most Watched Shows" )]
    public class GetMostWatchedShows : IReturn<object>
    {
        [ApiMember( Name = "User", Description = "The user for whom to retrieve statistics", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET" )]
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/most_watched_shows", "GET", Summary = "Get the List of Most Watched Shows" )]
    public class GetMostWatchedShowsNoUser : IReturn<object>
    {
    }

    [Route( "/Statistics2026/least_watched_movies", "GET", Summary = "Get the List of Least Watched Movies" )]
    public class GetLeastWatchedMovies : IReturn<object>
    {
    }

    [Route( "/Statistics2026/most_watched_movies/{User}", "GET", Summary = "Get the List of Most Watched Movies" )]
    public class GetMostWatchedMovies : IReturn<object>
    {
        [ApiMember( Name = "user", Description = "The user for whom to retrieve statistics", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET" )]
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/most_watched_movies", "GET", Summary = "Get the List of Most Watched Movies" )]
    public class GetMostWatchedMoviesNoUser : IReturn<object>
    {
    }

    [Route( "/Statistics2026/total_time_watched/{User}", "GET", Summary = "Get the Total Time Watched for User" )]
    public class GetTotalTimeWatched : IReturn<object>
    {
        public string user { get; set; } = string.Empty;

        [ApiMember( Name = "episodes", Description = "Episodes", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET" )]
        public string episodes { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/total_watchable_time/{User}", "GET", Summary = "Get the Total Time Watched for User" )]
    public class GetTotalWatchableTime : IReturn<object>
    {
        public string user { get; set; } = string.Empty;

        [ApiMember( Name = "episodes", Description = "Episodes", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET" )]
        public string episodes { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/last_seen/{User}", "GET", Summary = "Get the Favorite Movie Years" )]
    public class GetLastSeen : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
        public bool episodes { get; set; } = false;
    }

    [Route( "/Statistics2026/database_status", "GET" )]
    public class GetDatabaseStatus : IReturn<GetDatabaseStatusReponse>
    {
    }
}
