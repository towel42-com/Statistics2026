using Emby.Media.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Services;
using Statistics2026.Data;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Statistics2026.Api
{
    public partial class Statistics2026API : IService, IRequiresRequest
    {
        private object GetRequest(string requestName, Func<AutoTimer, object> requestFunc)
        {
            using (var timer = new AutoTimer($"Request: {requestName}", _embyManagers._logger))
            {
                try
                {
                    return requestFunc( timer );
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetTVSeriesProgress request)
        {
            return GetRequest("GetTVSeriesProgress", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var userName = request.user;
                var user = GetUser(userName);
                if (user == null)
                    return null!;

                var retVal = db.GetTVSeriesProgress(user);
                return retVal;
            });
        }

        public object Get(GetEpisodeList request)
        {
            return GetRequest("GetEpisodeList", timer =>
            {
                var retVal = GetVideos<Episode>(null);
                retVal = retVal.OrderBy(x => x.SortName).ThenBy(x => x.Season).ThenBy(x => x.Episode).ToList();
                return retVal;
            });
        }

        public object Get(GetMovieList request)
        {
            return GetRequest("GetMovieList", timer =>
            {
                var retVal = GetVideos<Movie>(null);
                retVal = retVal.OrderBy(x => x.SortName).ThenBy(x => x.StartYear).ToList();
                return retVal;
            });
        }

        public object Get(GetCodecSummary request)
        {
            return GetRequest("GetCodecSummary", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);

                var serverId = request.serverId ?? "";
                var rootDivName = request.rootDivName ?? "";

                var groupData = db.MediaCodecs();
                groupData.ServerId = serverId;
                groupData.HtmlDivId = rootDivName;
                groupData.SortByKey = true;
                var vgReponse = groupData.createStat();

                return vgReponse;
            });
        }

        public object Get(GetResolutionSummary request)
        {
            return GetRequest("GetResolutionSummary", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var serverId = request.serverId ?? "";
                var rootDivName = request.rootDivName ?? "";
                var showAllResolutions = request.showAllResolutions;

                var groupData = db.MediaResolutions(showAllResolutions);
                groupData.ServerId = serverId;
                groupData.HtmlDivId = rootDivName;
                groupData.SortByKey = false;
                var vgReponse = groupData.createStat();

                return vgReponse;
            });
        }

        public object Get(GetDVProfileSummary request)
        {
            return GetRequest("GetDVProfileSummary", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);

                var serverId = request.serverId ?? "";
                var rootDivName = request.rootDivName ?? "";
                var showUnknownDVProfiles = request.showUnknownDVProfiles;

                var groupData = db.DVProfileInfo(showUnknownDVProfiles);
                groupData.ServerId = serverId;
                groupData.HtmlDivId = rootDivName;
                groupData.SortByKey = true;
                var vgReponse = groupData.createStat();

                return vgReponse;
            });
        }

        public object Get(GetUserCount request)
        {
            return GetRequest("GetUserCount", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var hasConnectUserID = request.hasConnectUserID;
                var excludeAdmin = request.excludeAdmin;

                var groupData = db.UserCount(hasConnectUserID, excludeAdmin);
                var vgReponse = groupData.createStat();
                return vgReponse;
            });
        }

        public object Get(GetMostActiveUsers request)
        {
            return GetRequest("GetMostActiveUsers", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var hasConnectUserID = request.hasConnectUserID;
                var numUsers = request.numUsers;
                var excludeAdmin = request.excludeAdmin;

                var groupData = db.MostActiveUsers(hasConnectUserID, numUsers, excludeAdmin);
                groupData.SortByKey = false;
                var vgReponse = groupData.createStat();

                return vgReponse;
            });
        }

        public object TotalMovieCount(User? user)
        {
            var db = StatisticsDB.GetInstance(_embyManagers);
            var groupData = db.TotalMovieCount(user, false);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetTotalMovieCount request)
        {
            return GetRequest("GetTotalMovieCount", timer =>
            {
                var userName = request.user;
                var user = GetUser(userName);
                if (user == null)
                    return null!;

                return TotalMovieCount(user);
            });
        }

        public object Get(GetTotalMovieCountNoUser request)
        {
            return GetRequest("GetTotalMovieCountNoUser", timer =>
            {
                return TotalMovieCount(null);
            });
        }

        public object Get(GetTotalMoviesWatched request)
        {
            return GetRequest("GetTotalMoviesWatched", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var userName = request.user;
                var user = GetUser(userName);
                if (user == null)
                    return null!;

                var groupData = db.TotalMovieCount(user, true);
                var vgReponse = groupData.createStat();
                return vgReponse;
            });
        }

        public object Get(GetTotalCollectionCount request)
        {
            return GetRequest("GetTotalCollectionCount", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var groupData = db.TotalCollectionCount();
                var vgReponse = groupData.createStat();
                return vgReponse;
            });
        }

        public object Get(GetTotalMovieStudioCount request)
        {
            return GetRequest("GetTotalMovieStudioCount", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);

                var groupData = db.TotalMovieStudioCount(null);
                var vgReponse = groupData.createStat();
                return vgReponse;
            });
        }

        public object Get(GetItemImageUrl request)
        {
            return GetRequest("GetItemImageUrl", timer =>
            {
                var retVal = new GetItemImageUrlResponse { Name = "", PrimaryImageUrl = "" };
                var item = _embyManagers._libraryManager.GetItemById(request.ItemId);
                if (item == null)
                    return null!;

                var url = ItemImageUrl._ItemImageUrl(item);
                if (url == null)
                    retVal.PrimaryImageUrl = String.Empty;
                else
                    retVal.PrimaryImageUrl = url;
                if (retVal.PrimaryImageUrl.IsNullOrEmpty())
                    return retVal;
                retVal.Name = item.Name;
                return retVal;
            });
        }

        public object TotalTVCount(User? user, bool watched)
        {
            var db = StatisticsDB.GetInstance(_embyManagers);
            var groupData = db.TotalTVCount(user, watched);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetTotalTVCount request)
        {
            return GetRequest("GetTotalTVCount", timer =>
            {
                var userName = request.user;
                var user = GetUser(userName);
                if (user == null)
                    return null!;

                return TotalTVCount(user, false);
            });
        }

        public object Get(GetTotalTVCountNoUser request)
        {
            return GetRequest("GetTotalTVCountNoUser", timer =>
            {
                return TotalTVCount(null, false);
            });
        }

        public object Get(GetTotalTVWatched request)
        {
            return GetRequest("GetTotalTVWatched", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var userName = request.user;
                var user = GetUser(userName);
                if (user == null)
                    return null!;

                var groupData = db.TotalTVCount(user, true);
                var vgReponse = groupData.createStat();
                return vgReponse;
            });
        }

        public object Get(GetTotalSeriesFinished request)
        {
            return GetRequest("GetTotalSeriesFinished", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var userName = request.user;
                var user = GetUser(userName);
                if (user == null)
                    return null!;

                var groupData = db.TotalFinishedSeries(user);
                var vgReponse = groupData.createStat();
                return vgReponse;
            });
        }

        public object Get(GetTotalTVStudioCount request)
        {
            return GetRequest("GetTotalTVStudioCount", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);

                var groupData = db.TotalTVStudioCount(null);
                var vgReponse = groupData.createStat();
                return vgReponse;
            });
        }

        public object Get(GetLeastWatchedMovies request)
        {
            return GetRequest("GetLeastWatchedMovies", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var serverId = request.serverId ?? "";
                var numMovies = request.numMovies;
                var excludeAdmin = request.excludeAdmin;

                var groupData = db.WatchedMedia(null, true, numMovies, excludeAdmin, false);
                groupData.ServerId = serverId;

                var vgReponse = groupData.createStat();
                return vgReponse;
            });
        }

        public object Get(GetMostWatchedMovies request)
        {
            return GetRequest("GetMostWatchedMovies", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var serverId = request.serverId ?? "";
                var numMovies = request.numMovies;
                var excludeAdmin = request.excludeAdmin;

                var groupData = db.WatchedMedia(null, false, numMovies, excludeAdmin, false);
                groupData.ServerId = serverId;

                var vgReponse = groupData.createStat();
                return vgReponse;
            });
        }


        public object Get(GetLeastWatchedShows request)
        {
            return GetRequest("GetLeastWatchedShows", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var serverId = request.serverId ?? "";
                var numShows = request.numShows;
                var excludeAdmin = request.excludeAdmin;

                var groupData = db.WatchedMedia(null, true, numShows, excludeAdmin, true);
                groupData.ServerId = serverId;

                var vgReponse = groupData.createStat();
                return vgReponse;
            });
        }

        public object Get(GetMostWatchedShows request)
        {
            return GetRequest("GetMostWatchedShows", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var serverId = request.serverId ?? "";
                var numShows = request.numShows;
                var excludeAdmin = request.excludeAdmin;

                var groupData = db.WatchedMedia(null, false, numShows, excludeAdmin, true);
                groupData.ServerId = serverId;

                var vgReponse = groupData.createStat();
                return vgReponse;
            });
        }
        public object Get(GetTotalTimeWatched request)
        {
            return GetRequest("GetTotalTimeWatched", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var userName = request.user;
                var user = GetUser(userName);
                if (user == null)
                    return null!;

                bool? showEpisodes = null;
                if (request.episodes == "true")
                    showEpisodes = true;
                else if (request.episodes == "all")
                    showEpisodes = null;
                else
                    showEpisodes = false;

                var groupData = db.TotalTimeWatched(user, showEpisodes);

                var vgReponse = groupData.createStat();
                return vgReponse;
            });
        }

        public object Get(GetTotalWatchableTime request)
        {
            return GetRequest("GetTotalWatchableTime", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var userName = request.user;
                var user = GetUser(userName);
                if (user == null)
                    return null!;

                bool? showEpisodes = null;
                if (request.episodes == "true")
                    showEpisodes = true;
                else if (request.episodes == "all")
                    showEpisodes = null;
                else
                    showEpisodes = false;

                var groupData = db.TotalWatchableTime(user, showEpisodes);

                var vgReponse = groupData.createStat();
                return vgReponse;
            });
        }

        public object Get(GetLastSeen request)
        {
            return GetRequest("GetLastSeen", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var userName = request.user;
                var user = GetUser(userName);
                if (user == null)
                    return null!;
                var episodes = request.episodes;

                var groupData = db.LastSeen(user, !episodes);
                var vgReponse = groupData.createStat();
                return vgReponse;
            });
        }

        public object Get(GetMovieFavoriteYears request)
        {
            return GetRequest("GetMovieFavoriteYears", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var userName = request.user;
                var user = GetUser(userName);
                if (user == null)
                    return null!;

                var groupData = db.FavoriteYears(user, true);
                var vgReponse = groupData.createStat();
                return vgReponse;
            });
        }

        public object Get(GetMovieFavoriteGenres request)
        {
            return GetRequest("GetMovieFavoriteGenres", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var userName = request.user;
                var user = GetUser(userName);
                if (user == null)
                    return null!;

                var groupData = db.FavoriteGenre(user, true);
                var vgReponse = groupData.createStat();
                return vgReponse;
            });
        }

        public object Get(GetTVFavoriteGenres request)
        {
            return GetRequest("GetTVFavoriteGenres", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var userName = request.user;
                var user = GetUser(userName);
                if (user == null)
                    return null!;

                var groupData = db.FavoriteGenre(user, false);
                var vgReponse = groupData.createStat();
                return vgReponse;
            });
        }

        public object Get(GetMovie request)
        {
            return GetRequest("GetMovie", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var serverId = request.serverId ?? "";
                var whichStatistic = request.whichStatistic;
                timer.Text += $" - {whichStatistic}";

                var groupData = db.StatisticFor(null, whichStatistic, StatGen.EVideoType.Movie);
                groupData.ServerId = serverId;
                return groupData.createStat();
            });
        }

        public object Get(GetSeries request)
        {
            return GetRequest("GetSeries", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var serverId = request.serverId ?? "";
                var whichStatistic = request.whichStatistic;
                timer.Text += $" - {whichStatistic}";

                var groupData = db.StatisticFor(null, whichStatistic, StatGen.EVideoType.Series);
                groupData.ServerId = serverId;
                return groupData.createStat();
            });
        }

        public object Get(GetEpisode request)
        {
            return GetRequest("GetEpisode", timer =>
            {
                var db = StatisticsDB.GetInstance(_embyManagers);
                var serverId = request.serverId ?? "";
                var whichStatistic = request.whichStatistic;
                timer.Text += $" - {whichStatistic}";

                var groupData = db.StatisticFor(null, whichStatistic, StatGen.EVideoType.Episode);
                groupData.ServerId = serverId;

                return groupData.createStat();
            });
        }
    }
}
