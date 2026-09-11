using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Entities;
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

            if (_embyInterfaces == null)
                throw new ArgumentNullException("_embyInterfaces");

            if (Statistics2026.Plugin.Instance == null)
                throw new ArgumentNullException("Statistics2026.Plugin.Instance");

            if (Statistics2026.Plugin.Instance.Configuration == null)
                throw new ArgumentNullException("Statistics2026.Plugin.Instance.Configuration");

        }

        public void UpdateLastUpdated(DateTime lastUpdate, DateTime buildDate, string version)
        {
            CheckIsValid();

            var sqlCmds = new List<SQLCmdDef>();
            sqlCmds.Add(new SQLCmdDef("delete from LastUpdateTable"));
            sqlCmds.Add(new SQLCmdDef("INSERT INTO LastUpdateTable (LastUpdated, BuildDate, Version) values (@LastUpdated, @BuildDate, @Version)",
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
            var users = _embyInterfaces?._userManager.GetUserList(new UserQuery() { EnableRemoteAccess = true }).ToList();
            if (users == null)
                return;

            progress.Report(100);

            _embyInterfaces?._logger?.Debug($"AddAllUsers - Starting User Analysis");
            double count = users.Count;
            double curr = 0;

            progress.Report(0);
            var sqlCmds = new List<SQLCmdDef>();
            using (var timer = new AutoTimer($"    Adding All Users - Getting Commands", _embyInterfaces?._logger))
            {
                foreach (var user in users)
                {
                    progress.Report(80.0 * (++curr) / count);
                    using (var userTimer = new AutoTimer($"AddAllUsers -     Processed User ({curr} of {count}) - {user.Name}", _embyInterfaces?._logger))
                    {
                        sqlCmds.AddRange(AddUser(user));
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();
            }

            using (var timer = new AutoTimer($"    Adding All Users - Executing Commands", _embyInterfaces?._logger))
            {
                progress.Report(80);
                _dbHelper.ExecuteCommands(sqlCmds);
                progress.Report(100);
            }

            _embyInterfaces?._logger?.Debug($"AddAllUsers - Finished User Analysis");
        }

        public void AnalyzeUserWatchData(CancellationToken cancellationToken, IProgress<double> progress)
        {
            CheckIsValid();

            progress.Report(0);
            var users = _embyInterfaces?._userManager.GetUserList(new UserQuery() { EnableRemoteAccess = true }).ToList();
            if (users == null)
                return;

            progress.Report(100);

            _embyInterfaces?._logger?.Debug($"AnalyzeUserWatchData - Starting User Watch Data Analysis");
            double count = users.Count;
            double curr = 0;

            progress.Report(0);
            var sqlCmds = new List<SQLCmdDef>();
            using (var timer = new AutoTimer($"    Analyze User Watch Data - Getting Commands", _embyInterfaces?._logger))
            {
                foreach (var user in users)
                {
                    progress.Report(80.0 * (++curr) / count);
                    using (var userTimer = new AutoTimer($"AnalyzeUserWatchData -     Processed User ({curr} of {count}) - {user.Name}", _embyInterfaces?._logger))
                    {
                        sqlCmds.AddRange(AddUserWatchData(user, progress, cancellationToken));
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();
            }

            var config = Statistics2026.Plugin.Instance!.Configuration;
            config.resetPlayCount = false;
            Statistics2026.Plugin.Instance.UpdateConfiguration(config);

            using (var timer = new AutoTimer($"    Analyze User Watch Data - Executing Commands", _embyInterfaces?._logger))
            {
                progress.Report(80);
                _dbHelper.ExecuteCommands(sqlCmds);
                progress.Report(100);
            }

            _embyInterfaces?._logger?.Debug($"AnalyzeUserWatchData - Finished User Watch Data Analysis");
        }

        (long, long) AnalyzeOverallTime(User? user, List<User>? userList)
        {
            CheckIsValid();

            if (user == null && userList == null)
                throw new ArgumentException("Either user or allUsers must be provided.");

            var (allVideosForUser, allVideos) = Statistics2026API.GetAllEpisodesAndMovies(user, _embyInterfaces!._libraryManager, true);

            long watchable = 0;
            long watched = 0;
            if (user == null && userList != null) // use the list of users
            {
                watched = allVideos.Where(video => userList.Any(u => _embyInterfaces!._userDataManager.GetUserData(u, video).Played)).Sum(item => item.RunTimeTicks ?? 0);
                watchable = allVideos.Sum(item => item.RunTimeTicks ?? 0);
            }
            else
            {
                watched = allVideosForUser.Where(video => _embyInterfaces!._userDataManager.GetUserData(user, video).Played).Sum(item => item.RunTimeTicks ?? 0);
                watchable = allVideosForUser.Sum(item => item.RunTimeTicks ?? 0);
            }

            return (watched, watchable);
        }


        private Dictionary<string, (bool hit, int playCount)> _baseCount = new Dictionary<string, (bool hit, int playCount)>()
                        {
                            { "Rocky", ( false, 100 ) },
                            { "Star Wars", ( false, 200) },
                            { "The Empire Strikes Back", ( false, 100) },
                            { "Return of the Jedi", ( false, 100) },
                            { "Captain America: The First Avenger", ( false, 100) },
                            { "Captain America: Civil War", ( false, 20) },
                            { "Avengers: Endgame", ( false, 30) },
                            { "Top Gun: Maverick", ( false, 30) },
                            { "Caddyshack", ( false, 30) },
                            { "Wonder Woman", ( false, 20) },
                            { "Avengers: Infinity War", ( false, 10) },
                            { "The Avengers", ( false, 10) },
                            { "Fight Club", ( false, 10) },
                            { "Harry Potter and The Sorcerer's Stone", ( false, 10 ) },
                            { "Harry Potter and The Philosopher's Stone", ( false, 10 ) },
                            { "Harry Potter and The Chamber of Secrets", ( false, 10 ) },
                            { "Harry Potter and The Goblet of Fire", ( false, 10 ) },
                            { "Harry Potter and The Prisoner of Azkaban", ( false, 10 ) },
                            { "Harry Potter and The Order of the Phoenix", ( false, 10 ) },
                            { "Harry Potter and The Half-Blood Prince", ( false, 10 ) },
                            { "Harry Potter and The Deathly Hallows: Part 1", ( false, 10 ) },
                            { "Harry Potter and The Deathly Hallows: Part 2", ( false, 10 ) },
                            { "Tropic Thunder", ( false, 8) },
                            { "Ready Player One", ( false, 5) },
                            { "Rudy", ( false, 5) },
                            { "Free Guy", ( false, 4) },
                            { "Baby Driver", ( false, 3) },

                            { "The Sopranos", ( false, 20) },
                            { "Band of Brothers", ( false, 15) },
                            { "Sons of Anarchy", ( false, 10) },
                            { "Seinfeld", ( false, 5) },
                            { "South Park", ( false, 5) },
                            { "Better Call Saul", ( false, 5) },
                            { "Entourage", ( false, 5) },
                            { "Reacher", ( false, 2) },
                            { "Silicon Valley", ( false, 2) },
                            { "House", ( false, 2) },
                            { "Mr. Robot", ( false, 2) },
                            { "Schoolhouse Rock!", ( false, 2) },
                        };
        private static bool ResetMapFixed = false;

        private void FixResetMap()
        {
            if (ResetMapFixed)
                return;
            var tmp = new Dictionary<string, (bool, int)>(_baseCount);
            _baseCount.Clear();
            foreach (var curr in tmp)
            {
                _baseCount[curr.Key.ToLower()] = curr.Value;
            }
            ResetMapFixed = true;
        }

        private void ValidateResetMapResults()
        {
            var config = Statistics2026.Plugin.Instance!.Configuration;
            if (!config.resetPlayCount)
                return;

            foreach (var curr in _baseCount)
            {
                if (curr.Value.hit == false)
                {
                    _embyInterfaces!._logger!.Warn($"Video {curr.Key} not hit for scott");
                }
            }
        }

        private void ResetPlayCount(User user, Video video, CancellationToken cancellationToken, ref bool isPlayed, ref int playCount)
        {
            if (Statistics2026.Plugin.Instance == null || Statistics2026.Plugin.Instance!.Configuration == null)
                return;

            var config = Statistics2026.Plugin.Instance.Configuration;
            if (!config.resetPlayCount)
                return;

            FixResetMap();

            bool? newIsPlayed = null;
            int? newPlayCount = null;
            DateTimeOffset? newLastPlayedDate = null;
            bool? newHideFromResume = null;
            bool? newFavorite = null;
            bool updateLastPlayedDate = false;

            var userData = _embyInterfaces!._userDataManager.GetUserData(user, video);
            if (userData == null)
                return;

            if (user.Policy.IsAdministrator)
            {
                newIsPlayed = true;
                newPlayCount = 0;
                newLastPlayedDate = null;
                updateLastPlayedDate = true;
                newHideFromResume = true;
                newFavorite = false;
            }
            else if (user.Name == "scott")
            {
                var name = video!.Name;
                var episode = video as Episode;
                if (episode != null)
                {
                    name = episode.Series.Name;
                }

                if (_baseCount.TryGetValue(name.ToLower(), out var pc))
                {
                    newPlayCount = pc.playCount;
                    _baseCount[name.ToLower()] = (true, pc.playCount);
                    newFavorite = true;
                }
                else
                {
                    if (playCount > 0 && !isPlayed)
                    {
                        newIsPlayed = true;
                        newPlayCount = 1;
                    }

                    newFavorite = false;
                }
            }
            else
            {
                if (playCount > 0 && !isPlayed)
                {
                    newIsPlayed = true;
                    newPlayCount = 1;
                }
            }
            if (user.Name == "amy")
            {
                var name = video!.Name;
                var episode = video as Episode;
                if (episode != null)
                {
                    name = episode.Series.Name;
                }

                if (!isPlayed && playCount > 0)
                {
                    newIsPlayed = true;
                    newPlayCount = playCount;
                }
                else if (name.StartsWith("Outlander"))
                    return;
            }

            bool update = false;
            if (newIsPlayed != null && (userData.Played != newIsPlayed.Value))
            {
                userData.Played = newIsPlayed.Value;
                isPlayed = newIsPlayed.Value;
                update = true;
            }

            if (newPlayCount != null && (userData.PlayCount != newPlayCount.Value))
            {
                userData.PlayCount = newPlayCount.Value;
                playCount = newPlayCount.Value;
                update = true;
            }

            if (updateLastPlayedDate)
            {
                var wasIsNull = (userData.LastPlayedDate == null);
                var nowIsNull = (newLastPlayedDate == null);
                if (wasIsNull != nowIsNull)
                {
                    userData.LastPlayedDate = newLastPlayedDate;
                    update = true;
                }
            }

            if (newHideFromResume != null && (userData.HideFromResume != newHideFromResume.Value))
            {
                userData.HideFromResume = newHideFromResume.Value;
                update = true;
            }

            if (newFavorite != null && (userData.IsFavorite != newFavorite.Value))
            {
                userData.IsFavorite = newFavorite.Value;
                update = true;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!update)
                return;

            _embyInterfaces._userDataManager.SaveUserData(user, video, userData, UserDataSaveReason.Import, cancellationToken);
        }

        private List<SQLCmdDef> AddUserWatchData(User? user, IProgress<double> progress, CancellationToken cancellationToken)
        {
            CheckIsValid();

            if (user == null)
                throw new ArgumentNullException("user");

            var userTableName = getUserTableName(user);
            if (_userMediaTemplate == null)
                throw new Exception($"AddUserWatchData: TableDef for UserMedia_<USER_ID> is null");

            var sqlCmds = new List<SQLCmdDef>();

            var cmds = _userMediaTemplate.GetSQLCommands(TableDef.EAction.eCreate);
            for (var ii = 0; ii < cmds.Count(); ++ii)
            {
                var sqlCmd = cmds[ii].Replace("UserMedia_<USER_ID>", userTableName);
                sqlCmds.Add(new SQLCmdDef(sqlCmd));
            }

            var allVideosForUser = Statistics2026API.GetAllEpisodesAndMovies(user, _embyInterfaces!._libraryManager, false).forUser;
            //_tableList
            string sql =
                $"INSERT INTO {userTableName} " +
                "(" +
                    "  UserId" +
                    ", ItemId" +
                    ", IsPlayed" +
                    ", Name" +
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
                    ", @Name" +
                    ", @PlayCount" +
                    ", @LastPlayedDate" +
                    ", @IsEpisode" +
                    ", @NumEpisodes" +
                    ", @IsTVSpecial" +
                    ", @SeriesId" +
                ") " +
                " ON CONFLICT(ItemId) " +
                " DO UPDATE " +
                " SET " +
                    "  IsPlayed=@IsPlayed" +
                    ", PlayCount=@PlayCount" +
                    ", LastPlayedDate=@LastPlayedDate" +
                " WHERE " +
                " ItemId=@ItemId"
                ;

            foreach (var video in allVideosForUser)
            {
                if (video == null)
                    continue;

                var isPlayed = video.Played;
                var playCount = video.PlayCount;

                ResetPlayCount(user, video, cancellationToken, ref isPlayed, ref playCount);
                var lastPlayedDate = video?.LastPlayedDate ?? null;

                using (var mediaInfo = new MediaInfo(video!))
                {
                    sqlCmds.Add(new SQLCmdDef(sql, new List<(string name, object? value)>()
                        {
                            ( "@UserId", user.Id.ToString()),
                            ( "@ItemId", video?.Id.ToString() ?? String.Empty),
                            ( "@Name", mediaInfo.PrimaryName),
                            ( "@IsEpisode", mediaInfo.IsEpisode),
                            ( "@NumEpisodes", mediaInfo.NumEpisodes),
                            ( "@IsTVSpecial", mediaInfo.IsTVSpecial),
                            ( "@IsPlayed", isPlayed),
                            ( "@PlayCount", playCount),
                            ( "@LastPlayedDate", _dbHelper.ToDateTimeParamValue( lastPlayedDate.HasValue ? lastPlayedDate.Value.DateTime : null )),
                            ( "@SeriesId", mediaInfo.SeriesId)
                        }));
                }
            }

            if (user.Name == "scott")
                ValidateResetMapResults();
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
                _embyInterfaces!._logger?.Error($"AddUser {user.Name}: is missing Id");
                return sqlCmds;
            }

            if (user.Name == null)
            {
                _embyInterfaces!._logger?.Error($"AddUser {user.Id.ToString()}: is missing Name");
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
                ") " +
                " ON CONFLICT(UserId) " +
                " DO UPDATE " +
                " SET " +
                "  UserName=@UserName" +
                " ,ConnectUserId=@ConnectUserId" +
                " ,IsAdministrator=@IsAdministrator" +
                " ,TotalTimeWatched=@TotalTimeWatched" +
                " ,TotalWatchableTime=@TotalWatchableTime"
                ;
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

            _embyInterfaces!._logger?.Debug($"AddAllMedia - Starting Video Analysis");

            progress.Report(0);
            var videoList = _dbHelper.GetLibraryItems<Episode>().Cast<Video>().ToList();
            progress.Report(50);
            videoList.AddRange(_dbHelper.GetLibraryItems<Movie>().Cast<Video>().ToList());
            progress.Report(100);

            double count = videoList.Count;
            double curr = 0.0;

            progress.Report(0);
            var sqlCmds = new List<SQLCmdDef>();
            var existing = new Dictionary<string, bool>();

            foreach (var video in videoList)
            {
                if (video == null)
                    continue;

                progress.Report(80.0 * (++curr) / count);

                if (existing.ContainsKey(video.Id.ToString()))
                    continue;
                existing.Add(video.Id.ToString(), true);


                using (var mediaInfo = new MediaInfo(video))
                {

                    sqlCmds.AddRange(AddMediaInfo(mediaInfo));
                    _embyInterfaces!._logger?.Debug($"AddAllMedia -     Processed Video ({curr} of {count}) - {mediaInfo.DescriptiveName}");
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
            progress.Report(80);
            _dbHelper.ExecuteCommands(sqlCmds);
            progress.Report(100);
            _embyInterfaces!._logger?.Debug($"AddAllMedia - Finished Video Analysis");
        }

        public List<SQLCmdDef> AddMediaInfo(MediaInfo mediaInfo)
        {
            CheckIsValid();

            var sqlCmds = new List<SQLCmdDef>();
            if (mediaInfo == null || mediaInfo.ItemId == null)
            {
                _embyInterfaces!._logger?.Error($"AddMediaInfo '{mediaInfo?.SortName}': is missing ItemId");
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
               ")" +
                " ON CONFLICT(ItemId) " +
                " DO UPDATE " +
                " SET " +
                    "  PrimaryName=@PrimaryName" +
                    ", SortName=@SortName" +
                    ", SecondaryName=@SecondaryName" +
                    ", StartYear=@StartYear" +
                    ", IsEpisode=@IsEpisode" +
                    ", IsTVSpecial=@IsTVSpecial" +
                    ", SeriesId=@SeriesId" +
                    ", Season=@Season" +
                    ", Episode=@Episode" +
                    ", NumEpisodes=@NumEpisodes" +
                    ", ResolutionBase=@ResolutionBase" +
                    ", ResolutionDetail=@ResolutionDetail" +
                    ", Codec=@Codec" +
                    ", DolbyVisionProfile=@DolbyVisionProfile" +
                    ", StudioNames =@StudioNames " +
                    ", Genres =@Genres " +
                    ", ServerLocation=@ServerLocation" +
                    ", FileSize=@FileSize" +
                    ", ImageUrl=@ImageUrl" +
                    ", RunTimeTicks=@RunTimeTicks" +
                    ", Rating=@Rating" +
                    ", TotalBitrate=@TotalBitrate" +
                    ", PremiereDate=@PremiereDate" +
                    ", DateAdded=@DateAdded"
                    ;
            ;

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
                ("@PremiereDate", _dbHelper.ToDateTimeParamValue( mediaInfo.PremiereDate ) ),
                ("@DateAdded", _dbHelper.ToDateTimeParamValue( mediaInfo.DateAdded ) ),
            }));

            return sqlCmds;
        }

        public void AddAllCollections(CancellationToken cancellationToken, IProgress<double> progress)
        {
            CheckIsValid();

            _embyInterfaces!._logger?.Debug($"AddAllCollections - Starting Collection Analysis");
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
                _embyInterfaces!._logger?.Debug($"AddAllCollections -     Processed Collection ({curr} of {count}) - {collection.Name} items processed");
            }
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(80);
            _dbHelper.ExecuteCommands(sqlCmds);
            progress.Report(100);
            _embyInterfaces!._logger?.Debug($"AddAllCollections - Finished Collection Analysis");
        }

        private List<SQLCmdDef> AddChildToCollection(Video video, BoxSet collection)
        {
            CheckIsValid();

            var sqlCmds = new List<SQLCmdDef>();

            if (video == null || collection == null)
            {
                _embyInterfaces!._logger?.Error($"AddChildToCollection video, collection must be set");
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

            _embyInterfaces!._logger?.Debug($"AddAllCollections - AddCollectionMembers -     Adding members of Collection - {collection.Name}");

            var query = new InternalItemsQuery
            {
                CollectionIds = new[] { collection.InternalId },
                Recursive = true
            };

            var baseItems = _embyInterfaces._libraryManager.GetItemList(query);
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
            _embyInterfaces!._logger?.Debug($"AddAllCollections - AddCollectionMembers -     Finished Adding {videos.Count} members of Collection {collection.Name} ");
            return sqlCmds;
        }

        public List<SQLCmdDef> AddCollection(BoxSet collection, CancellationToken cancellationToken, IProgress<double> progress)
        {
            CheckIsValid();

            var sqlCmds = new List<SQLCmdDef>();
            if (collection.Id == null)
            {
                _embyInterfaces!._logger?.Error($"AddCollection {collection.SortName}: is missing ItemId");
                return sqlCmds;
            }

            _embyInterfaces!._logger?.Debug($"AddAllCollections - AddCollection - Adding Collection {collection.Name}");

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
                ")" +
                " ON CONFLICT(ItemId) " +
                " DO UPDATE " +
                " SET " +
                "  ItemId=@ItemId" +
                ", Name=@Name" +
                ", SortName=@SortName"
                ;
            sqlCmds.Add(new SQLCmdDef(sql, new List<(string name, object? value)>()
            {
                ("@ItemId", collection.Id.ToString()),
                ("@Name", collection.Name),
                ("@SortName", collection.SortName),
            }));
            _embyInterfaces!._logger?.Debug($"AddAllCollections -     AddCollection - Successfully Added Collection");

            sqlCmds.AddRange(AddCollectionMembers(collection, cancellationToken, progress));
            return sqlCmds;
        }

        public void AddAllSeries(CancellationToken cancellationToken, IProgress<double> progress)
        {
            CheckIsValid();

            _embyInterfaces!._logger?.Debug($"AddAllSeries- Starting Video Analysis");

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

                _embyInterfaces!._logger?.Debug($"AddAllSeries -     Processed Series ({curr} of {count}) - {series.Name}");
            }

            progress.Report(80);
            _dbHelper.ExecuteCommands(sqlCmds);
            progress.Report(100);
            cancellationToken.ThrowIfCancellationRequested();
            _embyInterfaces!._logger?.Debug($"AddAllSeries - Finished Video Analysis");
        }

        private int GetCountForSeries(Series series, CancellationToken cancellationToken, bool episodes)
        {
            CheckIsValid();

            var libraryOptions = _embyInterfaces!._libraryManager.GetLibraryOptions(series);
            var allEpisodes = _embyInterfaces!._providerManager.GetAllEpisodes(series, libraryOptions, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();
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
                _embyInterfaces!._logger?.Error($"AddSeries {series.SortName}: is missing ItemId");
                return sqlCmds;
            }

            _embyInterfaces!._logger?.Debug($"AddAllSeries -    AddSeries - Adding Series {series.Name}");

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
                ")" +
                " ON CONFLICT(ItemId) " +
                " DO UPDATE " +
                " SET " +
                    "  Name=@Name" +
                    ", SortName=@SortName" +
                    ", PremiereDate=@PremiereDate" +
                    ", NumEpisodes=@NumEpisodes" +
                    ", NumSpecials=@NumSpecials" +
                    ", DateAdded=@DateAdded" +
                    ", ImageUrl=@ImageUrl" +
                    ", FileSize=@FileSize" +
                    ", RunTimeTicks=@RunTimeTicks" +
                    ", Rating=@Rating" +
                    ", Status=@Status" +
                    ", AverageBitrate=@AverageBitrate";

                paramsList = new List<(string name, object? value)>()
                       {
                           ("@ItemId", series.Id.ToString()),
                           ("@Name", series.Name),
                           ("@SortName", series.SortName),
                           ("@PremiereDate", _dbHelper.ToDateTimeParamValue( series.PremiereDate.HasValue ? series.PremiereDate.Value.DateTime : null )),
                           ("@NumEpisodes", numEpisodes),
                           ("@NumSpecials", numSpecials),
                           ("@DateAdded", _dbHelper.ToDateTimeParamValue( series.DateCreated.DateTime )),
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

            _embyInterfaces!._logger?.Debug($"AddAllSeries -    AddSeries - Successfully Added Series {series.Name}");
            return sqlCmds;
        }
    }
}
