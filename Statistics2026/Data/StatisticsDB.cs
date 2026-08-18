using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using SQLitePCL.pretty;
using Statistics2026;
using Statistics2026.Configuration;
using Statistics2026.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;


namespace Statistics2026.Data
{
    public sealed class StatisticsDB
    {
        private static StatisticsDB instance = null;
        private static readonly object _padlock = new object();

        private static string[] _datetimeFormats = new string[] {
            "THHmmssK",
            "THHmmK",
            "HH:mm:ss.FFFFFFFK",
            "HH:mm:ssK",
            "HH:mmK",
            "yyyy-MM-dd HH:mm:ss.FFFFFFFK", /* NOTE: UTC default (5). */
            "yyyy-MM-dd HH:mm:ssK",
            "yyyy-MM-dd HH:mmK",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
            "yyyy-MM-ddTHH:mmK",
            "yyyy-MM-ddTHH:mm:ssK",
            "yyyyMMddHHmmssK",
            "yyyyMMddHHmmK",
            "yyyyMMddTHHmmssFFFFFFFK",
            "THHmmss",
            "THHmm",
            "HH:mm:ss.FFFFFFF",
            "HH:mm:ss",
            "HH:mm",
            "yyyy-MM-dd HH:mm:ss.FFFFFFF", /* NOTE: Non-UTC default (19). */
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyyMMddHHmmss",
            "yyyyMMddHHmm",
            "yyyyMMddTHHmmssFFFFFFF",
            "yyyy-MM-dd",
            "yyyyMMdd",
            "yy-MM-dd"
        };
        private string _datetimeFormatUtc = _datetimeFormats[5];
        private string _datetimeFormatLocal = _datetimeFormats[19];

        private ILogger _logger = null;
        private IDatabaseConnection connection = null;

        public static StatisticsDB GetInstance(string db_file, ILogger log)
        {
            lock (_padlock)
            {
                if (instance == null)
                {
                    instance = new StatisticsDB(db_file, log);
                    log.Info("StatisticsData : New Instance Created : " + instance.GetHashCode());
                }
                return instance;
            }
        }

        public static StatisticsDB GetExistingInstance()
        {
            lock (_padlock)
            {
                if (instance == null)
                {
                    throw new InvalidOperationException("No existing instance found.");
                }
                return instance;
            }
        }


        private StatisticsDB()
        {

        }

        private StatisticsDB(string db_path, ILogger l)
        {
            _logger = l;
            _logger.Info("StatisticsData : Creating");
            string db_file_name = Path.Combine(db_path, "Statistics2026.db");
            connection = CreateConnection(db_file_name);
        }

        ~StatisticsDB()
        {
            _logger.Info("StatisticsData : Cleaning up");
            if (connection != null)
            {
                connection.Close();
                _logger.Info("StatisticsData : DB Connection Closed");
            }
        }

        public void Initialize()
        {
            InitializeInternal();
            ClearMediaInfo();
            ClearUserInfo();
        }

        private void TryBind(IStatement statement, string name, int value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                _logger.Debug($"Error Binding {name} to {value}");
            }
        }

