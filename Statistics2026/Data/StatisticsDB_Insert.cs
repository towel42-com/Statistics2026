using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        private void CheckIsValid()
        {
            if (!_dbHelper.isValid())
                throw new ArgumentNullException("dbHelper");
            if (_embyManagers == null)
                throw new ArgumentNullException("_embyManagers");
        }

        public void UpdateLastUpdated(DateTime lastUpdate, DateTime buildDate, string version)
        {
            CheckIsValid();

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

        public void AddAllUsers(CancellationToken cancellationToken, IProgress<double> progress)
        {
            CheckIsValid();

            progress.Report(0);
            var users = _embyManagers?._userManager.GetUserList(new UserQuery() { EnableRemoteAccess = true }).ToList();
            if (users == null)
                return;

            progress.Report(100);

            _embyManagers?._logger?.Debug($"AddAllUsers - Starting User Analysis");
            double count = users.Count;
            double curr = 0;

            progress.Report(0);
            var sqlCmds = new List<SQLCmdDef>();
            using (var timer = new AutoTimer($"    Adding All Users - Getting Commands", _embyManagers?._logger))
            {
                foreach (var user in users)
                {
                    progress.Report(80.0 * (++curr) / count);
                    using (var userTimer = new AutoTimer($"AddAllUsers -     Processed User ({curr} of {count}) - {user.Name}", _embyManagers?._logger))
                    {
                        sqlCmds.AddRange(AddUser(user));
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();
            }

            using (var timer = new AutoTimer($"    Adding All Users - Executing Commands", _embyManagers?._logger))
            {
                progress.Report(80);
                _dbHelper.ExecuteCommands(sqlCmds);
                progress.Report(100);
            }

            _embyManagers?._logger?.Debug($"AddAllUsers - Finished User Analysis");
        }

        public void AnalyzeUserWatchData(CancellationToken cancellationToken, IProgress<double> progress)
        {
            CheckIsValid();

            progress.Report(0);
            var users = _embyManagers?._userManager.GetUserList(new UserQuery() { EnableRemoteAccess = true }).ToList();
            if (users == null)
                return;

            progress.Report(100);

            _embyManagers?._logger?.Debug($"AnalyzeUserWatchData - Starting User Watch Data Analysis");
            double count = users.Count;
            double curr = 0;

            progress.Report(0);
            var sqlCmds = new List<SQLCmdDef>();
            using (var timer = new AutoTimer($"    Analyze User Watch Data - Getting Commands", _embyManagers?._logger))
            {
                foreach (var user in users)
                {
                    progress.Report(80.0 * (++curr) / count);
                    using (var userTimer = new AutoTimer($"AnalyzeUserWatchData -     Processed User ({curr} of {count}) - {user.Name}", _embyManagers?._logger))
                    {
                        sqlCmds.AddRange(AddUserWatchData(user, progress));
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();
            }

            using (var timer = new AutoTimer($"    Analyze User Watch Data - Executing Commands", _embyManagers?._logger))
            {
                progress.Report(80);
                _dbHelper.ExecuteCommands(sqlCmds);
                progress.Report(100);
            }

            _embyManagers?._logger?.Debug($"AnalyzeUserWatchData - Finished User Watch Data Analysis");
        }

        (long, long) AnalyzeOverallTime(User? user, List<User>? userList)
        {
            CheckIsValid();

            if (user == null && userList == null)
                throw new ArgumentException("Either user or allUsers must be provided.");

            var (allVideosForUser, allVideos) = Statistics2026API.GetAllEpisodesAndMovies(user, _embyManagers!._libraryManager, true);

            long watchable = 0;
            long watched = 0;
            if (user == null && userList != null) // use the list of users
            {
                watched = allVideos.Where(video => userList.Any(u => _embyManagers!._userDataManager.GetUserData(u, video).Played)).Sum(item => item.RunTimeTicks ?? 0);
                watchable = allVideos.Sum(item => item.RunTimeTicks ?? 0);
            }
            else
            {
                watched = allVideosForUser.Where(video => _embyManagers!._userDataManager.GetUserData(user, video).Played).Sum(item => item.RunTimeTicks ?? 0);
                watchable = allVideosForUser.Sum(item => item.RunTimeTicks ?? 0);
            }

            return (watched, watchable);
        }

        private List<SQLCmdDef> AddUserWatchData(User? user, IProgress<double> progress)
        {
            CheckIsValid();

            if (user == null)
                throw new ArgumentNullException("user");

            var allVideosForUser = Statistics2026API.GetAllEpisodesAndMovies(user, _embyManagers!._libraryManager, false).forUser;

            string sql =
                "INSERT INTO UserVideoList " +
                "(" +
                    "  UserId" +
                    ", ItemId" +
                    ", IsPlayed" +
                    ", PlayCount" +
                    ", LastPlayedDate" +
                    ", IsEpisode" +
                    ", NumEpisodes" +
                    ", IsTVSpecial" +
                    ", SeriesId" +
                ")" +
                " VALUES " +
                "(" +
                    "  @UserId" +
                    ", @ItemId" +
                    ", @IsPlayed" +
                    ", @PlayCount" +
                    ", @LastPlayedDate" +
                    ", @IsEpisode" +
                    ", @NumEpisodes" +
                    ", @IsTVSpecial" +
                    ", @SeriesId" +
                ")";

            var sqlCmds = new List<SQLCmdDef>();
            foreach (var video in allVideosForUser)
            {
                var userData = _embyManagers!._userDataManager.GetUserData(user, video);
                using (var mediaInfo = new MediaInfo(video))
                {
                    sqlCmds.Add(new SQLCmdDef(sql, new List<(string name, object? value)>()
                        {
                            ( "@UserId", user.Id.ToString()),
                            ( "@ItemId", video.Id.ToString()),
                            ( "@IsEpisode", mediaInfo.IsEpisode),
                            ( "@NumEpisodes", mediaInfo.NumEpisodes),
                            ( "@IsTVSpecial", mediaInfo.IsTVSpecial),
                            ( "@IsPlayed", userData?.Played ?? false),
                            ( "@PlayCount", userData?.PlayCount ?? 0),
                            ( "@LastPlayedDate", userData?.LastPlayedDate?.Date ?? null ),
                            ( "@SeriesId", mediaInfo.SeriesId)
                        }));
                }
            }
            return sqlCmds;
        }

        private List<SQLCmdDef> AddUser(User? user)
        {
            CheckIsValid();

            if (user == null)
                throw new ArgumentNullException("user");

            var sqlCmds = new List<SQLCmdDef>();
            if (user.Id == null)
            {
                _embyManagers!._logger?.Error($"AddUser {user.Name}: is missing Id");
                return sqlCmds;
            }

            if (user.Name == null)
            {
                _embyManagers!._logger?.Error($"AddUser {user.Id.ToString()}: is missing Name");
                return sqlCmds;
            }

            var (timeWatched, totalTime) = AnalyzeOverallTime(user, null);

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

        public void AddAllMedia(CancellationToken cancellationToken, IProgress<double> progress)
        {
            CheckIsValid();

            _embyManagers!._logger?.Debug($"AddAllMedia - Starting Video Analysis");

            progress.Report(0);
            var videoList = _dbHelper.GetLibraryItems<Episode>().Cast<Video>().ToList();
            progress.Report(50);
            videoList.AddRange(_dbHelper.GetLibraryItems<Movie>().Cast<Video>().ToList());
            progress.Report(100);

            double count = videoList.Count;
            double curr = 0.0;

            progress.Report(0);
            var sqlCmds = new List<SQLCmdDef>();
            foreach (var video in videoList)
            {
                progress.Report(80.0 * (++curr) / count);
                using (var mediaInfo = new MediaInfo(video))
                {

                    sqlCmds.AddRange(AddMediaInfo(mediaInfo));
                    _embyManagers!._logger?.Debug($"AddAllMedia -     Processed Video ({curr} of {count}) - {mediaInfo.DescriptiveName}");
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
            progress.Report(80);
            _dbHelper.ExecuteCommands(sqlCmds);
            progress.Report(100);
            _embyManagers!._logger?.Debug($"AddAllMedia - Finished Video Analysis");
        }

        public List<SQLCmdDef> AddMediaInfo(MediaInfo mediaInfo)
        {
            CheckIsValid();

            var sqlCmds = new List<SQLCmdDef>();
            if (mediaInfo == null || mediaInfo.ItemId == null)
            {
                _embyManagers!._logger?.Error($"AddMediaInfo '{mediaInfo?.SortName}': is missing ItemId");
                return sqlCmds;
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
                    ", IsTVSpecial" +
                    ", SeriesId" +
                    ", Season" +
                    ", Episode" +
                    ", NumEpisodes" +
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
                    ", @IsTVSpecial" +
                    ", @SeriesId" +
                    ", @Season" +
                    ", @Episode" +
                    ", @NumEpisodes" +
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
                ("@IsTVSpecial", mediaInfo.IsTVSpecial),
                ("@SeriesId", mediaInfo.SeriesId),
                ("@Season", mediaInfo.Season),
                ("@Episode", mediaInfo.Episode),
                ("@NumEpisodes", mediaInfo.NumEpisodes),
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

        public void AddAllCollections(CancellationToken cancellationToken, IProgress<double> progress)
        {
            CheckIsValid();

            _embyManagers!._logger?.Debug($"AddAllCollections - Starting Collection Analysis");
            progress.Report(0);
            var collections = _dbHelper.GetLibraryItems<BoxSet>();
            progress.Report(100);

            double count = collections.Count();
            double curr = 0.0;

            progress.Report(0);
            var sqlCmds = new List<SQLCmdDef>();

            foreach (var collection in collections)
            {
                progress.Report(80.0 * (++curr) / count);
                sqlCmds.AddRange(AddCollection(collection, cancellationToken, progress));
                cancellationToken.ThrowIfCancellationRequested();
                _embyManagers!._logger?.Debug($"AddAllCollections -     Processed Collection ({curr} of {count}) - {collection.Name} items processed");
            }
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(80);
            _dbHelper.ExecuteCommands(sqlCmds);
            progress.Report(100);
            _embyManagers!._logger?.Debug($"AddAllCollections - Finished Collection Analysis");
        }

        private List<SQLCmdDef> AddChildToCollection(Video video, BoxSet collection)
        {
            CheckIsValid();

            var sqlCmds = new List<SQLCmdDef>();

            if (video == null || collection == null)
            {
                _embyManagers!._logger?.Error($"AddChildToCollection video, collection must be set");
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

        private List<SQLCmdDef> AddCollectionMembers(BoxSet collection, CancellationToken cancellationToken, IProgress<double> progress)
        {
            CheckIsValid();

            _embyManagers!._logger?.Debug($"AddAllCollections - AddCollectionMembers -     Adding members of Collection - {collection.Name}");

            var query = new InternalItemsQuery
            {
                CollectionIds = new[] { collection.InternalId },
                Recursive = true
            };

            var baseItems = _embyManagers._libraryManager.GetItemList(query);
            var videos = baseItems.OfType<Video>().ToList();

            double count = videos.Count;
            double curr = 0.0;

            var sqlCmds = new List<SQLCmdDef>();

            videos.ForEach(video =>
            {
                progress.Report(100.0 * (++curr) / count);
                sqlCmds.AddRange(AddChildToCollection(video, collection));
                cancellationToken.ThrowIfCancellationRequested();
            });
            _embyManagers!._logger?.Debug($"AddAllCollections - AddCollectionMembers -     Finished Adding {videos.Count} members of Collection {collection.Name} ");
            return sqlCmds;
        }

        public List<SQLCmdDef> AddCollection(BoxSet collection, CancellationToken cancellationToken, IProgress<double> progress)
        {
            CheckIsValid();

            var sqlCmds = new List<SQLCmdDef>();
            if (collection.Id == null)
            {
                _embyManagers!._logger?.Error($"AddCollection {collection.SortName}: is missing ItemId");
                return sqlCmds;
            }

            _embyManagers!._logger?.Debug($"AddAllCollections - AddCollection - Adding Collection {collection.Name}");

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
            _embyManagers!._logger?.Debug($"AddAllCollections -     AddCollection - Successfully Added Collection");

            sqlCmds.AddRange(AddCollectionMembers(collection, cancellationToken, progress));
            return sqlCmds;
        }

        public void AddAllSeries(CancellationToken cancellationToken, IProgress<double> progress)
        {
            CheckIsValid();

            _embyManagers!._logger?.Debug($"AddAllSeries- Starting Video Analysis");

            progress.Report(0);
            var seriesList = _dbHelper.GetLibraryItems<Series>().Cast<Series>().ToList();
            progress.Report(100);

            double count = seriesList.Count;
            double curr = 0.0;

            progress.Report(0);
            var sqlCmds = new List<SQLCmdDef>();

            foreach (Series series in seriesList)
            {
                progress.Report(80.0 * (++curr) / count);
                sqlCmds.AddRange(AddSeries(series, cancellationToken, progress));
                cancellationToken.ThrowIfCancellationRequested();

                _embyManagers!._logger?.Debug($"AddAllSeries -     Processed Series ({curr} of {count}) - {series.Name}");
            }

            progress.Report(80);
            _dbHelper.ExecuteCommands(sqlCmds);
            progress.Report(100);
            cancellationToken.ThrowIfCancellationRequested();
            _embyManagers!._logger?.Debug($"AddAllSeries - Finished Video Analysis");
        }

        private int GetCountForSeries(Series series, CancellationToken cancellationToken, bool episodes)
        {
            CheckIsValid();

            var libraryOptions = _embyManagers!._libraryManager.GetLibraryOptions(series);
            var allEpisodes = _embyManagers!._providerManager.GetAllEpisodes(series, libraryOptions, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
            var retVal = allEpisodes.Where(e => (
                ((episodes && !MediaInfo.isTVSpecial(e)) ||
                  (!episodes && MediaInfo.isTVSpecial(e)))
                && (e.PremiereDate <= DateTime.Now))).Count();

            if (retVal == 0)// when providers are disabled
            {
                var fieldName = episodes ? "NumEpisodes" : "NumSpecials";
                var cmd = new SQLCmdDef($"SELECT {fieldName} FROM Series WHERE ItemId=@SeriesId",
                            new List<(string name, object? value)>()
                            {
                            ("@SeriesId", series.Id.ToString())
                            });

                _dbHelper.ExecuteCommand(cmd, statement =>
                {
                    if (statement != null)
                    {
                        var row = statement.Current;
                        retVal = row.GetInt(0);
                    }
                    return false;
                });
            }
            if (retVal == 0) // Series hasnt been setup yet
            {
                var whereClause = episodes ? "NOT IsTVSpecial" : "IsTVSpecial";
                var cmd = new SQLCmdDef($"SELECT SUM(NumEpisodes) FROM Media WHERE SeriesId=@SeriesId AND IsEpisode AND {whereClause}",
                            new List<(string name, object? value)>()
                            {
                            ("@SeriesId", series.Id.ToString())
                            });

                _dbHelper.ExecuteCommand(cmd, statement =>
                {
                    if (statement != null)
                    {
                        var row = statement.Current;
                        retVal = row.GetInt(0);
                    }
                    return false;
                });
            }
            return retVal;
        }

        private int GetEpisodeCountForSeries(Series series, CancellationToken cancellationToken)
        {
            return GetCountForSeries(series, cancellationToken, true);
        }

        private int GetSpecialCountForSeries(Series series, CancellationToken cancellationToken)
        {
            return GetCountForSeries(series, cancellationToken, false);
        }

        private List<SQLCmdDef> AddSeries(Series series, CancellationToken cancellationToken, IProgress<double> progress)
        {
            CheckIsValid();

            var sqlCmds = new List<SQLCmdDef>();
            if (series.Id == null)
            {
                _embyManagers!._logger?.Error($"AddSeries {series.SortName}: is missing ItemId");
                return sqlCmds;
            }

            _embyManagers!._logger?.Debug($"AddAllSeries - AddSeries - Adding Series {series.Name}");


            long totalFileSize = 0;
            long totalRuntime = 0;
            Double averageRating = 0.0;
            long averageBitrate = 0;

            var sql = "SELECT " +
                "  SUM(FileSize)" +
                ", SUM(RunTimeTicks)" +
                ", SUM(Rating)/Count(1)" +
                ", Sum(TotalBitrate)/Count(1) " +
                "FROM " +
                "Media " +
                "WHERE SeriesId=@SeriesId";

            var cmd = new SQLCmdDef(sql,
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

            var seriesStatus = series.Status?.ToString() ?? "";

            cmd = new SQLCmdDef("SELECT COUNT(*) FROM Series where ItemId=@ItemId", new List<(string name, object? value)>() { ("@ItemId", series.Id.ToString()) });
            var exists = false;
            _dbHelper.ExecuteCommand(cmd, statement =>
            {
                var row = statement.Current;
                exists = row.GetInt(0) > 0;
                return false;
            });

            int numEpisodes = GetEpisodeCountForSeries(series, cancellationToken);
            int numSpecials = GetSpecialCountForSeries(series, cancellationToken);
            sql = String.Empty;
            List<(string name, object? value)>? paramsList = null;
            if (!exists)
            {
                sql = "INSERT INTO Series " +
                "(" +
                    "  ItemId" +
                    ", Name" +
                    ", SortName" +
                    ", PremiereDate" +
                    ", NumEpisodes" +
                    ", NumSpecials" +
                    ", DateAdded" +
                    ", ImageUrl" +
                    ", FileSize" +
                    ", RunTimeTicks" +
                    ", Rating" +
                    ", Status" +
                    ", AverageBitrate" +
                ")" +
                " VALUES " +
                "(" +
                    "  @ItemId" +
                    ", @Name" +
                    ", @SortName" +
                    ", @PremiereDate" +
                    ", @NumEpisodes" +
                    ", @NumSpecials" +
                    ", @DateAdded" +
                    ", @ImageUrl" +
                    ", @FileSize" +
                    ", @RunTimeTicks" +
                    ", @Rating" +
                    ", @Status" +
                    ", @AverageBitrate" +
                ")";

                paramsList = new List<(string name, object? value)>()
                       {
                           ("@ItemId", series.Id.ToString()),
                           ("@Name", series.Name),
                           ("@SortName", series.SortName),
                           ("@PremiereDate", series.PremiereDate.HasValue ? series.PremiereDate.Value.DateTime : null),
                           ("@NumEpisodes", numEpisodes),
                           ("@NumSpecials", numSpecials),
                           ("@DateAdded", series.DateCreated.DateTime),
                           ("@ImageUrl", ItemImageUrl._ItemImageUrl(series)),
                           ("@FileSize", totalFileSize),
                           ("@RunTimeTicks", totalRuntime),
                           ("@Rating", averageRating),
                           ("@Status", seriesStatus),
                           ("@AverageBitrate", averageBitrate),
                       };
            }
            else
            {
                sql = "UPDATE Series " +
                    "  SET " +
                    "  NumEpisodes=NumEpisodes+@NumEpisodes" +
                    ", NumSpecials=NumSpecials+@NumSpecials" +
                    ", FileSize=FileSize+@FileSize" +
                    ", RunTimeTicks=RunTimeTicks+@RunTimeTicks " +
                    ", Rating=@Rating " +
                    ", Status=@Status " +
                    "WHERE ItemId=@ItemId";

                paramsList = new List<(string name, object? value)>()
                       {
                           ("@ItemId", series.Id.ToString()),
                           ("@NumEpisodes", numEpisodes),
                           ("@NumSpecials", numSpecials),
                           ("@FileSize", totalFileSize),
                           ("@RunTimeTicks", totalRuntime),
                           ("@Rating", averageRating),
                           ("@Status", seriesStatus),
                       };
            }
            sqlCmds.Add(new SQLCmdDef(sql, paramsList));

            _embyManagers!._logger?.Debug($"AddAllCollections -     AddCollection - Successfully Added Collection");
            return sqlCmds;
        }
    }
}
