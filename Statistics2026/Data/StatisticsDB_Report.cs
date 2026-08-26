using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using RestSharp;
using ServiceStack;
using ServiceStack.Text;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using SQLitePCL.pretty;


namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public StatCard MediaResolutions(bool showAllResolutions)
        {
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

            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement(sql))
                {
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        var resolution = row.GetString(0);
                        var episodeCount = row.GetInt(1);
                        var movieCount = row.GetInt(2);
                        retVal.addRow(resolution, new List<int> { movieCount, episodeCount });
                    }
                }
            }

            return retVal;
        }

        public StatCard MediaCodecs(bool showAllCodecs)
        {
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

            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement(sql))
                {
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        var codec = row.GetString(0);
                        var episodeCount = row.GetInt(1);
                        var movieCount = row.GetInt(2);
                        retVal.addRow(codec, new List<int> { movieCount, episodeCount });
                    }
                }
            }

            return retVal;
        }
        public StatCard DVProfileInfo(bool showUnknownDVProfiles, bool showAllDVProfiles)
        {
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
            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement(sql))
                {
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        var dvProfile = row.GetString(0);
                        var episodeCount = row.GetInt(1);
                        var movieCount = row.GetInt(2);
                        retVal.addRow(dvProfile, new List<int> { movieCount, episodeCount });
                    }
                }
            }

            return retVal;
        }

        private string GetSingleValueFromSQL(string sql, List<(string field, string value)> parameters = null, Func<long, string> formatter = null)
        {
            lock (sql)
            {
                using (var statement = _connection.PrepareStatement(sql))
                {
                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                        {
                            _dbHelper.TryBind(statement, parameter.field, parameter.value);
                        }
                    }
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        var count = row.GetInt64(0);
                        return formatter?.Invoke(count) ?? count.ToString();
                    }
                }
            }
            return "";
        }

        private TextBasedStatCard ValueGroupForSingleItem(string title, string help, string sql, List<(string field, string value)> parameters = null, Func<long, string> formatter = null)
        {
            var retVal = new TextBasedStatCard(title, help, "small");
            var value = GetSingleValueFromSQL(sql, parameters, formatter);
            retVal.AddLine(value);
            return retVal;
        }

        public StatCard UserCount(bool hasConnectUserID, bool excludeAdmin)
        {
            string sql = "SELECT COUNT(UserName) FROM Users ";

            List<string> conditions = new List<string>();

            if (hasConnectUserID)
                conditions.Add("( ConnectUserId <> \"\" AND ConnectUserId IS NOT NULL )");

            if (excludeAdmin)
                conditions.Add("( NOT IsAdministrator )");

            if (conditions.Count > 0)
                sql += "WHERE " + string.Join(" AND ", conditions) + " ";

            return ValueGroupForSingleItem(Constants.TotalUsers, null, sql);
        }

        public StatCard MostActiveUsers(bool hasConnectUserID, int numUsers, bool excludeAdmin, IUserManager userManager)
        {
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
            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement(sql))
                {
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        var userName = row.GetString(0);
                        var runtime = new RunTime(row.GetInt64(1));

                        groupData.addRow(userName, new List<int> { runtime.Days, runtime.Hours, runtime.Minutes });
                    }
                }
            }

            return groupData;
        }

        public StatCard TotalMovieCount(User user, bool watched)
        {
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
                    total = GetSingleValueFromSQL("SELECT SUM(NOT IsEpisode) FROM UserVideoList WHERE UserId=@UserId", parameters).ToLong();
                }
            }

            return ValueGroupForSingleItem(title, help, sql, parameters, count =>
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
            });
        }

        public StatCard TotalTVCount(User user)
        {
            string sql = "SELECT COUNT(DISTINCT(PrimaryName)) FROM Media WHERE IsEpisode";
            var retVal = ValueGroupForSingleItem(Constants.TotalTVShows, Constants.HelpTotalTVShows, sql);

            retVal.AddLine(Constants.TotalTVEpisodes);
            var value = GetSingleValueFromSQL("SELECT SUM(IsEpisode) FROM Media");
            retVal.AddLine(value);

            return retVal;
        }

        public StatCard TotalCollectionCount()
        {
            string sql = "SELECT COUNT( ItemId ) FROM Collections";

            return ValueGroupForSingleItem(Constants.TotalCollections, Constants.HelpTotalCollections, sql);
        }

        public StatCard TotalStudioCount(User user, bool movies)
        {
            string sql = "SELECT DISTINCT StudioNames FROM Media WHERE ";
            if (movies)
                sql += "NOT ";
            sql += "IsEpisode AND StudioNames IS NOT NULL AND StudioNames<>\"\"";

            var retVal = new TextBasedStatCard(movies ? Constants.TotalStudios : Constants.TotalTVNetworks, movies ? Constants.HelpTotalStudios : Constants.HelpTotalTVNetworks, "small");
            // Create an unordered set of strings
            HashSet<string> studios = new HashSet<string>();

            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement(sql))
                {
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        var currStudios = row.GetString(0).Split(';');
                        studios.UnionWith(currStudios);
                    }
                }
            }
            retVal.AddLine(studios.Count().ToString());

            return retVal;
        }

        public StatCard TotalMovieStudioCount(User user)
        {
            return TotalStudioCount(user, true);
        }
        public StatCard TotalTVStudioCount(User user)
        {
            return TotalStudioCount(user, false);
        }

        public StatCard StatisticFor(User user, StatGen.EStatisticType whichStatistic, StatGen.EVideoType videoType)
        {
            var statGen = new StatGen(whichStatistic, videoType, _connection);
            return statGen.GetStatCard();
        }

        public StatCard WatchedShows(User user, bool leastWatched, ILibraryManager libManager)
        {
            var series = new List<(string id, string name, string url, long numEpisodes, long watched, double percentWatched, double percentWatchedPerUser)>();
            long numUsers = 0;

            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement("SELECT COUNT(1) FROM Users"))
                {
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        numUsers = row.GetInt64(0);
                        break;
                    }
                }

                using (var statement = _connection.PrepareStatement("SELECT ItemId, Name FROM Series"))
                {
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        var id = row.GetString(0);
                        var name = row.GetString(1);
                        var url = ItemImageUrl._ItemImageUrl(id, libManager);
                        series.Add((id, name, url, 0, 0, 0, 0));
                    }
                }
            }

            for (int ii = 0; ii < series.Count(); ++ii)
            {
                var curr = series[ii];
                lock (_connection)
                {
                    using (var statement = _connection.PrepareStatement("SELECT Count(1) FROM Media WHERE SeriesId=@SeriesId"))
                    {
                        _dbHelper.TryBind(statement, "@SeriesId", curr.id);
                        while (statement.MoveNext())
                        {
                            var row = statement.Current;
                            curr.numEpisodes = row.GetInt64(0);
                            break;
                        }
                    }
                    using (var statement = _connection.PrepareStatement("SELECT Count(1) FROM UserVideoList WHERE SeriesId=@SeriesId"))
                    {
                        _dbHelper.TryBind(statement, "@SeriesId", curr.id);
                        while (statement.MoveNext())
                        {
                            var row = statement.Current;
                            curr.watched = row.GetInt64(0);
                            break;
                        }
                    }

                    curr.percentWatched = (1.0 * curr.watched) / (1.0 * curr.numEpisodes);
                    curr.percentWatchedPerUser = curr.percentWatched / (1.0 * numUsers);
                    series[ii] = curr;
                }
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

        public StatCard TotalTimeWatched(User user)
        {
            string sql = "SELECT TotalTimeWatched FROM Users  WHERE UserId=@UserId";
            return ValueGroupForSingleItem(Constants.UserTotalTimeWatched, null, sql, new List<(string field, string value)>() { ("@UserId", user.Id.ToString()) }, FormatTicks);
        }

        public StatCard TotalWatchableTime(User user)
        {
            string sql = "SELECT TotalWatchableTime FROM Users WHERE UserId=@UserId";
            return ValueGroupForSingleItem(Constants.UserTotalWatchableTime, null, sql, new List<(string field, string value)>() { ("@UserId", user.Id.ToString()) }, FormatTicks);
        }

        public StatCard FavoriteYears(User user, bool movies)
        {
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

            var retVal = new TableBasedStatCard(Constants.FavoriteMovieYears, "", new List< string>() { $"# of {videoType} Watched"});
            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement(sql))
                {
                    _dbHelper.TryBind(statement, "@UserId", user.Id.ToString());
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        var count = row.GetInt(0);
                        var year = row.GetInt64(1);
                        retVal.addRow(year.ToString(), new List<int>() { count});
                    }
                }
            }

            return retVal;
        }

        public StatCard FavoriteGenre(User user, bool movies)
        {
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
            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement(sql))
                {
                    _dbHelper.TryBind(statement, "@UserId", user.Id.ToString());
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        var genres = row.GetString(0).Split(';');
                        foreach (var genre in genres)
                        {
                            if (!genreMap.ContainsKey(genre))
                                genreMap[genre] = 0;
                            genreMap[genre]++;
                        }
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
