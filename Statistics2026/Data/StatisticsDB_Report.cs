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
        private string CheckForPlural(string value, decimal number, string starting = "", string ending = "", bool removeZero = true)
        {
            if (number == 1)
                return $" {starting} {number} {value} {ending}";
            if (number == 0 && removeZero)
                return "";
            return $" {starting} {number} {value}s {ending}";
        }

        private string CheckMaxLength(string value)
        {
            return value.Length > 30 ? value.Substring(0, 27) + "..." : value;
        }

        private string TimeSince(System.DateTime date)
        {

            var yearDiff = (DateTime.Now.Year - date.Year);
            var monthDiff = (DateTime.Now.Month - date.Month);

            var numberOfTotalMonths = (yearDiff * 12) + monthDiff;
            if (numberOfTotalMonths > 3)
            {
                var numberOfYears = Math.Floor(numberOfTotalMonths / (decimal)12);
                var numberOfMonth = Math.Floor((numberOfTotalMonths / (decimal)12 - numberOfYears) * 12);
                return $"{CheckForPlural("year", numberOfYears, "", "", false)} {CheckForPlural("month", numberOfMonth, "and")} ago";
            }
            else
            {
                var numberOfDays = DateTime.Now.Date - date;
                if (numberOfDays.Days == 0)
                    return $"Today";
                else
                    return $"{CheckForPlural("day", numberOfDays.Days, "", "", false)} ago";
            }
        }

        public ValueGroup Movie(User user, WhichMovie whichMovie)
        {
            string title = "";
            string help = "";

            string orderClause = "";
            string whereClause = "";
            switch (whichMovie)
            {
                case WhichMovie.Largest:
                    orderClause = "FileSize DESC";
                    title = Constants.BiggestMovie;
                    break;
                case WhichMovie.Smallest:
                    orderClause = "FileSize ASC";
                    title = Constants.SmallestMovie;
                    break;
                case WhichMovie.Longest:
                    orderClause = "RunTimeTicks DESC";
                    title = Constants.LongestMovie;
                    break;
                case WhichMovie.Shortest:
                    orderClause = "RunTimeTicks ASC";
                    title = Constants.ShortestMovie;
                    break;
                case WhichMovie.HighestRated:
                    orderClause = "Rating DESC";
                    title = Constants.HighestRatedMovie;
                    break;
                case WhichMovie.LowestRated:
                    orderClause = "Rating ASC";
                    whereClause = "(Rating > 0)";
                    title = Constants.LowestRatedMovie;
                    break;
                case WhichMovie.HighestBitrate:
                    orderClause = "TotalBitrate DESC";
                    title = Constants.HighestBitrateMovie;
                    break;
                case WhichMovie.LowestBitrate:
                    orderClause = "TotalBitrate ASC";
                    title = Constants.LowestBitrateMovie;
                    break;
                case WhichMovie.LatestPremiereDate:
                    orderClause = "PremiereDate DESC";
                    whereClause = "(PremiereDate IS NOT NULL AND PremiereDate != '')";
                    title = Constants.LatestMoviePremiere;
                    break;
                case WhichMovie.OldestPremiereDate:
                    orderClause = "PremiereDate ASC";
                    whereClause = "(PremiereDate IS NOT NULL AND PremiereDate != '')";
                    title = Constants.OldestMoviePremiere;
                    break;
                case WhichMovie.LatestMovieAdded:
                    orderClause = "DateAdded DESC";
                    whereClause = "(DateAdded IS NOT NULL AND DateAdded != '')";
                    title = Constants.LatestMovieAddition;
                    break;
                case WhichMovie.OldestMovieAdded:
                    orderClause = "DateAdded ASC";
                    whereClause = "(DateAdded IS NOT NULL AND DateAdded != '')";
                    title = Constants.OldestMovieAdded;
                    break;
                default:
                    return new ValueGroup();
            }

            string sql = "SELECT "
                + "   ItemId"
                + ", PrimaryName"
                + ", ImageUrl"
                + ", FileSize"
                + ", RunTimeTicks"
                + ", Rating"
                + ", TotalBitrate"
                + ", PremiereDate"
                + ", DateAdded"
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
                        switch (whichMovie)
                        {
                            case WhichMovie.Smallest:
                            case WhichMovie.Largest:
                                {
                                    long maxSize = row.GetInt64(3);
                                    maxSize /= (1024 * 1024 * 1024); // in GB;
                                    value = $"{maxSize:F1} Gb";
                                }
                                break;
                            case WhichMovie.Longest:
                            case WhichMovie.Shortest:
                                {
                                    long runTimeTicks = row.GetInt64(4);
                                    value = new TimeSpan(runTimeTicks).ToString(@"hh\:mm\:ss");
                                }
                                break;
                            case WhichMovie.HighestRated:
                            case WhichMovie.LowestRated:
                                {
                                    var rating = row.GetFloat(5).ToString("F1");
                                    value = $"{rating} / 10";
                                }
                                break;
                            case WhichMovie.HighestBitrate:
                            case WhichMovie.LowestBitrate:
                                {
                                    var bitrate = Math.Round((decimal)row.GetInt64(6) / 1000);
                                    value = $"{bitrate:N0} Kbps";
                                }
                                break;
                            case WhichMovie.OldestPremiereDate:
                            case WhichMovie.LatestPremiereDate:
                                {
                                    var premiereDate = DateTime.ParseExact(row.GetString(7), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                                    value = premiereDate.ToShortDateString();

                                    secondValue = TimeSince(premiereDate);
                                }
                                break;
                            case WhichMovie.OldestMovieAdded:
                            case WhichMovie.LatestMovieAdded:
                                {
                                    var premiereDate = DateTime.ParseExact(row.GetString(8), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                                    value = premiereDate.ToShortDateString();

                                    secondValue = TimeSince(premiereDate);
                                }
                                break;

                            default:
                                value = "";
                                break;
                        }
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
    }
}
