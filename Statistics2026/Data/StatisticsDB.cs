using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Users;
using SQLitePCL.pretty;
using Statistics2026;
using Statistics2026.Api;
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
            _logger.Info("StatisticsData : Finished Creating");
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
                _logger.Error($"Error Binding {name} to {value}");
            }
        }

        private void TryBind(IStatement statement, string name, long value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                _logger.Error($"Error Binding {name} to {value}");
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
                _logger.Error($"Error Binding {name} to {value}");
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
                _logger.Error($"Error Binding {name} to {value}");
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
            _logger.Info("CreateConnection : " + db_file);
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

            _logger.Info("ConnectionCreated : " + db.GetHashCode());
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
                                //"CollectionName TEXT, " +
                                "StudioNames TEXT, " +
                                "ServerLocation TEXT" +
                                ")");

                connection.Execute("create table if not exists UserInfo (" +
                    "UserId TEXT NOT NULL, " +
                    "UserName TEXT NOT NULL, " +
                    "ConnectUserId TEXT, " +
                    "IsAdministrator BOOLEAN, " +
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

        public void CalculateUserInfo(IUserManager userManager, IUserDataManager userDataManager, ILibraryManager libraryManager, IProgress<double> progress)
        {
            progress.Report(0);
            var users = userManager.GetUserList(new UserQuery() { EnableRemoteAccess = true }).ToList();
            progress.Report(100);

            _logger.Info($"CalculateUserInfo - Starting User Analysis");
            double count = users.Count;
            double curr = 0;

            progress.Report(0);
            foreach (var user in users)
            {
                progress.Report(100.0 * (++curr) / count);
                try
                {
                    AddUserInfo(user, userDataManager, libraryManager);
                    _logger.Info($"CalculateUserInfo -     Processed User ({curr} of {count}) - {user.Name}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"CalculateUserInfo {user.SortName}: {ex.Message}");
                    throw ex;
                }
            }
            _logger.Info($"CalculateUserInfo - Finished User Analysis");
        }

        long CaclulateOverallTime(User user, bool onlyPlayed, IUserDataManager userDataManager, ILibraryManager libraryManager, List<User> allUsers = null)
        {
            if (user == null && allUsers == null)
                throw new ArgumentException("Either user or allUsers must be provided.");

            var allVideos = Statistics2026API.GetAllEpisodesAndMovies(user, libraryManager);
            var totalTicks = (user == null
                    ? allVideos.Where(m => allUsers.Any(u => !onlyPlayed || userDataManager.GetUserData(u, m).Played))
                    : allVideos.Where(m => (!onlyPlayed || userDataManager.GetUserData(user, m).Played) && m.IsVisible(user)))
                .Sum(item => item.RunTimeTicks ?? 0);

            return totalTicks;
        }


        public void AddUserInfo(User user, IUserDataManager userDataManager, ILibraryManager libraryManager)
        {
            if (user.Id == null)
            {
                _logger.Error($"AddUserInfo {user.Name}: is missing Id");
                return;
            }

            if (user.Name == null)
            {
                _logger.Error($"AddUserInfo {user.Id.ToString()}: is missing Name");
                return;
            }

            var totalWatchableTime = CaclulateOverallTime(user, false, userDataManager, libraryManager);
            var totalTimeWatched = CaclulateOverallTime(user, true, userDataManager, libraryManager);
            var isAdmin = user.Policy.IsAdministrator;
            string sql =
                "insert into UserInfo " +
                "(" +
                    "  UserId" +
                    ", UserName" +
                    ", ConnectUserId" +
                    ", IsAdministrator" +
                    ", TotalTimeWatched" +
                    ", TotalWatchableTime" +
                ")" +
                " values " +
                "(" +
                "  @UserId" +
                ", @UserName" +
                ", @ConnectUserId" +
                ", @IsAdministrator" +
                ", @TotalTimeWatched" +
                ", @TotalWatchableTime" +
                ")";
            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    TryBind(statement, "@UserId", user.Id.ToString());
                    TryBind(statement, "@UserName", user.Name);
                    TryBind(statement, "@ConnectUserId", user.ConnectUserId);
                    TryBind(statement, "@IsAdministrator", isAdmin);

                    TryBind(statement, "@TotalTimeWatched", totalTimeWatched);
                    TryBind(statement, "@TotalWatchableTime", totalWatchableTime);
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

            _logger.Info($"CalculateMediaInfo - Starting Video Analysis");
            double count = videoList.Count;
            double curr = 0.0;

            progress.Report(0);
            foreach (var video in videoList)
            {
                progress.Report(100.0 * (++curr) / count);
                try
                {
                    var mediaInfo = new MediaInfo(video);

                    AddMediaInfo(mediaInfo);
                    _logger.Info($"CalculateMediaInfo -     Processed Video ({curr} of {count}) - {mediaInfo.DescriptiveName} items processed");
                }
                catch (Exception ex)
                {
                    _logger.Error($"CalculateMediaInfo {video.SortName}:");
                    var path = video.Path ?? "Unknown";
                    _logger.Error($"CalculateMediaInfo {path}:");
                    _logger.Error($"CalculateMediaInfo {ex.Message}");
                    throw ex;
                }
            }
            _logger.Info($"CalculateMediaInfo - Finished Video Analysis");
        }


        public void AddMediaInfo(MediaInfo mediaInfo)
        {
            if (mediaInfo.ItemId == null)
            {
                _logger.Error($"AddMediaInfo {mediaInfo.SortName}: is missing ItemId");
                return;
            }

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
                    //", CollectionName " +
                    ", StudioNames " +
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
                //", @CollectionName " +
                ", @StudioNames " +
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
                    //TryBind(statement, "@CollectionName", mediaInfo.BoxSet);
                    TryBind(statement, "@StudioNames", string.Join( ";", mediaInfo.StudioNames ));
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

            var retVal = new ValueGroup(Constants.MediaResolutions, Constants.HelpMediaResolutions, new List<string> { "Movies", "Episodes" });

            if (showAllResolutions)
            {
                retVal.addRow(Constants.HD, new List<int> { 0, 0 });
                retVal.addRow(Constants._4k, new List<int> { 0, 0 });
                retVal.addRow(Constants._8k, new List<int> { 0, 0 });
                retVal.addRow(Constants._720p, new List<int> { 0, 0 });
                retVal.addRow(Constants.SD, new List<int> { 0, 0 });
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
                        retVal.addRow(resolution, new List<int> { movieCount, episodeCount });
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

            var retVal = new ValueGroup(Constants.MediaCodecs, Constants.HelpMediaCodecs, new List<string> { "Movies", "Episodes" });
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
                        retVal.addRow(codec, new List<int> { movieCount, episodeCount });
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

            var retVal = new ValueGroup(Constants.DolbyVisionProfiles, Constants.HelpDolbyVisionProfile, new List<string> { "Movies", "Episodes" });
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
                        retVal.addRow(dvProfile, new List<int> { movieCount, episodeCount });
                    }
                }
            }

            return retVal;
        }

        public ValueGroup CalculateUserCount(bool hasConnectUserID, bool excludeAdmin, IUserManager userManager)
        {
            // TODO use the UserInfo table

            var users = userManager.GetUserList(new UserQuery() { HasConnectUserId = true, IsAdministrator = false }).ToList();
            if (!hasConnectUserID)
            {
                users = users
                    .Union(userManager.GetUserList(new UserQuery() { HasConnectUserId = false }))
                    .Union(userManager.GetUserList(new UserQuery() { HasConnectUserId = null })).ToList();
            }
            if (!excludeAdmin)
            {
                users = users
                    .Union(userManager.GetUserList(new UserQuery() { IsAdministrator = true }))
                    .Union(userManager.GetUserList(new UserQuery() { IsAdministrator = null })).ToList();
            }

            var groupData = new ValueGroup(Constants.TotalUsers, null, null, "small");
            groupData.ValueLineTwo = users.Count.ToString();

            return groupData;
        }

        public ValueGroup CalculateMostActiveUsers(bool hasConnectUserID, int numUsers, bool excludeAdmin, IUserManager userManager)
        {
            string sql =
                "SELECT " +
                "UserName, " +
                "TotalTimeWatched " +
                "FROM UserInfo ";
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
            var groupData = new ValueGroup(Constants.MostActiveUsers, help, new List<string> { "Days", "Hours", "Minutes" });
            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
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

        public ValueGroup CalculateTotalMovieCount(User user)
        {
            string sql = "SELECT SUM(NOT IsEpisode) FROM MEDIAINFO";

            var retVal = new ValueGroup(Constants.TotalMovies, Constants.HelpTotalMovies, null, "small");
            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        var count = row.GetInt(0);
                        retVal.ValueLineTwo = count.ToString();
                        break;
                    }
                }
            }

            return retVal;
        }

        public ValueGroup CalculateTotalCollectionCount(User user)
        {
            string sql = "SELECT COUNT( DISTINCT CollectionName ) FROM MEDIAINFO WHERE CollectionName IS NOT NULL AND CollectionName<>\"\"";

            var retVal = new ValueGroup(Constants.TotalCollections, Constants.HelpTotalCollections, null, "small");
            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        var count = row.GetInt(0);
                        retVal.ValueLineTwo = count.ToString();
                        break;
                    }
                }
            }

            return retVal;
        }

        public ValueGroup CalculateTotalMovieStudioCount(User user)
        {
            string sql = "SELECT DISTINCT StudioNames FROM MEDIAINFO WHERE NOT IsEpisode AND StudioNames IS NOT NULL AND StudioNames<>\"\"";

            var retVal = new ValueGroup(Constants.TotalStudios, Constants.HelpTotalStudios, null, "small");
            // Create an unordered set of strings
            HashSet<string> studios = new HashSet<string>();

            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        var currStudios = row.GetString(0).Split(';');
                        studios.UnionWith(currStudios);
                    }
                }
            }
            retVal.ValueLineTwo = studios.Count().ToString();

            return retVal;
        }
    }
}
