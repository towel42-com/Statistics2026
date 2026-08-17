using Statistics20;
using Statistics20.Data;
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

namespace Statistics20.Api
{
    // http://localhost:8096/emby/codec_info/episode_list
    [Route("/codec_info/episode_list", "GET", Summary = "Gets Codec Info for Episodes")]
    [Authenticated(Roles = "admin")]
    public class GetEpisodeList : IReturn<Object>
    {

    }

    // http://localhost:8096/emby/codec_info/movie_list
    [Route("/codec_info/movie_list", "GET", Summary = "Gets Codec Info for Movies")]
    [Authenticated(Roles = "admin")]
    public class GetMovieList : IReturn<Object>
    {

    }

    [Route("/codec_info/codec_summary", "GET", Summary = "Gets Codec Summary for Library")]
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

    [Route("/codec_info/resolution_summary", "GET", Summary = "Gets Resolution Summary for Library")]
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

    [Route("/codec_info/dvprofile_summary", "GET", Summary = "Gets Dolby Vision Profile Summary for Library")]
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

    public class Statistics20API : IService, IRequiresRequest
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly ILibraryManager _libraryManager;

        public Statistics20API(ILogManager logger,
            IFileSystem fileSystem,
            IServerConfigurationManager config,
            IUserManager userManager,
            ILibraryManager libraryManager,
            ISessionManager sessionManager,
            IUserDataManager userDataManager)
        {
            _logger = logger.GetLogger("Statistics20 - Statistics20API");
            _fileSystem = fileSystem;
            _config = config;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
            _userDataManager = userDataManager;
        }

        public IRequest Request { get; set; }

        private IEnumerable<T> GetItems<T>()
        {
            var query = new InternalItemsQuery(null)
            {
                IncludeItemTypes = new[] { typeof(T).Name },
                Recursive = true,
                IsVirtualItem = false,
                DtoOptions = new DtoOptions(true)
                {
                    EnableImages = false
                }
            };

            return _libraryManager.GetItemList(query).OfType<T>();
        }

        private List<MediaInfo> GetVideos<T>() where T : Video
        {
            List<MediaInfo> mediaInfos = new List<MediaInfo>();
            var items = GetItems<T>();
            foreach (var item in items)
            {
                mediaInfos.Add(new MediaInfo(item));
            }
            return mediaInfos;
        }

        public object Get(GetEpisodeList request)
        {
            _logger.Info("GetEpisodeList");
            var retVal = GetVideos<Episode>();
            retVal = retVal.OrderBy(x => x.SortName).ThenBy(x => x.Season).ThenBy(x => x.Episode).ToList();
            return retVal;
        }

        public object Get(GetMovieList request)
        {
            _logger.Info("GetMovieList");
            var retVal = GetVideos<Movie>();
            retVal = retVal.OrderBy(x => x.SortName).ThenBy(x => x.StartYear).ToList();
            return retVal;
        }

        public object Get(GetCodecSummary request)
        {
            _logger.Info("GetCodecSummary");

            var db = ConfigInfoDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

            var serverId = request.serverId ?? "";
            var rootDivName = request.rootDivName ?? "";
            var showAllResolutions = request.showAllCodecs;

            var groupData = db.CalculateMediaCodecs(showAllResolutions);

            var vgReponse = groupData.createStat(serverId, rootDivName);

            return vgReponse;
        }

        public object Get(GetResolutionSummary request)
        {
            _logger.Info("GetResolutionSummary");

            var db = ConfigInfoDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var serverId = request.serverId ?? "";
            var rootDivName = request.rootDivName ?? "";
            var showAllResolutions = request.showAllResolutions;

            var groupData = db.CalculateMediaResolutions(showAllResolutions);

            var vgReponse = groupData.createStat(serverId, rootDivName);

            return vgReponse;
        }

        public object Get(GetDVProfileSummary request)
        {
            _logger.Info("GetDVProfileSummary");

            var db = ConfigInfoDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

            var serverId = request.serverId ?? "";
            var rootDivName = request.rootDivName ?? "";
            var showUnknownDVProfiles = request.showUnknownDVProfiles;
            var showAllDVProfiles = request.showAllDVProfiles;

            var groupData = db.CalculateDVProfileInfo(showUnknownDVProfiles, showAllDVProfiles);

            var vgReponse = groupData.createStat(serverId, rootDivName);

            return vgReponse;
        }
    }
}
