using Emby.Media.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
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
using System.Xml.Linq;

namespace Statistics2026.Api
{
    public partial class Statistics2026API : IService, IRequiresRequest
    {
        //using (var timer = new AutoTimer($"Adding All Users", _logger))
        public object Get(GetEpisodeList request)
        {
            using (var timer = new AutoTimer("Request: GetEpisodeList", _logger))
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
            using (var timer = new AutoTimer("Request: GetMovieList", _logger))
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
            using (var timer = new AutoTimer("Request: GetCodecSummary", _logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

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
            using (var timer = new AutoTimer("Request: GetResolutionSummary", _logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
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
            using (var timer = new AutoTimer("Request: GetDVProfileSummary", _logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

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
            using (var timer = new AutoTimer("Request: GetUserCount", _logger))
            {
                try
                {

                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
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
            using (var timer = new AutoTimer("Request: GetMostActiveUsers", _logger))
            {
                try
                {

                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var hasConnectUserID = request.hasConnectUserID;
                    var numUsers = request.numUsers;
                    var excludeAdmin = request.excludeAdmin;

                    var groupData = db.MostActiveUsers(hasConnectUserID, numUsers, excludeAdmin, _userManager);
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
            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var groupData = db.TotalMovieCount(user, false);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetTotalMovieCount request)
        {
            using (var timer = new AutoTimer("Request: GetTotalMovieCount", _logger))
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
            using (var timer = new AutoTimer("Request: GetTotalMovieCountNoUser", _logger))
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
            using (var timer = new AutoTimer("Request: GetTotalMoviesWatched", _logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
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
            using (var timer = new AutoTimer("Request: GetTotalCollectionCount", _logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
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
            using (var timer = new AutoTimer("Request: GetTotalMovieStudioCount", _logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

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
            using (var timer = new AutoTimer("Request: GetItemImageUrl", _logger))
            {
                try
                {
                    var retVal = new GetItemImageUrlResponse { Name = "", PrimaryImageUrl = "" };
                    var item = _libraryManager.GetItemById(request.ItemId);
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
            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var groupData = db.TotalTVCount(user, watched);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetTotalTVCount request)
        {
            using (var timer = new AutoTimer("Request: GetTotalTVCount", _logger))
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
            using (var timer = new AutoTimer("Request: GetTotalTVCountNoUser", _logger))
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
            using (var timer = new AutoTimer("Request: GetTotalMoviesWatched", _logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
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

        public object Get(GetTotalTVStudioCount request)
        {
            using (var timer = new AutoTimer("Request: GetTotalTVStudioCount", _logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

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
            using (var timer = new AutoTimer("Request: GetLeastWatchedShows", _logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var serverId = request.serverId ?? "";

                    var groupData = db.WatchedShows(null, true, _libraryManager);
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
            using (var timer = new AutoTimer("Request: GetMostWatchedShows", _logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var serverId = request.serverId ?? "";

                    var groupData = db.WatchedShows(null, false, _libraryManager);
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
            using (var timer = new AutoTimer("Request: GetTotalTimeWatched", _logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
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
            using (var timer = new AutoTimer("Request: GetTotalWatchableTime", _logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
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
            return null!;
        }

        public object Get(GetMovieFavoriteYears request)
        {
            using (var timer = new AutoTimer("Request: GetMovieFavoriteYears", _logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
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
            using (var timer = new AutoTimer("Request: GetMovieFavoriteGenres", _logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
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

        public object Get(GetMovie request)
        {
            using (var timer = new AutoTimer("Request: GetMovie", _logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
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
            using (var timer = new AutoTimer("Request: GetSeries", _logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
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
            using (var timer = new AutoTimer("Request: GetEpisode", _logger))
            {
                try
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
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
