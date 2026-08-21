using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Users;
using SQLitePCL;
using SQLitePCL.pretty;
using Statistics2026;
using Statistics2026.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Statistics2026.Api
{
    [Route("/Statistics2026/GetItemImageUrl/{ItemId}", "GET")]
    [Authenticated(Roles = "admin")]
    public class GetItemImageUrl : IReturn<GetItemImageUrlResponse>
    {
        public string ItemId { get; set; }
    }

    public class GetItemImageUrlResponse
    {
        public string Name { get; set; }
        public string PrimaryImageUrl { get; set; }
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
        public string serverId { get; set; }


        [ApiMember(Name = "rootDivName", Description = "Root Division Name", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string rootDivName { get; set; }

        [ApiMember(Name = "showAllCodecs", Description = "Show All Codecs", IsRequired = true, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool showAllCodecs { get; set; }

    }

    [Route("/Statistics2026/resolution_summary", "GET", Summary = "Gets Resolution Summary for Library")]
    [Authenticated(Roles = "admin")]
    public class GetResolutionSummary : IReturn<Object>
    {
        [ApiMember(Name = "serverId", Description = "Server ID", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string serverId { get; set; }


        [ApiMember(Name = "rootDivName", Description = "Root Division Name", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string rootDivName { get; set; }

        [ApiMember(Name = "showAllResolutions", Description = "Show All Resolutions", IsRequired = true, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool showAllResolutions { get; set; }

    }

    [Route("/Statistics2026/dvprofile_summary", "GET", Summary = "Gets Dolby Vision Profile Summary for Library")]
    [Authenticated(Roles = "admin")]
    public class GetDVProfileSummary : IReturn<Object>
    {
        [ApiMember(Name = "serverId", Description = "Server ID", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string serverId { get; set; }


        [ApiMember(Name = "rootDivName", Description = "Root Division Name", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string rootDivName { get; set; }

        [ApiMember(Name = "showUnknownDVProfiles", Description = "Show Unknown Dolby Vision Profile", IsRequired = true, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool showUnknownDVProfiles { get; set; }
        [ApiMember(Name = "showAllDVProfiles", Description = "Show All Dolby Vision Profiles", IsRequired = true, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool showAllDVProfiles { get; set; }
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

    [Route("/Statistics2026/total_movie_count", "GET", Summary = "Get the total Movie Count")]
    [Authenticated(Roles = "admin")]
    public class GetTotalMovieCount : IReturn<Object>
    {
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
    public class GetTotalTVCount : IReturn<Object>
    {
    }

    [Route("/Statistics2026/get_movie/{WhichStatistic}", "GET", Summary = "Get the movie in the database")]
    [Authenticated(Roles = "admin")]
    public class GetMovie : IReturn<Object>
    {
        [ApiMember(Name = "serverId", Description = "Server ID", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string serverId { get; set; }

        public StatGen.EStatisticType whichStatistic { get; set; }
    }

    [Route("/Statistics2026/get_series/{WhichStatistic}", "GET", Summary = "Get the series information from the database")]
    [Authenticated(Roles = "admin")]
    public class GetSeries : IReturn<Object>
    {
        [ApiMember(Name = "serverId", Description = "Server ID", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string serverId { get; set; }

        public StatGen.EStatisticType whichStatistic { get; set; }
    }

    [Route("/Statistics2026/least_watch_shows", "GET", Summary = "Get the total TV Count")]
    [Authenticated(Roles = "admin")]
    public class GetLeastWatchedShows : IReturn<Object>
    {
        [ApiMember(Name = "serverId", Description = "Server ID", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string serverId { get; set; }
    }
}
