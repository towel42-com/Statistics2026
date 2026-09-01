using MediaBrowser.Controller.Entities;
using ServiceStack;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;


namespace Statistics2026.Data
{
    public class PercentValue
    {
        private int? _count = null;
        public int? Total { get; set; } = null;

        public string _string { get; set; } = string.Empty;
        public string String
        {
            get
            {
                if (_string.IsEmpty())
                {
                    if (_count == null && Total == null)
                        _string = "0 of 0 (0%)";
                    else if (_count == null && Total != null)
                        _string = $"0 of {Total} (0%)";
                    else if (_count != null && Total == null)
                        _string = $"{_count} of 0 (100%)";
                    else
                    {
                        Percent = (100.0 * _count) / (1.0 * Total) ?? 0.0;
                        _string = $"{_count} of {Total} ({Percent:F0}%)";
                    }
                }
                return _string;
            }
            set { _string = value; }
        }
        public double Percent { get; set; } = 0.0;

        public int Count
        {
            get
            {
                return _count ?? 0;
            }
            set
            {
                _count = value;
                if (_count == null || Total == null)
                    return;

                if (Total == 0)
                {
                    String = string.Empty;
                    Percent = 0.0;
                    return;
                }

                Percent = (100.0 * _count) / (1.0 * Total) ?? 0.0;
                _string = $"{_count}/{Total} ({Percent:F0}%)";
            }
        }
    }

    public class GetTVSeriesProgressResponse
    {
        public string Name { get; set; } = string.Empty;
        public string SeriesId { get; set; } = string.Empty;
        public int PremiereYear { get; set; } = -1;

        public PercentValue Episodes { get; set; } = new PercentValue();
        public PercentValue Specials { get; set; } = new PercentValue();

        private double _score = 0.0;
        public double Score
        {
            get { return _score; }
            set
            {
                _score = value;
                ScoreStr = _score.ToString("F1");
            }
        }
        public string ScoreStr { get; set; } = String.Empty;
        public string SeriesStatus { get; set; } = string.Empty;
    }

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

        public StatCard MediaCodecs(bool showAllCodecs)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

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

        public StatCard DVProfileInfo(bool showUnknownDVProfiles, bool showAllDVProfiles)
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

