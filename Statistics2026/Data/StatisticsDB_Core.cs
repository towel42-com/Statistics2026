using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using SQLitePCL.pretty;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;


namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        private static StatisticsDB instance = null;
        private static readonly object _padlock = new object();

        private ILogger _logger = null;
        private IDatabaseConnection _connection = null;
        DBHelpers _dbHelper = null;


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
            _dbHelper = new DBHelpers();
        }

        private StatisticsDB(string db_path, ILogger l)
        {
            _logger = l;
            _logger.Info("StatisticsData : Creating");
            _dbHelper = new DBHelpers(db_path, _logger);
            _connection = _dbHelper.SQLConnection;
            _logger.Info("StatisticsData : Finished Creating");

        }

        ~StatisticsDB()
        {
            _logger.Info("StatisticsData : Cleaning up");
            if (_connection != null)
            {
                _connection.Close();
                _logger.Info("StatisticsData : DB Connection Closed");
            }
        }

        public void Initialize()
        {
            CreateTables();
        }


        private void CreateTables()
        {
            lock (_connection)
            {
                bool clearFirst = true;
                new TableDef("LastUpdateTable",
                    new List<TableColDef>()
                    {
                        new TableColDef( "LastUpdated", "DATETIME", true ),
                        new TableColDef( "Version", "TEXT", true ),
                        new TableColDef( "BuildDate", "TEXT", true )
                    },
                    null
                ).Execute(clearFirst, _connection);

                new TableDef("Media",
                    new List<TableColDef>()
                    {
                        new TableColDef( "ItemId", "TEXT", false ),
                        new TableColDef( "PrimaryName", "TEXT", false ),
                        new TableColDef( "SortName", "TEXT", true ),
                        new TableColDef( "SecondaryName", "TEXT", true ),
                        new TableColDef( "StartYear", "INT", true ),
                        new TableColDef( "IsEpisode", "BOOLEAN", true ),
                        new TableColDef( "Season", "INT", true ),
                        new TableColDef( "Episode", "INT", true ),
                        new TableColDef( "ResolutionBase", "TEXT", true ),
                        new TableColDef( "ResolutionDetail", "TEXT", true ),
                        new TableColDef( "Codec", "TEXT", true ),
                        new TableColDef( "DolbyVisionProfile", "TEXT", true ),
                        new TableColDef( "StudioNames", "TEXT", true ),
                        new TableColDef( "ServerLocation", "TEXT", true ),
                        new TableColDef( "FileSize", "INT", true),
                        new TableColDef( "ImageUrl", "TEXT", true )
                    },
                    null,
                    true
                ).Execute(clearFirst, _connection);

                new TableDef("Users",
                    new List<TableColDef>()
                    {
                        new TableColDef( "UserId", "TEXT", false ),
                        new TableColDef( "UserName", "TEXT", false ),
                        new TableColDef( "ConnectUserId", "TEXT", true ),
                        new TableColDef( "IsAdministrator", "BOOLEAN", true ),
                        new TableColDef( "TotalTimeWatched", "INT", true ),
                        new TableColDef( "TotalWatchableTime", "INT", true ),
                        new TableColDef( "TotalMovies", "INT", true ),
                        new TableColDef( "TotalCollections", "INT", true ),
                        new TableColDef( "TotalMoviesWatched", "INT", true ),
                        new TableColDef( "FavoriteMovieYears", "TEXT", true ),
                        new TableColDef( "FavoriteMovieGenres", "TEXT", true ),
                        new TableColDef( "TotalMovieTimeWatched", "INT", true ),
                        new TableColDef( "TotalMovieWatchableTime", "INT", true ),
                        new TableColDef( "LastSeenMovies", "TEXT", true ),
                        new TableColDef( "TotalTVSeries", "INT", true ),
                        new TableColDef( "TotalEpisodes", "INT", true ),
                        new TableColDef( "TotalEpisodesWatched", "INT", true ),
                        new TableColDef( "TotalSeriesFinished", "INT", true ),
                        new TableColDef( "FavoriteShowGenres", "TEXT", true ),
                        new TableColDef( "TotalTVTimeWatched", "INT", true ),
                        new TableColDef( "TotalTVWatchableTime", "INT", true ),
                        new TableColDef( "LastSeenShows", "TEXT", true ),
                    },
                    new List<string>() { "UserId", "UserName", "ConnectUserId" }
                ).Execute(clearFirst, _connection);


                new TableDef("EpisodeProgress",
                    new List<TableColDef>()
                    {
                        new TableColDef( "ItemId", "TEXT", false ),
                        new TableColDef( "UserId", "TEXT", false ),
                        new TableColDef( "Name", "TEXT", false ),
                        new TableColDef( "SortName", "TEXT" , true ),
                        new TableColDef( "StartYear", "INT" , true ),
                        new TableColDef( "Watched", "INT" , true ),
                        new TableColDef( "Score", "REAL" , true ),
                        new TableColDef( "Status", "TEXT" , true ),
                        new TableColDef( "TotalEpisodes", "INT" , true ),
                        new TableColDef( "CollectedEpisodes", "INT" , true ),
                        new TableColDef( "SeenEpisodes", "INT" , true ),
                        new TableColDef( "TotalSpecials", "INT" , true ),
                        new TableColDef( "CollectedSpecials", "INT" , true ),
                        new TableColDef( "SeenSpecials", "INT" , true ),
                        new TableColDef( "PercentSeen", "INT" , true ),
                        new TableColDef( "PercentCollected", "INT" , true ),
                    },
                    new List<string>() { "ItemId", "UserId", "Name", "SortName" }
                ).Execute(clearFirst, _connection);


                new TableDef("Collections",
                    new List<TableColDef>()
                    {
                        new TableColDef( "ItemId", "TEXT", false ),
                        new TableColDef( "Name", "TEXT", false ),
                        new TableColDef( "SortName", "TEXT", false )
                    },
                    null,
                    true
                ).Execute(clearFirst, _connection);

                new TableDef("CollectionMembership",
                    new List<TableColDef>()
                    {
                        new TableColDef( "CollectionId", "TEXT", false ),
                        new TableColDef( "ItemId", "TEXT", false ),
                        new TableColDef( "CollectionName", "TEXT", false ) // for debugging purposes
                   },
                    null,
                    true
                ).Execute(clearFirst, _connection);

            }
        }

        public void UpdateLastUpdated(DateTime lastUpdate, DateTime buildDate, string version)
        {
            string sql = "delete from LastUpdateTable";
            lock (_connection)
            {
                _connection.Execute(sql);

                sql = "INSERT INTO LastUpdateTable (LastUpdated, BuildDate, Version) values (@LastUpdated, @BuildDate,@Version)";
                using (var statement = _connection.PrepareStatement(sql))
                {
                    _dbHelper.TryBind(statement, "@LastUpdated", _dbHelper.ToDateTimeParamValue(lastUpdate));
                    _dbHelper.TryBind(statement, "@BuildDate", _dbHelper.ToDateTimeParamValue(buildDate));
                    _dbHelper.TryBind(statement, "@Version", version);
                    statement.MoveNext();
                }
            }
        }

        public void AnalyzeUsers(IUserManager userManager, IUserDataManager userDataManager, ILibraryManager libraryManager, CancellationToken cancellationToken, IProgress<double> progress)
        {
            progress.Report(0);
            var users = userManager.GetUserList(new UserQuery() { EnableRemoteAccess = true }).ToList();
            progress.Report(100);

            _logger.Info($"AnalyzeUsers - Starting User Analysis");
            double count = users.Count;
            double curr = 0;

            progress.Report(0);
            foreach (var user in users)
            {
                progress.Report(100.0 * (++curr) / count);
                try
                {
                    AddUserInfo(user, userDataManager, libraryManager);
                    _logger.Info($"AnalyzeUsers -     Processed User ({curr} of {count}) - {user.Name}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"AnalyzeUsers {user.SortName}: {ex.Message}");
                    throw ex;
                }
                cancellationToken.ThrowIfCancellationRequested();
            }
            _logger.Info($"AnalyzeUsers - Finished User Analysis");
        }

        (long, long) AnalyzeOverallTime(User user, List<User> userList, IUserDataManager userDataManager, ILibraryManager libraryManager)
        {
            if (user == null && userList == null)
                throw new ArgumentException("Either user or allUsers must be provided.");

            var (allVideosForUser, allVideos) = Statistics2026API.GetAllEpisodesAndMovies(user, libraryManager);


            long watchable = 0;
            long watched = 0;
            if (user == null && userList != null) // use the list of users
            {
                watched = allVideos.Where(video => userList.Any(u => userDataManager.GetUserData(u, video).Played)).Sum(item => item.RunTimeTicks ?? 0);
                watchable = allVideos.Sum(item => item.RunTimeTicks ?? 0);
            }
            else
            {
                watched = allVideosForUser.Where(video => userDataManager.GetUserData(user, video).Played).Sum(item => item.RunTimeTicks ?? 0);
                watchable = allVideosForUser.Sum(item => item.RunTimeTicks ?? 0);
            }

            return (watched, watchable);
        }

        //long AnalyzeOverallTime(User user, bool onlyPlayed, IUserDataManager userDataManager, ILibraryManager libraryManager, List<User> allUsers = null)
        //{
        //    if (user == null && allUsers == null)
        //        throw new ArgumentException("Either user or allUsers must be provided.");

        //    var allVideos = Statistics2026API.GetAllEpisodesAndMovies(user, libraryManager);
        //    var totalTicks = (user == null
        //            ? allVideos.Where(m => allUsers.Any(u => !onlyPlayed || userDataManager.GetUserData(u, m).Played))
        //            : allVideos.Where(m => (!onlyPlayed || userDataManager.GetUserData(user, m).Played) && m.IsVisible(user)))
        //        .Sum(item => item.RunTimeTicks ?? 0);

        //    return totalTicks;
        //}

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

            Stopwatch stopWatch = Stopwatch.StartNew();
            var (timeWatched, totalTime) = AnalyzeOverallTime(user, null, userDataManager, libraryManager);
            stopWatch.Stop();
            _logger.Debug($"It took {stopWatch.ElapsedMilliseconds} ms to calculate totalWatchableTime for user {user.Name}");

            var isAdmin = user.Policy.IsAdministrator;
            string sql =
                "INSERT INTO Users " +
                "(" +
                    "  UserId" +
                    ", UserName" +
                    ", ConnectUserId" +
                    ", IsAdministrator" +
                    ", TotalTimeWatched" +
                    ", TotalWatchableTime" +
                ")" +
                " VALUES " +
                "(" +
                "  @UserId" +
                ", @UserName" +
                ", @ConnectUserId" +
                ", @IsAdministrator" +
                ", @TotalTimeWatched" +
                ", @TotalWatchableTime" +
                ")";
            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement(sql))
                {
                    _dbHelper.TryBind(statement, "@UserId", user.Id.ToString());
                    _dbHelper.TryBind(statement, "@UserName", user.Name);
                    _dbHelper.TryBind(statement, "@ConnectUserId", user.ConnectUserId);
                    _dbHelper.TryBind(statement, "@IsAdministrator", isAdmin);

                    _dbHelper.TryBind(statement, "@TotalTimeWatched", timeWatched);
                    _dbHelper.TryBind(statement, "@TotalWatchableTime", totalTime);
                    statement.MoveNext();
                }
            }
        }

        public void AnalyzeMedia(ILibraryManager libManager, IFileSystem fileSystem, Statistics2026API apiService, CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.Info($"AnalyzeMedia - Starting Video Analysis");

            progress.Report(0);
            var videoList = _dbHelper.GetLibraryItems<Episode>(libManager).Cast<Video>().ToList();
            progress.Report(50);
            videoList.AddRange(_dbHelper.GetLibraryItems<Movie>(libManager).Cast<Video>().ToList());
            progress.Report(100);

            double count = videoList.Count;
            double curr = 0.0;

            progress.Report(0);
            foreach (var video in videoList)
            {
                progress.Report(100.0 * (++curr) / count);
                try
                {
                    var mediaInfo = new MediaInfo(video, fileSystem, apiService);

                    AddMediaInfo(mediaInfo);
                    _logger.Info($"AnalyzeMedia -     Processed Video ({curr} of {count}) - {mediaInfo.DescriptiveName}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"AnalyzeMedia {video.SortName}:");
                    var path = video.Path ?? "Unknown";
                    _logger.Error($"AnalyzeMedia {path}:");
                    _logger.Error($"AnalyzeMedia {ex.Message}");
                    throw ex;
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
            _logger.Info($"AnalyzeMedia - Finished Video Analysis");
        }


        public void AddMediaInfo(MediaInfo mediaInfo)
        {
            if (mediaInfo.ItemId == null)
            {
                _logger.Error($"AddMediaInfo {mediaInfo.SortName}: is missing ItemId");
                return;
            }

            string sql =
                "INSERT INTO Media " +
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
                    ", StudioNames " +
                    ", ServerLocation" +
                    ", FileSize" +
                    ", ImageUrl" +
                ")" +
                " VALUES " +
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
                ", @StudioNames " +
                ", @ServerLocation" +
                ", @FileSize" +
                ", @ImageUrl" +
                ")";
            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement(sql))
                {
                    _dbHelper.TryBind(statement, "@ItemId", mediaInfo.ItemId);
                    _dbHelper.TryBind(statement, "@PrimaryName", mediaInfo.PrimaryName);
                    _dbHelper.TryBind(statement, "@SortName", mediaInfo.SortName);
                    _dbHelper.TryBind(statement, "@SecondaryName", mediaInfo.SecondaryName);
                    _dbHelper.TryBind(statement, "@StartYear", mediaInfo.StartYear);
                    _dbHelper.TryBind(statement, "@IsEpisode", mediaInfo.IsEpisode);
                    _dbHelper.TryBind(statement, "@Season", mediaInfo.Season);
                    _dbHelper.TryBind(statement, "@Episode", mediaInfo.Episode);
                    _dbHelper.TryBind(statement, "@ResolutionBase", mediaInfo.ResolutionBase);
                    _dbHelper.TryBind(statement, "@ResolutionDetail", mediaInfo.ResolutionDetail);
                    _dbHelper.TryBind(statement, "@Codec", mediaInfo.Codec);
                    _dbHelper.TryBind(statement, "@DolbyVisionProfile", mediaInfo.DolbyVisionProfile);
                    _dbHelper.TryBind(statement, "@StudioNames", string.Join(";", mediaInfo.StudioNames));
                    _dbHelper.TryBind(statement, "@ServerLocation", mediaInfo.ServerLocation);
                    _dbHelper.TryBind(statement, "@FileSize", mediaInfo.FileSize);
                    _dbHelper.TryBind(statement, "@ImageUrl", mediaInfo.ImageUrl);
                    statement.MoveNext();
                }
            }
        }


        public void AnalyzeCollections(ILibraryManager libManager, CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.Info($"AnalyzeCollections - Starting Collection Analysis");
            progress.Report(0);
            var collections = _dbHelper.GetLibraryItems<BoxSet>(libManager);
            progress.Report(100);

            double count = collections.Count();
            double curr = 0.0;

            progress.Report(0);
            foreach (var collection in collections)
            {
                progress.Report(100.0 * (++curr) / count);
                try
                {
                    AddCollection(collection, libManager, cancellationToken, progress);
                    _logger.Info($"AnalyzeCollections -     Processed Collection ({curr} of {count}) - {collection.Name} items processed");
                }
                catch (Exception ex)
                {
                    _logger.Error($"AnalyzeCollections {collection.SortName}:");
                    var path = collection.Path ?? "Unknown";
                    _logger.Error($"AnalyzeCollections {path}:");
                    _logger.Error($"AnalyzeCollections {ex.Message}");
                    throw ex;
                }
                cancellationToken.ThrowIfCancellationRequested();
            }
            _logger.Info($"AnalyzeCollections - Finished Collection Analysis");
        }

        private void AddChildToCollection(Video video, BoxSet collection, ILibraryManager libManager)
        {
            if (video == null || collection == null || libManager == null)
            {
                _logger.Error($"AddChildToCollection video, collection and libManager must be set");
                return;
            }

            string sql =
                "INSERT INTO CollectionMembership " +
                "(" +
                    "  CollectionId" +
                    ", ItemId" +
                    ", CollectionName" +
                ")" +
                " VALUES " +
                "(" +
                "  @CollectionId" +
                ", @ItemId" +
                ", @CollectionName" +
                ")";
            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement(sql))
                {
                    _dbHelper.TryBind(statement, "@CollectionId", collection.Id.ToString());
                    _dbHelper.TryBind(statement, "@ItemId", video.Id.ToString());
                    _dbHelper.TryBind(statement, "@CollectionName", collection.Name);
                    statement.MoveNext();
                }
            }
        }

        private int AddCollectionMembers(BoxSet collection, ILibraryManager libManager, CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.Info($"AnalyzeCollections - AddCollectionMembers -     Adding members of Collection - {collection.Name}");

            var query = new InternalItemsQuery
            {
                CollectionIds = new[] { collection.InternalId },
                Recursive = true
            };

            var baseItems = libManager.GetItemList(query);
            var videos = baseItems.OfType<Video>().ToList();

            double count = videos.Count;
            double curr = 0.0;

            videos.ForEach(video =>
            {
                progress.Report(100.0 * (++curr) / count);
                AddChildToCollection(video, collection, libManager);
                cancellationToken.ThrowIfCancellationRequested();
            });
            _logger.Info($"AnalyzeCollections - AddCollectionMembers -     Finished Adding {videos.Count} members of Collection {collection.Name} ");
            return videos.Count;
        }

        public void AddCollection(BoxSet collection, ILibraryManager libManager, CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (collection.Id == null)
            {
                _logger.Error($"AddMediaInfo {collection.SortName}: is missing ItemId");
                return;
            }

            _logger.Info($"AnalyzeCollections - AddCollection - Adding Collection {collection.Name}");

            string sql =
                "INSERT INTO Collections " +
                "(" +
                    "  ItemId" +
                    ", Name" +
                    ", SortName" +
                ")" +
                " VALUES " +
                "(" +
                "  @ItemId" +
                ", @Name" +
                ", @SortName" +
                ")";
            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement(sql))
                {
                    _dbHelper.TryBind(statement, "@ItemId", collection.Id.ToString());
                    _dbHelper.TryBind(statement, "@Name", collection.Name);
                    _dbHelper.TryBind(statement, "@SortName", collection.SortName);
                    statement.MoveNext();
                }
            }
            _logger.Info($"AnalyzeCollections -     AddCollection - Successfully Added Collection");

            AddCollectionMembers(collection, libManager, cancellationToken, progress);
        }
    }
}
