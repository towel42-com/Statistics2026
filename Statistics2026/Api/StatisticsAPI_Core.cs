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
    public partial class Statistics2026API : IService, IRequiresRequest
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IHttpServer _httpServer;

        public Statistics2026API(ILogManager logger,
            IFileSystem fileSystem,
            IServerConfigurationManager config,
            IUserManager userManager,
            ILibraryManager libraryManager,
            ISessionManager sessionManager,
            IUserDataManager userDataManager,
            IHttpServer httpServer)
        {
            _logger = logger.GetLogger("Statistics2026 - Statistics2026API");
            _fileSystem = fileSystem;
            _config = config;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
            _userDataManager = userDataManager;
            _httpServer = httpServer;
        }

        public IRequest Request { get; set; }


        private IEnumerable<T> GetItems<T>(User user)
        {
            return DBHelperFuncs.GetUserItems<T>(user, _libraryManager);
        }

        static public (IEnumerable<Video>, IEnumerable<Video>) GetAllEpisodesAndMovies(User user, ILibraryManager libraryManager)
        {
            var episodesForUser = DBHelperFuncs.GetUserItems<Episode>(user, libraryManager).OfType<Video>().ToList();
            var moviesForUser = DBHelperFuncs.GetUserItems<Movie>(user, libraryManager).OfType<Video>().ToList();
            var forUser = episodesForUser.Concat(moviesForUser);

            var allEpisodes = DBHelperFuncs.GetUserItems<Episode>(null, libraryManager).OfType<Video>().ToList();
            var allMovies = DBHelperFuncs.GetUserItems<Movie>(null, libraryManager).OfType<Video>().ToList();
            var all = allEpisodes.Concat(allMovies);
            return (forUser, all);
        }

        static public IEnumerable<BoxSet> GetAllBoxSets(User user, ILibraryManager libraryManager)
        {
            var boxSets = DBHelperFuncs.GetUserItems<BoxSet>(user, libraryManager).OfType<BoxSet>().ToList();
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
                mediaInfos.Add(new MediaInfo(item, _fileSystem));
            }
            return mediaInfos;
        }

        private User GetUser(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                return null;

            var users = _userManager.GetUserList(new UserQuery() { Name = userName }).ToList();
            if (users.Count() == 0)
                return null;

            var user = users[0];
            return user;
        }

    }
}