        private void TryBind(IStatement statement, string name, bool value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                _logger.Debug($"Error Binding {name} to {value}");
            }
        }

        public void TryBind(IStatement statement, string name, string value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                if (value == null)
                {
                    bindParam.BindNull();
                }
                else
                {
                    bindParam.Bind(value);
                }
            }
            else
            {
                _logger.Debug($"Error Binding {name} to {value}");
            }
        }

        private string GetDateTimeKindFormat(DateTimeKind kind)
        {
            return (kind == DateTimeKind.Utc) ? _datetimeFormatUtc : _datetimeFormatLocal;
        }

        public DateTime ReadDateTime(string dateText)
        {
            return DateTime.ParseExact(
                dateText,
                _datetimeFormats,
                DateTimeFormatInfo.InvariantInfo,
                DateTimeStyles.None).ToUniversalTime();
        }

        public string ToDateTimeParamValue(DateTime dateValue)
        {
            var kind = DateTimeKind.Utc;
            if (dateValue.Kind == DateTimeKind.Unspecified) // if Unspecified force UTC
            {
                return DateTime.SpecifyKind(dateValue, kind).ToString(GetDateTimeKindFormat(kind), CultureInfo.InvariantCulture);
            }
            else
            {
                return dateValue.ToString(GetDateTimeKindFormat(dateValue.Kind), CultureInfo.InvariantCulture);
            }
        }

        private IDatabaseConnection CreateConnection(string db_file)
        {
            _logger.Info("StatisticsData : CreateConnection : " + db_file);
            ConnectionFlags connectionFlags;

            //Logger.Info("Opening write connection");
            connectionFlags = ConnectionFlags.Create;
            connectionFlags |= ConnectionFlags.ReadWrite;
            connectionFlags |= ConnectionFlags.PrivateCache;
            connectionFlags |= ConnectionFlags.NoMutex;

            SQLiteDatabaseConnection db = SQLite3.Open(db_file, connectionFlags, null, true);

            try
            {
                var queries = new List<string>
                {
                    //"PRAGMA cache size=-10000"
                    //"PRAGMA read_uncommitted = true",
                    "PRAGMA synchronous=Normal",
                    "PRAGMA temp_store=file"
                };

                db.ExecuteAll(string.Join(";", queries.ToArray()));
            }
            catch
            {
                throw;
            }

            _logger.Info("StatisticsData : ConnectionCreated : " + db.GetHashCode());
            return db;
        }

        private void InitializeInternal()
        {
            lock (connection)
            {
                // create tables if they dont already exist
                // ROWID 
                connection.Execute("create table if not exists StatusInfo (" +
                                "LastUpdated DATETIME NOT NULL, " +
                                "Version TEXT, " +
                                "BuildDate TEXT " +
                                ")");

                connection.Execute("create table if not exists MediaInfo (" +
                                "ItemId TEXT NOT NULL, " +
                                "PrimaryName TEXT, " +
                                "SortName TEXT, " +
                                "SecondaryName, " +
                                "StartYear INT, " +
                                "IsEpisode BOOLEAN, " +
                                "Season INT, " +
                                "Episode INT, " +
                                "ResolutionBase TEXT, " +
                                "ResolutionDetail TEXT, " +
                                "Codec TEXT, " +
                                "DolbyVisionProfile TEXT, " +
                                "ServerLocation TEXT" +
                                ")");

                connection.Execute("create table if not exists UserInfo (" +
                    "UserId TEXT NOT NULL, " +
                    "UserName TEXT NOT NULL, " +
                    "ConnectUserId TEXT, " +
                    "TotalTimeWatched INT, " +
                    "TotalWatchableTime INT, " +
                    "TotalMovies INT, " +
                    "TotalCollections INT, " +
                    "TotalMoviesWatched INT, " +
                    "FavoriteMovieYears TEXT, " +
                    "FavoriteMovieGenres TEXT, " +
                    "TotalMovieTimeWatched INT, " +
                    "TotalMovieWatchableTime INT, " +
                    "LastSeenMovies TEXT, " +
                    "TotalTVSeries INT, " +
                    "TotalEpisodes INT, " +
                    "TotalEpisodesWatched INT, " +
                    "TotalSeriesFinished INT, " +
                    "FavoriteShowGenres TEXT, " +
                    "TotalTVTimeWatched INT, " +
                    "TotalTVWatchableTime INT, " +
                    "LastSeenShows TEXT " +
                    ")"
                );
                connection.Execute("create index if not exists idx_UserInfo_UserId on UserInfo (UserId)");

                connection.Execute("create table if not exists ShowProgress (" +
                    "ShowID TEXT NOT NULL, " +
                    "UserId TEXT NOT NULL, " +
                    "Name TEXT NOT NULL, " +
                    "SortName TEXT, " +
                    "StartYear INT, " +
                    "Watched INT, " +
                    "Score REAL, " +
                    "Status TEXT, " +
                    "TotalEpisodes INT, " +
                    "CollectedEpisodes INT, " +
                    "SeenEpisodes INT, " +
                    "TotalSpecials INT, " +
                    "CollectedSpecials INT, " +
                    "SeenSpecials INT, " +
                    "PercentSeen INT, " +
                    "PercentCollected INT " +
                    ")"
                );
            }
        }

        public void UpdateLastUpdated(DateTime lastUpdate, DateTime buildDate, string version)
        {
            string sql = "delete from StatusInfo";
            lock (connection)
            {
                connection.Execute(sql);
            }
            sql = "insert into StatusInfo (LastUpdated, BuildDate, Version) values (@LastUpdated, @BuildDate,@Version)";
            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    TryBind(statement, "@LastUpdated", ToDateTimeParamValue(lastUpdate));
                    TryBind(statement, "@BuildDate", ToDateTimeParamValue(buildDate));
                    TryBind(statement, "@Version", version);
                    statement.MoveNext();
                }
            }
        }

        public void ClearUserInfo()
        {
            string sql = "delete from UserInfo";
            lock (connection)
            {
                connection.Execute(sql);
            }

            sql = "delete from ShowProgress";
            lock (connection)
            {
                connection.Execute(sql);
            }
        }

        public void CalculateUserInfo(IUserManager userManager, IProgress<double> progress)
        {
            progress.Report(0);
            var users = userManager.GetUserList(new UserQuery() { EnableRemoteAccess = true }).ToList();
            progress.Report(100);

            _logger.Debug($"CalculateUserInfo - Starting User Analysis");
            var count = users.Count;
            var curr = 0;

            progress.Report(0);
            foreach (var user in users)
            {
                progress.Report((++curr) / count);
                try
                {
                    var userInfo = new UserInfo(user);

                    AddUserInfo(userInfo);
                    _logger.Debug($"CalculateUserInfo -     Processed User - {userInfo.UserName}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"CalculateUserInfo {user.SortName}: {ex.Message}");
                }
            }
            _logger.Debug($"CalculateUserInfo - Finished User Analysis");
        }

        public void AddUserInfo(UserInfo userInfo)
        {
            string sql =
                "insert into UserInfo " +
                "(" +
                    "  UserId" +
                    ", UserName" +
                    ", ConnectUserId" +
                ")" +
                " values " +
                "(" +
                "  @UserId" +
                ", @UserName" +
                ", @ConnectUserId" +
                ")";
            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    TryBind(statement, "@UserId", userInfo.UserId);
                    TryBind(statement, "@UserName", userInfo.UserName);
                    TryBind(statement, "@ConnectUserId", userInfo.ConnectUserId);
                    statement.MoveNext();
                }
            }
        }

        public void ClearMediaInfo()
        {
            string sql = "delete from MediaInfo";
            lock (connection)
            {
                connection.Execute(sql);
            }
        }

        private IEnumerable<T> GetLibraryItems<T>(ILibraryManager libMananger)
        {
            var query = new InternalItemsQuery(null)
            {
                IncludeItemTypes = new[] { typeof(T).Name },
                Recursive = true,
                IsVirtualItem = false,
                DtoOptions = new DtoOptions(true)
                {
                    EnableImages = false
                }
            };

            return libMananger.GetItemList(query).OfType<T>();
        }

        public void CalculateMediaInfo(ILibraryManager libMananger, IProgress<double> progress)
        {
            progress.Report(0);
            var videoList = GetLibraryItems<Episode>(libMananger).Cast<Video>().ToList();
            progress.Report(50);
            videoList.AddRange(GetLibraryItems<Movie>(libMananger).Cast<Video>().ToList());
            progress.Report(100);

            _logger.Debug($"CalculateMediaInfo - Starting Video Analysis");
            var count = videoList.Count;
            var curr = 0;

            progress.Report(0);
            foreach (var video in videoList)
            {
                progress.Report((++curr) / count);
                try
                {
                    var mediaInfo = new MediaInfo(video);

                    AddMediaInfo(mediaInfo);
                    _logger.Debug($"CalculateMediaInfo -     Processed Video - {mediaInfo.DescriptiveName} items processed");
                }
                catch (Exception ex)
                {
                    _logger.Error($"CalculateMediaInfo {video.SortName}: {ex.Message}");
                }
            }
            _logger.Debug($"CalculateMediaInfo - Finished Video Analysis");
        }


        public void AddMediaInfo(MediaInfo mediaInfo)
        {
            string sql =
                "insert into MediaInfo " +
                "(" +
                    "  ItemId" +
                    ", PrimaryName" +
                    ", SortName" +
                    ", SecondaryName" +
                    ", StartYear" +
                    ", IsEpisode" +
                    ", Season" +
                    ", Episode" +
                    ", ResolutionBase" +
                    ", ResolutionDetail" +
                    ", Codec" +
                    ", DolbyVisionProfile" +
                    ", ServerLocation" +
                ")" +
                " values " +
                "(" +
                "  @ItemId" +
                ", @PrimaryName" +
                ", @SortName" +
                ", @SecondaryName" +
                ", @StartYear" +
                ", @IsEpisode" +
                ", @Season" +
                ", @Episode" +
                ", @ResolutionBase" +
                ", @ResolutionDetail" +
                ", @Codec" +
                ", @DolbyVisionProfile" +
                ", @ServerLocation" +
                ")";
            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    TryBind(statement, "@ItemId", mediaInfo.ItemId);
                    TryBind(statement, "@PrimaryName", mediaInfo.PrimaryName);
                    TryBind(statement, "@SortName", mediaInfo.SortName);
                    TryBind(statement, "@SecondaryName", mediaInfo.SecondaryName);
                    TryBind(statement, "@StartYear", mediaInfo.StartYear);
                    TryBind(statement, "@IsEpisode", mediaInfo.IsEpisode);
                    TryBind(statement, "@Season", mediaInfo.Season);
                    TryBind(statement, "@Episode", mediaInfo.Episode);
                    TryBind(statement, "@ResolutionBase", mediaInfo.ResolutionBase);
                    TryBind(statement, "@ResolutionDetail", mediaInfo.ResolutionDetail);
                    TryBind(statement, "@Codec", mediaInfo.Codec);
                    TryBind(statement, "@DolbyVisionProfile", mediaInfo.DolbyVisionProfile);
                    TryBind(statement, "@ServerLocation", mediaInfo.ServerLocation);
                    statement.MoveNext();
                }
            }
        }
        public ValueGroup CalculateMediaResolutions(bool showAllResolutions)
        {
            string sql =
                "SELECT " +
                "ResolutionBase as Resolution, " +
                "sum(IsEpisode) AS Episodes, " +
                "sum(NOT IsEpisode) AS Movies " +
                "FROM MediaInfo " +
                "GROUP BY Resolution " +
                "ORDER BY Resolution ASC"
                ;

            var retVal = new ValueGroup(Constants.MediaResolutions, Constants.HelpMediaResolutions);

            if (showAllResolutions)
            {
                retVal.addRow(Constants.HD, 0, 0);
                retVal.addRow(Constants._4k, 0, 0);
                retVal.addRow(Constants._8k, 0, 0);
                retVal.addRow(Constants._720p, 0, 0);
                retVal.addRow(Constants.SD, 0, 0);
            }

            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        var resolution = row.GetString(0);
                        var episodeCount = row.GetInt(1);
                        var movieCount = row.GetInt(2);
                        retVal.addRow(resolution, episodeCount, movieCount);
                    }
                }
            }

            return retVal;
        }

        public ValueGroup CalculateMediaCodecs(bool showAllCodecs)
        {
            string sql =
                "SELECT " +
                "Codec as Codec, " +
                "sum(IsEpisode) AS Episodes, " +
                "sum(NOT IsEpisode) AS Movies " +
                "FROM MediaInfo " +
                "GROUP BY Codec " +
                "ORDER BY Codec ASC"
                ;

            var retVal = new ValueGroup(Constants.MediaCodecs, Constants.HelpMediaCodecs);
            if (showAllCodecs)
            {
                retVal.addRow("av1", 0, 0);
                retVal.addRow("h264", 0, 0);
                retVal.addRow("hevc", 0, 0);
                retVal.addRow("mpeg2video", 0, 0);
                retVal.addRow("mpeg4", 0, 0);
                retVal.addRow("msmpeg4v3", 0, 0);
                retVal.addRow("prores", 0, 0);
                retVal.addRow("vc1", 0, 0);
            }

            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        var codec = row.GetString(0);
                        var episodeCount = row.GetInt(1);
                        var movieCount = row.GetInt(2);
                        retVal.addRow(codec, episodeCount, movieCount);
                    }
                }
            }

            return retVal;
        }

        public ValueGroup CalculateDVProfileInfo(bool showUnknownDVProfiles, bool showAllDVProfiles)
        {
            string sql =
                "SELECT " +
                "DolbyVisionProfile as DVProfile, " +
                "sum(IsEpisode) AS Episodes, " +
                "sum(NOT IsEpisode) AS Movies " +
                "FROM MediaInfo ";

            if (!showUnknownDVProfiles)
                sql += $"WHERE DolbyVisionProfile NOT IN ({string.Join(",", Constants.UnknownDolbyProfiles.Select(p => $"'{p}'"))}) ";

            sql += "GROUP BY DolbyVisionProfile " +
                   "ORDER BY DolbyVisionProfile ASC"
                   ;

            var retVal = new ValueGroup(Constants.DolbyVisionProfiles, Constants.HelpDolbyVisionProfile);
            if (showUnknownDVProfiles)
                retVal.addRow("Unknown Dolby Profile", 0, 0);
            if (showAllDVProfiles)
            {
                retVal.addRow("Profile 5.0", 0, 0);
                retVal.addRow("Profile 7.0", 0, 0);
                retVal.addRow("Profile 8.0", 0, 0);
                retVal.addRow("Profile 8.1", 0, 0);
                retVal.addRow("Profile 8.2", 0, 0);
                retVal.addRow("Profile 8.4", 0, 0);
                retVal.addRow("Profile 9.0", 0, 0);
                retVal.addRow("Profile 20.0", 0, 0);
            }
            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        var dvProfile = row.GetString(0);
                        var episodeCount = row.GetInt(1);
                        var movieCount = row.GetInt(2);
                        retVal.addRow(dvProfile, episodeCount, movieCount);
                    }
                }
            }

            return retVal;
        }

        public ValueGroup CalculateUserCount(bool hasConnectUserID, IUserManager userManager)
        {
            var users = userManager.GetUserList(new UserQuery() { HasConnectUserId = true }).ToList();
            if (!hasConnectUserID)
            {
                users = users
                    .Union(userManager.GetUserList(new UserQuery() { HasConnectUserId = false }))
                    .Union(userManager.GetUserList(new UserQuery() { HasConnectUserId = null })).ToList();
            }

            var groupData = new ValueGroup(Constants.TotalUsers, null, "small");
            groupData.ValueLineTwo = users.Count.ToString();

            return groupData;
        }

        public ValueGroup CalculateMostActiveUsers(bool hasConnectUserID, IUserManager userManager)
        {
            var groupData = new ValueGroup(Constants.MostActiveUsers, Constants.HelpMostActiveUsers);

            return groupData;
        }

    }
}
