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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;


namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
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

        public void AddUsers(IUserManager userManager, IUserDataManager userDataManager, ILibraryManager libraryManager, CancellationToken cancellationToken, IProgress<double> progress)
        {
            progress.Report(0);
            var users = userManager.GetUserList(new UserQuery() { EnableRemoteAccess = true }).ToList();
            progress.Report(100);

            _logger.Info($"AddUsers - Starting User Analysis");
            double count = users.Count;
            double curr = 0;

            progress.Report(0);
            _connection.RunInTransaction(connection =>
            {
                foreach (var user in users)
                {
                    progress.Report(100.0 * (++curr) / count);
                    try
                    {
                        AddUser(user, userDataManager, libraryManager);
                        AddUserWatchData(user, userDataManager, libraryManager, progress);
                        _logger.Info($"AddUsers -     Processed User ({curr} of {count}) - {user.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"AddUsers {user.SortName}: {ex.Message}");
                        throw ex;
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }
                _logger.Info($"AddUsers - Finished User Analysis");
            });
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
        private void AddUserWatchData(User user, IUserDataManager userDataManager, ILibraryManager libManager, IProgress<double> progress)
        {
            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(Episode).Name, typeof(Movie).Name },
                Recursive = true,
                IsSpecialSeason = false,
                MaxPremiereDate = DateTime.Now,
                IsVirtualItem = false,
                IsPlayed = true
            };
            var videos = libManager.GetItemList(query).OfType<Video>().ToList();

            string sql =
                "INSERT INTO VideoPlayList " +
                "(" +
                    "  UserId" +
                    ", ItemId" +
                    ", IsEpisode" +
                    ", SeriesId" +
                ")" +
                " VALUES " +
                "(" +
                    "  @UserId" +
                    ", @ItemId" +
                    ", @IsEpisode" +
                    ", @SeriesId" +
                ")";

            foreach (var video in videos)
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

                lock (_connection)
                {
                    using (var statement = _connection.PrepareStatement(sql))
                    {
                        _dbHelper.TryBind(statement, "@UserId", user.Id.ToString());
                        _dbHelper.TryBind(statement, "@ItemId", video.Id.ToString());
                        _dbHelper.TryBind(statement, "@IsEpisode", isEpisode);
                        _dbHelper.TryBind(statement, "@SeriesId", seriesId);
                        statement.MoveNext();
                    }
                }
            }
        }

        public void AddUser(User user, IUserDataManager userDataManager, ILibraryManager libraryManager)
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

        public void AnalyzeMedia(ILibraryManager libManager, IFileSystem fileSystem, CancellationToken cancellationToken, IProgress<double> progress)
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
                    var mediaInfo = new MediaInfo(video, fileSystem);

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
                    ", @ServerLocation" +
                    ", @FileSize" +
                    ", @ImageUrl" +
                    ", @RunTimeTicks" +
                    ", @Rating" +
                    ", @TotalBitrate" +
                    ", @PremiereDate" +
                    ", @DateAdded" +
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
                    _dbHelper.TryBind(statement, "@SeriesId", mediaInfo.SeriesId);
                    _dbHelper.TryBind(statement, "@Season", mediaInfo.Season);
                    _dbHelper.TryBind(statement, "@Episode", mediaInfo.Episode);
                    _dbHelper.TryBind(statement, "@ResolutionBase", mediaInfo.ResolutionBase);
                    _dbHelper.TryBind(statement, "@ResolutionDetail", mediaInfo.ResolutionDetail);
                    _dbHelper.TryBind(statement, "@Codec", mediaInfo.Codec);
                    _dbHelper.TryBind(statement, "@DolbyVisionProfile", mediaInfo.DolbyVisionProfile);
                    _dbHelper.TryBind(statement, "@StudioNames", string.Join(";", mediaInfo.StudioNames));
                    _dbHelper.TryBind(statement, "@ServerLocation", mediaInfo.ServerLocation);
                    _dbHelper.TryBind(statement, "@FileSize", mediaInfo.FileSize);
                    _dbHelper.TryBind(statement, "@ImageUrl", (mediaInfo.ImageUrl == null) ? "" : mediaInfo.ImageUrl);
                    _dbHelper.TryBind(statement, "@RunTimeTicks", mediaInfo.RunTimeTicks);
                    _dbHelper.TryBind(statement, "@Rating", mediaInfo.Rating);
                    _dbHelper.TryBind(statement, "@TotalBitrate", mediaInfo.TotalBitrate);
                    _dbHelper.TryBind(statement, "@PremiereDate", mediaInfo.PremiereDate);
                    _dbHelper.TryBind(statement, "@DateAdded", mediaInfo.DateAdded);
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

        public void AnalyzeSeries(ILibraryManager libManager, IFileSystem fileSystem, CancellationToken cancellationToken, IProgress<double> progress)
        {
            _logger.Info($"AnalyzeSeries- Starting Video Analysis");

            progress.Report(0);
            var seriesList = _dbHelper.GetLibraryItems<Series>(libManager).Cast<Series>().ToList();
            progress.Report(100);

            double count = seriesList.Count;
            double curr = 0.0;

            progress.Report(0);
            foreach (Series series in seriesList)
            {
                progress.Report(100.0 * (++curr) / count);
                try
                {
                    AddSeries(series, libManager, cancellationToken, progress);
                    _logger.Info($"AnalyzeMedia -     Processed Series ({curr} of {count}) - {series.Name}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"AnalyzeMedia {series.SortName}:");
                    var path = series.Path ?? "Unknown";
                    _logger.Error($"AnalyzeMedia {path}:");
                    _logger.Error($"AnalyzeMedia {ex.Message}");
                    throw ex;
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
            _logger.Info($"AnalyzeMedia - Finished Video Analysis");
        }

        private void AddSeries(Series series, ILibraryManager libManager, CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (series.Id == null)
            {
                _logger.Error($"AddMediaInfo {series.SortName}: is missing ItemId");
                return;
            }

            _logger.Info($"AnalyzeSeries - AddSeries - Adding Series {series.Name}");

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

            lock (_connection)
            {
                long totalFileSize = 0;
                long totalRuntime = 0;
                Double averageRating = 0.0;
                long averageBitrate = 0;

                using (var statement = _connection.PrepareStatement("SELECT SUM(FileSize), SUM(RunTimeTicks), SUM(Rating)/Count(1),Sum(TotalBitrate)/Count(1) FROM Media WHERE SeriesId=@SeriesId"))
                {
                    _dbHelper.TryBind(statement, "@SeriesId", series.Id.ToString());
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        totalFileSize = row.GetInt64(0);
                        totalRuntime = row.GetInt64(1);
                        averageRating = row.GetFloat(2);
                        averageBitrate = row.GetInt64(3);
                        break;
                    }
                }

                using (var statement = _connection.PrepareStatement(sql))
                {
                    _dbHelper.TryBind(statement, "@ItemId", series.Id.ToString());
                    _dbHelper.TryBind(statement, "@Name", series.Name);
                    _dbHelper.TryBind(statement, "@SortName", series.SortName);
                    if (series.PremiereDate.HasValue)
                        _dbHelper.TryBind(statement, "@PremiereDate", series.PremiereDate.Value.DateTime);
                    _dbHelper.TryBind(statement, "@DateAdded", series.DateCreated.DateTime);
                    _dbHelper.TryBind(statement, "@ImageUrl", ItemImageUrl._ItemImageUrl(series));
                    _dbHelper.TryBind(statement, "@FileSize", totalFileSize);
                    _dbHelper.TryBind(statement, "@RunTimeTicks", totalRuntime);
                    _dbHelper.TryBind(statement, "@Rating", averageRating);
                    _dbHelper.TryBind(statement, "@AverageBitrate", averageBitrate);
                    statement.MoveNext();
                }
            }
            _logger.Info($"AnalyzeCollections -     AddCollection - Successfully Added Collection");

            //AddCollectionMembers(collection, libManager, cancellationToken, progress);
        }

    }
}
