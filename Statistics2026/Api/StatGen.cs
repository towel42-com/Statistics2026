using SQLitePCL;
using SQLitePCL.pretty;
using Statistics2026.Data;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;

namespace Statistics2026.Api
{
    public class StatGen
    {
        public enum EVideoType
        {
            Movie,
            Series,
            Episode
        };

        public enum EStatisticType
        {
            Largest,
            Smallest,
            Longest,
            Shortest,
            OldestPremiereDate,
            LatestPremiereDate,
            HighestRated,
            LowestRated,
            OldestAddition,
            LatestAddition,
            HighestBitrate,
            LowestBitrate
        }

        private EStatisticType WhichStatistic { get; set; }
        private EVideoType VideoType { get; set; }

        private IDatabaseConnection _connection { get; set; }
        public StatGen(EStatisticType statType, EVideoType videoType, IDatabaseConnection connection)
        {
            WhichStatistic = statType;
            VideoType = videoType;
            _connection = connection;
        }

        public StatCard GetStatCard()
        {
            string sql = SQL();
            string title = Title();
            string help = Help();

            var retVal = new TextBasedStatCard(title, help, "half");

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
                            value = Value(row, 3);
                            secondValue = SecondValue(row, 3);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            retVal.AddLine(value);
            if (string.IsNullOrEmpty(secondValue))
                retVal.AddLine(name);
            else
            {
                retVal.AddLine(secondValue);
                retVal.AddLine(name);
            }
            retVal.ImageUrl = imageUrl;
            retVal.MediaItemId = itemId;
            return retVal;
        }

        private string SQL()
        {
            string nameField = NameField();
            string fieldName = FieldFor();
            string orderClause = OrderClause();
            string tableName = TableName();
            string whereClause = WhereClause();
            string retVal = "SELECT "
                + $"  ItemId"
                + $", {nameField}"
                + $", ImageUrl"
                + $", {fieldName}"
                + $" FROM {tableName} "
                + $" {whereClause} "
                + $" ORDER BY {orderClause} LIMIT 1"
                ;
            return retVal;
        }
        private string NameField()
        {
            if (VideoType == EVideoType.Movie)
                return "PrimaryName";
            else if (VideoType == EVideoType.Episode)
                return "PrimaryName";
            else // if (VideoType == EVideoType.Series)
                return "Name";
        }
        private string TableName()
        {
            if (VideoType == EVideoType.Movie)
                return "Media";
            else if (VideoType == EVideoType.Episode)
                return "Media";
            else // if (videoType == EVideoType.Series)
                return "Series";
        }

        private string FieldFor()
        {
            string fieldName = "";
            switch (WhichStatistic)
            {
                case EStatisticType.Largest:
                case EStatisticType.Smallest:
                    fieldName = "FileSize";
                    break;
                case EStatisticType.Longest:
                case EStatisticType.Shortest:
                    fieldName = "RunTimeTicks";
                    break;
                case EStatisticType.HighestRated:
                case EStatisticType.LowestRated:
                    fieldName = "Rating";
                    break;
                case EStatisticType.HighestBitrate:
                case EStatisticType.LowestBitrate:
                    {
                        if (VideoType == EVideoType.Movie)
                            fieldName = "TotalBitrate";
                        else if (VideoType == EVideoType.Series)
                            fieldName = "AverageBitrate AS TotalBitrate";
                        else
                            throwHandleEpisode();
                    }
                    break;
                case EStatisticType.LatestPremiereDate:
                case EStatisticType.OldestPremiereDate:
                    fieldName = "PremiereDate";
                    break;
                case EStatisticType.LatestAddition:
                case EStatisticType.OldestAddition:
                    fieldName = "DateAdded";
                    break;
                default:
                    break;
            }
            return fieldName;
        }

        private string OrderClause()
        {
            string orderClause = "";
            switch (WhichStatistic)
            {
                case EStatisticType.Largest:
                    orderClause = "FileSize DESC";
                    break;
                case EStatisticType.Smallest:
                    orderClause = "FileSize ASC";
                    break;
                case EStatisticType.Longest:
                    orderClause = "RunTimeTicks DESC";
                    break;
                case EStatisticType.Shortest:
                    orderClause = "RunTimeTicks ASC";
                    break;
                case EStatisticType.HighestRated:
                    orderClause = "Rating DESC";
                    break;
                case EStatisticType.LowestRated:
                    orderClause = "Rating ASC";
                    break;
                case EStatisticType.HighestBitrate:
                    orderClause = "TotalBitrate DESC";
                    break;
                case EStatisticType.LowestBitrate:
                    orderClause = "TotalBitrate ASC";
                    break;
                case EStatisticType.LatestPremiereDate:
                    orderClause = "PremiereDate DESC";
                    break;
                case EStatisticType.OldestPremiereDate:
                    orderClause = "PremiereDate ASC";
                    break;
                case EStatisticType.LatestAddition:
                    orderClause = "DateAdded DESC";
                    break;
                case EStatisticType.OldestAddition:
                    orderClause = "DateAdded ASC";
                    break;
                default:
                    break;
            }
            return orderClause;
        }

