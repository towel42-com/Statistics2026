using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using ServiceStack;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using static ServiceStack.Diagnostics;


namespace Statistics2026.Data
{

    public sealed partial class StatisticsDB
    {
        public StatCard MediaResolutions(bool showAllResolutions)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            var retVal = new TableBasedStatCard(Constants.MediaResolutions, Constants.HelpMediaResolutions, new List<string> { "Movies", "Episodes" });

            if (showAllResolutions)
            {
                retVal.addRow(Constants.HD, new List<int> { 0, 0 });
                retVal.addRow(Constants._4k, new List<int> { 0, 0 });
                retVal.addRow(Constants._8k, new List<int> { 0, 0 });
                retVal.addRow(Constants._720p, new List<int> { 0, 0 });
                retVal.addRow(Constants.SD, new List<int> { 0, 0 });
            }

            string sql =
                "SELECT " +
                "ResolutionBase as Resolution, " +
                "sum(IsEpisode) AS Episodes, " +
                "sum(NOT IsEpisode) AS Movies " +
                "FROM Media " +
                "GROUP BY Resolution " +
                "ORDER BY Resolution ASC"
                ;
            _dbHelper.ExecuteCommand(new SQLCmdDef(sql), statement =>
            {
                var row = statement.Current;
                var resolution = row.GetString(0);
                var episodeCount = row.GetInt(1);
                var movieCount = row.GetInt(2);
                retVal.addRow(resolution, new List<int> { movieCount, episodeCount });
                return true;
            });

            return retVal;
        }

        public StatCard MediaCodecs()
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            var retVal = new TableBasedStatCard(Constants.MediaCodecs, Constants.HelpMediaCodecs, new List<string> { "Movies", "Episodes" });
            string sql =
                "SELECT " +
                "Codec as Codec, " +
                "sum(IsEpisode) AS Episodes, " +
                "sum(NOT IsEpisode) AS Movies " +
                "FROM Media " +
                "GROUP BY Codec " +
                "ORDER BY Codec ASC"
                ;

            _dbHelper.ExecuteCommand(new SQLCmdDef(sql), statement =>
            {
                var row = statement.Current;
                var codec = row.GetString(0);
                var episodeCount = row.GetInt(1);
                var movieCount = row.GetInt(2);
                retVal.addRow(codec, new List<int> { movieCount, episodeCount });
                return true;
            });

