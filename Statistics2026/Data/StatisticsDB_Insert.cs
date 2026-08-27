using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using SQLitePCL.pretty;
using Statistics2026.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using static Statistics2026.Data.DBHelper;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public void UpdateLastUpdated(DateTime lastUpdate, DateTime buildDate, string version)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            var sqlCmds = new List<SQLCmdDef>();
            sqlCmds.Add(new SQLCmdDef("delete from LastUpdateTable"));
            sqlCmds.Add(new SQLCmdDef("INSERT INTO LastUpdateTable (LastUpdated, BuildDate, Version) values (@LastUpdated, @BuildDate,@Version)",
                        new List<(string name, object? value)>()
                        {
                            ("@LastUpdated", _dbHelper.ToDateTimeParamValue(lastUpdate)),
                            ("@BuildDate", _dbHelper.ToDateTimeParamValue(buildDate)),
                            ("@Version", version)
                        }));

            _dbHelper.ExecuteCommands(sqlCmds);
        }

        public void AddAllUsers(IUserManager userManager, IUserDataManager userDataManager, ILibraryManager libraryManager, CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            progress.Report(0);
            var users = userManager.GetUserList(new UserQuery() { EnableRemoteAccess = true }).ToList();
            progress.Report(100);

            _dbHelper.Logger?.Debug($"AddAllUsers - Starting User Analysis");
            double count = users.Count;
            double curr = 0;

            progress.Report(0);
            var sqlCmds = new List<SQLCmdDef>();
            using (var timer = new AutoTimer($"    Adding All Users - Getting Commands", _dbHelper.Logger))
            {
                foreach (var user in users)
                {
                    progress.Report(100.0 * (++curr) / count);
                    using (var userTimer = new AutoTimer($"AddUsers -     Processed User ({curr} of {count}) - {user.Name}", _dbHelper.Logger))
                    {
                        sqlCmds.AddRange(AddUser(user, userDataManager, libraryManager));
                        cancellationToken.ThrowIfCancellationRequested();
                        sqlCmds.AddRange(AddUserWatchData(user, userDataManager, libraryManager, progress));
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();
            }

            using (var timer = new AutoTimer($"    Adding All Users - Executing Commands", _dbHelper.Logger))
            {
                progress.Report(80);
                _dbHelper.ExecuteCommands(sqlCmds);
                progress.Report(100);
            }

            _dbHelper.Logger?.Debug($"AddAllUsers - Finished User Analysis");
        }

        (long, long) AnalyzeOverallTime(User? user, List<User>? userList, IUserDataManager userDataManager, ILibraryManager libraryManager)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            if (user == null && userList == null)
                throw new ArgumentException("Either user or allUsers must be provided.");

            var (allVideosForUser, allVideos) = Statistics2026API.GetAllEpisodesAndMovies(user, libraryManager, true);

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
        private List<SQLCmdDef> AddUserWatchData(User? user, IUserDataManager userDataManager, ILibraryManager libManager, IProgress<double> progress)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            if (user == null)
                throw new ArgumentNullException("user");

            var allVideosForUser = Statistics2026API.GetAllEpisodesAndMovies(user, libManager, false).forUser;

            string sql =
                "INSERT INTO UserVideoList " +
                "(" +
                    "  UserId" +
                    ", ItemId" +
                    ", IsPlayed" +
                    ", IsEpisode" +
                    ", SeriesId" +
                ")" +
                " VALUES " +
                "(" +
                    "  @UserId" +
                    ", @ItemId" +
                    ", @IsPlayed" +
                    ", @IsEpisode" +
                    ", @SeriesId" +
                ")";

            var sqlCmds = new List<SQLCmdDef>();
            foreach (var video in allVideosForUser)
            {
                bool isEpisode = video is Episode;
                string seriesId = "";
                if (isEpisode)
                {
                    var episode = video as Episode;
                    var series = (episode != null) ? episode.Series : null;
                    if (series != null)
                    {
                        seriesId = series.Id.ToString();
                    }
                }

                sqlCmds.Add(new SQLCmdDef(sql, new List<(string name, object? value)>()
                {
                    ( "@UserId", user.Id.ToString()),
                    ( "@ItemId", video.Id.ToString()),
                    ( "@IsEpisode", isEpisode),
                    ( "@IsPlayed", video.IsPlayed(user)),
                    ( "@SeriesId", seriesId)
                }));
            }
            return sqlCmds;
        }

        private List<SQLCmdDef> AddUser(User? user, IUserDataManager userDataManager, ILibraryManager libraryManager)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            if (user == null)
                throw new ArgumentNullException("user");

            var sqlCmds = new List<SQLCmdDef>();
            if (user.Id == null)
            {
                _dbHelper.Logger?.Error($"AddUser {user.Name}: is missing Id");
                return sqlCmds;
            }

            if (user.Name == null)
            {
                _dbHelper.Logger?.Error($"AddUser {user.Id.ToString()}: is missing Name");
                return sqlCmds;
            }

            var (timeWatched, totalTime) = AnalyzeOverallTime(user, null, userDataManager, libraryManager);

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
            sqlCmds.Add(new SQLCmdDef(sql, new List<(string name, object? value)>()
            {
                ( "@UserId", user.Id.ToString()),
                ( "@UserName", user.Name),
                ( "@ConnectUserId", user.ConnectUserId),
                ( "@IsAdministrator", isAdmin),
                ( "@TotalTimeWatched", timeWatched),
                ( "@TotalWatchableTime", totalTime),
            }));
            return sqlCmds;
        }

        public void AddAllMedia(ILibraryManager libManager, IFileSystem fileSystem, CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            _dbHelper.Logger?.Debug($"AddAllMedia - Starting Video Analysis");

            progress.Report(0);
            var videoList = _dbHelper.GetLibraryItems<Episode>(libManager).Cast<Video>().ToList();
            progress.Report(50);
            videoList.AddRange(_dbHelper.GetLibraryItems<Movie>(libManager).Cast<Video>().ToList());
            progress.Report(100);

            double count = videoList.Count;
            double curr = 0.0;

            progress.Report(0);
            var sqlCmds = new List<SQLCmdDef>();
            foreach (var video in videoList)
            {
                progress.Report(100.0 * (++curr) / count);
                using (var mediaInfo = new MediaInfo(video, fileSystem))
                {

                    sqlCmds.AddRange(AddMediaInfo(mediaInfo));
                    _dbHelper.Logger?.Debug($"AddAllMedia -     Processed Video ({curr} of {count}) - {mediaInfo.DescriptiveName}");
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
            progress.Report(80);
            _dbHelper.ExecuteCommands(sqlCmds);
            progress.Report(100);
            _dbHelper.Logger?.Debug($"AddAllMedia - Finished Video Analysis");
        }

        public List<SQLCmdDef> AddMediaInfo(MediaInfo mediaInfo)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            var sqlCmds = new List<SQLCmdDef>();
            if (mediaInfo == null || mediaInfo.ItemId == null)
            {
                _dbHelper.Logger?.Error($"AddMediaInfo '{mediaInfo?.SortName}': is missing ItemId");
                return sqlCmds;
            }

            //if (mediaInfo.IsEpisode && mediaInfo.Season == 0)
            //{
            //    // dont count specials
            //    return;
            //}


            string sql =
                "INSERT INTO Media " +
                "(" +
                    "  ItemId" +
                    ", PrimaryName" +
                    ", SortName" +
                    ", SecondaryName" +
                    ", StartYear" +
                    ", IsEpisode" +
                    ", SeriesId" +
                    ", Season" +
                    ", Episode" +
                    ", ResolutionBase" +
                    ", ResolutionDetail" +
                    ", Codec" +
                    ", DolbyVisionProfile" +
                    ", StudioNames " +
                    ", Genres " +
                    ", ServerLocation" +
                    ", FileSize" +
                    ", ImageUrl" +
                    ", RunTimeTicks" +
                    ", Rating" +
                    ", TotalBitrate" +
                    ", PremiereDate" +
                    ", DateAdded" +
                ")" +
                " VALUES " +
                "(" +
                    "  @ItemId" +
                    ", @PrimaryName" +
                    ", @SortName" +
                    ", @SecondaryName" +
                    ", @StartYear" +
                    ", @IsEpisode" +
                    ", @SeriesId" +
                    ", @Season" +
                    ", @Episode" +
                    ", @ResolutionBase" +
                    ", @ResolutionDetail" +
                    ", @Codec" +
                    ", @DolbyVisionProfile" +
                    ", @StudioNames " +
                    ", @Genres " +
                    ", @ServerLocation" +
                    ", @FileSize" +
                    ", @ImageUrl" +
                    ", @RunTimeTicks" +
                    ", @Rating" +
                    ", @TotalBitrate" +
                    ", @PremiereDate" +
                    ", @DateAdded" +
               ")";

            sqlCmds.Add(new SQLCmdDef(sql, new List<(string name, object? value)>()
            {
                ("@ItemId", mediaInfo.ItemId),
                ("@PrimaryName", mediaInfo.PrimaryName),
                ("@SortName", mediaInfo.SortName),
                ("@SecondaryName", mediaInfo.SecondaryName),
                ("@StartYear", mediaInfo.StartYear),
                ("@IsEpisode", mediaInfo.IsEpisode),
                ("@SeriesId", mediaInfo.SeriesId),
                ("@Season", mediaInfo.Season),
                ("@Episode", mediaInfo.Episode),
                ("@ResolutionBase", mediaInfo.ResolutionBase),
                ("@ResolutionDetail", mediaInfo.ResolutionDetail),
                ("@Codec", mediaInfo.Codec),
                ("@DolbyVisionProfile", mediaInfo.DolbyVisionProfile),
                ("@StudioNames", string.Join(",", mediaInfo.StudioNames)),
                ("@Genres", string.Join(",", mediaInfo.Genres)),
                ("@ServerLocation", mediaInfo.ServerLocation),
                ("@FileSize", mediaInfo.FileSize),
                ("@ImageUrl", (mediaInfo.ImageUrl == null) ? "" : mediaInfo.ImageUrl),
                ("@RunTimeTicks", mediaInfo.RunTimeTicks),
                ("@Rating", mediaInfo.Rating),
                ("@TotalBitrate", mediaInfo.TotalBitrate),
                ("@PremiereDate", mediaInfo.PremiereDate ),
                ("@DateAdded", mediaInfo.DateAdded),
            }));

            return sqlCmds;
        }

        public void AddAllCollections(ILibraryManager libManager, CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            _dbHelper.Logger?.Debug($"AddAllCollections - Starting Collection Analysis");
            progress.Report(0);
            var collections = _dbHelper.GetLibraryItems<BoxSet>(libManager);
            progress.Report(100);

            double count = collections.Count();
            double curr = 0.0;

            progress.Report(0);
            var sqlCmds = new List<SQLCmdDef>();

            foreach (var collection in collections)
            {
                progress.Report(100.0 * (++curr) / count);
                sqlCmds.AddRange(AddCollection(collection, libManager, cancellationToken, progress));
                cancellationToken.ThrowIfCancellationRequested();
                _dbHelper.Logger?.Debug($"AddAllCollections -     Processed Collection ({curr} of {count}) - {collection.Name} items processed");
            }
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(80);
            _dbHelper.ExecuteCommands(sqlCmds);
            progress.Report(100);
            _dbHelper.Logger?.Debug($"AddAllCollections - Finished Collection Analysis");
        }

        private List<SQLCmdDef> AddChildToCollection(Video video, BoxSet collection, ILibraryManager libManager)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            var sqlCmds = new List<SQLCmdDef>();

            if (video == null || collection == null || libManager == null)
            {
                _dbHelper.Logger?.Error($"AddChildToCollection video, collection and libManager must be set");
                return sqlCmds;
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

            sqlCmds.Add(new SQLCmdDef(sql, new List<(string name, object? value)>
            {
                ("@CollectionId", collection.Id.ToString()),
                ("@ItemId", video.Id.ToString()),
                ("@CollectionName", collection.Name),
            }));
            return sqlCmds;
        }

        private List<SQLCmdDef> AddCollectionMembers(BoxSet collection, ILibraryManager libManager, CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            _dbHelper.Logger?.Debug($"AddAllCollections - AddCollectionMembers -     Adding members of Collection - {collection.Name}");

            var query = new InternalItemsQuery
            {
                CollectionIds = new[] { collection.InternalId },
                Recursive = true
            };

            var baseItems = libManager.GetItemList(query);
            var videos = baseItems.OfType<Video>().ToList();

            double count = videos.Count;
            double curr = 0.0;

            var sqlCmds = new List<SQLCmdDef>();

            videos.ForEach(video =>
            {
                progress.Report(100.0 * (++curr) / count);
                sqlCmds.AddRange(AddChildToCollection(video, collection, libManager));
                cancellationToken.ThrowIfCancellationRequested();
            });
            _dbHelper.Logger?.Debug($"AddAllCollections - AddCollectionMembers -     Finished Adding {videos.Count} members of Collection {collection.Name} ");
            return sqlCmds;
        }

        public List<SQLCmdDef> AddCollection(BoxSet collection, ILibraryManager libManager, CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            var sqlCmds = new List<SQLCmdDef>();
            if (collection.Id == null)
            {
                _dbHelper.Logger?.Error($"AddCollection {collection.SortName}: is missing ItemId");
                return sqlCmds;
            }

            _dbHelper.Logger?.Debug($"AddAllCollections - AddCollection - Adding Collection {collection.Name}");

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
            sqlCmds.Add(new SQLCmdDef(sql, new List<(string name, object? value)>()
            {
                ("@ItemId", collection.Id.ToString()),
                ("@Name", collection.Name),
                ("@SortName", collection.SortName),
            }));
            _dbHelper.Logger?.Debug($"AddAllCollections -     AddCollection - Successfully Added Collection");

            sqlCmds.AddRange(AddCollectionMembers(collection, libManager, cancellationToken, progress));
            return sqlCmds;
        }

        public void AddAllSeries(ILibraryManager libManager, IFileSystem fileSystem, CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            _dbHelper.Logger?.Debug($"AddAllSeries- Starting Video Analysis");

            progress.Report(0);
            var seriesList = _dbHelper.GetLibraryItems<Series>(libManager).Cast<Series>().ToList();
            progress.Report(100);

            double count = seriesList.Count;
            double curr = 0.0;

            progress.Report(0);
            var sqlCmds = new List<SQLCmdDef>();

            foreach (Series series in seriesList)
            {
                progress.Report(100.0 * (++curr) / count);
                sqlCmds.AddRange(AddSeries(series, libManager, cancellationToken, progress));
                cancellationToken.ThrowIfCancellationRequested();

                _dbHelper.Logger?.Debug($"AddAllSeries -     Processed Series ({curr} of {count}) - {series.Name}");
            }

            progress.Report(80);
            _dbHelper.ExecuteCommands(sqlCmds);
            progress.Report(100);
            cancellationToken.ThrowIfCancellationRequested();
            _dbHelper.Logger?.Debug($"AddAllSeries - Finished Video Analysis");
        }

        private List<SQLCmdDef> AddSeries(Series series, ILibraryManager libManager, CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            var sqlCmds = new List<SQLCmdDef>();
            if (series.Id == null)
            {
                _dbHelper.Logger?.Error($"AddSeries {series.SortName}: is missing ItemId");
                return sqlCmds;
            }

            _dbHelper.Logger?.Debug($"AddAllSeries - AddSeries - Adding Series {series.Name}");


            long totalFileSize = 0;
            long totalRuntime = 0;
            Double averageRating = 0.0;
            long averageBitrate = 0;

            var cmd = new SQLCmdDef("SELECT SUM(FileSize), SUM(RunTimeTicks), SUM(Rating)/Count(1),Sum(TotalBitrate)/Count(1) FROM Media WHERE SeriesId=@SeriesId",
                        new List<(string name, object? value)>()
                        {
                            ("@SeriesId", series.Id.ToString())
                        });
            _dbHelper.ExecuteCommands(new List<SQLCmdDef>() { cmd },
                statement =>
                {
                    if (statement != null)
                    {
                        var row = statement.Current;
                        totalFileSize = row.GetInt64(0);
                        totalRuntime = row.GetInt64(1);
                        averageRating = row.GetFloat(2);
                        averageBitrate = row.GetInt64(3);
                        return false;
                    }
                    return true;
                });

            string sql =
                "INSERT INTO Series " +
                "(" +
                    "  ItemId" +
                    ", Name" +
                    ", SortName" +
                    ", PremiereDate" +
                    ", DateAdded" +
                    ", ImageUrl" +
                    ", FileSize" +
                    ", RunTimeTicks" +
                    ", Rating" +
                    ", AverageBitrate" +
                ")" +
                " VALUES " +
                "(" +
                    "  @ItemId" +
                    ", @Name" +
                    ", @SortName" +
                    ", @PremiereDate" +
                    ", @DateAdded" +
                    ", @ImageUrl" +
                    ", @FileSize" +
                    ", @RunTimeTicks" +
                    ", @Rating" +
                    ", @AverageBitrate" +
                ")";

            sqlCmds.Add(new SQLCmdDef(sql, new List<(string name, object? value)>()
            {
                ("@ItemId", series.Id.ToString()),
                ("@Name", series.Name),
                ("@SortName", series.SortName),
                ("@PremiereDate", series.PremiereDate.HasValue ? series.PremiereDate.Value.DateTime : null),
                ("@DateAdded", series.DateCreated.DateTime),
                ("@ImageUrl", ItemImageUrl._ItemImageUrl(series)),
                ("@FileSize", totalFileSize),
                ("@RunTimeTicks", totalRuntime),
                ("@Rating", averageRating),
                ("@AverageBitrate", averageBitrate),
            }));
            _dbHelper.Logger?.Debug($"AddAllCollections -     AddCollection - Successfully Added Collection");
            return sqlCmds;
        }

        public void ComputePercentWatchedCache(ILibraryManager libraryManager, CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            _dbHelper.Logger?.Debug($"ComputePercentWatchedCache - Starting Analysis");

            progress.Report(0);
            var watchedShows = ComputeWatchedShowValues(null, libraryManager, cancellationToken, progress);
            progress.Report(100);
            cancellationToken.ThrowIfCancellationRequested();

            var sql = "INSERT INTO CachedWatchedAnalysis" +
                    "(" +
                        "  ItemId" +
                        ", Name" +
                        ", ImageUrl" +
                        ", NumEpisodes" +
                        ", NumWatched" +
                        ", PercentWatched" +
                        ", PercentWatchedPerUser" +
                    ")" +
                    " VALUES " +
                    "(" +
                        "  @ItemId" +
                        ", @Name" +
                        ", @ImageUrl" +
                        ", @NumEpisodes" +
                        ", @NumWatched" +
                        ", @PercentWatched" +
                        ", @PercentWatchedPerUser" +
                    ")";

            var sqlCmds = new List<SQLCmdDef>();

            progress.Report(0);
            int curr = 0;
            foreach(var watched in watchedShows)
            {
                sqlCmds.Add(new SQLCmdDef(sql, new List<(string name, object? value)>()
                {
                    ("@ItemId", watched.ItemId),
                    ("@Name", watched.Name),
                    ("@ImageUrl", watched.ImageUrl),
                    ("@NumEpisodes", watched.NumEpisodes),
                    ("@NumWatched", watched.NumWatched),
                    ("@PercentWatched", watched.PercentWatched),
                    ("@PercentWatchedPerUser", watched.PercentWatchedPerUser)
                }));
                progress.Report(100.0 * (curr++) / watchedShows.Count());
            }
            progress.Report(80);
            _dbHelper.ExecuteCommands(sqlCmds);
            progress.Report(100);
        }

        public void ComputeCachedStats(ILibraryManager libraryManager, CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");

            _dbHelper.Logger?.Debug($"ComputeCachedStats - Starting Analysis");

            progress.Report(0);
            double percentPer = 100.0 / 11.0;
            int curr = 0;

            var longestSeries = StatCardValuesFor(null, StatGen.EStatisticType.Longest, StatGen.EVideoType.Series);
            progress.Report((++curr) * percentPer);
            cancellationToken.ThrowIfCancellationRequested();

            var shortestSeries = StatCardValuesFor(null, StatGen.EStatisticType.Shortest, StatGen.EVideoType.Series);
            progress.Report((++curr) * percentPer);
            cancellationToken.ThrowIfCancellationRequested();

            var largestSeries = StatCardValuesFor(null, StatGen.EStatisticType.Largest, StatGen.EVideoType.Series);
            progress.Report((++curr) * percentPer);
            cancellationToken.ThrowIfCancellationRequested();

            var smallestSeries = StatCardValuesFor(null, StatGen.EStatisticType.Smallest, StatGen.EVideoType.Series);
            progress.Report((++curr) * percentPer);
            cancellationToken.ThrowIfCancellationRequested();

            var longestMovie = StatCardValuesFor(null, StatGen.EStatisticType.Longest, StatGen.EVideoType.Movie);
            progress.Report((++curr) * percentPer);
            cancellationToken.ThrowIfCancellationRequested();

            var shortestMovie = StatCardValuesFor(null, StatGen.EStatisticType.Shortest, StatGen.EVideoType.Movie);
            progress.Report((++curr) * percentPer);
            cancellationToken.ThrowIfCancellationRequested();

            var largestMovie = StatCardValuesFor(null, StatGen.EStatisticType.Largest, StatGen.EVideoType.Movie);
            progress.Report((++curr) * percentPer);
            cancellationToken.ThrowIfCancellationRequested();

            var smallestMovie = StatCardValuesFor(null, StatGen.EStatisticType.Smallest, StatGen.EVideoType.Movie);
            progress.Report((++curr) * percentPer);
            cancellationToken.ThrowIfCancellationRequested();

            var totalTVStudioCount = TotalStudioCountValue(null, false);
            progress.Report((++curr) * percentPer);
            cancellationToken.ThrowIfCancellationRequested();

            var totalMovieStudioCount = TotalStudioCountValue(null, true);
            progress.Report((++curr) * percentPer);
            cancellationToken.ThrowIfCancellationRequested();

            string sql = "INSERT INTO CachedStats " +
                        "(" +
                            "  LongestSeries" +
                            ", ShortestSeries" +
                            ", LargestSeries" +
                            ", SmallestSeries" +
                            ", TotalTVStudioCount" +
                            ", LongestMovie" +
                            ", ShortestMovie" +
                            ", LargestMovie" +
                            ", SmallestMovie" +
                            ", TotalMovieStudioCount" +
                        ")" +
                        " VALUES " +
                        "(" +
                            "  @LongestSeries" +
                            ", @ShortestSeries" +
                            ", @LargestSeries" +
                            ", @SmallestSeries" +
                            ", @TotalTVStudioCount" +
                            ", @LongestMovie" +
                            ", @ShortestMovie" +
                            ", @LargestMovie" +
                            ", @SmallestMovie" +
                            ", @TotalMovieStudioCount" +
                        ")";
            var cmdDef = new SQLCmdDef(sql, new List<(string name, object? value)>()
            {
                ("@LongestSeries", longestSeries.ItemId),
                ("@ShortestSeries", shortestSeries.ItemId),
                ("@LargestSeries", largestSeries.ItemId),
                ("@SmallestSeries", smallestSeries.ItemId),
                ("@TotalTVStudioCount", totalTVStudioCount),
                ("@LongestMovie", longestMovie.ItemId),
                ("@ShortestMovie", shortestMovie.ItemId),
                ("@LargestMovie", largestMovie.ItemId),
                ("@SmallestMovie", smallestMovie.ItemId),
                ("@TotalMovieStudioCount", totalMovieStudioCount),
            });
            _dbHelper.ExecuteCommand(cmdDef);
            progress.Report((++curr) * percentPer);
        }
    }
}
