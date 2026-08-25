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
using Microsoft.Data.Sqlite;
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
using System.Threading.Tasks;
using Dapper;



namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public async Task UpdateLastUpdated(DateTime lastUpdate, DateTime buildDate, string version)
        {
            if (_dbHelper == null)
                throw new ArgumentNullException("_dbHelper is null");

            string sql = "delete from LastUpdateTable";
            await _dbHelper.Execute(sql);

            sql = "INSERT INTO LastUpdateTable (LastUpdated, BuildDate, Version) values (@LastUpdated, @BuildDate,@Version)";

            await _dbHelper.WaitAsync();
            try
            {
                await _dbHelper.ExecuteAsync(sql, new
                {
                    LastUpdated = _dbHelper.ToDateTimeParamValue(lastUpdate),
                    BuildDate = _dbHelper.ToDateTimeParamValue(buildDate),
                    Version = version
                });
            }
            catch
            {
                throw;
            }
            finally
            {
                _dbHelper.Release();
            }
        }

        public async Task AddAllUsers(IUserManager userManager, IUserDataManager userDataManager, ILibraryManager libraryManager, CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (_dbHelper == null)
                throw new ArgumentNullException("Either user or allUsers must be provided.");

            progress.Report(0);
            var users = userManager.GetUserList(new UserQuery() { EnableRemoteAccess = true }).ToList();
            progress.Report(100);

            _logger?.Debug($"AddUsers - Starting User Analysis");
            double count = users.Count;
            double curr = 0;

            await _dbHelper._connection._lock.WaitAsync();

            progress.Report(0);
            try
            {
                using (var transaction = _dbHelper.BeginTransaction())
                {
                    foreach (var user in users)
                    {
                        progress.Report(100.0 * (++curr) / count);
                        try
                        {
                            using (var timer = new AutoTimer($"AddUsers -     Processed User ({curr} of {count}) - {user.Name}", _logger))
                            {
                                await AddUser(user, userDataManager, libraryManager, transaction);
                                await AddUserWatchData(user, userDataManager, libraryManager, progress, transaction);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.Error($"AddUsers {user.SortName}: {ex.Message}");
                            throw ex;
                        }
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                    _logger?.Debug($"AddUsers - Finished User Analysis");
                }
                ;
            }
            catch
            {
                // Automatically rolls back the transaction safely if an issue arises
                throw;
            }
            finally
            {
                // 4. Always release the semaphore slot in the finally block
                _dbHelper._connection._lock.Release();
            }
        }

        (long, long) AnalyzeOverallTime(User? user, List<User>? userList, IUserDataManager userDataManager, ILibraryManager libraryManager)
        {
            if (user == null && userList == null)
                throw new ArgumentNullException("Either user or allUsers must be provided.");

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

        private async Task AddUserWatchData(User user, IUserDataManager userDataManager, ILibraryManager libManager, IProgress<double> progress, SqliteTransaction transaction)
        {
            if (user == null || _dbHelper == null)
                throw new ArgumentNullException("user or _dbHelper is null");

            var (allVideosForUser, allVideos) = Statistics2026API.GetAllEpisodesAndMovies(user, libManager);

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

            await _dbHelper.WaitAsync();
            try
            {
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

                    await _dbHelper.ExecuteAsync(sql, new
                    {
                        UserId = user.Id.ToString(),
                        ItemId = video.Id.ToString(),
                        IsEpisode = isEpisode,
                        IsPlayed = video.IsPlayed(user),
                        SeriesId = seriesId

                    }, transaction);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                _dbHelper.Release();
            }
        }

        public async Task AddUser(User user, IUserDataManager userDataManager, ILibraryManager libraryManager, SqliteTransaction transaction)
        {
            if (user == null || _dbHelper == null)
            {
                throw new ArgumentNullException("user or _dbHelper is null");
            }

            if (user.Id == null)
            {
                _logger?.Error($"AddUserInfo {user.Name}: is missing Id");
                return;
            }

            if (user.Name == null)
            {
                _logger?.Error($"AddUserInfo {user.Id.ToString()}: is missing Name");
                return;
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

            await _dbHelper.WaitAsync();
            try
            {
                await _dbHelper.ExecuteAsync(sql, new
                {
                    UserId = user.Id.ToString(),
                    UserName = user.Name,
                    ConnectUserId = user.ConnectUserId,
                    IsAdministrator = isAdmin,

                    TotalTimeWatched = timeWatched,
                    TotalWatchableTime = totalTime
                }, transaction);
            }
            catch
            {
                throw;
            }
            finally
            {
                _dbHelper.Release();
            }

        }

        public async Task AddAllMedia(ILibraryManager libManager, IFileSystem fileSystem, CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (_dbHelper == null)
                throw new ArgumentNullException("_dbHelper is null");

            _logger?.Debug($"AddAllMedia - Starting Video Analysis");

            progress.Report(0);
            var videoList = _dbHelper.GetLibraryItems<Episode>(libManager).Cast<Video>().ToList();
            progress.Report(50);
            videoList.AddRange(_dbHelper.GetLibraryItems<Movie>(libManager).Cast<Video>().ToList());
            progress.Report(100);

            double count = videoList.Count;
            double curr = 0.0;

            await _dbHelper._connection._lock.WaitAsync();
            progress.Report(0);
            try
            {
                using (var transaction = _dbHelper.BeginTransaction())
                {
                    foreach (var video in videoList)
                    {
                        progress.Report(100.0 * (++curr) / count);
                        try
                        {
                            using (var mediaInfo = new MediaInfo(video, fileSystem))
                            {
                                await AddMediaInfo(mediaInfo, transaction);
                                _logger?.Debug($"AddAllMedia -     Processed Video ({curr} of {count}) - {mediaInfo.DescriptiveName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.Error($"AddAllMedia {video.SortName}:");
                            var path = video.Path ?? "Unknown";
                            _logger?.Error($"AddAllMedia {path}:");
                            _logger?.Error($"AddAllMedia {ex.Message}");
                            throw ex;
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                ;
                _logger?.Debug($"AddAllMedia - Finished Video Analysis");
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _dbHelper._connection._lock.Release();
            }
        }

        public async Task AddMediaInfo(MediaInfo? mediaInfo, SqliteTransaction transaction)
        {
            if (_dbHelper == null)
                throw new ArgumentNullException("_dbHelper is null");

            if (mediaInfo == null)
            {
                throw new ArgumentNullException("_dbHelper is null");
            }

            if (mediaInfo.ItemId == null)
            {
                _logger?.Error($"AddMediaInfo {mediaInfo.SortName}: is missing ItemId");
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

            await _dbHelper.WaitAsync();
            try
            {
                await _dbHelper.ExecuteAsync(sql, new
                {
                    ItemId = mediaInfo.ItemId,
                    PrimaryName = mediaInfo.PrimaryName,
                    SortName = mediaInfo.SortName,
                    SecondaryName = mediaInfo.SecondaryName,
                    StartYear = mediaInfo.StartYear,
                    IsEpisode = mediaInfo.IsEpisode,
                    SeriesId = mediaInfo.SeriesId,
                    Season = mediaInfo.Season,
                    Episode = mediaInfo.Episode,
                    ResolutionBase = mediaInfo.ResolutionBase,
                    ResolutionDetail = mediaInfo.ResolutionDetail,
                    Codec = mediaInfo.Codec,
                    DolbyVisionProfile = mediaInfo.DolbyVisionProfile,
                    StudioNames = string.Join(";", mediaInfo.StudioNames),
                    Genres = string.Join(";", mediaInfo.Genres),
                    ServerLocation = mediaInfo.ServerLocation,
                    FileSize = mediaInfo.FileSize,
                    ImageUrl = (mediaInfo.ImageUrl == null) ? "" : mediaInfo.ImageUrl,
                    RunTimeTicks = mediaInfo.RunTimeTicks,
                    Rating = mediaInfo.Rating,
                    TotalBitrate = mediaInfo.TotalBitrate,
                    PremiereDate = mediaInfo.PremiereDate,
                    DateAdded = mediaInfo.DateAdded,
                }, transaction);
            }
            catch
            {
                throw;
            }
            finally
            {
                _dbHelper.Release();
            }

        }

        public async Task AddAllCollections(ILibraryManager libManager, CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (_dbHelper == null)
                throw new ArgumentNullException("_dbHelper is null");

            _logger?.Debug($"AddAllCollections - Starting Collection Analysis");
            progress.Report(0);
            var collections = _dbHelper.GetLibraryItems<BoxSet>(libManager);
            progress.Report(100);

            double count = collections.Count();
            double curr = 0.0;

            progress.Report(0);
            using (var transaction = _dbHelper.BeginTransaction())
            {
                foreach (var collection in collections)
                {
                    progress.Report(100.0 * (++curr) / count);
                    try
                    {
                        await AddCollection(collection, libManager, cancellationToken, progress, transaction);
                        _logger?.Debug($"AddAllCollections -     Processed Collection ({curr} of {count}) - {collection.Name} items processed");
                    }
                    catch (Exception ex)
                    {
                        _logger?.Error($"AddAllCollections {collection.SortName}:");
                        var path = collection.Path ?? "Unknown";
                        _logger?.Error($"AddAllCollections {path}:");
                        _logger?.Error($"AddAllCollections {ex.Message}");
                        throw ex;
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            _logger?.Debug($"AddAllCollections - Finished Collection Analysis");
        }

        private async Task AddChildToCollection(Video video, BoxSet collection, ILibraryManager libManager, SqliteTransaction transaction)
        {
            if (_dbHelper == null)
                throw new ArgumentNullException("_dbHelper is null");

            if (video == null || collection == null || libManager == null)
            {
                _logger?.Error($"AddChildToCollection video, collection and libManager must be set");
                throw new ArgumentNullException("_dbHelper is null");
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

            await _dbHelper.WaitAsync();
            try
            {
                using (var command = _dbHelper.ExecuteAsync(sql, new
                {
                    CollectionId = collection.Id.ToString(),
                    ItemId = video.Id.ToString(),
                    CollectionName = collection.Name
                }))
                {
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                _dbHelper._connection._lock.Release();
            }
        }

        private async Task<int> AddCollectionMembers(BoxSet collection, ILibraryManager libManager, CancellationToken cancellationToken, IProgress<double> progress, SqliteTransaction transaction)
        {
            if (_dbHelper == null)
                throw new ArgumentNullException("_dbHelper is null");

            _logger?.Debug($"AddAllCollections - AddCollectionMembers -     Adding members of Collection - {collection.Name}");

            var query = new InternalItemsQuery
            {
                CollectionIds = new[] { collection.InternalId },
                Recursive = true
            };

            var baseItems = libManager.GetItemList(query);
            var videos = baseItems.OfType<Video>().ToList();

            double count = videos.Count;
            double curr = 0.0;
            await _dbHelper.WaitAsync();
            foreach (var video in videos)
            {
                progress.Report(100.0 * (++curr) / count);
                await AddChildToCollection(video, collection, libManager, transaction);
                cancellationToken.ThrowIfCancellationRequested();
            };
            _logger?.Debug($"AddAllCollections - AddCollectionMembers -     Finished Adding {videos.Count} members of Collection {collection.Name} ");
            return videos.Count;
        }

        public async Task AddCollection(BoxSet collection, ILibraryManager libManager, CancellationToken cancellationToken, IProgress<double> progress, SqliteTransaction transaction)
        {
            if (_dbHelper == null)
                throw new ArgumentNullException("_dbHelper is null");

            if (collection.Id == null)
            {
                _logger?.Error($"AddCollection {collection.SortName}: is missing ItemId");
                return;
            }

            _logger?.Debug($"AddAllCollections - AddCollection - Adding Collection {collection.Name}");

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

            await _dbHelper.WaitAsync();
            try
            {
                using (var command = _dbHelper.ExecuteAsync(sql, new
                {
                    ItemId = collection.Id.ToString(),
                    Name = collection.Name,
                    SortName = collection.SortName
                }))
                {
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                _dbHelper.Release();
            }
            _logger?.Debug($"AddAllCollections -     AddCollection - Successfully Added Collection");

            await AddCollectionMembers(collection, libManager, cancellationToken, progress, transaction);
        }

        public async Task AddAllSeries(ILibraryManager libManager, IFileSystem fileSystem, CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (_dbHelper == null)
                throw new ArgumentNullException("_dbHelper is null");

            _logger?.Debug($"AddAllSeries- Starting Video Analysis");

            progress.Report(0);
            var seriesList = _dbHelper.GetLibraryItems<Series>(libManager).Cast<Series>().ToList();
            progress.Report(100);

            double count = seriesList.Count;
            //double curr = 0.0;

            progress.Report(0);
            //_dbHelper.RunInTransaction(connection =>
            //{

            //    foreach (Series series in seriesList)
            //    {
            //        progress.Report(100.0 * (++curr) / count);
            //        try
            //        {
            //            AddSeries(series, libManager, cancellationToken, progress, transaction);
            //            _logger?.Debug($"AddAllSeries -     Processed Series ({curr} of {count}) - {series.Name}");
            //        }
            //        catch (Exception ex)
            //        {
            //            _logger?.Error($"AddAllSeries {series.SortName}:");
            //            var path = series.Path ?? "Unknown";
            //            _logger?.Error($"AddAllSeries {path}:");
            //            _logger?.Error($"AddAllSeries {ex.Message}");
            //            throw ex;
            //        }

            //        cancellationToken.ThrowIfCancellationRequested();
            //    }
            //});
            _logger?.Debug($"AddAllSeries - Finished Video Analysis");
        }

        private async Task AddSeries(Series series, ILibraryManager libManager, CancellationToken cancellationToken, IProgress<double> progress, SqliteTransaction transaction)
        {
            if (_dbHelper == null)
                throw new ArgumentNullException("_dbHelper is null");

            if (series.Id == null)
            {
                _logger?.Error($"AddSeries {series.SortName}: is missing ItemId");
                return;
            }

            _logger?.Debug($"AddAllSeries - AddSeries - Adding Series {series.Name}");

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

            //long totalFileSize = 0;
            //long totalRuntime = 0;
            //Double averageRating = 0.0;
            //long averageBitrate = 0;
            sql = "SELECT SUM(FileSize), SUM(RunTimeTicks), SUM(Rating)/Count(1),Sum(TotalBitrate)/Count(1) FROM Media WHERE SeriesId=@SeriesId";
            await _dbHelper.WaitAsync();
            try
            {
                using (var cmd = _dbHelper.ExecuteAsync(sql, new
                {
                    SeriesId = series.Id.ToString()
                }))
                {
                    ////var seriesInfo = await _connection._connection._connection.QueryAsync( )
                    //        while (statement.MoveNext())
                    //{
                    //    var row = statement.Current;
                    //    totalFileSize = row.GetInt64(0);
                    //    totalRuntime = row.GetInt64(1);
                    //    averageRating = row.GetFloat(2);
                    //    averageBitrate = row.GetInt64(3);
                    //    break;
                    //}
                }

                //lock (connection)
                //{
                //    using (var statement = connection.PrepareStatement(sql))
                //    {
                //        ItemId", series.Id.ToString());
                //                Name", series.Name);
                //                SortName", series.SortName);
                //                if (series.PremiereDate.HasValue)
                //            PremiereDate", series.PremiereDate.Value.DateTime);
                //                DateAdded", series.DateCreated.DateTime);
                //                ImageUrl", ItemImageUrl._ItemImageUrl(series));
                //                FileSize", totalFileSize);
                //                RunTimeTicks", totalRuntime);
                //                Rating", averageRating);
                //                AverageBitrate", averageBitrate);
                //                statement.MoveNext();
                //    }
                //}
                //_logger?.Debug($"AddAllCollections -     AddCollection - Successfully Added Collection");
            }
            catch
            {
                throw;
            }
            finally
            {
                _dbHelper.Release();
            }
        }

    }
}
