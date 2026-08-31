using MediaBrowser.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Tasks;
using Statistics2026.Data;
using Statistics2026.ScheduledTasks;
using System.Collections.Generic;
using System.Linq;

namespace Statistics2026.Api
{
    public partial class Statistics2026API : IService, IRequiresRequest
    {
        private readonly EmbyManagers _embyManagers;

        public Statistics2026API(
            ILogManager logManager,
            IServerConfigurationManager config,
            IUserManager userManager,
            IUserDataManager userDataManager,
            ILibraryManager libraryManager,
            IFileSystem fileSystem,
            IJsonSerializer jsonSerializer,
            IServerApplicationPaths serverApplicationPaths,
            IApplicationHost appHost,
            IProviderManager providerManager,
            ITaskManager taskManager
            )
        {
            _embyManagers = new EmbyManagers(fileSystem, libraryManager, logManager, logManager.GetLogger("Statistics2026 - Statistics2026API"), serverApplicationPaths, userDataManager, userManager, appHost, this, jsonSerializer, providerManager, config, taskManager);
        }

        public IRequest? Request { get; set; } = null;


        private IEnumerable<T>? GetItems<T>(User? user)
        {
            return DBHelper.GetUserItems<T>(user,_embyManagers!._libraryManager);
        }

        static public (IEnumerable<Video> forUser, IEnumerable<Video>? forAll) GetAllEpisodesAndMovies(User? user, ILibraryManager libManager, bool computeAll)
        {
            var episodesForUser = DBHelper.GetUserItems<Episode>(user, libManager).OfType<Video>().ToList();
            var moviesForUser = DBHelper.GetUserItems<Movie>(user, libManager).OfType<Video>().ToList();
            var forUser = episodesForUser.Concat(moviesForUser);

            IEnumerable<Video>? all = null;
            if (computeAll)
            {
                var allEpisodes = DBHelper.GetUserItems<Episode>(null, libManager).OfType<Video>().ToList();
                var allMovies = DBHelper.GetUserItems<Movie>(null, libManager).OfType<Video>().ToList();
                all = allEpisodes.Concat(allMovies);
            }
            return (forUser, all);
        }

        static public IEnumerable<BoxSet> GetAllBoxSets(User user, ILibraryManager libManager)
        {
            var boxSets = DBHelper.GetUserItems<BoxSet>(user, libManager).OfType<BoxSet>().ToList();
            return boxSets;
        }


        private List<MediaInfo>? GetVideos<T>(User? user) where T : Video
        {
            List<MediaInfo> mediaInfos = new List<MediaInfo>();
            var items = GetItems<T>(user);
            if (items == null)
                return null;
            foreach (var item in items)
            {
                mediaInfos.Add(new MediaInfo(item));
            }
            return mediaInfos;
        }

        private User? GetUser(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                return null;

            var users = _embyManagers._userManager.GetUserList(new UserQuery() { Name = userName }).ToList();
            if (users.Count() == 0)
                return null;

            var user = users[0];
            return user;
        }

    }
}
