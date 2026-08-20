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

            var groupData = db.UserCount(hasConnectUserID, excludeAdmin, _userManager);
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

        public object Get(GetTotalMovieCount request)
        {
            _logger.Debug("Request: GetTotalMovieCount");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

            var groupData = db.TotalMovieCount(null);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetTotalCollectionCount request)
        {
            _logger.Debug("Request: GetTotalCollectionCount");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

            var groupData = db.TotalCollectionCount(null);
            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get(GetTotalStudioCount request)
        {
            _logger.Debug("Request: GetTotalStudioCount ");

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
            
            retVal.PrimaryImageUrl = ItemImageUrl._ItemImageUrl(item, ImageType.Primary, 400, 90, 0);
            if (retVal.PrimaryImageUrl.IsNullOrEmpty())
                return retVal;
            retVal.Name = item.Name;
            return retVal;
        }

        public object Get(GetMovie request)
        {
            _logger.Debug("Request: GetMovie ");

            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var serverId = request.serverId ?? "";
            WhichMovie whichMovie = request.whichMovie;

            var groupData = db.Movie(null, whichMovie);
            groupData.ServerId = serverId;

            var vgReponse = groupData.createStat();
            return vgReponse;
        }
    }
}