        private void throwHandleEpisode()
        {
            throwHandleEpisode();
        }

        private string WhereClause()
        {
            var whereClauseList = new List<string>();
            switch (WhichStatistic)
            {
                case EStatisticType.Largest:
                case EStatisticType.Smallest:
                case EStatisticType.Longest:
                case EStatisticType.Shortest:
                case EStatisticType.HighestBitrate:
                case EStatisticType.LowestBitrate:
                    break;
                case EStatisticType.HighestRated:
                case EStatisticType.LowestRated:
                    whereClauseList.Add("(Rating > 0)");
                    break;
                case EStatisticType.OldestPremiereDate:
                case EStatisticType.LatestPremiereDate:
                    whereClauseList.Add("(PremiereDate IS NOT NULL AND PremiereDate != '')");
                    break;
                case EStatisticType.LatestAddition:
                case EStatisticType.OldestAddition:
                    whereClauseList.Add("(DateAdded IS NOT NULL AND DateAdded != '')");
                    break;
                default:
                    break;
            }

            if (VideoType == EVideoType.Movie)
                whereClauseList.Add("NOT IsEpisode");
            else if (VideoType == EVideoType.Episode)
                whereClauseList.Add("IsEpisode");

            for (int ii = 0; ii < whereClauseList.Count; ++ii)
            {
                whereClauseList[ii] = $"( {whereClauseList[ii]} )";
            }

            if (whereClauseList.Count > 0)
                return "WHERE " + string.Join(" AND ", whereClauseList) + " ";
            return "";
        }

        private string Title()
        {
            string title = "";
            switch (WhichStatistic)
            {
                case EStatisticType.Largest:
                    {
                        if (VideoType == EVideoType.Movie)
                            title = Constants.BiggestMovie;
                        else if (VideoType == EVideoType.Series)
                            title = Constants.BiggestSeries;
                        else
                            throwHandleEpisode();
                    }
                    break;
                case EStatisticType.Smallest:
                    {
                        if (VideoType == EVideoType.Movie)
                            title = Constants.SmallestMovie;
                        else if (VideoType == EVideoType.Series)
                            title = Constants.SmallestSeries;
                        else
                            throwHandleEpisode();
                    }

                    break;
                case EStatisticType.Longest:
                    {
                        if (VideoType == EVideoType.Movie)
                            title = Constants.LongestMovie;
                        else if (VideoType == EVideoType.Series)
                            title = Constants.LongestSeries;
                        else
                            throwHandleEpisode();
                    }
                    break;
                case EStatisticType.Shortest:
                    {
                        if (VideoType == EVideoType.Movie)
                            title = Constants.ShortestMovie;
                        else if (VideoType == EVideoType.Series)
                            title = Constants.ShortestSeries;
                        else
                            throwHandleEpisode();
                    }
                    break;
                case EStatisticType.HighestRated:
                    {
                        if (VideoType == EVideoType.Movie)
                            title = Constants.HighestRatedMovie;
                        else if (VideoType == EVideoType.Series)
                            title = Constants.HighestRatedSeries;
                        else
                            throwHandleEpisode();
                    }
                    break;
                case EStatisticType.LowestRated:
                    {
                        if (VideoType == EVideoType.Movie)
                            title = Constants.LowestRatedMovie;
                        else if (VideoType == EVideoType.Series)
                            title = Constants.LowestRatedSeries;
                        else
                            throwHandleEpisode();
                    }
                    break;
                case EStatisticType.HighestBitrate:
                    {
                        if (VideoType == EVideoType.Movie)
                            title = Constants.HighestBitrateMovie;
                        else if (VideoType == EVideoType.Series)
                            title = Constants.HighestBitrateSeries;
                        else
                            throwHandleEpisode();
                    }
                    break;
                case EStatisticType.LowestBitrate:
                    {
                        if (VideoType == EVideoType.Movie)
                            title = Constants.LowestBitrateMovie;
                        else if (VideoType == EVideoType.Series)
                            title = Constants.LowestBitrateSeries;
                        else
                            throwHandleEpisode();
                    }
                    break;
                case EStatisticType.LatestPremiereDate:
                    {
                        if (VideoType == EVideoType.Movie)
                            title = Constants.LatestMoviePremiere;
                        else if (VideoType == EVideoType.Series)
                            title = Constants.LatestSeriesPremiere;
                        else
                            throwHandleEpisode();
                    }
                    break;
                case EStatisticType.OldestPremiereDate:
                    {
                        if (VideoType == EVideoType.Movie)
                            title = Constants.OldestMoviePremiere;
                        else if (VideoType == EVideoType.Series)
                            title = Constants.OldestSeriesPremiere;
                        else
                            throwHandleEpisode();
                    }
                    break;
                case EStatisticType.LatestAddition:
                    {
                        if (VideoType == EVideoType.Movie)
                            title = Constants.LatestMovieAddition;
                        else if (VideoType == EVideoType.Series)
                            title = Constants.LatestSeriesAddition;
                        else
                            throwHandleEpisode();
                    }

                    break;
                case EStatisticType.OldestAddition:
                    {
                        if (VideoType == EVideoType.Movie)
                            title = Constants.OldestMovieAddition;
                        else if (VideoType == EVideoType.Series)
                            title = Constants.LatestMovieAddition;
                        else
                            throwHandleEpisode();
                    }
                    break;
                default:
                    break;
            }
            return title;
        }