            var retVal = new TextBasedStatCard(title, help, "small");
            var value = GetSingleValueFromSQL(sql, parameters, formatter);
            retVal.AddLine(value);
            return retVal;
        }

        public StatCard UserCount(bool hasConnectUserID, bool excludeAdmin)
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

            return ValueGroupForSingleItem(Constants.TotalUsers, null, sql);
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

        public StatCard TotalFinishedSeries(User? user)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            if (user == null)
                throw new ArgumentNullException("user");

            var sql =
                "SELECT " +
                    "  PrimaryName" +
                    ", Media.SeriesId " +
                    ", SUM(UserVideoList.NumEpisodes) " +
                    ", Series.NumEpisodes " +
                "FROM UserVideoList " +
                "LEFT JOIN Media ON UserVideoList.ItemId=Media.ItemId " +
                "LEFT JOIN Series ON Series.ItemId=Media.SeriesId " +
                "WHERE Media.IsEpisode AND NOT Media.IsTVSpecial AND UserVideoList.IsPlayed " +
                "AND UserVideoList.UserId=@UserId " +
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

            var retVal = new TextBasedStatCard(Constants.TotalSeriesFinished, Constants.HelpTotalSeriesFinished, "small");
            retVal.AddLine(seriesInfo.Count().ToString());
            return retVal;
        }

        public StatCard TotalTVCount(User? user, bool watched)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            string seriesColumn = string.Empty;
            string seriesFrom = string.Empty;
            string episodeColumn = string.Empty;
            string episodeFrom = string.Empty;

            string titleSeries = string.Empty;
            string titleEpisodes = string.Empty;
            string helpEpisodes = string.Empty;
            List<(string name, object? value)>? paramList = null;
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
                episodeColumn = "SUM(UserVideoList.NumEpisodes)";

                titleSeries = Constants.TotalUserTVShows;
                titleEpisodes = Constants.TotalUserTVEpisodes;
                helpEpisodes = Constants.HelpTotalUserTVShows;

                var from = "UserVideoList LEFT JOIN Media ON UserVideoList.ItemId=Media.ItemId WHERE Media.IsEpisode AND NOT Media.IsTVSpecial AND ( UserVideoList.UserId=@UserId )";

                if (watched)
                {
                    from += " AND ( UserVideoList.IsPlayed )";

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
                var currStudios = row.GetString(0).Split(',');
                studios.UnionWith(currStudios);
                return true;
            });

            return studios.Count();
        }

        public StatCard TotalStudioCount(User? user, bool movies)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            var retVal = new TextBasedStatCard(movies ? Constants.TotalStudios : Constants.TotalTVNetworks, movies ? Constants.HelpTotalStudios : Constants.HelpTotalTVNetworks, "small");
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

        public class WatchedShowValue
        {
            public WatchedShowValue() { }
            public WatchedShowValue(long numUsers) { _numUsers = numUsers; }
            public string ItemId { get; set; } = String.Empty;
            public string Name { get; set; } = String.Empty;
            public string ImageUrl { get; set; } = String.Empty;
            public long NumEpisodes { get; set; } = 0;
            public long NumWatched
            {
                get
                {
                    return _numWatched;
                }
                set
                {
                    _numWatched = value;
                    UpdatePercents();
                }
            }
            public double PercentWatchedPerUser { get; set; } = 0;

            private long _numUsers = 0;
            private long _numWatched = 0;
            private void UpdatePercents()
            {
                var percentWatched = (1.0 * NumWatched) / (1.0 * NumEpisodes);
                PercentWatchedPerUser = percentWatched / (1.0 * _numUsers);
            }

        }

        public List<WatchedShowValue> ComputeWatchedShowValues(User? user, CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            var sqlCommand = new SQLCmdDef("SELECT COUNT(1) FROM Users");
            long numUsers = 0;
            _dbHelper.ExecuteCommand(sqlCommand, statement =>
            {
                var row = statement.Current;
                numUsers = row.GetInt64(0);
                return false;
            });

            sqlCommand = new SQLCmdDef("SELECT COUNT(1) FROM Series");
            double numSeries = 0;
            _dbHelper.ExecuteCommand(sqlCommand, statement =>
            {
                var row = statement.Current;
                numSeries = 1.0 * row.GetInt64(0);
                return false;
            });

            var retVal = new List<WatchedShowValue>();
            var sql =
                "SELECT " +
                "  Series.ItemId" +
                ", Series.Name" +
                ", Series.NumEpisodes" + // number in the series
                ", SUM(UserVideoList.NumEpisodes) " +  // number watched
                "FROM Series " +
                "LEFT JOIN UserVideoList On UserVideoList.SeriesId=Series.ItemId " +
                "WHERE " +
                "UserVideoList.IsEpisode AND " +
                "NOT UserVideoList.IsTVSpecial AND " +
                "UserVideoList.IsPlayed " +
                "GROUP BY Series.ItemId "
                ;

            sqlCommand = new SQLCmdDef(sql);
            progress.Report(0);
            long count = 0;
            _dbHelper.ExecuteCommand(sqlCommand, statement =>
            {
                var row = statement.Current;
                var itemId = row.GetString(0);
                var name = row.GetString(1);
                var numEpisodes = row.GetInt64(2);
                var numWatched = row.GetInt64(3);
                var url = ItemImageUrl._ItemImageUrl(itemId, _embyManagers!._libraryManager);
                retVal.Add(new WatchedShowValue(numUsers)
                {
                    ItemId = itemId,
                    Name = name,
                    ImageUrl = url,
                    NumEpisodes = numEpisodes,
                    NumWatched = numWatched,
                });
                cancellationToken.ThrowIfCancellationRequested();
                progress.Report((100.0 * (count++) / numSeries));
                return true;
            });

            return retVal;
        }

        public List<WatchedShowValue> WatchedShowValues(User? user, bool leastWatched)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            var series = new List<WatchedShowValue>();

            var sql = "SELECT "
                + "  ItemId "
                + ", Name "
                + ", ImageUrl "
                + ", NumEpisodes "
                + ", NumWatched "
                + ", PercentWatchedPerUser " +
                "FROM CachedWatchedAnalysis " +
                "WHERE PercentWatchedPerUser <> 0 " +
                "ORDER BY PercentWatchedPerUser ";
            ;
            if (leastWatched)
                sql += "ASC ";
            else
                sql += "DESC ";
            sql += "LIMIT 5";


            _dbHelper.ExecuteCommand(new SQLCmdDef(sql), statement =>
            {
                var row = statement.Current;
                var id = row.GetString(0);
                var name = row.GetString(1);
                var url = row.GetString(2);
                var numEpisodes = row.GetInt(3);
                var numWatched = row.GetInt(4);
                var percentWatchedPerUser = row.GetFloat(5);
                series.Add(new WatchedShowValue()
                {
                    ItemId = id,
                    Name = name,
                    ImageUrl = url,
                    NumWatched = numWatched,
                    NumEpisodes = numEpisodes,
                    PercentWatchedPerUser = percentWatchedPerUser
                });
                return series.Count <= 5;
            });

            return series;
        }

        public StatCard WatchedShows(User? user, bool leastWatched)
        {
            var series = WatchedShowValues(user, leastWatched);
            var title = leastWatched ? Constants.LeastWatchedShows : Constants.MostWatchedShows;
            var help = leastWatched ? Constants.HelpLeastWatchedShows : Constants.HelpMostWatchedShows;

            var retVal = new TextBasedStatCard(title, help, "small");
            retVal.SubTitle = "(Average Percentage Watched across All Users)";
            retVal.AsNumberedList = true;
            for (int ii = 0; ii < series.Count && ii < 5; ++ii)
            {
                retVal.AddLine($"{series[ii].Name} ({series[ii].PercentWatchedPerUser:P2})", series[ii].ItemId, series[ii].ImageUrl);
            }

            return retVal;
        }

        public StatCard TotalTime(User? user, bool? episodesOnly, bool played)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            if (user == null)
                throw new ArgumentNullException("user");

            string sql = "SELECT SUM(RunTimeTicks) " +
                "FROM UserVideoList " +
                "LEFT JOIN Media ON UserVideoList.ItemId=Media.ItemId "
            ;
            var clauses = new List<string>() { "UserId=@UserId" };
            var title = String.Empty;

            if (episodesOnly == null)
            {
                title = played ? Constants.UserTotalTimeWatched : Constants.UserTotalWatchableTime;
            }
            else if (episodesOnly.Value)
            {
                title = played ? Constants.UserTotalEpisodeTimeWatched : Constants.UserTotalEpisodeWatchableTime;
                clauses.Add("UserVideoList.IsEpisode");
            }
            else
            {
                title = played ? Constants.UserTotalMovieTimeWatched : Constants.UserTotalMovieWatchableTime;
                clauses.Add("NOT UserVideoList.IsEpisode");
            }
            if (played)
                clauses.Add("IsPlayed");

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

            string sql =
                "SELECT COUNT(*) as NumVideos, StartYear From Media "
                + "INNER JOIN UserVideoList On Media.ItemId=UserVideoList.ItemId "
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

            string sql =
                "SELECT Genres From Media "
                + "INNER JOIN UserVideoList On Media.ItemId=UserVideoList.ItemId "
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
                var genres = row.GetString(0).Split(',');
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
            if (movies)
            {
                videoType = "Movies";
            }
            else
            {
                videoType = "Episodes";
            }
            var retVal = new TableBasedStatCard(Constants.FavoriteMovieGenres, "Premiere Year", new List<string>() { $"# of {videoType} Watched" });
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

            string sql = "SELECT ";
            if (movies)
                sql += "PrimaryName ";
            else
                sql += "PrimaryName || ' - S' || printf('%02d', Season ) || 'E' || printf('%02d', Episode) || ' - ' || SecondaryName ";
            sql += "AS Name " +
                   ", LastPlayedDate " +
                   "FROM UserVideoList " +
                   "LEFT JOIN Media ON Media.ItemId=UserVideoList.ItemId " +
                   "WHERE UserVideoList.IsPlayed " +
                   "AND " + StatGen.validDateClause("UserVideoList.LastPlayedDate") +
                   "AND UserVideoList.UserId = @UserId " +
                   "AND "
                   ;
            if (movies)
                sql += "NOT";
            sql += " UserVideoList.IsEpisode " +
               "ORDER BY UserVideoList.LastPlayedDate DESC " +
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

            string videoType = string.Empty;
            string title = string.Empty;
            string help = string.Empty;
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
            var retVal = new TextBasedStatCard(title, help, "half");
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

            var sqlBase = "SELECT " +
                "  SUM(UserVideoList.NumEpisodes) " +
                ", UserVideoList.SeriesId " +
                " FROM " +
                "   UserVideoList " +
                " WHERE " +
                " UserVideoList.IsEpisode AND " +
                " <isTVSpecial> AND " +
                " UserVideoList.IsPlayed AND " +
                " UserVideoList.UserId=@UserId " +
                " GROUP BY UserVideoList.SeriesId "
                ;

            var sqlEpisodes = sqlBase.Replace("<isTVSpecial>", "NOT UserVideoList.IsTVSpecial");
            var sqlSpecials = sqlBase.Replace("<isTVSpecial>", "UserVideoList.IsTVSpecial");

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
