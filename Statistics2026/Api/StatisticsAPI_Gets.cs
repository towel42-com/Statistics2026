using Emby.Media.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Services;
using Statistics2026.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Statistics2026.Api
{
    public partial class Statistics2026API : IService, IRequiresRequest
    {
        public async Task<object?> Get(GetEpisodeList request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetEpisodeList", _logger, 100))
                {
                    var retVal = GetVideos<Episode>(null);
                    retVal = retVal.OrderBy(x => x.SortName).ThenBy(x => x.Season).ThenBy(x => x.Episode).ToList();
                    return retVal;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetEpisodeList: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetMovieList request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetMovieList", _logger, 100))
                {
                    var retVal = GetVideos<Movie>(null);
                    retVal = retVal.OrderBy(x => x.SortName).ThenBy(x => x.StartYear).ToList();
                    return retVal;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetMovieList: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetCodecSummary request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetCodecSummary", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

                    var serverId = request.serverId ?? "";
                    var rootDivName = request.rootDivName ?? "";
                    var showAllCodecs = request.showAllCodecs;
                    timer.Message = $"Request: GetCodecSummary - {serverId} - {rootDivName} - {showAllCodecs}";

                    var groupData = (await db.MediaCodecs(showAllCodecs));
                    groupData.ServerId = serverId;
                    groupData.HtmlDivId = rootDivName;
                    groupData.SortByKey = true;
                    var vgResponse = groupData.createStat();

                    return vgResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetCodecSummary: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetResolutionSummary request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetResolutionSummary", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var serverId = request.serverId ?? "";
                    var rootDivName = request.rootDivName ?? "";
                    var showAllResolutions = request.showAllResolutions;
                    timer.Message = $"Request: GetResolutionSummary - {serverId} - {rootDivName} - {showAllResolutions}";

                    var groupData = (await db.MediaResolutions(showAllResolutions));

                    groupData.ServerId = serverId;
                    groupData.HtmlDivId = rootDivName;
                    groupData.SortByKey = false;
                    var vgResponse = groupData.createStat();

                    return vgResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetMovie: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetDVProfileSummary request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetDVProfileSummary", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

                    var serverId = request.serverId ?? "";
                    var rootDivName = request.rootDivName ?? "";
                    var showUnknownDVProfiles = request.showUnknownDVProfiles;
                    var showAllDVProfiles = request.showAllDVProfiles;
                    timer.Message = $"Request: GetDVProfileSummary - {serverId} - {rootDivName} - {showUnknownDVProfiles} - {showAllDVProfiles}";

                    var groupData = (await db.DVProfileInfo(showUnknownDVProfiles, showAllDVProfiles));

                    groupData.ServerId = serverId;
                    groupData.HtmlDivId = rootDivName;
                    groupData.SortByKey = true;
                    var vgResponse = groupData.createStat();

                    return vgResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetDVProfileSummary: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetUserCount request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetUserCount", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var hasConnectUserID = request.hasConnectUserID;
                    var excludeAdmin = request.excludeAdmin;
                    timer.Message = $"Request: GetUserCount - {hasConnectUserID} - {excludeAdmin}";

                    var groupData = (await db.UserCount(hasConnectUserID, excludeAdmin));
                    var vgResponse = groupData.createStat();
                    return vgResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetUserCount: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetMostActiveUsers request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetMostActiveUsers", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var hasConnectUserID = request.hasConnectUserID;
                    var numUsers = request.numUsers;
                    var excludeAdmin = request.excludeAdmin;
                    timer.Message = $"Request: GetMostActiveUsers - {hasConnectUserID} - {numUsers} - {excludeAdmin}";

                    var groupData = (await db.MostActiveUsers(hasConnectUserID, numUsers, excludeAdmin, _userManager));
                    groupData.SortByKey = false;
                    var vgResponse = groupData.createStat();

                    return vgResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetMostActiveUsers: " + ex.Message);
                return null;
            }
        }

        public async Task<object> TotalMovieCount(User? user)
        {
            var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
            var groupData = (await db.TotalMovieCount(user, false));
            var vgResponse = groupData.createStat();
            return vgResponse;
        }

        public async Task<object?> Get(GetTotalMovieCount request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetTotalMovieCount", _logger, 100))
                {
                    var userName = request.user;
                    timer.Message = $"Request: GetTotalMovieCount - {userName}";
                    var user = GetUser(userName);
                    if (user == null)
                        return null;

                    return TotalMovieCount(user);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetTotalMovieCount: " + ex.Message);
                return null;
            }
        }
        public async Task<object?> Get(GetTotalMovieCountNoUser request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetTotalMovieCountNoUser", _logger, 100))
                {
                    timer.Message = $"Request: GetTotalMovieCountNoUser";
                    return TotalMovieCount(null);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetTotalMovieCountNoUser: " + ex.Message);
                return null;
            }
        }
        public async Task<object?> Get(GetTotalMoviesWatched request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetTotalMoviesWatched", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var userName = request.user;
                    timer.Message = $"Request: GetTotalMoviesWatched - {userName}";
                    var user = GetUser(userName);
                    if (user == null)
                        return null;

                    var groupData = (await db.TotalMovieCount(user, true));
                    var vgResponse = groupData.createStat();
                    return vgResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetTotalMoviesWatched: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetTotalCollectionCount request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetTotalCollectionCount", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var groupData = (await db.TotalCollectionCount());
                    var vgResponse = groupData.createStat();
                    return vgResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetTotalCollectionCount: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetTotalMovieStudioCount request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetTotalMovieStudioCount", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var groupData = (await db.TotalMovieStudioCount(null));
                    var vgResponse = groupData.createStat();
                    return vgResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetTotalMovieStudioCount: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetItemImageUrl request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetItemImageUrl", _logger, 100))
                {
                    var retVal = new GetItemImageUrlResponse { Name = "", PrimaryImageUrl = "" };
                    timer.Message = $"Request: GetItemImageUrl - {request.ItemId}";
                    var item = _libraryManager.GetItemById(request.ItemId);
                    if (item == null)
                        return null;

                    retVal.PrimaryImageUrl = ItemImageUrl._ItemImageUrl(item);
                    if (retVal.PrimaryImageUrl.IsNullOrEmpty())
                        return retVal;
                    retVal.Name = item.Name;
                    return retVal;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetItemImageUrl: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetTotalTVCount request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetTotalTVCount", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

                    var groupData = (await db.TotalTVCount(null));
                    var vgResponse = groupData.createStat();
                    _logger.Debug("=====================================");
                    _logger.Debug($"{vgResponse}");
                    _logger.Debug("=====================================");
                    return vgResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetTotalTVCount: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetTotalTVStudioCount request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetTotalTVStudioCount", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);

                    var groupData = (await db.TotalTVStudioCount(null));
                    var vgResponse = groupData.createStat();
                    return vgResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetTotalTVStudioCount: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetMovie request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetMovie", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var serverId = request.serverId ?? "";
                    var whichStatistic = request.whichStatistic;
                    timer.Message = $"Request: GetMovie - {serverId} - {whichStatistic}";

                    var groupData = (await db.StatisticFor(null, whichStatistic, StatGen.EVideoType.Movie));
                    groupData.ServerId = serverId;
                    var retVal = groupData.createStat();
                    return retVal;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetMovie: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetSeries request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetSeries", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var serverId = request.serverId ?? "";
                    var whichStatistic = request.whichStatistic;
                    timer.Message = $"Request: GetSeries - {serverId} - {whichStatistic}";

                    var groupData = (await db.StatisticFor(null, whichStatistic, StatGen.EVideoType.Series));
                    groupData.ServerId = serverId;

                    return groupData.createStat();
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetSeries: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetEpisode request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetEpisode", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var serverId = request.serverId ?? "";
                    var whichStatistic = request.whichStatistic;
                    timer.Message = $"Request: GetEpisode - {serverId} - {whichStatistic}";

                    var groupData = (await db.StatisticFor(null, whichStatistic, StatGen.EVideoType.Episode));
                    groupData.ServerId = serverId;

                    var retVal = groupData.createStat();
                    return retVal;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetEpisode: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetLeastWatchedShows request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetLeastWatchedShows", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var serverId = request.serverId ?? "";

                    timer.Message = $"Request: GetLeastWatchedShows - {serverId}";

                    var groupData = (await db.WatchedShows(null, true, _libraryManager));
                    groupData.ServerId = serverId;

                    var vgResponse = groupData.createStat();
                    return vgResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetLeastWatchedShows: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetMostWatchedShows request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetMostWatchedShows", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var serverId = request.serverId ?? "";

                    timer.Message = $"Request: GetMostWatchedShows - {serverId}";

                    var groupData = (await db.WatchedShows(null, false, _libraryManager));
                    groupData.ServerId = serverId;

                    var vgResponse = groupData.createStat();
                    return vgResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetMostWatchedShows: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetTotalTimeWatched request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetTotalTimeWatched", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var userName = request.user;
                    timer.Message = $"Request: GetTotalTimeWatched - {userName}";

                    var user = GetUser(userName);
                    if (user == null)
                        return null;

                    var groupData = (await db.TotalTimeWatched(user));
                    var vgResponse = groupData.createStat();
                    return vgResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetTotalTimeWatched: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetTotalWatchableTime request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetTotalWatchableTime - {request.user}", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var userName = request.user;

                    timer.Message = $"Request: GetTotalWatchableTime - {userName}";
                    var user = GetUser(userName);
                    if (user == null)
                        return null;

                    var groupData = (await db.TotalWatchableTime(user));
                    var vgResponse = groupData.createStat();
                    return vgResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetTotalWatchableTime: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetMovieFavoriteYears request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetMovieFavoriteYears", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var userName = request.user;
                    timer.Message = $"Request: GetMovieFavoriteYears - {userName}";
                    var user = GetUser(userName);
                    if (user == null)
                        return null;

                    var groupData = (await db.FavoriteYears(user, true));
                    var vgResponse = groupData.createStat();
                    return vgResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetMovieFavoriteYears: " + ex.Message);
                return null;
            }
        }

        public async Task<object?> Get(GetMovieFavoriteGenres request)
        {
            try
            {
                using (var timer = new AutoTimer($"Request: GetMovieFavoriteGenres", _logger, 100))
                {
                    var db = StatisticsDB.GetInstance(_config.ApplicationPaths.DataPath, _logger);
                    var userName = request.user;
                    timer.Message = $"Request: GetMovieFavoriteGenres - {userName}";
                    var user = GetUser(userName);
                    if (user == null)
                        return null;

                    var groupData = (await db.FavoriteGenre(user, true));
                    var vgResponse = groupData.createStat();
                    return vgResponse;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception thrown in GetMovieFavoriteGenres: " + ex.Message);
                return null;
            }
        }
    }
}