            return retVal;
        }

        public StatCard DVProfileInfo(bool showUnknownDVProfiles)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

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

            _dbHelper.ExecuteCommand(new SQLCmdDef(sql), statement =>
            {
                var row = statement.Current;
                var dvProfile = row.GetString(0);
                var episodeCount = row.GetInt(1);
                var movieCount = row.GetInt(2);
                retVal.addRow(dvProfile, new List<int> { movieCount, episodeCount });
                return true;
            });

            return retVal;
        }

        private string GetSingleValueFromSQL(string sql, List<(string name, object? value)>? parameters = null, Func<long, string>? formatter = null)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            var cmd = new SQLCmdDef(sql, parameters);

            var retVal = String.Empty;
            _dbHelper.ExecuteCommand(cmd, statement =>
                {
                    var row = statement.Current;
                    var count = row.GetInt64(0);
                    retVal = formatter?.Invoke(count) ?? count.ToString();
                    return false;
                });
            return retVal;
        }

        private TextBasedStatCard ValueGroupForSingleItem(string title, string? help, string sql, List<(string name, object? value)>? parameters = null, Func<long, string>? formatter = null)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            var retVal = new TextBasedStatCard(title, help, EStatCardSize.eSmall);
            var value = GetSingleValueFromSQL(sql, parameters, formatter);
            retVal.AddLine(value);
            return retVal;
        }

        private TextBasedStatCard ValueGroupForSingleValue(string title, string? help, Object value)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            var retVal = new TextBasedStatCard(title, help, EStatCardSize.eSmall);
            retVal.AddLine(value.ToString());
            return retVal;
        }

        public long NumUsers(bool hasConnectUserID, bool excludeAdmin)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            string sql = "SELECT COUNT(UserName) FROM Users ";

            List<string> conditions = new List<string>();

            if (hasConnectUserID)
                conditions.Add("ConnectUserId <> '' AND ConnectUserId IS NOT NULL");

            if (excludeAdmin)
                conditions.Add("NOT IsAdministrator");

            sql += DBHelper.JoinClauses(conditions);

            return GetSingleValueFromSQL(sql).ToInt64();
        }

        public StatCard UserCount(bool hasConnectUserID, bool excludeAdmin)
        {
            var numUsers = NumUsers(hasConnectUserID, excludeAdmin);
            return ValueGroupForSingleValue(Constants.TotalUsers, null, numUsers);
        }

        public StatCard MostActiveUsers(bool hasConnectUserID, int numUsers, bool excludeAdmin)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            string sql =
                "SELECT " +
                "UserName, " +
                "TotalTimeWatched " +
                "FROM Users ";
            List<string> conditions = new List<string>();

            if (hasConnectUserID)
                conditions.Add("ConnectUserId <> '' AND ConnectUserId IS NOT NULL");

            if (excludeAdmin)
                conditions.Add("NOT IsAdministrator");

            sql += DBHelper.JoinClauses(conditions);

            sql +=
                "ORDER BY TotalTimeWatched DESC " +
                $"LIMIT {numUsers} "
                ;

            var help = Constants.HelpMostActiveUsers;
            help = help.Replace("<numUsers>", numUsers.ToString());

            var groupData = new TableBasedStatCard(Constants.MostActiveUsers, help, new List<string> { "Days", "Hours", "Minutes" });
            var cmd = new SQLCmdDef(sql);
            _dbHelper.ExecuteCommand(new SQLCmdDef(sql), statement =>
            {
                var row = statement.Current;
                var userName = row.GetString(0);
                var runtime = new RunTime(row.GetInt64(1));

                groupData.addRow(userName, new List<int> { runtime.Days, runtime.Hours, runtime.Minutes });
                return true;
            });

            return groupData;
        }

        public StatCard TotalMovieCount(User? user, bool watched)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            string sql = "";
            var parameters = new List<(string, object?)>();
            string title = Constants.TotalMovies;
            string help = Constants.HelpTotalMovies;
            long total = 0;
            if (user == null)
            {
                sql = "SELECT SUM(NOT IsEpisode) FROM Media";
            }
            else
            {
                title = watched ? Constants.TotalUserMoviesWatched : Constants.TotalUserMovies;
                help = watched ? Constants.HelpTotalUserMoviesWatched : Constants.HelpTotalUserMovies;

                sql = $"SELECT SUM(NOT IsEpisode) FROM {getUserTableName(user)} WHERE UserId=@UserId";
                if (watched)
                    sql += " AND IsPlayed";
                parameters.Add(("@UserId", user.Id.ToString()));

                if (watched)
                {
                    total = GetSingleValueFromSQL($"SELECT SUM(NOT IsEpisode) FROM {getUserTableName(user)} WHERE UserId=@UserId", parameters).ToLong();
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

        public StatCard TotalFinishedSeries(User? user)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            if (user == null)
                throw new ArgumentNullException("user");

            var tableName = getUserTableName(user);
            var sql =
                "SELECT " +
                    "  PrimaryName" +
                    ", Media.SeriesId " +
                    $", SUM({tableName}.NumEpisodes) " +
                    ", Series.NumEpisodes " +
                $"FROM {tableName} " +
                $"LEFT JOIN Media ON {tableName}.ItemId=Media.ItemId " +
                "LEFT JOIN Series ON Series.ItemId=Media.SeriesId " +
                $"WHERE Media.IsEpisode AND NOT Media.IsTVSpecial AND {tableName}.IsPlayed " +
                $"AND {tableName}.UserId=@UserId " +
                "GROUP BY Media.SeriesId"
                ;

            var parameters = new List<(string, object?)>() { ("@UserId", user.Id.ToString()) };
            var seriesInfo = new Dictionary<string, (string name, long watched, long total)>();

            _dbHelper.ExecuteCommand(new SQLCmdDef(sql, parameters), statement =>
            {
                var row = statement.Current;
                var seriesName = row.GetString(0);
                var seriesId = row.GetString(1);
                var numPlayed = row.GetInt64(2);
                var numEpisodes = row.GetInt64(3);
                if (numPlayed == numEpisodes)
                    seriesInfo[seriesId] = (seriesName, 0, numEpisodes);
                return true;
            });

            var retVal = new TextBasedStatCard(Constants.TotalSeriesFinished, Constants.HelpTotalSeriesFinished, EStatCardSize.eSmall);
            retVal.AddLine(seriesInfo.Count().ToString());
            return retVal;
        }

        public StatCard TotalTVCount(User? user, bool watched)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            string seriesColumn = String.Empty;
            string seriesFrom = String.Empty;
            string episodeColumn = String.Empty;
            string episodeFrom = String.Empty;

            string titleSeries = String.Empty;
            string titleEpisodes = String.Empty;
            string helpEpisodes = String.Empty;
            List<(string name, object? value)>? paramList = null;

            var tableName = getUserTableName(user);
            if (user == null)
            {
                seriesColumn = "COUNT(DISTINCT(PrimaryName))";
                episodeColumn = "SUM(NumEpisodes)";
                episodeFrom = seriesFrom = "Media WHERE IsEpisode";

                titleSeries = Constants.TotalTVShows;
                titleEpisodes = Constants.TotalTVEpisodes;
                helpEpisodes = Constants.HelpTotalTVShows;
            }
            else
            {
                paramList = new List<(string name, object? value)>() { ("@UserId", user.Id.ToString()) };

                seriesColumn = "COUNT(DISTINCT(Media.PrimaryName))";
                episodeColumn = $"SUM({tableName}.NumEpisodes)";

                titleSeries = Constants.TotalUserTVShows;
                titleEpisodes = Constants.TotalUserTVEpisodes;
                helpEpisodes = Constants.HelpTotalUserTVShows;

                var from = $"{tableName} LEFT JOIN Media ON {tableName}.ItemId=Media.ItemId WHERE Media.IsEpisode AND NOT Media.IsTVSpecial AND ( {tableName}.UserId=@UserId )";

                if (watched)
                {
                    from += $" AND ( {tableName}.IsPlayed )";

                    titleSeries = Constants.TotalTVShowsWatched;
                    titleEpisodes = Constants.TotalUserTVEpisodesWatched;
                    helpEpisodes = Constants.HelpTotalTVShowsWatched;
                }

                seriesFrom = episodeFrom = from;
            }

            var sqlEpisodes = $"SELECT {episodeColumn} FROM {episodeFrom}";
            var retVal = ValueGroupForSingleItem(titleEpisodes, helpEpisodes, sqlEpisodes, paramList);

            retVal.AddLine(titleSeries);
            var sqlSeries = $"SELECT {seriesColumn} FROM {seriesFrom}";
            var value = GetSingleValueFromSQL(sqlSeries, paramList);
            retVal.AddLine(value);

            return retVal;
        }

        public StatCard TotalCollectionCount()
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            string sql = "SELECT COUNT( ItemId ) FROM Collections";

            return ValueGroupForSingleItem(Constants.TotalCollections, Constants.HelpTotalCollections, sql);
        }

        public long TotalStudioCountValue(User? user, bool movies)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            string sql = "SELECT DISTINCT StudioNames FROM Media WHERE ";
            if (movies)
                sql += "NOT ";
            sql += "IsEpisode AND StudioNames IS NOT NULL AND StudioNames<>''";

            // Create an unordered set of strings
            HashSet<string> studios = new HashSet<string>();

            var cmd = new SQLCmdDef(sql);
            _dbHelper.ExecuteCommand(new SQLCmdDef(sql), statement =>
            {
                var row = statement.Current;
                var currStudios = row.GetString(0)?.Split(',') ?? Array.Empty<string>(); ;
                studios.UnionWith(currStudios);
                return true;
            });

            return studios.Count();
        }

        public StatCard TotalStudioCount(User? user, bool movies)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            var retVal = new TextBasedStatCard(movies ? Constants.TotalStudios : Constants.TotalTVNetworks, movies ? Constants.HelpTotalStudios : Constants.HelpTotalTVNetworks, EStatCardSize.eSmall);
            var value = TotalStudioCountValue(user, movies);
            retVal.AddLine(value.ToString());
            return retVal;
        }

        public StatCard TotalMovieStudioCount(User? user)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            return TotalStudioCount(user, true);
        }
        public StatCard TotalTVStudioCount(User? user)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            return TotalStudioCount(user, false);
        }

        public StatCard StatisticFor(User? user, StatGen.EStatisticType whichStatistic, StatGen.EVideoType videoType)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            var statGen = new StatGen(whichStatistic, videoType, _dbHelper);
            return statGen.GetStatCard();
        }

        public StatGen.StatCardValues StatCardValuesFor(User? user, StatGen.EStatisticType whichStatistic, StatGen.EVideoType videoType)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");
            var statGen = new StatGen(whichStatistic, videoType, _dbHelper);
            return statGen.GetStatCardValues();
        }

        public class WatchedMediaValue
        {
            public string ItemId { get; set; } = String.Empty;
            public string Name { get; set; } = String.Empty;
            public string ImageUrl { get; set; } = String.Empty;
            public double PlayCountPerUser { get; set; } = 0.0;

            public string Title(User? user, bool isMovie)
            {
                string title = Name;
                if (user != null)
                {
                    if (isMovie)
                    {
                        title += $" - Watched {PlayCountPerUser:F0} time";

                        if (PlayCountPerUser != 1)
                            title += "s";
                    }
                    else
                    {
                        var percentWatched = 100 * PlayCountPerUser;
                        if (percentWatched > 100)
                            title += $" - Percent of Episodes Watched: {percentWatched:F0}%";
                        else
                            title += $" - Percent of Series Watched: {percentWatched:F2}%";
                    }
                }

                return title;
            }
        }

        public List<WatchedMediaValue> WatchedMediaValues(User? user, bool leastWatched, int numShows, bool excludeAdmin, bool series)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            var numUsers = (user == null) ? NumUsers(false, excludeAdmin) : 1;

            var tableNames = new List<string>();
            if (user != null)
                tableNames.Add(getUserTableName(user));
            else
            {
                tableNames = allUserMediaTables();
            }

            var playMap = new Dictionary<string, (string id, string name, double playCountPerUser)>();
            foreach (var tableName in tableNames)
            {
                var sql = String.Empty;
                if (series)
                {
                    sql = "SELECT " +
                    $"  Series.ItemId" +
                    $", Series.Name" +
                    $", ((1.0 * SUM(PlayCount)) / (1.0 * Series.NumEpisodes)) AS PerUser " +
                    $"FROM Series " +
                    $"LEFT OUTER JOIN {tableName} ON Series.ItemId = {tableName}.SeriesId " +
                    $"LEFT OUTER JOIN Users ON {tableName}.UserId = Users.UserId ";

                    var clauses = new List<string>() { "PlayCount > 0" };
                    if (excludeAdmin)
                    {
                        clauses.Add("NOT Users.IsAdministrator");
                    }
                    if (user != null)
                    {
                        clauses.Add($"{tableName}.UserId = '{user.Id.ToString()}'");
                    }

                    sql += DBHelper.JoinClauses(clauses);

                    sql += $"GROUP BY SeriesId " +
                           $"ORDER BY PerUser ";
                    if (leastWatched)
                        sql += "ASC ";
                    else
                        sql += "DESC ";
                }
                else
                {
                    sql = "SELECT " +
                        $"  {tableName}.ItemId" +
                        $", {tableName}.Name" +
                        $", (1.0 * SUM({tableName}.PlayCount)) AS PerUser " +
                        $"FROM {tableName} " +
                        $"LEFT OUTER JOIN Users ON {tableName}.UserId = Users.UserId "
                        ;

                    var clauses = new List<string>()
                    {
                        "PlayCount > 0",
                        $"NOT {tableName}.IsEpisode"
                    };

                    if (excludeAdmin)
                    {
                        clauses.Add("NOT Users.IsAdministrator");
                    }
                    if (user != null)
                    {
                        clauses.Add($"{tableName}.UserId = '{user.Id.ToString()}'");
                    }

                    sql += DBHelper.JoinClauses(clauses);

                    sql += $"GROUP BY {tableName}.ItemId " +
                           $"ORDER BY PerUser ";
                    if (leastWatched)
                        sql += "ASC ";
                    else
                        sql += "DESC ";
                }

                _dbHelper.ExecuteCommand(new SQLCmdDef(sql), statement =>
                {
                    var row = statement.Current;
                    var id = row.GetString(0);
                    var name = row.GetString(1);
                    var playCountPerUser = row.GetDouble(2);
                    if (playMap.TryGetValue(id, out var currentValue))
                    {
                        // Safely updates based on the current value
                        playMap[id] = (id, name, currentValue.playCountPerUser + playCountPerUser);
                    }
                    else
                        playMap[id] = (id, name, playCountPerUser);
                    return true;
                });
            }

            List<(string id, string name, double playCountPerUser)> asList = new(playMap.Values);

            asList.Sort((a, b) =>
            {
                if (leastWatched)
                    return a.playCountPerUser.CompareTo(b.playCountPerUser);
                else
                    return b.playCountPerUser.CompareTo(a.playCountPerUser);
            });

            var retVal = new List<WatchedMediaValue>();

            for (int ii = 0; ii < Math.Min(numShows, asList.Count); ++ii)
            {
                var id = asList[ii].id;
                var name = asList[ii].name;
                var playCountPerUser = asList[ii].playCountPerUser;
                retVal.Add(new WatchedMediaValue()
                {
                    ItemId = id,
                    Name = name,
                    ImageUrl = ItemImageUrl._ItemImageUrl(id, _embyManagers!._libraryManager),
                    PlayCountPerUser = playCountPerUser
                });
            }

            return retVal;
        }

        public StatCard WatchedMedia(User? user, bool leastWatched, int numShows, bool excludeAdmin, bool series)
        {
            var watchedMedia = WatchedMediaValues(user, leastWatched, numShows, excludeAdmin, series);
            var title = String.Empty;
            var help = String.Empty;

            if (series)
            {
                title = leastWatched ? Constants.LeastWatchedShows : Constants.MostWatchedShows;
                help = leastWatched ? Constants.HelpLeastWatchedShows : Constants.HelpMostWatchedShows;
            }
            else
            {
                title = leastWatched ? Constants.LeastWatchedMovies : Constants.MostWatchedMovies;
                help = leastWatched ? Constants.HelpLeastWatchedMovies : Constants.HelpMostWatchedMovies;
            }

            var retVal = new TextBasedStatCard(title, help, EStatCardSize.eMedium);
            retVal.SubTitle = "(Weighted Watched across Users)";
            retVal.AsNumberedList = true;
            for (int ii = 0; ii < watchedMedia.Count; ++ii)
            {
                retVal.AddLine($"{watchedMedia[ii].Title(user, !series)}", watchedMedia[ii].ItemId, watchedMedia[ii].ImageUrl);
            }
            if (watchedMedia.IsNullOrEmpty())
            {
                var name = series ? "TV Shows" : "Movies";
                retVal.AddLine($"Watch some {name} already!");
            }

            return retVal;
        }

        public StatCard TotalTime(User? user, bool? episodesOnly, bool played)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            if (user == null)
                throw new ArgumentNullException("user");

            var tableName = getUserTableName(user);
            string sql = "SELECT SUM(RunTimeTicks) " +
                $"FROM {tableName} " +
                $"LEFT JOIN Media ON {tableName}.ItemId=Media.ItemId "
            ;
            var clauses = new List<string>() { $"{tableName}.UserId=@UserId" };
            var title = String.Empty;

            if (episodesOnly == null)
            {
                title = played ? Constants.UserTotalTimeWatched : Constants.UserTotalWatchableTime;
            }
            else if (episodesOnly.Value)
            {
                title = played ? Constants.UserTotalEpisodeTimeWatched : Constants.UserTotalEpisodeWatchableTime;
                clauses.Add($"{tableName}.IsEpisode");
            }
            else
            {
                title = played ? Constants.UserTotalMovieTimeWatched : Constants.UserTotalMovieWatchableTime;
                clauses.Add($"NOT {tableName}.IsEpisode");
            }
            if (played)
                clauses.Add($"{tableName}.IsPlayed");

            sql += DBHelper.JoinClauses(clauses);

            return ValueGroupForSingleItem(title, null, sql, new List<(string name, object? value)>() { ("@UserId", user.Id.ToString()) }, DBHelper.FormatTicks);
        }

        public StatCard TotalTimeWatched(User? user, bool? episodesOnly)
        {
            return TotalTime(user, episodesOnly, true);
        }
        public StatCard TotalWatchableTime(User? user, bool? episodesOnly)
        {
            return TotalTime(user, episodesOnly, false);
        }

        public List<(int year, long count)> FavoriteYearValues(User? user, bool movies)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            if (user == null)
                throw new ArgumentNullException("user");

            var tableName = getUserTableName(user);
            string sql =
                "SELECT COUNT(*) as NumVideos, StartYear From Media "
                + $"INNER JOIN {tableName} On Media.ItemId={tableName}.ItemId "
                + "WHERE "
                ;
            if (movies)
            {
                sql += "NOT Media.IsEpisode ";
            }
            else
            {
                sql += "Media.IsEpisode ";
            }
            sql +=
                "AND UserId=@UserId AND IsPlayed "
              + "GROUP BY StartYear "
              + "ORDER BY NumVideos DESC, StartYear ASC "
              + "LIMIT 5 "
              ;

            var sqlCmd = new SQLCmdDef(sql, new List<(string, object?)>()
{
    ( "@UserId", user.Id.ToString())
});

            var retVal = new List<(int year, long count)>();
            _dbHelper.ExecuteCommand(sqlCmd, statement =>
            {
                var row = statement.Current;
                var count = row.GetInt64(0);
                var year = row.GetInt(1);
                retVal.Add((year, count));
                return true;
            });

            return retVal;
        }

        public StatCard FavoriteYears(User? user, bool movies)
        {
            string videoType = "";
            if (movies)
            {
                videoType = "Movies";
            }
            else
            {
                videoType = "Episodes";
            }
            var retVal = new TableBasedStatCard(Constants.FavoriteMovieYears, "Genre", new List<string>() { $"# of {videoType} Watched" });
            retVal.SetDataColumnAlignment(0, StatCard.EAlignment.eCenter);
            var values = FavoriteYearValues(user, movies);

            foreach (var value in values)
            {
                retVal.addRow(value.year.ToString(), new List<long>() { value.count });
            }

            return retVal;
        }

        public List<(string genre, long count)> FavoriteGenreValues(User? user, bool movies)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            if (user == null)
                throw new ArgumentNullException("user");

            var tableName = getUserTableName(user);
            string sql =
                "SELECT Genres From Media "
                + $"INNER JOIN {tableName} On Media.ItemId={tableName}.ItemId "
                + "WHERE "

                ;
            if (movies)
            {
                sql += "NOT Media.IsEpisode ";
            }
            else
            {
                sql += "Media.IsEpisode ";
            }
            sql +=
                "AND UserId=@UserId AND IsPlayed "
              ;

            var sqlCmd = new SQLCmdDef(sql, new List<(string, object?)>()
                                        {
                                            ( "@UserId", user.Id.ToString())
                                        });

            Dictionary<string, int> genreMap = new Dictionary<string, int>();
            _dbHelper.ExecuteCommand(sqlCmd, statement =>
            {
                var row = statement.Current;
                var genres = row.GetString(0)?.Split(',') ?? Array.Empty<string>();
                foreach (var genre in genres)
                {
                    if (!genreMap.ContainsKey(genre))
                        genreMap[genre] = 0;
                    genreMap[genre]++;
                }
                return true;
            });

            var sortedGenre = genreMap.OrderByDescending(kvp => kvp.Value).ToList();

            var retVal = new List<(string genre, long count)>();
            for (int ii = 0; ii < sortedGenre.Count() && ii < 5; ++ii)
            {
                retVal.Add((sortedGenre[ii].Key, sortedGenre[ii].Value));
            }
            return retVal;
        }

        public StatCard FavoriteGenre(User? user, bool movies)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            if (user == null)
                throw new ArgumentNullException("user");

            string videoType = "";
            string title = String.Empty;
            if (movies)
            {
                videoType = "Movies";
                title = Constants.FavoriteMovieGenres;
            }
            else
            {
                videoType = "Episodes";
                title = Constants.FavoriteTVGenres;
            }
            var retVal = new TableBasedStatCard(title, "Genre", new List<string>() { $"# of {videoType} Watched" });
            retVal.SetDataColumnAlignment(0, StatCard.EAlignment.eCenter);

            var values = FavoriteGenreValues(user, movies);
            foreach (var value in values)
            {
                retVal.addRow(value.genre, new List<long>() { value.count });
            }
            return retVal;
        }

        public List<(string name, DateTime lastPlayed)> LastSeenValues(User? user, bool movies)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            if (user == null)
                throw new ArgumentNullException("user");

            var tableName = getUserTableName(user);

            string sql = "SELECT ";
            if (movies)
                sql += "PrimaryName ";
            else
                sql += "PrimaryName || ' - S' || printf('%02d', Season ) || 'E' || printf('%02d', Episode) || ' - ' || SecondaryName ";
            sql += "AS Name " +
                   ", LastPlayedDate " +
                   $"FROM {tableName} " +
                   $"LEFT JOIN Media ON Media.ItemId={tableName}.ItemId " +
                   $"WHERE {tableName}.IsPlayed " +
                   $"AND " + StatGen.validDateClause($"{tableName}.LastPlayedDate") +
                   $"AND {tableName}.UserId = @UserId " +
                   "AND "
                   ;
            if (movies)
                sql += "NOT";
            sql += $" {tableName}.IsEpisode " +
               $"ORDER BY {tableName}.LastPlayedDate DESC " +
               "LIMIT 10 "
               ;

            var sqlCmd = new SQLCmdDef(sql, new List<(string, object?)>()
{
    ( "@UserId", user.Id.ToString())
});

            var retVal = new List<(string genre, DateTime lastPlayed)>();
            _dbHelper.ExecuteCommand(sqlCmd, statement =>
            {
                var row = statement.Current;
                var name = row.GetString(0);
                var date = row.GetString(1);
                var lastPlayedDate = DateTime.ParseExact(date, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                retVal.Add((name, lastPlayedDate));
                return true;
            });

            return retVal;
        }

        public StatCard LastSeen(User? user, bool movies)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            if (user == null)
                throw new ArgumentNullException("user");

            string videoType = String.Empty;
            string title = String.Empty;
            string help = String.Empty;
            if (movies)
            {
                videoType = "Movies";
                title = Constants.LastSeenMovies;
                help = Constants.HelpLastSeenMovies;
            }
            else
            {
                videoType = "TV Series";
                title = Constants.LastSeenTVSeries;
                help = Constants.HelpLastSeenTVSeries;
            }
            var retVal = new TextBasedStatCard(title, help, EStatCardSize.eMedium);
            retVal.AsNumberedList = true;
            retVal.IgnoreLength = true;
            var values = LastSeenValues(user, movies);

            foreach (var value in values)
            {
                retVal.AddLine($"{value.name} - {value.lastPlayed:d}");
            }

            return retVal;
        }

        public List<GetTVSeriesProgressResponse> GetTVSeriesProgress(User? user)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            if (user == null)
                throw new ArgumentNullException("user");

            var getSeriesSQL = "SELECT " +
                "  Series.Name" +
                ", strftime('%Y', Series.PremiereDate) AS PremierDate" +
                ", Series.NumEpisodes" +
                ", Series.NumSpecials" +
                ", Series.Rating" +
                ", Series.Status" +
                ", Series.ItemId " +
                " FROM " +
                "   Series "
                ;

            var series = new Dictionary<string, GetTVSeriesProgressResponse>();
            _dbHelper.ExecuteCommand(new SQLCmdDef(getSeriesSQL), statement =>
            {
                var row = statement.Current;
                var col = 0;
                var name = row.GetString(col++);
                var premiereYear = row.GetInt(col++);
                var totalEpisodes = row.GetInt(col++);
                var totalSpecials = row.GetInt(col++);
                var score = row.GetDouble(col++);
                var status = row.GetString(col++);
                var seriesId = row.GetString(col++); // should be true
                var curr = new GetTVSeriesProgressResponse()
                {
                    SeriesId = seriesId,
                    Name = name,
                    PremiereYear = premiereYear,
                    Score = score,
                    SeriesStatus = status
                };
                curr.Episodes.Total = totalEpisodes;
                curr.Specials.Total = totalSpecials;

                series[seriesId] = curr;
                return true;
            });

            var tableName = getUserTableName(user);
            var sqlBase = "SELECT " +
                $"  SUM({tableName}.NumEpisodes) " +
                $", {tableName}.SeriesId " +
                $" FROM " +
                $"   {tableName} " +
                $" WHERE " +
                $" {tableName}.IsEpisode AND " +
                $" <isTVSpecial> AND " +
                $" {tableName}.IsPlayed AND " +
                $" {tableName}.UserId=@UserId " +
                $" GROUP BY {tableName}.SeriesId "
                ;

            var sqlEpisodes = sqlBase.Replace("<isTVSpecial>", $"NOT {tableName}.IsTVSpecial");
            var sqlSpecials = sqlBase.Replace("<isTVSpecial>", $"{tableName}.IsTVSpecial");

            var paramList = new List<(string name, object? value)>() { ("@UserId", user.Id.ToString()) };

            _dbHelper.ExecuteCommand(new SQLCmdDef(sqlEpisodes, paramList), statement =>
                {
                    var row = statement.Current;
                    var col = 0;
                    var watchedCount = row.GetInt(col++);
                    var seriesId = row.GetString(col++); // should be true

                    if (series.TryGetValue(seriesId, out var curr))
                    {
                        curr.Episodes.Count = watchedCount;
                        series[seriesId] = curr;
                    }
                    return true;
                });

            _dbHelper.ExecuteCommand(new SQLCmdDef(sqlSpecials, paramList), statement =>
            {
                var row = statement.Current;
                var col = 0;
                var watchedCount = row.GetInt(col++);
                var seriesId = row.GetString(col++); // should be true

                if (series.TryGetValue(seriesId, out var curr))
                {
                    curr.Specials.Count = watchedCount;
                    series[seriesId] = curr;
                }
                return true;
            });

            var retVal = series.Values.ToList();

            return retVal;
        }
    }
}
