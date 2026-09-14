using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        private void CheckIsValid(bool inInit = false)
        {
            if (inInit)
                _dbHelper.CheckIsValid(ECheckLevel.eInterfaces | ECheckLevel.eThrowOnFailure);
            else
                _dbHelper.CheckIsValid(ECheckLevel.eAllWithThrow);

            if (Statistics2026.Plugin.Instance == null)
                throw new ArgumentNullException("Statistics2026.Plugin.Instance");

            if (Statistics2026.Plugin.Instance.Configuration == null)
                throw new ArgumentNullException("Statistics2026.Plugin.Instance.Configuration");

        }

        public void AddAllUsers()
        {
            CheckIsValid();

            _dbHelper!.Progress?.Report(0);
            var users = _embyInterfaces?._userManager.GetUserList(new UserQuery() { EnableRemoteAccess = true }).ToList();
            if (users == null)
                return;

            _dbHelper!.Progress?.Report(100);

            _embyInterfaces?._logger?.Debug($"AddAllUsers - Starting User Analysis");
            double count = users.Count;
            double curr = 0;

            _dbHelper!.Progress?.Report(0);
            var sqlCmds = new List<SQLCmdDef>();
            using (var timer = new AutoTimer($"    Adding All Users - Getting Commands", _embyInterfaces?._logger))
            {
                foreach (var user in users)
                {
                    _dbHelper!.Progress?.Report(80.0 * (++curr) / count);
                    using (var userTimer = new AutoTimer($"AddAllUsers -     Processed User ({curr} of {count}) - {user.Name}", _embyInterfaces?._logger))
                    {
                        sqlCmds.AddRange(AddUser(user));
                        _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();
                    }
                }
                _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();
            }

            using (var timer = new AutoTimer($"    Adding All Users - Executing Commands", _embyInterfaces?._logger))
            {
                _dbHelper!.Progress?.Report(80);
                _dbHelper.ExecuteCommands(sqlCmds);
                _dbHelper!.Progress?.Report(100);
            }

            _embyInterfaces?._logger?.Debug($"AddAllUsers - Finished User Analysis");
        }

        public void InitUserWatchData()
        {
            CheckIsValid(true);

            _dbHelper!.Progress?.Report(0);
            var users = _embyInterfaces?._userManager.GetUserList(new UserQuery() { EnableRemoteAccess = true }).ToList();
            if (users == null)
            {
                _dbHelper!.Progress?.Report(100);
                return;
            }
            _dbHelper!.Progress?.Report(100);

            var sqlCmds = new List<SQLCmdDef>();
            double curr = 0.0;
            double count = users.Count;

            using (var timer = new AutoTimer($"    Analyze User Watch Data - Getting Init Table Commands", _embyInterfaces?._logger))
            {
                curr = 0;
                foreach (var user in users)
                {
                    _dbHelper!.Progress?.Report(80.0 * (++curr) / count);
                    using (var userTimer = new AutoTimer($"AnalyzeUserWatchData -     Processed User ({curr} of {count}) - {user.Name}", _embyInterfaces?._logger))
                    {
                        sqlCmds.AddRange(GetInitTableCommands(user));
                        _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();
                    }
                }
                _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();
            }

            using (var timer = new AutoTimer($"    Analyze User Watch Data - Executing Init Table Commands", _embyInterfaces?._logger))
            {
                _dbHelper!.Progress?.Report(80);
                _dbHelper.ExecuteCommands(sqlCmds);
                _dbHelper!.Progress?.Report(100);
            }
        }

        public void AnalyzeUserWatchData()
        {
            CheckIsValid();

            _dbHelper!.Progress?.Report(0);
            var users = _embyInterfaces?._userManager.GetUserList(new UserQuery() { EnableRemoteAccess = true }).ToList();
            if (users == null)
                return;
            _dbHelper!.Progress?.Report(100);

            _embyInterfaces?._logger?.Debug($"AnalyzeUserWatchData - Starting User Watch Data Analysis");

            double count = users.Count;
            double curr = 0;

            _dbHelper!.Progress?.Report(0);
            var sqlCmds = new List<SQLCmdDef>();
            using (var timer = new AutoTimer($"    Analyze User Watch Data - Getting Commands", _embyInterfaces?._logger))
            {
                curr = 0;
                foreach (var user in users)
                {
                    _dbHelper!.Progress?.Report(80.0 * (++curr) / count);
                    using (var userTimer = new AutoTimer($"AnalyzeUserWatchData -     Processed User ({curr} of {count}) - {user.Name}", _embyInterfaces?._logger))
                    {
                        sqlCmds.AddRange(AddUserWatchData(user));
                        _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();
                    }
                }
                _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();
            }

            var config = Statistics2026.Plugin.Instance!.Configuration;
            config.resetPlayCount = false;
            Statistics2026.Plugin.Instance.UpdateConfiguration(config);

            using (var timer = new AutoTimer($"    Analyze User Watch Data - Executing Commands", _embyInterfaces?._logger))
            {
                _dbHelper!.Progress?.Report(80);
                _dbHelper.ExecuteCommands(sqlCmds);
                _dbHelper!.Progress?.Report(100);
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

        private void ResetPlayCount(User user, Video video, ref bool isPlayed, ref int playCount)
        {
            CheckIsValid();

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

            _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();

            if (!update)
                return;

            if (_dbHelper.CancellationToken == null)
                return;

            CancellationToken token = _dbHelper!.CancellationToken.Value;

            _embyInterfaces._userDataManager.SaveUserData(user, video, userData, UserDataSaveReason.Import, token);
        }

        public List<SQLCmdDef> GetInitTableCommands(User? user)
        {
            if (_userMediaTemplate == null)
                throw new Exception($"GetInitTableCommands: TableDef for UserMedia_<USER_ID> is null");

            if (user == null)
                throw new Exception($"GetInitTableCommands: User is null");

            var userTableName = getUserTableName(user);

            var sqlCmds = new List<SQLCmdDef>();

            var cmds = _userMediaTemplate.GetSQLCommands(TableDef.EAction.eCreate);
            for (var ii = 0; ii < cmds.Count(); ++ii)
            {
                var cmd = new SQLCmdDef(cmds[ii]);
                cmd.Replace("UserMedia_<USER_ID>", userTableName);
                sqlCmds.Add(cmd);
            }

            return sqlCmds;
        }

        private List<SQLCmdDef> AddUserWatchData(User? user)
        {
            CheckIsValid();

            if (user == null)
                throw new ArgumentNullException("user");

            var userTableName = getUserTableName(user);
            if (_userMediaTemplate == null)
                throw new Exception($"AddUserWatchData: TableDef for UserMedia_<USER_ID> is null");

            var sqlCmds = new List<SQLCmdDef>();

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

                ResetPlayCount(user, video, ref isPlayed, ref playCount);
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

            var config = Statistics2026.Plugin.Instance!.Configuration;
            if (config.resetPlayCount)
            {
                var sqlUpdateTicks =
                    $"UPDATE {userTableName} " +
                    $" SET " +
                    $"  TotalTicksPlayed=PlayCount*Media.RunTimeTicks " +
                    $" FROM Media " +
                    $" WHERE Media.ItemId={userTableName}.ItemId"
                    ;
                sqlCmds.Add(new SQLCmdDef(sqlUpdateTicks));
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

    }
}
