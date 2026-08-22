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

            var groupData = db.MediaCodecs(showAllResolutions);
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

            var groupData = db.MediaResolutions(showAllResolutions);
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

            var groupData = db.DVProfileInfo(showUnknownDVProfiles, showAllDVProfiles);
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

            var groupData = db.UserCount(hasConnectUserID, excludeAdmin);
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

            var groupData = db.MostActiveUsers(hasConnectUserID, numUsers, excludeAdmin, _userManager);
            groupData.SortByKey = false;
            var vgReponse = groupData.createStat();

            return vgReponse;
        }

        public object TotalMovieCount(User user)
        {
            _logger.Debug("Request: TotalMovieCount");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var groupData = db.TotalMovieCount(user, false);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetTotalMovieCount request)
        {
            _logger.Debug("Request: GetTotalMovieCount");

            var userName = request.user;
            var user = GetUser(userName);
            if (user == null)
                return null;

            return TotalMovieCount(user);
        }
        public object Get(GetTotalMovieCountNoUser request)
        {
            _logger.Debug("Request: GetTotalMovieCountNoUser");
            return TotalMovieCount(null);
        }
        public object Get(GetTotalMoviesWatched request)
        {
            _logger.Debug("Request: GetTotalMoviesWatched");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var userName = request.user;
            var user = GetUser(userName);
            if (user == null)
                return null;

            var groupData = db.TotalMovieCount(user, true);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetTotalCollectionCount request)
        {
            _logger.Debug("Request: GetTotalCollectionCount");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var groupData = db.TotalCollectionCount();
            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetTotalMovieStudioCount request)
        {
            _logger.Debug("Request: GetTotalMovieStudioCount ");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

            var groupData = db.TotalMovieStudioCount(null);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetItemImageUrl request)
        {
            var retVal = new GetItemImageUrlResponse { Name = "", PrimaryImageUrl = "" };
            var item = _libraryManager.GetItemById(request.ItemId);
            if (item == null)
                return null;

            retVal.PrimaryImageUrl = ItemImageUrl._ItemImageUrl(item);
            if (retVal.PrimaryImageUrl.IsNullOrEmpty())
                return retVal;
            retVal.Name = item.Name;
            return retVal;
        }

        public object Get(GetTotalTVCount request)
        {
            _logger.Debug("Request: GetTotalTVCount");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

            var groupData = db.TotalTVCount(null);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetTotalTVStudioCount request)
        {
            _logger.Debug("Request: GetTotalTVStudioCount ");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

            var groupData = db.TotalTVStudioCount(null);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetMovie request)
        {
            _logger.Debug("Request: GetMovie ");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var serverId = request.serverId ?? "";
            var whichStatistic = request.whichStatistic;

            object retVal = null;
            try
            {
                var groupData = db.StatisticFor(null, whichStatistic, StatGen.EVideoType.Movie);
                groupData.ServerId = serverId;
                retVal = groupData.createStat();
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetMovie: " + ex.Message);
                return null;
            }

            return retVal;
        }

        public object Get(GetSeries request)
        {
            _logger.Debug("Request: GetSeries ");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var serverId = request.serverId ?? "";
            var whichStatistic = request.whichStatistic;

            object retVal = null;
            try
            {
                var groupData = db.StatisticFor(null, whichStatistic, StatGen.EVideoType.Series);
                groupData.ServerId = serverId;

                retVal = groupData.createStat();
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetMovie: " + ex.Message);
                return null;
            }
            return retVal;
        }

        public object Get(GetEpisode request)
        {
            _logger.Debug("Request: GetEpisode");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var serverId = request.serverId ?? "";
            var whichStatistic = request.whichStatistic;

            object retVal = null;
            try
            {
                var groupData = db.StatisticFor(null, whichStatistic, StatGen.EVideoType.Episode);
                groupData.ServerId = serverId;

                retVal = groupData.createStat();
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetMovie: " + ex.Message);
                return null;
            }
            return retVal;
        }

        public object Get(GetLeastWatchedShows request)
        {
            _logger.Debug("Request: GetLeastWatchedShows ");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var serverId = request.serverId ?? "";

            var groupData = db.WatchedShows(null, true, _libraryManager);
            groupData.ServerId = serverId;

            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetMostWatchedShows request)
        {
            _logger.Debug("Request: GetMostWatchedShows ");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var serverId = request.serverId ?? "";

            var groupData = db.WatchedShows(null, false,_libraryManager);
            groupData.ServerId = serverId;

            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetTotalTimeWatched request)
        {
            _logger.Debug("Request: GetTotalTimeWatched ");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var userName = request.user;
            var user = GetUser(userName);
            if (user == null)
                return null;

            var groupData = db.TotalTimeWatched(user);

            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetTotalWatchableTime request)
        {
            _logger.Debug("Request: GetTotalWatchableTime");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var userName = request.user;
            var user = GetUser(userName);
            if (user == null)
                return null;

            var groupData = db.TotalWatchableTime(user);

            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetMovieFavoriteYears request)
        {
            _logger.Debug("Request: GetMovieFavoriteYears");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var userName = request.user;
            var user = GetUser(userName);
            if (user == null)
                return null;

            var groupData = db.FavoriteYears(user, true);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }
        
        public object Get(GetMovieFavoriteGenres request)
        {
            _logger.Debug("Request: GetMovieFavoriteGenres");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var userName = request.user;
            var user = GetUser(userName);
            if (user == null)
                return null;

            var groupData = db.FavoriteGenre(user, true);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }
    }
}
