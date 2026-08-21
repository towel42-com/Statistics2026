using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using ServiceStack;
using ServiceStack.Text;
using SQLitePCL.pretty;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;



namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {

        public ValueGroup MediaResolutions(bool showAllResolutions)
        {
            string sql =
                "SELECT " +
                "ResolutionBase as Resolution, " +
                "sum(IsEpisode) AS Episodes, " +
                "sum(NOT IsEpisode) AS Movies " +
                "FROM Media " +
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

            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement(sql))
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

        public ValueGroup MediaCodecs(bool showAllCodecs)
        {
            string sql =
                "SELECT " +
                "Codec as Codec, " +
                "sum(IsEpisode) AS Episodes, " +
                "sum(NOT IsEpisode) AS Movies " +
                "FROM Media " +
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

            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement(sql))
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
        public ValueGroup DVProfileInfo(bool showUnknownDVProfiles, bool showAllDVProfiles)
        {
            string sql =
                "SELECT " +
                "DolbyVisionProfile as DVProfile, " +
                "sum(IsEpisode) AS Episodes, " +
                "sum(NOT IsEpisode) AS Movies " +
                "FROM Media ";

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
            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement(sql))
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

        private string GetSingleValueFromSQL(string sql)
        {
            lock (sql)
            {
                using (var statement = _connection.PrepareStatement(sql))
                {
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        var count = row.GetInt(0);
                        return count.ToString();
                    }
                }
            }
            return "";
        }

        private ValueGroup ValueGroupForSingleItem(string title, string help, string sql)
        {
            var retVal = new ValueGroup(title, help, null, "small");
            var value = GetSingleValueFromSQL(sql);
            retVal.ValueLineTwo = value;
            return retVal;
        }

        public ValueGroup UserCount(bool hasConnectUserID, bool excludeAdmin, IUserManager userManager)
        {
            string sql = "SELECT COUNT(UserName) FROM Users ";

            List<string> conditions = new List<string>();

            if (hasConnectUserID)
                conditions.Add("( ConnectUserId <> \"\" AND ConnectUserId IS NOT NULL )");

            if (excludeAdmin)
                conditions.Add("( NOT IsAdministrator )");

            if (conditions.Count > 0)
                sql += "WHERE " + string.Join(" AND ", conditions) + " ";

            return ValueGroupForSingleItem(Constants.TotalUsers, null, sql);
        }

        public ValueGroup MostActiveUsers(bool hasConnectUserID, int numUsers, bool excludeAdmin, IUserManager userManager)
        {
            string sql =
                "SELECT " +
                "UserName, " +
                "TotalTimeWatched " +
                "FROM Users ";
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
            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement(sql))
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

        public ValueGroup TotalMovieCount(User user)
        {
            string sql = "SELECT SUM(NOT IsEpisode) FROM Media";

            return ValueGroupForSingleItem(Constants.TotalMovies, Constants.HelpTotalMovies, sql);
        }

        public ValueGroup TotalTVCount(User user)
        {
            string sql = "SELECT COUNT(DISTINCT(PrimaryName)) FROM Media WHERE IsEpisode";
            var retVal = ValueGroupForSingleItem(Constants.TotalTVShows, Constants.HelpTotalTVShows, sql);

            retVal.ValueLineThree = Constants.TotalTVEpisodes;
            retVal.ValueLineFour = GetSingleValueFromSQL("SELECT SUM(IsEpisode) FROM Media");

            return retVal;
        }

        public ValueGroup TotalCollectionCount(User user)
        {
            string sql = "SELECT COUNT( ItemId ) FROM Collections";

            return ValueGroupForSingleItem(Constants.TotalCollections, Constants.HelpTotalCollections, sql);
        }

        public ValueGroup TotalStudioCount(User user, bool movies )
        {
            string sql = "SELECT DISTINCT StudioNames FROM Media WHERE ";
            if (movies)
                sql += "NOT ";
            sql += "IsEpisode AND StudioNames IS NOT NULL AND StudioNames<>\"\"";

            var retVal = new ValueGroup(movies?Constants.TotalStudios:Constants.TotalTVNetworks, movies?Constants.HelpTotalStudios:Constants.HelpTotalTVNetworks, null, "small");
            // Create an unordered set of strings
            HashSet<string> studios = new HashSet<string>();

            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement(sql))
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

        public ValueGroup TotalMovieStudioCount(User user)
        {
            return TotalStudioCount(user, true);
        }
        public ValueGroup TotalTVStudioCount(User user)
        {
            return TotalStudioCount(user, false);
        }
        private string CheckMaxLength(string value)
        {
            return value.Length > 30 ? value.Substring(0, 27) + "..." : value;
        }

        public ValueGroup Movie(User user, WhichStatistic.Statistic whichStatistic)
        {
            string title = WhichStatistic.Title(whichStatistic, WhichStatistic.VideoType.Movie);
            string help = WhichStatistic.Help(whichStatistic, WhichStatistic.VideoType.Movie);

            string fieldName = WhichStatistic.FieldFor(whichStatistic, WhichStatistic.VideoType.Movie );
            string orderClause = WhichStatistic.OrderClause(whichStatistic);
            string whereClause = WhichStatistic.WhereClause(whichStatistic);

            string sql = "SELECT "
                + "   ItemId"
                + ", PrimaryName"
                + ", ImageUrl"
                + $", {fieldName}"
                + " FROM Media "
                + "WHERE NOT IsEpisode ";
            if (!whereClause.IsNullOrEmpty())
                sql += $"AND {whereClause} ";

            sql += $"ORDER BY {orderClause} LIMIT 1";

            var retVal = new ValueGroup(title, help, null, "half");

            string value = "";
            string secondValue = "";
            string name = "";
            string itemId = "";
            string imageUrl = "";
            lock (_connection)
            {
                using (var statement = _connection.PrepareStatement(sql))
                {
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        itemId = row.GetString(0);
                        name = row.GetString(1);
                        imageUrl = row.GetString(2);
                        value = WhichStatistic.Value(whichStatistic, row, 3);
                        secondValue = WhichStatistic.SecondValue(whichStatistic, row, 3);
                        break;
                    }
                }
            }
            retVal.ValueLineTwo = CheckMaxLength(value);
            if (secondValue.IsNullOrEmpty())
                retVal.ValueLineThree = CheckMaxLength(name);
            else
            {
                retVal.ValueLineThree = secondValue;
                retVal.ValueLineFour = CheckMaxLength(name);
            }
            retVal.ImageUrl = imageUrl;
            retVal.MediaItemId = itemId;
            return retVal;
        }

        public ValueGroup Series(User user, WhichStatistic.Statistic whichStatistic)
        {
            string title = WhichStatistic.Title(whichStatistic, WhichStatistic.VideoType.Series);
            string help = WhichStatistic.Help(whichStatistic, WhichStatistic.VideoType.Series);

            string fieldName = WhichStatistic.FieldFor(whichStatistic, WhichStatistic.VideoType.Series);
            string orderClause = WhichStatistic.OrderClause(whichStatistic);
            string whereClause = WhichStatistic.WhereClause(whichStatistic);

            string sql = "SELECT "
                + "   ItemId"
                + ", PrimaryName"
                + ", ImageUrl"
                + $", {fieldName}"
                + " FROM Media "
                + "WHERE IsEpisode ";
            if (!whereClause.IsNullOrEmpty())
                sql += $"AND {whereClause} ";

            sql += "GROUP BY PrimaryName ";
            sql += $"ORDER BY {orderClause} LIMIT 1";

            var retVal = new ValueGroup(title, help, null, "half");

            string value = "";
            string secondValue = "";
            string name = "";
            string itemId = "";
            string imageUrl = "";
            lock (_connection)
            {
                try
                {
                    using (var statement = _connection.PrepareStatement(sql))
                    {
                        while (statement.MoveNext())
                        {
                            var row = statement.Current;
                            itemId = row.GetString(0);
                            name = row.GetString(1);
                            imageUrl = row.GetString(2);
                            value = WhichStatistic.Value(whichStatistic, row, 3);
                            secondValue = WhichStatistic.SecondValue(whichStatistic, row, 3);
                            break;
                        }
                    }
                } 
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            retVal.ValueLineTwo = CheckMaxLength(value);
            if (secondValue.IsNullOrEmpty())
                retVal.ValueLineThree = CheckMaxLength(name);
            else
            {
                retVal.ValueLineThree = secondValue;
                retVal.ValueLineFour = CheckMaxLength(name);
            }
            retVal.ImageUrl = imageUrl;
            retVal.MediaItemId = itemId;
            return retVal;
        }
    }
}
