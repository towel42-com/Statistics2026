using Statistics2026;
using Statistics2026.Data;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Users;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Statistics2026.Api
{
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

    [Route("/Statistics2026/total_studio_count", "GET", Summary = "Get the total Studio Count")]
    [Authenticated(Roles = "admin")]
    public class GetTotalStudioCount : IReturn<Object>
    {
    }

    public class Statistics2026API : IService, IRequiresRequest
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly ILibraryManager _libraryManager;

        public Statistics2026API(ILogManager logger,
            IFileSystem fileSystem,
            IServerConfigurationManager config,
            IUserManager userManager,
            ILibraryManager libraryManager,
            ISessionManager sessionManager,
            IUserDataManager userDataManager)
        {
            _logger = logger.GetLogger("Statistics2026 - Statistics2026API");
            _fileSystem = fileSystem;
            _config = config;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
            _userDataManager = userDataManager;
        }

        public IRequest Request { get; set; }

        static public IEnumerable<T> GetItems<T>(User user, ILibraryManager libraryManager)
        {
            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(T).Name },
                Recursive = true,
                IsVirtualItem = false,
                DtoOptions = new DtoOptions(true)
                {
                    EnableImages = false
                }
            };

            return libraryManager.GetItemList(query).OfType<T>();
        }

        private IEnumerable<T> GetItems<T>(User user)
        {
            return GetItems<T>(user, _libraryManager);
        }

        static public IEnumerable<Video> GetAllEpisodesAndMovies(User user, ILibraryManager libraryManager)
        {
            var episodes = GetItems<Episode>(user, libraryManager).OfType<Video>().ToList();
            var movies = GetItems<Movie>(user, libraryManager).OfType<Video>().ToList();
            return episodes.Concat(movies);
        }

        static public IEnumerable<BoxSet> GetAllBoxSets(User user, ILibraryManager libraryManager)
        {
            var boxSets = GetItems<BoxSet>(user, libraryManager).OfType<BoxSet>().ToList();
            return boxSets;
        }


        private IEnumerable<Video> GetAllEpisodesAndMovies(User user)
        {
            var episodes = GetItems<Episode>(user).OfType<Video>().ToList();
            var movies = GetItems<Movie>(user).OfType<Video>().ToList();
            return episodes.Concat(movies);
        }

        private List<MediaInfo> GetVideos<T>(User user) where T : Video
        {
            List<MediaInfo> mediaInfos = new List<MediaInfo>();
            var items = GetItems<T>(user);
            foreach (var item in items)
            {
                mediaInfos.Add(new MediaInfo(item));
            }
            return mediaInfos;
        }

        public object Get(GetEpisodeList request)
        {
            _logger.Debug("Request: GetEpisodeList");
            var retVal = GetVideos<Episode>(null);
            retVal = retVal.OrderBy(x => x.SortName).ThenBy(x => x.Season).ThenBy(x => x.Episode).ToList();
            return retVal;
        }

        public object Get(GetMovieList request)
        {
            _logger.Debug("Request: GetMovieList");
            var retVal = GetVideos<Movie>(null);
            retVal = retVal.OrderBy(x => x.SortName).ThenBy(x => x.StartYear).ToList();
            return retVal;
        }

        public object Get(GetCodecSummary request)
        {
            _logger.Debug("Request: GetCodecSummary");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

            var serverId = request.serverId ?? "";
            var rootDivName = request.rootDivName ?? "";
            var showAllResolutions = request.showAllCodecs;

            var groupData = db.CalculateMediaCodecs(showAllResolutions);
            groupData.ServerId = serverId;
            groupData.HtmlDivId = rootDivName;
            groupData.SortByKey = true;
            var vgReponse = groupData.createStat();

            return vgReponse;
        }

        public object Get(GetResolutionSummary request)
        {
            _logger.Debug("Request: GetResolutionSummary");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var serverId = request.serverId ?? "";
            var rootDivName = request.rootDivName ?? "";
            var showAllResolutions = request.showAllResolutions;

            var groupData = db.CalculateMediaResolutions(showAllResolutions);
            groupData.ServerId = serverId;
            groupData.HtmlDivId = rootDivName;
            groupData.SortByKey = false;
            var vgReponse = groupData.createStat();

            return vgReponse;
        }

        public object Get(GetDVProfileSummary request)
        {
            _logger.Debug("Request: GetDVProfileSummary");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

            var serverId = request.serverId ?? "";
            var rootDivName = request.rootDivName ?? "";
            var showUnknownDVProfiles = request.showUnknownDVProfiles;
            var showAllDVProfiles = request.showAllDVProfiles;

            var groupData = db.CalculateDVProfileInfo(showUnknownDVProfiles, showAllDVProfiles);
            groupData.ServerId = serverId;
            groupData.HtmlDivId = rootDivName;
            groupData.SortByKey = true;
            var vgReponse = groupData.createStat();

            return vgReponse;
        }

        public object Get(GetUserCount request)
        {
            _logger.Debug("Request: GetUserCount");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var hasConnectUserID = request.hasConnectUserID;
            var excludeAdmin = request.excludeAdmin;

            var groupData = db.CalculateUserCount(hasConnectUserID, excludeAdmin, _userManager);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetMostActiveUsers request)
        {
            _logger.Debug("Request: GetMostActiveUsers");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var hasConnectUserID = request.hasConnectUserID;
            var numUsers = request.numUsers;
            var excludeAdmin = request.excludeAdmin;

            var groupData = db.CalculateMostActiveUsers(hasConnectUserID, numUsers, excludeAdmin, _userManager);
            groupData.SortByKey = false;
            var vgReponse = groupData.createStat();

            return vgReponse;
        }

        public object Get(GetTotalMovieCount request)
        {
            _logger.Debug("Request: GetTotalMovieCount");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

            var groupData = db.CalculateTotalMovieCount(null);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetTotalCollectionCount request)
        {
            _logger.Debug("Request: GetTotalCollectionCount");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

            //var groupData = db.CalculateTotalCollectionCount(null);
            //var vgReponse = groupData.createStat();
            return "";
        }

        public object Get(GetTotalStudioCount request)
        {
            _logger.Debug("Request: GetTotalStudioCount ");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

            var groupData = db.CalculateTotalMovieStudioCount(null);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }
    }
}
