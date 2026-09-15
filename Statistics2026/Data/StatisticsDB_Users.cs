using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack;
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
        public void AddAllUsers()
        {
            CheckIsValid(ECheckType.eUpdate);

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

        private (long watched, long watchable) AnalyzeOverallTime(User? user, List<User>? userList)
        {
            CheckIsValid(ECheckType.eNone);

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

        private List<SQLCmdDef> AddUser(User? user)
        {
            CheckIsValid(ECheckType.eUpdate);

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

        public long NumUsers(bool hasConnectUserId, bool excludeAdmin)
        {
            CheckIsValid(ECheckType.eReport);

            string sql = "SELECT COUNT(UserName) FROM Users ";

            List<string> conditions = new List<string>();

            if (hasConnectUserId)
                conditions.Add("ConnectUserId <> '' AND ConnectUserId IS NOT NULL");

            if (excludeAdmin)
                conditions.Add("NOT IsAdministrator");

            sql += DBHelper.JoinClauses(conditions);

            return GetSingleValueFromSQL(sql).ToInt64();
        }

        public long NumUsers()
        {
            CheckIsValid(ECheckType.eReport);

            return NumUsers(Statistics2026.Plugin.Instance!.Configuration.hasConnectUserID, Statistics2026.Plugin.Instance!.Configuration.excludeAdmin);
        }

        public StatCard UserCount()
        {
            var numUsers = NumUsers();
            return ValueGroupForSingleValue(Constants.TotalUsers, null, numUsers);
        }

        public StatCard MostActiveUsers()
        {
            CheckIsValid(ECheckType.eReport);

            string sql =
                "SELECT " +
                "UserName, " +
                "TotalTimeWatched " +
                "FROM Users ";
            List<string> conditions = new List<string>();

            if (Statistics2026.Plugin.Instance!.Configuration.hasConnectUserID)
                conditions.Add("ConnectUserId <> '' AND ConnectUserId IS NOT NULL");

            if (Statistics2026.Plugin.Instance!.Configuration.excludeAdmin)
                conditions.Add("NOT IsAdministrator");

            sql += DBHelper.JoinClauses(conditions);

            var numUsers = Plugin.Instance.Configuration.numMostActiveUsers;
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
    }
}
