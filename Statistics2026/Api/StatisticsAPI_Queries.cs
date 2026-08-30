using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;
using System;

namespace Statistics2026.Api
{
    [Route("/Statistics2026/GetItemImageUrl/{ItemId}", "GET")]
    [Authenticated(Roles = "admin")]
    public class GetItemImageUrl : IReturn<GetItemImageUrlResponse>
    {
        public string ItemId { get; set; } = String.Empty;
    }

    public class GetItemImageUrlResponse
    {
        public string Name { get; set; } = string.Empty;
        public string PrimaryImageUrl { get; set; } = string.Empty;
    }

    // http://localhost:8096/emby/Statistics2026/episode_list
    [Route("/Statistics2026/episode_list", "GET", Summary = "Gets Codec Info for Episodes")]
    [Authenticated(Roles = "admin")]
    public class GetEpisodeList : IReturn<Object>
    {

    }

    // http://localhost:8096/emby/Statistics2026/movie_list
    [Route("/Statistics2026/movie_list", "GET", Summary = "Gets Codec Info for Movies")]
    [Authenticated(Roles = "admin")]
    public class GetMovieList : IReturn<Object>
    {

    }

    [Route("/Statistics2026/codec_summary", "GET", Summary = "Gets Codec Summary for Library")]
    [Authenticated(Roles = "admin")]
    public class GetCodecSummary : IReturn<Object>
    {
        [ApiMember(Name = "serverId", Description = "Server ID", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string serverId { get; set; } = String.Empty;


        [ApiMember(Name = "rootDivName", Description = "Root Division Name", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string rootDivName { get; set; } = String.Empty;

        [ApiMember(Name = "showAllCodecs", Description = "Show All Codecs", IsRequired = true, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool showAllCodecs { get; set; }

    }

    [Route("/Statistics2026/resolution_summary", "GET", Summary = "Gets Resolution Summary for Library")]
    [Authenticated(Roles = "admin")]
    public class GetResolutionSummary : IReturn<Object>
    {
        [ApiMember(Name = "serverId", Description = "Server ID", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string serverId { get; set; } = String.Empty;


        [ApiMember(Name = "rootDivName", Description = "Root Division Name", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string rootDivName { get; set; } = String.Empty;

        [ApiMember(Name = "showAllResolutions", Description = "Show All Resolutions", IsRequired = true, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool showAllResolutions { get; set; }

    }

    [Route("/Statistics2026/dvprofile_summary", "GET", Summary = "Gets Dolby Vision Profile Summary for Library")]
    [Authenticated(Roles = "admin")]
    public class GetDVProfileSummary : IReturn<Object>
    {
        [ApiMember(Name = "serverId", Description = "Server ID", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string serverId { get; set; } = String.Empty;


        [ApiMember(Name = "rootDivName", Description = "Root Division Name", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string rootDivName { get; set; } = String.Empty;

        [ApiMember(Name = "showUnknownDVProfiles", Description = "Show Unknown Dolby Vision Profile", IsRequired = true, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool showUnknownDVProfiles { get; set; } = false;
        [ApiMember(Name = "showAllDVProfiles", Description = "Show All Dolby Vision Profiles", IsRequired = true, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool showAllDVProfiles { get; set; } = false;
    }

    [Route("/Statistics2026/user_count", "GET", Summary = "Gets the total User Count")]
    [Authenticated(Roles = "admin")]
    public class GetUserCount : IReturn<Object>
    {
        [ApiMember(Name = "hasConnectUserID", Description = "Include only if HasConnectUserId = true", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool hasConnectUserID { get; set; } = false;

        [ApiMember(Name = "excludeAdmin", Description = "Exclude Administrators from analysis", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool excludeAdmin { get; set; } = true;
    }

    [Route("/Statistics2026/most_active_users", "GET", Summary = "Gets the top 5 most active users")]
    [Authenticated(Roles = "admin")]
    public class GetMostActiveUsers : IReturn<Object>
    {
        [ApiMember(Name = "hasConnectUserID", Description = "Include only if HasConnectUserId = true", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool hasConnectUserID { get; set; } = false;

        [ApiMember(Name = "numUsers", Description = "Show the top X users", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int numUsers { get; set; } = 0;

        [ApiMember(Name = "excludeAdmin", Description = "Exclude Administrators from analysis", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool excludeAdmin { get; set; } = true;
    }

    [Route("/Statistics2026/total_movie_count/{User}", "GET", Summary = "Get the total Movie Count")]
    [Authenticated(Roles = "admin")]
    public class GetTotalMovieCount : IReturn<Object>
    {
        public string user { get; set; } = String.Empty;
    }

    [Route("/Statistics2026/total_movie_count", "GET", Summary = "Get the total Movie Count")]
    [Authenticated(Roles = "admin")]
    public class GetTotalMovieCountNoUser : IReturn<Object>
    {
    }


    [Route("/Statistics2026/total_movies_watched/{User}", "GET", Summary = "Get the total Movie Count")]
    [Authenticated(Roles = "admin")]
    public class GetTotalMoviesWatched : IReturn<Object>
    {
        public string user { get; set; } = String.Empty;
    }

    [Route("/Statistics2026/total_tv_watched/{User}", "GET", Summary = "Get the total Movie Count")]
    [Authenticated(Roles = "admin")]
    public class GetTotalTVWatched : IReturn<Object>
    {
        public string user { get; set; } = String.Empty;
    }

    [Route("/Statistics2026/total_series_finished/{User}", "GET", Summary = "Get the total Movie Count")]
    [Authenticated(Roles = "admin")]
    public class GetTotalSeriesFinished : IReturn<Object>
    {
        public string user { get; set; } = String.Empty;
    }
    
    [Route("/Statistics2026/movie_favorite_years/{User}", "GET", Summary = "Get the Favorite Movie Years")]
    [Authenticated(Roles = "admin")]
    public class GetMovieFavoriteYears : IReturn<Object>
    {
        public string user { get; set; } = String.Empty;
    }

    [Route("/Statistics2026/movie_favorite_genres/{User}", "GET", Summary = "Get the Favorite Movie Genres")]
    [Authenticated(Roles = "admin")]
    public class GetMovieFavoriteGenres : IReturn<Object>
    {
        public string user { get; set; } = String.Empty;
    }

    [Route("/Statistics2026/tv_favorite_genres/{User}", "GET", Summary = "Get the Favorite Movie Genres")]
    [Authenticated(Roles = "admin")]
    public class GetTVFavoriteGenres : IReturn<Object>
    {
        public string user { get; set; } = String.Empty;
    }
    

    [Route("/Statistics2026/total_collection_count", "GET", Summary = "Get the total Collection Count")]
    [Authenticated(Roles = "admin")]
    public class GetTotalCollectionCount : IReturn<Object>
    {
    }

    [Route("/Statistics2026/total_movie_studio_count", "GET", Summary = "Get the total Movie Studio Count")]
    [Authenticated(Roles = "admin")]
    public class GetTotalMovieStudioCount : IReturn<Object>
    {
    }

    [Route("/Statistics2026/total_tv_studio_count", "GET", Summary = "Get the total TV Studio Count")]
    [Authenticated(Roles = "admin")]
    public class GetTotalTVStudioCount : IReturn<Object>
    {
    }

    [Route("/Statistics2026/total_tv_count", "GET", Summary = "Get the total TV Count")]
    [Authenticated(Roles = "admin")]
    public class GetTotalTVCountNoUser : IReturn<Object>
    {
    }

    [Route("/Statistics2026/total_tv_count/{User}", "GET", Summary = "Get the total TV Count")]
    [Authenticated(Roles = "admin")]
    public class GetTotalTVCount : IReturn<Object>
    {
        public string user { get; set; } = String.Empty;
    }

    [Route("/Statistics2026/get_movie/{WhichStatistic}", "GET", Summary = "Get the movie statistic in from the database")]
    [Authenticated(Roles = "admin")]
    public class GetMovie : IReturn<Object>
    {
        [ApiMember(Name = "serverId", Description = "Server ID", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string serverId { get; set; } = String.Empty;

        public StatGen.EStatisticType whichStatistic { get; set; }
    }

    [Route("/Statistics2026/get_series/{WhichStatistic}", "GET", Summary = "Get the series statistic information from the database")]
    [Authenticated(Roles = "admin")]
    public class GetSeries : IReturn<Object>
    {
        [ApiMember(Name = "serverId", Description = "Server ID", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string serverId { get; set; } = String.Empty;

        public StatGen.EStatisticType whichStatistic { get; set; }
    }

    [Route("/Statistics2026/get_episode/{WhichStatistic}", "GET", Summary = "Get the episode statistic information from the database")]
    [Authenticated(Roles = "admin")]
    public class GetEpisode : IReturn<Object>
    {
        [ApiMember(Name = "serverId", Description = "Server ID", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string serverId { get; set; } = String.Empty;

        public StatGen.EStatisticType whichStatistic { get; set; }
    }

    [Route("/Statistics2026/least_watched_shows", "GET", Summary = "Get the List of Least Watched Shows")]
    [Authenticated(Roles = "admin")]
    public class GetLeastWatchedShows : IReturn<Object>
    {
        [ApiMember(Name = "serverId", Description = "Server ID", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string serverId { get; set; } = String.Empty;
    }

    [Route("/Statistics2026/most_watched_shows", "GET", Summary = "Get the List of Most Watched Shows")]
    [Authenticated(Roles = "admin")]
    public class GetMostWatchedShows : IReturn<Object>
    {
        [ApiMember(Name = "serverId", Description = "Server ID", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string serverId { get; set; } = String.Empty;
    }

    [Route("/Statistics2026/total_time_watched/{User}", "GET", Summary = "Get the Total Time Watched for User")]
    [Authenticated(Roles = "admin")]
    public class GetTotalTimeWatched : IReturn<Object>
    {
        public string user { get; set; } = String.Empty;

        [ApiMember(Name = "episodes", Description = "Episodes", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string episodes { get; set; } = String.Empty;
    }

    [Route("/Statistics2026/total_watchable_time/{User}", "GET", Summary = "Get the Total Time Watched for User")]
    [Authenticated(Roles = "admin")]
    public class GetTotalWatchableTime : IReturn<Object>
    {
        public string user { get; set; } = String.Empty;

        [ApiMember(Name = "episodes", Description = "Episodes", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string episodes { get; set; } = String.Empty;
    }

    [Route("/Statistics2026/last_seen/{User}", "GET", Summary = "Get the Favorite Movie Years")]
    [Authenticated(Roles = "admin")]
    public class GetLastSeen : IReturn<Object>
    {
        public string user { get; set; } = String.Empty;
        public bool episodes { get; set; } = false;
    }

}