        private string Help()
        {
            string help = "";
            switch (WhichStatistic)
            {
                case EStatisticType.Largest:
                    break;
                case EStatisticType.Smallest:
                    break;
                case EStatisticType.Longest:
                    break;
                case EStatisticType.Shortest:
                    break;
                case EStatisticType.HighestRated:
                    break;
                case EStatisticType.LowestRated:
                    break;
                case EStatisticType.HighestBitrate:
                    break;
                case EStatisticType.LowestBitrate:
                    break;
                case EStatisticType.LatestPremiereDate:
                    break;
                case EStatisticType.OldestPremiereDate:
                    break;
                case EStatisticType.LatestAddition:
                    break;
                case EStatisticType.OldestAddition:
                    break;
                default:
                    break;
            }
            return help;
        }

        private string Value(IResultSet sqlResultValue, int index)
        {
            string value = "";
            switch (WhichStatistic)
            {
                case EStatisticType.Smallest:
                case EStatisticType.Largest:
                    {
                        float maxSize = sqlResultValue.GetInt64(index);
                        maxSize /= (1024 * 1024 * 1024); // in GB;
                        value = $"{maxSize:F1} Gb";
                    }
                    break;
                case EStatisticType.Longest:
                case EStatisticType.Shortest:
                    {
                        long runTimeTicks = sqlResultValue.GetInt64(index);
                        value = new TimeSpan(runTimeTicks).ToString(@"hh\:mm\:ss");
                    }
                    break;
                case EStatisticType.HighestRated:
                case EStatisticType.LowestRated:
                    {
                        var rating = sqlResultValue.GetFloat(index).ToString("F1");
                        value = $"{rating} / 10";
                    }
                    break;
                case EStatisticType.HighestBitrate:
                case EStatisticType.LowestBitrate:
                    {
                        var bitrate = Math.Round((decimal)sqlResultValue.GetInt64(index) / 1000);
                        value = $"{bitrate:N0} Kbps";
                    }
                    break;
                case EStatisticType.OldestPremiereDate:
                case EStatisticType.LatestPremiereDate:
                    {
                        var premiereDate = DateTime.ParseExact(sqlResultValue.GetString(index), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                        value = premiereDate.ToShortDateString();
                    }
                    break;
                case EStatisticType.OldestAddition:
                case EStatisticType.LatestAddition:
                    {
                        var premiereDate = DateTime.ParseExact(sqlResultValue.GetString(index), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                        value = premiereDate.ToShortDateString();
                    }
                    break;

                default:
                    value = "";
                    break;
            }
            return value;
        }

        private string SecondValue(IResultSet sqlResultValue, int index)
        {
            string secondValue = "";
            switch (WhichStatistic)
            {
                case EStatisticType.Smallest:
                case EStatisticType.Largest:
                case EStatisticType.Longest:
                case EStatisticType.Shortest:
                case EStatisticType.HighestRated:
                case EStatisticType.LowestRated:
                case EStatisticType.HighestBitrate:
                case EStatisticType.LowestBitrate:
                    break;
                case EStatisticType.OldestPremiereDate:
                case EStatisticType.LatestPremiereDate:
                    {
                        var premiereDate = DateTime.ParseExact(sqlResultValue.GetString(index), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                        secondValue = TimeSince(premiereDate);
                    }
                    break;
                case EStatisticType.OldestAddition:
                case EStatisticType.LatestAddition:
                    {
                        var premiereDate = DateTime.ParseExact(sqlResultValue.GetString(index), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                        secondValue = TimeSince(premiereDate);
                    }
                    break;

                default:
                    break;
            }
            return secondValue;
        }
        private string CheckForPlural(string value, decimal number, string starting = "", string ending = "", bool removeZero = true)
        {
            if (number == 1)
                return $" {starting} {number} {value} {ending}";
            if (number == 0 && removeZero)
                return "";
            return $" {starting} {number} {value}s {ending}";
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

    };
}
