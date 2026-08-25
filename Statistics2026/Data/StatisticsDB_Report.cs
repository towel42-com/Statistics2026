using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using SQLitePCL;

//using RestSharp;
//using ServiceStack;
//using ServiceStack.Text;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;


//using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;



namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public async Task<StatCard> MediaResolutions(bool showAllResolutions)
        {
            if (_dbHelper == null || _dbHelper._connection == null)
                throw new ArgumentNullException("null dbHelper");

            string sql =
                "SELECT " +
                "ResolutionBase as Resolution, " +
                "sum(IsEpisode) AS Episodes, " +
                "sum(NOT IsEpisode) AS Movies " +
                "FROM Media " +
                "GROUP BY Resolution " +
                "ORDER BY Resolution ASC"
                ;

            var retVal = new TableBasedStatCard(Constants.MediaResolutions, Constants.HelpMediaResolutions, new List<string> { "Movies", "Episodes" });

            if (showAllResolutions)
            {
                retVal.addRow(Constants.HD, new List<int> { 0, 0 });
                retVal.addRow(Constants._4k, new List<int> { 0, 0 });
                retVal.addRow(Constants._8k, new List<int> { 0, 0 });
                retVal.addRow(Constants._720p, new List<int> { 0, 0 });
                retVal.addRow(Constants.SD, new List<int> { 0, 0 });
            }

            await _dbHelper.WaitAsync();
            try
            {
                using (var reader = await _dbHelper.ExecuteReaderAsync(sql))
                {
                    while (reader.Read())
                    {
                        var resolution = reader.GetString(0);
                        var episodeCount = reader.GetInt32(1);
                        var movieCount = reader.GetInt32(2);
                        retVal.addRow(resolution, new List<int> { movieCount, episodeCount });
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _dbHelper.Release();
            }

            return retVal;
        }

        public async Task<StatCard> MediaCodecs(bool showAllCodecs)
        {
            if (_dbHelper == null || _dbHelper._connection == null)
                throw new ArgumentNullException("null dbHelper");

            string sql =
                "SELECT " +
                "Codec as Codec, " +
                "sum(IsEpisode) AS Episodes, " +
                "sum(NOT IsEpisode) AS Movies " +
                "FROM Media " +
                "GROUP BY Codec " +
                "ORDER BY Codec ASC"
                ;

            var retVal = new TableBasedStatCard(Constants.MediaCodecs, Constants.HelpMediaCodecs, new List<string> { "Movies", "Episodes" });
            if (showAllCodecs)
            {
                retVal.addRow("av1", new List<int> { 0, 0 });
                retVal.addRow("h264", new List<int> { 0, 0 });
                retVal.addRow("hevc", new List<int> { 0, 0 });
                retVal.addRow("mpeg2video", new List<int> { 0, 0 });
                retVal.addRow("mpeg4", new List<int> { 0, 0 });
                retVal.addRow("msmpeg4v3", new List<int> { 0, 0 });
                retVal.addRow("prores", new List<int> { 0, 0 });
                retVal.addRow("vc1", new List<int> { 0, 0 });
            }

            await _dbHelper.WaitAsync();
            try
            {
                using (var reader = await _dbHelper.ExecuteReaderAsync(sql))
                {
                    while (reader.Read())
                    {
                        var codec = reader.GetString(0);
                        var episodeCount = reader.GetInt32(1);
                        var movieCount = reader.GetInt32(2);
                        retVal.addRow(codec, new List<int> { movieCount, episodeCount });
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _dbHelper.Release();
            }

            return retVal;
        }

        public async Task<StatCard> DVProfileInfo(bool showUnknownDVProfiles, bool showAllDVProfiles)
        {
            if (_dbHelper == null || _dbHelper._connection == null)
                throw new ArgumentNullException("null dbHelper");

            string sql =
                "SELECT " +
                "DolbyVisionProfile as DVProfile, " +
                "sum(IsEpisode) AS Episodes, " +
                "sum(NOT IsEpisode) AS Movies " +
                "FROM Media ";

            if (!showUnknownDVProfiles)
                sql += $"WHERE DolbyVisionProfile NOT IN ({string.Join(",", Constants.UnknownDolbyProfiles.Select(p => $"'{p}'"))}) ";

            sql += "GROUP BY DolbyVisionProfile " +
                   "ORDER BY DolbyVisionProfile ASC"
                   ;

            var retVal = new TableBasedStatCard(Constants.DolbyVisionProfiles, Constants.HelpDolbyVisionProfile, new List<string> { "Movies", "Episodes" });
            if (showUnknownDVProfiles)
                retVal.addRow("Unknown Dolby Profile", new List<int> { 0, 0 });
            if (showAllDVProfiles)
            {
                retVal.addRow("Profile 5.0", new List<int> { 0, 0 });
                retVal.addRow("Profile 7.0", new List<int> { 0, 0 });
                retVal.addRow("Profile 8.0", new List<int> { 0, 0 });
                retVal.addRow("Profile 8.1", new List<int> { 0, 0 });
                retVal.addRow("Profile 8.2", new List<int> { 0, 0 });
                retVal.addRow("Profile 8.4", new List<int> { 0, 0 });
                retVal.addRow("Profile 9.0", new List<int> { 0, 0 });
                retVal.addRow("Profile 20.0", new List<int> { 0, 0 });
            }

            await _dbHelper.WaitAsync();
            try
            {
                using (var reader = await _dbHelper.ExecuteReaderAsync(sql))
                {
                    while (reader.Read())
                    {
                        var dvProfile = reader.GetString(0);
                        var episodeCount = reader.GetInt32(1);
                        var movieCount = reader.GetInt32(2);
                        retVal.addRow(dvProfile, new List<int> { movieCount, episodeCount });
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _dbHelper.Release();
            }

            return retVal;
        }

        private async Task<(string text, long value)> GetSingleValueFromSQL(string sql, List<(string field, string value)>? parameters = null, Func<long, string>? formatter = null)
        {
            if (_dbHelper == null || _dbHelper._connection == null)
                throw new ArgumentNullException("null dbHelper");

            await _dbHelper.WaitAsync();
            try
            {
                using (var reader = await _dbHelper.ExecuteReaderAsync(sql, parameters.Select(item => new { item.field, item.value }).ToList()))
                {
                    while (reader.Read())
                    {
                        var count = reader.GetInt64(0);
                        return (formatter?.Invoke(count) ?? count.ToString(), count);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _dbHelper.Release();
            }

            return ("", 0);
        }

        private async Task<TextBasedStatCard> ValueGroupForSingleItem(string title, string help, string sql, List<(string field, string value)>? parameters = null, Func<long, string>? formatter = null)
        {
            var retVal = new TextBasedStatCard(title, help, "small");
            var value = (await GetSingleValueFromSQL(sql, parameters, formatter)).text;
            retVal.AddLine(value);
            return retVal;
        }

        public async Task<TextBasedStatCard> UserCount(bool hasConnectUserID, bool excludeAdmin)
        {
            if (_dbHelper == null || _dbHelper._connection == null)
                throw new ArgumentNullException("null dbHelper");

            string sql = "SELECT COUNT(UserName) FROM Users ";

            List<string> conditions = new List<string>();

            if (hasConnectUserID)
                conditions.Add("( ConnectUserId <> \"\" AND ConnectUserId IS NOT NULL )");

            if (excludeAdmin)
                conditions.Add("( NOT IsAdministrator )");

            if (conditions.Count > 0)
                sql += "WHERE " + string.Join(" AND ", conditions) + " ";

            var retVal = await ValueGroupForSingleItem(Constants.TotalUsers, String.Empty, sql);
            return retVal;
        }

        public async Task<StatCard> MostActiveUsers(bool hasConnectUserID, int numUsers, bool excludeAdmin, IUserManager userManager)
        {
            if (_dbHelper == null || _dbHelper._connection == null)
                throw new ArgumentNullException("null dbHelper");

            string sql =
                "SELECT " +
                "UserName, " +
                "TotalTimeWatched " +
                "FROM Users ";
            List<string> conditions = new List<string>();

            if (hasConnectUserID)
                conditions.Add("( ConnectUserId <> \"\" AND ConnectUserId IS NOT NULL )");

            if (excludeAdmin)
                conditions.Add("( NOT IsAdministrator )");
            if (conditions.Count > 0)
                sql += "WHERE " + string.Join(" AND ", conditions) + " ";

            sql +=
                "ORDER BY TotalTimeWatched DESC " +
                $"LIMIT {numUsers} "
                ;

            var help = Constants.HelpMostActiveUsers;
            help = help.Replace("<numUsers>", numUsers.ToString());
            var groupData = new TableBasedStatCard(Constants.MostActiveUsers, help, new List<string> { "Days", "Hours", "Minutes" });

            await _dbHelper.WaitAsync();
            try
            {
                using (var reader = await _dbHelper.ExecuteReaderAsync(sql))
                {
                    while (reader.Read())
                    {
                        var userName = reader.GetString(0);
                        var runtime = new RunTime(reader.GetInt64(1));

                        groupData.addRow(userName, new List<int> { runtime.Days, runtime.Hours, runtime.Minutes });
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _dbHelper.Release();
            }

            return groupData;
        }

        public async Task<TextBasedStatCard> TotalMovieCount(User? user, bool watched)
        {
            if (_dbHelper == null || _dbHelper._connection == null)
                throw new ArgumentNullException("null dbHelper");

            string sql = "";
            var parameters = new List<(string, string)>();
            string title = Constants.TotalMovies;
            string help = Constants.HelpTotalMovies;
            long total = 0;
            if (user == null)
            {
                sql = "SELECT SUM(NOT IsEpisode) FROM Media";
            }
            else
            {
                title = Constants.TotalUserMoviesWatched;
                help = Constants.HelpTotalUserMoviesWatched;

                sql = "SELECT SUM(NOT IsEpisode) FROM UserVideoList WHERE UserId=@UserId";
                if (watched)
                    sql += " AND IsPlayed";
                parameters.Add(("@UserId", user.Id.ToString()));

                if (watched)
                {
                    var totalValue = (await GetSingleValueFromSQL("SELECT SUM(NOT IsEpisode) FROM UserVideoList WHERE UserId=@UserId", parameters));
                    total = totalValue.value;
                }
            }

            var retVal = (await ValueGroupForSingleItem(title, help, sql, parameters, count =>
            {
                if (watched && total != 0)
                {
                    if (total != 0)
                    {
                        double value = (100.0 * count) / (1.0 * total);
                        return $"{count} ({value.ToString("F1")})%";
                    }
                    else
                        return $"0 (0%)";
                }
                return count.ToString();
            }));

            return retVal;
        }

        public async Task<StatCard> TotalTVCount(User? user)
        {
            if (_dbHelper == null || _dbHelper._connection == null)
                throw new ArgumentNullException("null dbHelper");

            string sql = "SELECT COUNT(DISTINCT(PrimaryName)) FROM Media WHERE IsEpisode";
            var retVal = (await ValueGroupForSingleItem(Constants.TotalTVShows, Constants.HelpTotalTVShows, sql));

            retVal.AddLine(Constants.TotalTVEpisodes);
            var value = (await GetSingleValueFromSQL("SELECT SUM(IsEpisode) FROM Media")).text;
            retVal.AddLine(value);

            return retVal;
        }

        public async Task<StatCard> TotalCollectionCount()
        {
            string sql = "SELECT COUNT( ItemId ) FROM Collections";

            return (await ValueGroupForSingleItem(Constants.TotalCollections, Constants.HelpTotalCollections, sql));
        }

        public async Task<StatCard> TotalStudioCount(User? user, bool movies)
        {
            if (_dbHelper == null || _dbHelper._connection == null)
                throw new ArgumentNullException("null dbHelper");

            string sql = "SELECT DISTINCT StudioNames FROM Media WHERE ";
            if (movies)
                sql += "NOT ";
            sql += "IsEpisode AND StudioNames IS NOT NULL AND StudioNames IS NOT NULL AND StudioNames != ''";

            var retVal = new TextBasedStatCard(movies ? Constants.TotalStudios : Constants.TotalTVNetworks, movies ? Constants.HelpTotalStudios : Constants.HelpTotalTVNetworks, "small");
            // Create an unordered set of strings
            HashSet<string> studios = new HashSet<string>();

            await _dbHelper.WaitAsync();
            try
            {
                using (var reader = await _dbHelper.ExecuteReaderAsync(sql))
                {
                    while (reader.Read())
                    {
                        var currStudios = reader.GetString(0).Split(';');
                        studios.UnionWith(currStudios);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _dbHelper.Release();
            }

            retVal.AddLine(studios.Count().ToString());

            return retVal;
        }

        public async Task<StatCard> TotalMovieStudioCount(User? user)
        {
            return (await TotalStudioCount(user, true));
        }
        public async Task<StatCard> TotalTVStudioCount(User? user)
        {
            return (await TotalStudioCount(user, false));
        }

        public async Task<StatCard> StatisticFor(User? user, StatGen.EStatisticType whichStatistic, StatGen.EVideoType videoType)
        {
            if (_dbHelper == null || _dbHelper._connection == null)
                throw new ArgumentNullException("null dbHelper");

            var statGen = new StatGen(whichStatistic, videoType, _dbHelper._connection);
            return (await statGen.GetStatCard());
        }

        public async Task<StatCard> WatchedShows(User? user, bool leastWatched, ILibraryManager libManager)
        {
            if (_dbHelper == null || _dbHelper._connection == null)
                throw new ArgumentNullException("null dbHelper");

            var series = new List<(string id, string name, string url, long numEpisodes, long watched, double percentWatched, double percentWatchedPerUser)>();
            long numUsers = 0;

            await _dbHelper.WaitAsync();
            try
            {
                using (var reader = await _dbHelper.ExecuteReaderAsync("SELECT COUNT(1) FROM Users"))
                {
                    while (reader.Read())
                    {
                        numUsers = reader.GetInt64(0);
                        break;
                    }
                }

                using (var reader = await _dbHelper.ExecuteReaderAsync("SELECT ItemId, Name FROM Series"))
                {
                    while (reader.Read())
                    {
                        var id = reader.GetString(0);
                        var name = reader.GetString(1);
                        var url = ItemImageUrl._ItemImageUrl(id, libManager);
                        series.Add((id, name, url, 0, 0, 0, 0));
                    }
                }

                for (int ii = 0; ii < series.Count(); ++ii)
                {
                    var curr = series[ii];
                    using (var reader = await _dbHelper.ExecuteReaderAsync("SELECT Count(1) FROM Media WHERE SeriesId=@SeriesId", new
                    {
                        SeriesId = curr.id.ToString()
                    }))
                    {
                        while (reader.Read())
                        {
                            curr.numEpisodes = reader.GetInt64(0);
                            break;
                        }
                    }

                    using (var reader = await _dbHelper.ExecuteReaderAsync("SELECT Count(1) FROM Media WHERE SeriesId=@SeriesId", new
                    {
                        SeriesId = curr.id.ToString()
                    }))
                    {
                        while (reader.Read())
                        {
                            curr.numEpisodes = reader.GetInt64(0);
                            break;
                        }
                    }

                    curr.percentWatched = (1.0 * curr.watched) / (1.0 * curr.numEpisodes);
                    curr.percentWatchedPerUser = curr.percentWatched / (1.0 * numUsers);
                    series[ii] = curr;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _dbHelper.Release();
            }

            // this sorts it in ascending order
            series.Sort(
                (lhs, rhs) =>
                    {
                        var diff = lhs.percentWatched - rhs.percentWatched;
                        if (diff < 0)
                            return -1;
                        if (diff > 0)
                            return 1;
                        return 0;

                    });

            if (!leastWatched)
            {
                series.Reverse();
            }

            var title = leastWatched ? Constants.LeastWatchedShows : Constants.MostWatchedShows;
            var help = leastWatched ? Constants.HelpLeastWatchedShows : Constants.HelpMostWatchedShows;

            var retVal = new TextBasedStatCard(title, help, "small");
            retVal.AsNumberedList = true;
            for (int ii = 0; ii < series.Count && ii < 5; ++ii)
            {
                retVal.AddLine(series[ii].name, series[ii].id, series[ii].url);
            }

            return retVal;
        }

        public static string FormatTicks(long ticks)
        {
            var runtime = new RunTime(ticks);
            return runtime.ToLongString();
        }

        public async Task<StatCard> TotalTimeWatched(User user)
        {
            string sql = "SELECT TotalTimeWatched FROM Users  WHERE UserId=@UserId";
            return (await ValueGroupForSingleItem(Constants.UserTotalTimeWatched, String.Empty, sql, new List<(string field, string value)>() { ("@UserId", user.Id.ToString()) }, FormatTicks));
        }

        public async Task<StatCard> TotalWatchableTime(User user)
        {
            string sql = "SELECT TotalWatchableTime FROM Users WHERE UserId=@UserId";
            return (await ValueGroupForSingleItem(Constants.UserTotalWatchableTime, String.Empty, sql, new List<(string field, string value)>() { ("@UserId", user.Id.ToString()) }, FormatTicks));
        }

        public async Task<StatCard> FavoriteYears(User user, bool movies)
        {
            if (_dbHelper == null || _dbHelper._connection == null)
                throw new ArgumentNullException("null dbHelper");

            string sql =
                "SELECT COUNT(*) as NumVideos, StartYear From Media "
                + "INNER JOIN UserVideoList On Media.ItemId=UserVideoList.ItemId "
                + "WHERE "

                ;
            string videoType = "";
            if (movies)
            {
                sql += "NOT Media.IsEpisode ";
                videoType = "Movies";
            }
            else
            {
                sql += "Media.IsEpisode ";
                videoType = "Episodes";
            }
            sql +=
                "AND UserId=@UserId AND IsPlayed "
              + "GROUP BY StartYear "
              + "ORDER BY NumVideos DESC "
              + "LIMIT 5 "
              ;

            var retVal = new TableBasedStatCard(Constants.FavoriteMovieYears, "", new List<string>() { $"# of {videoType} Watched" });

            await _dbHelper.WaitAsync();
            using (var reader = await _dbHelper.ExecuteReaderAsync(sql, new { UserId = user.Id.ToString() }))
            {
                while (reader.Read())
                {
                    var count = reader.GetInt32(0);
                    var year = reader.GetInt64(1);
                    retVal.addRow(year.ToString(), new List<int>() { count });
                }
            }

            return retVal;
        }

        public async Task<StatCard> FavoriteGenre(User user, bool movies)
        {
            if (_dbHelper == null || _dbHelper._connection == null)
                throw new ArgumentNullException("null dbHelper");

            string sql =
                "SELECT Genres From Media "
                + "INNER JOIN UserVideoList On Media.ItemId=UserVideoList.ItemId "
                + "WHERE "

                ;
            string videoType = "";
            if (movies)
            {
                sql += "NOT Media.IsEpisode ";
                videoType = "Movies";
            }
            else
            {
                sql += "Media.IsEpisode ";
                videoType = "Episodes";
            }
            sql +=
                "AND UserId=@UserId AND IsPlayed "
              ;

            Dictionary<string, int> genreMap = new Dictionary<string, int>();

            await _dbHelper.WaitAsync();
            using (var reader = await _dbHelper.ExecuteReaderAsync(sql, new { UserId = user.Id.ToString() }))
            {
                while (reader.Read())
                {
                    var genres = reader.GetString(0).Split(';');
                    foreach (var genre in genres)
                    {
                        if (!genreMap.ContainsKey(genre))
                            genreMap[genre] = 0;
                        genreMap[genre]++;
                    }
                }
            }

            var retVal = new TableBasedStatCard(Constants.FavoriteMovieGenres, "", new List<string>() { $"# of {videoType} Watched" });

            var sortedGenre = genreMap.OrderByDescending(kvp => kvp.Value).ToList();
            for (int ii = 0; ii < sortedGenre.Count() && ii < 5; ++ii)
            {
                retVal.addRow(sortedGenre[ii].Key, new List<int>() { sortedGenre[ii].Value });
            }
            return retVal;
        }
    }
}
