using Emby.Media.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Services;
using Statistics2026.Data;
using System;
using System.Linq;

namespace Statistics2026.Api
{
    public partial class Statistics2026API : IService, IRequiresRequest
    {
        //using (var timer = new AutoTimer($"Adding All Users", _logger))
        public object Get(GetEpisodeList request)
        {
            using (var timer = new AutoTimer("Request: GetEpisodeList", _embyManagers._logger))
            {
                try
                {
                    var retVal = GetVideos<Episode>(null);
                    retVal = retVal.OrderBy(x => x.SortName).ThenBy(x => x.Season).ThenBy(x => x.Episode).ToList();
                    return retVal;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetMovieList request)
        {
            using (var timer = new AutoTimer("Request: GetMovieList", _embyManagers._logger))
            {
                try
                {
                    var retVal = GetVideos<Movie>(null);
                    retVal = retVal.OrderBy(x => x.SortName).ThenBy(x => x.StartYear).ToList();
                    return retVal;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetCodecSummary request)
        {
            using (var timer = new AutoTimer("Request: GetCodecSummary", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_embyManagers);

                    var serverId = request.serverId ?? "";
                    var rootDivName = request.rootDivName ?? "";
                    var showAllResolutions = request.showAllCodecs;

                    var groupData = db.MediaCodecs(showAllResolutions);
                    groupData.ServerId = serverId;
                    groupData.HtmlDivId = rootDivName;
                    groupData.SortByKey = true;
                    var vgReponse = groupData.createStat();

                    return vgReponse;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetResolutionSummary request)
        {
            using (var timer = new AutoTimer("Request: GetResolutionSummary", _embyManagers._logger))
            {
                try
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
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetDVProfileSummary request)
        {
            using (var timer = new AutoTimer("Request: GetDVProfileSummary", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_embyManagers);

                    var serverId = request.serverId ?? "";
                    var rootDivName = request.rootDivName ?? "";
                    var showUnknownDVProfiles = request.showUnknownDVProfiles;
                    var showAllDVProfiles = request.showAllDVProfiles;

                    var groupData = db.DVProfileInfo(showUnknownDVProfiles, showAllDVProfiles);
                    groupData.ServerId = serverId;
                    groupData.HtmlDivId = rootDivName;
                    groupData.SortByKey = true;
                    var vgReponse = groupData.createStat();

                    return vgReponse;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetUserCount request)
        {
            using (var timer = new AutoTimer("Request: GetUserCount", _embyManagers._logger))
            {
                try
                {

                    var db = StatisticsDB.GetInstance(_embyManagers);
                    var hasConnectUserID = request.hasConnectUserID;
                    var excludeAdmin = request.excludeAdmin;

                    var groupData = db.UserCount(hasConnectUserID, excludeAdmin);
                    var vgReponse = groupData.createStat();
                    return vgReponse;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetMostActiveUsers request)
        {
            using (var timer = new AutoTimer("Request: GetMostActiveUsers", _embyManagers._logger))
            {
                try
                {

                    var db = StatisticsDB.GetInstance(_embyManagers);
                    var hasConnectUserID = request.hasConnectUserID;
                    var numUsers = request.numUsers;
                    var excludeAdmin = request.excludeAdmin;

                    var groupData = db.MostActiveUsers(hasConnectUserID, numUsers, excludeAdmin);
                    groupData.SortByKey = false;
                    var vgReponse = groupData.createStat();

                    return vgReponse;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object TotalMovieCount(User? user)
        {
            var db = StatisticsDB.GetInstance((_embyManagers));
            var groupData = db.TotalMovieCount(user, false);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetTotalMovieCount request)
        {
            using (var timer = new AutoTimer("Request: GetTotalMovieCount", _embyManagers._logger))
            {
                try
                {
                    var userName = request.user;
                    var user = GetUser(userName);
                    if (user == null)
                        return null!;

                    return TotalMovieCount(user);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetTotalMovieCountNoUser request)
        {
            using (var timer = new AutoTimer("Request: GetTotalMovieCountNoUser", _embyManagers._logger))
            {
                try
                {
                    return TotalMovieCount(null);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetTotalMoviesWatched request)
        {
            using (var timer = new AutoTimer("Request: GetTotalMoviesWatched", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance((_embyManagers));
                    var userName = request.user;
                    var user = GetUser(userName);
                    if (user == null)
                        return null!;

                    var groupData = db.TotalMovieCount(user, true);
                    var vgReponse = groupData.createStat();
                    return vgReponse;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetTotalCollectionCount request)
        {
            using (var timer = new AutoTimer("Request: GetTotalCollectionCount", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance((_embyManagers));
                    var groupData = db.TotalCollectionCount();
                    var vgReponse = groupData.createStat();
                    return vgReponse;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetTotalMovieStudioCount request)
        {
            using (var timer = new AutoTimer("Request: GetTotalMovieStudioCount", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance((_embyManagers));

                    var groupData = db.TotalMovieStudioCount(null);
                    var vgReponse = groupData.createStat();
                    return vgReponse;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetItemImageUrl request)
        {
            using (var timer = new AutoTimer("Request: GetItemImageUrl", _embyManagers._logger))
            {
                try
                {
                    var retVal = new GetItemImageUrlResponse { Name = "", PrimaryImageUrl = "" };
                    var item = _embyManagers._libraryManager.GetItemById(request.ItemId);
                    if (item == null)
                        return null!;

                    var url = ItemImageUrl._ItemImageUrl(item);
                    if (url == null)
                        retVal.PrimaryImageUrl = string.Empty;
                    else
                        retVal.PrimaryImageUrl = url;
                    if (retVal.PrimaryImageUrl.IsNullOrEmpty())
                        return retVal;
                    retVal.Name = item.Name;
                    return retVal;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object TotalTVCount(User? user, bool watched)
        {
            var db = StatisticsDB.GetInstance((_embyManagers));
            var groupData = db.TotalTVCount(user, watched);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetTotalTVCount request)
        {
            using (var timer = new AutoTimer("Request: GetTotalTVCount", _embyManagers._logger))
            {
                try
                {
                    var userName = request.user;
                    var user = GetUser(userName);
                    if (user == null)
                        return null!;

                    return TotalTVCount(user, false);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetTotalTVCountNoUser request)
        {
            using (var timer = new AutoTimer("Request: GetTotalTVCountNoUser", _embyManagers._logger))
            {
                try
                {
                    return TotalTVCount(null, false);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetTotalTVWatched request)
        {
            using (var timer = new AutoTimer("Request: GetTotalMoviesWatched", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance((_embyManagers));
                    var userName = request.user;
                    var user = GetUser(userName);
                    if (user == null)
                        return null!;

                    var groupData = db.TotalTVCount(user, true);
                    var vgReponse = groupData.createStat();
                    return vgReponse;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetTotalSeriesFinished request)
        {
            using (var timer = new AutoTimer("Request: GetTotalSeriesFinished", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance((_embyManagers));
                    var userName = request.user;
                    var user = GetUser(userName);
                    if (user == null)
                        return null!;

                    var groupData = db.TotalFinishedSeries(user);
                    var vgReponse = groupData.createStat();
                    return vgReponse;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        public object Get(GetTotalTVStudioCount request)
        {
            using (var timer = new AutoTimer("Request: GetTotalTVStudioCount", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance((_embyManagers));

                    var groupData = db.TotalTVStudioCount(null);
                    var vgReponse = groupData.createStat();
                    return vgReponse;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetLeastWatchedShows request)
        {
            using (var timer = new AutoTimer("Request: GetLeastWatchedShows", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance((_embyManagers));
                    var serverId = request.serverId ?? "";

                    var groupData = db.WatchedShows(null, true);
                    groupData.ServerId = serverId;

                    var vgReponse = groupData.createStat();
                    return vgReponse;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetMostWatchedShows request)
        {
            using (var timer = new AutoTimer("Request: GetMostWatchedShows", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance((_embyManagers));
                    var serverId = request.serverId ?? "";

                    var groupData = db.WatchedShows(null, false);
                    groupData.ServerId = serverId;

                    var vgReponse = groupData.createStat();
                    return vgReponse;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetTotalTimeWatched request)
        {
            using (var timer = new AutoTimer("Request: GetTotalTimeWatched", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance((_embyManagers));
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
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetTotalWatchableTime request)
        {
            using (var timer = new AutoTimer("Request: GetTotalWatchableTime", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance((_embyManagers));
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
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetLastSeen request)
        {
            using (var timer = new AutoTimer("Request: GetMovieFavoriteYears", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance((_embyManagers));
                    var userName = request.user;
                    var user = GetUser(userName);
                    if (user == null)
                        return null!;
                    var episodes = request.episodes;

                    var groupData = db.LastSeen(user, !episodes);
                    var vgReponse = groupData.createStat();
                    return vgReponse;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetMovieFavoriteYears request)
        {
            using (var timer = new AutoTimer("Request: GetMovieFavoriteYears", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance((_embyManagers));
                    var userName = request.user;
                    var user = GetUser(userName);
                    if (user == null)
                        return null!;

                    var groupData = db.FavoriteYears(user, true);
                    var vgReponse = groupData.createStat();
                    return vgReponse;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetMovieFavoriteGenres request)
        {
            using (var timer = new AutoTimer("Request: GetMovieFavoriteGenres", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_embyManagers);
                    var userName = request.user;
                    var user = GetUser(userName);
                    if (user == null)
                        return null!;

                    var groupData = db.FavoriteGenre(user, true);
                    var vgReponse = groupData.createStat();
                    return vgReponse;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetTVFavoriteGenres request)
        {
            using (var timer = new AutoTimer("Request: GetTVFavoriteGenres", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_embyManagers);
                    var userName = request.user;
                    var user = GetUser(userName);
                    if (user == null)
                        return null!;

                    var groupData = db.FavoriteGenre(user, false);
                    var vgReponse = groupData.createStat();
                    return vgReponse;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        public object Get(GetMovie request)
        {
            using (var timer = new AutoTimer("Request: GetMovie", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_embyManagers);
                    var serverId = request.serverId ?? "";
                    var whichStatistic = request.whichStatistic;
                    timer.Text += $" - {whichStatistic}";

                    var groupData = db.StatisticFor(null, whichStatistic, StatGen.EVideoType.Movie);
                    groupData.ServerId = serverId;
                    return groupData.createStat();
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetSeries request)
        {
            using (var timer = new AutoTimer("Request: GetSeries", _embyManagers?._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_embyManagers);
                    var serverId = request.serverId ?? "";
                    var whichStatistic = request.whichStatistic;
                    timer.Text += $" - {whichStatistic}";

                    var groupData = db.StatisticFor(null, whichStatistic, StatGen.EVideoType.Series);
                    groupData.ServerId = serverId;
                    return groupData.createStat();
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public object Get(GetEpisode request)
        {
            using (var timer = new AutoTimer("Request: GetEpisode", _embyManagers._logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_embyManagers);
                    var serverId = request.serverId ?? "";
                    var whichStatistic = request.whichStatistic;
                    timer.Text += $" - {whichStatistic}";

                    var groupData = db.StatisticFor(null, whichStatistic, StatGen.EVideoType.Episode);
                    groupData.ServerId = serverId;

                    return groupData.createStat();
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
    }
}
