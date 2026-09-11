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
    using WatchedMediaValueItemData = (string id, string name, long playCount, long denominator, double playCountPerUser);

    public enum EMediaType
    {
        eMovie,
        eSeries,
        eEpisode
    }

    public sealed partial class StatisticsDB
    {
        public StatCard MediaResolutions()
        {
            CheckIsValid();

            var retVal = new TableBasedStatCard(Constants.MediaResolutions, Constants.HelpMediaResolutions, new List<string> { "Movies", "Episodes" });

            if (Plugin.Instance!.Configuration.showAllResolutions)
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
            CheckIsValid();

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

        public StatCard DVProfileInfo()
        {
            CheckIsValid();

            string sql =
                "SELECT " +
                "DolbyVisionProfile as DVProfile, " +
                "sum(IsEpisode) AS Episodes, " +
                "sum(NOT IsEpisode) AS Movies " +
                "FROM Media ";

            if (!Plugin.Instance!.Configuration.showUnknownDVProfiles)
                sql += $"WHERE DolbyVisionProfile NOT IN ({string.Join(",", Constants.UnknownDolbyProfiles.Select(p => $"'{p}'"))}) ";

            sql += "GROUP BY DolbyVisionProfile " +
                   "ORDER BY DolbyVisionProfile ASC"
                   ;

            var retVal = new TableBasedStatCard(Constants.DolbyVisionProfiles, Constants.HelpDolbyVisionProfile, new List<string> { "Movies", "Episodes" });
            if (Plugin.Instance!.Configuration.showUnknownDVProfiles)
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
            CheckIsValid();

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
            CheckIsValid();

            var retVal = new TextBasedStatCard(title, help, EStatCardSize.eSmall);
            var value = GetSingleValueFromSQL(sql, parameters, formatter);
            retVal.AddLine(value);
            return retVal;
        }

        private TextBasedStatCard ValueGroupForSingleValue(string title, string? help, Object value)
        {
            CheckIsValid();

            var retVal = new TextBasedStatCard(title, help, EStatCardSize.eSmall);
            retVal.AddLine(value.ToString());
            return retVal;
        }

        public long NumUsers(bool hasConnectUserId, bool excludeAdmin)
        {
            CheckIsValid();

            string sql = "SELECT COUNT(UserName) FROM Users ";

            List<string> conditions = new List<string>();

            if (hasConnectUserId)
                conditions.Add("ConnectUserId <> '' AND ConnectUserId IS NOT NULL");

            if (excludeAdmin)
                conditions.Add("NOT IsAdministrator");

            sql += DBHelper.JoinClauses(conditions);

            return GetSingleValueFromSQL(sql).ToInt64();
        }

        public long NumUsers()
        {
            CheckIsValid();

            return NumUsers(Statistics2026.Plugin.Instance!.Configuration.hasConnectUserID, Statistics2026.Plugin.Instance!.Configuration.excludeAdmin);
        }

        public StatCard UserCount()
        {
            var numUsers = NumUsers();
            return ValueGroupForSingleValue(Constants.TotalUsers, null, numUsers);
        }

        public StatCard MostActiveUsers()
        {
            CheckIsValid();

            string sql =
                "SELECT " +
                "UserName, " +
                "TotalTimeWatched " +
                "FROM Users ";
            List<string> conditions = new List<string>();

            if (Statistics2026.Plugin.Instance!.Configuration.hasConnectUserID)
                conditions.Add("ConnectUserId <> '' AND ConnectUserId IS NOT NULL");

            if (Statistics2026.Plugin.Instance!.Configuration.excludeAdmin)
                conditions.Add("NOT IsAdministrator");

            sql += DBHelper.JoinClauses(conditions);

            var numUsers = Plugin.Instance.Configuration.numMostActiveUsers;
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
            CheckIsValid();

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
            CheckIsValid();

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
            CheckIsValid();

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
            CheckIsValid();

            string sql = "SELECT COUNT( ItemId ) FROM Collections";

            return ValueGroupForSingleItem(Constants.TotalCollections, Constants.HelpTotalCollections, sql);
        }

        public long TotalStudioCountValue(User? user, bool movies)
        {
            CheckIsValid();

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
            CheckIsValid();

            var retVal = new TextBasedStatCard(movies ? Constants.TotalStudios : Constants.TotalTVNetworks, movies ? Constants.HelpTotalStudios : Constants.HelpTotalTVNetworks, EStatCardSize.eSmall);
            var value = TotalStudioCountValue(user, movies);
            retVal.AddLine(value.ToString());
            return retVal;
        }

        public StatCard TotalMovieStudioCount(User? user)
        {
            CheckIsValid();

            return TotalStudioCount(user, true);
        }

        public StatCard TotalTVStudioCount(User? user)
        {
            CheckIsValid();

            return TotalStudioCount(user, false);
        }

        public StatCard StatisticFor(User? user, StatGen.EStatisticType whichStatistic, StatGen.EVideoType videoType)
        {
            CheckIsValid();

            var statGen = new StatGen(whichStatistic, videoType, _dbHelper);
            return statGen.GetStatCard();
        }

        public StatGen.StatCardValues StatCardValuesFor(User? user, StatGen.EStatisticType whichStatistic, StatGen.EVideoType videoType)
        {
            CheckIsValid();

            var statGen = new StatGen(whichStatistic, videoType, _dbHelper);
            return statGen.GetStatCardValues();
        }

        public class WatchedMediaValue
        {
            public string ItemId { get; set; } = String.Empty;
            public string Name { get; set; } = String.Empty;
            public string ImageUrl { get; set; } = String.Empty;
            public long PlayCount { get; set; } = 0;
            public long Denominator { get; set; } = 0;
            public double PlayCountPerUser { get; set; } = 0.0;
            public EMediaType MediaType { get; set; } = EMediaType.eEpisode;

            public string Title()
            {
                string title = Name;
                if (MediaType == EMediaType.eMovie)
                {
                    title += $" - Watched {PlayCount} time";

                    if (PlayCount != 1)
                        title += "s";
                }
                else
                {
                    if (Denominator == PlayCount)
                    {
                        title += $" - {Denominator} Episodes played 1 time each";
                    }
                    else
                    {
                        title += $" - For {Denominator} Episodes, a total of {PlayCount} play";
                        if (PlayCount != 1)
                            title += "s";
                    }
                }

                return title;
            }
        }

        public Dictionary<long, List<WatchedMediaValue>> WatchedMediaValues(User? user, bool leastWatched, EMediaType mediaType)
        {
            CheckIsValid();

            var excludeAdmin = Statistics2026.Plugin.Instance!.Configuration.excludeAdmin;
            var numUsers = (user == null) ? NumUsers(false, excludeAdmin) : 1;

            var tableNames = new List<string>();
            if (user != null)
                tableNames.Add(getUserTableName(user));
            else
            {
                tableNames = allUserMediaTables();
            }

            //using UserInfo = (string Name, int Age);

            var playMap = new Dictionary<string, WatchedMediaValueItemData>();
            foreach (var tableName in tableNames)
            {
                var sql = String.Empty;
                if (mediaType == EMediaType.eSeries)
                {
                    sql = "SELECT " +
                    $"  Series.ItemId" +
                    $", Series.Name" +
                    $", SUM(PlayCount) AS PlayCount " +
                    $", Series.NumEpisodes AS NumEpisodes" +
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
                else if (mediaType == EMediaType.eMovie)
                {
                    sql = "SELECT " +
                        $"  {tableName}.ItemId" +
                        $", {tableName}.Name" +
                        $", SUM({tableName}.PlayCount) AS PlayCount " +
                        $", 1 AS NumEpisodes" +
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
                    var col = 0;
                    var id = row.GetString(col++);
                    var name = row.GetString(col++);
                    var playCount = row.GetInt64(col++);
                    var numEpisodes = row.GetInt64(col++);
                    var playCountPerUser = row.GetDouble(col++);
                    if (playMap.TryGetValue(id, out var currentValue))
                    {
                        // Safely updates based on the current value
                        playMap[id] = (id, name, currentValue.playCount + playCount, currentValue.denominator + numEpisodes, currentValue.playCountPerUser + playCountPerUser);
                    }
                    else
                        playMap[id] = (id, name, playCount, numEpisodes, playCountPerUser);
                    return true;
                });
            }

            List<WatchedMediaValueItemData> asList = new(playMap.Values);

            asList.Sort((a, b) =>
            {
                if (leastWatched)
                    return a.playCountPerUser.CompareTo(b.playCountPerUser);
                else
                    return b.playCountPerUser.CompareTo(a.playCountPerUser);
            });

            var retVal = new Dictionary<long, List<WatchedMediaValue>>();

            var numResultsToGet = Statistics2026.Plugin.Instance!.Configuration.numWatchedToReport;

            for (int ii = 0; (retVal.Count < numResultsToGet) && (ii < asList.Count); ++ii)
            {
                var curr = asList[ii];
                if (!retVal.TryGetValue(curr.playCount, out var value))
                {
                    retVal[curr.playCount] = new List<WatchedMediaValue>();
                }

                retVal[curr.playCount].Add(new WatchedMediaValue()
                {
                    ItemId = curr.id,
                    Name = curr.name,
                    ImageUrl = ItemImageUrl._ItemImageUrl(curr.id, _embyInterfaces!._libraryManager),
                    PlayCount = curr.playCount,
                    Denominator = curr.denominator,
                    PlayCountPerUser = curr.playCountPerUser,
                    MediaType = mediaType
                });
            }

            return retVal;
        }

        public StatCard WatchedMedia(User? user, bool leastWatched, EMediaType mediaType)
        {
            var watchedMedia = WatchedMediaValues(user, leastWatched, mediaType);
            var title = String.Empty;
            var help = String.Empty;

            if ((mediaType == EMediaType.eSeries) || (mediaType == EMediaType.eEpisode))
            {
                title = leastWatched ? Constants.LeastWatchedShows : Constants.MostWatchedShows;
                help = leastWatched ? Constants.HelpLeastWatchedShows : Constants.HelpMostWatchedShows;
            }
            else if (mediaType == EMediaType.eMovie)
            {
                title = leastWatched ? Constants.LeastWatchedMovies : Constants.MostWatchedMovies;
                help = leastWatched ? Constants.HelpLeastWatchedMovies : Constants.HelpMostWatchedMovies;
            }

            var retVal = new TextBasedStatCard(title, help, EStatCardSize.eMedium);
            retVal.SubTitle = "(Weighted Watched across Users)";
            retVal.ListType = TextBasedStatCard.EListType.eNumberedGroupByKey;
            foreach (var currList in watchedMedia.OrderBy(x => x.Key))
            {
                foreach (var curr in currList.Value)
                {
                    retVal.AddLine($"{curr.Title()}", curr.ItemId, curr.ImageUrl);
                    retVal.AddKey(curr.PlayCount.ToString());
                }
            }
            if (watchedMedia.Count == 0)
            {
                string name;
                if (mediaType == EMediaType.eSeries)
                    name = "TV Shows";
                else if (mediaType == EMediaType.eEpisode)
                    name = "TV Episodes";
                else // mediaType == EMediaType.eMovies
                    name = "Movies";
                retVal.AddLine($"Watch some {name} already!");
            }

            return retVal;
        }

        public StatCard TotalTime(User? user, bool? episodesOnly, bool played)
        {
            CheckIsValid();

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
            CheckIsValid();

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
            CheckIsValid();

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
            CheckIsValid();

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
            CheckIsValid();

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
                var lastPlayedDate = DBHelper.ReadDateTime(date);
                retVal.Add((name, lastPlayedDate));
                return true;
            });

            return retVal;
        }

        public StatCard LastSeen(User? user, bool movies)
        {
            CheckIsValid();

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
            retVal.ListType = TextBasedStatCard.EListType.eNumbered;
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
            CheckIsValid();

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
                ", Series.ImageUrl " +
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
                var seriesId = row.GetString(col++);
                var imageUrl = row.GetString(col++);

                var curr = new GetTVSeriesProgressResponse()
                {
                    SeriesId = seriesId,
                    Name = name,
                    PremiereYear = premiereYear,
                    Score = score,
                    SeriesStatus = status,
                    ItemUrl = imageUrl
                };
                if (curr.ItemUrl != null && curr.ItemUrl != "")
                {
                    curr.ItemUrl = ItemImageUrl.ItemUrl(seriesId, curr.ItemUrl, curr.Name);
                    curr.Name = curr.ItemUrl;
                }

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

        private List<MediaItemResponse> getMediaListResponse(bool episodes)
        {
            var retVal = new List<MediaItemResponse>();

            var sql = "SELECT ";
            if (episodes)
                sql += "  PrimaryName || ' - S' || printf( '%02d', Season ) || 'E' || printf('%02d', Episode) || ' - ' || SecondaryName AS ListDisplayName";
            else
                sql += "  PrimaryName AS ListDisplayName";

            sql +=
                ", StartYear" +
                ", ResolutionDetail" +
                ", Codec" +
                ", DolbyVisionProfile" +
                ", ServerLocation" +
                ", ItemId" +
                ", ImageUrl" +
                " FROM " +
                "   Media ";
            if (episodes)
                sql += " WHERE IsEpisode ";
            else
                sql += " WHERE NOT IsEpisode ";

            sql += " ORDER BY PrimaryName ASC, Season ASC, Episode ASC ";
            _dbHelper.ExecuteCommand(new SQLCmdDef(sql), statement =>
            {
                var row = statement.Current;
                var col = 0;
                var curr = new MediaItemResponse()
                {
                    ListDisplayName = row.GetString(col++),
                    StartYear = row.GetString(col++),
                    ResolutionDetail = row.GetString(col++),
                    Codec = row.GetString(col++),
                    DolbyVisionProfile = row.GetString(col++),
                    ServerLocation = row.GetString(col++)
                };
                var itemId = row.GetString(col++);
                var itemUrl = row.GetString(col++);
                curr.ItemUrl = ItemImageUrl.ItemUrl(itemId, itemUrl, curr.ListDisplayName);
                if (curr.ItemUrl != null && curr.ItemUrl != "")
                {
                    curr.ListDisplayName = curr.ItemUrl;
                }

                if (curr.Codec != "hevc" && curr.Codec != "av1")
                    curr.DolbyVisionProfile = String.Empty;

                retVal.Add(curr);
                return true;
            });

            return retVal;
        }

        public List<MediaItemResponse> GetEpisodeList()
        {
            return getMediaListResponse(true);
        }

        public List<MediaItemResponse> GetMovieList()
        {
            return getMediaListResponse(false);
        }
    }
}
