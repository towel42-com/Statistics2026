using SQLitePCL;
using SQLitePCL.pretty;
using System;
using System.Globalization;

namespace Statistics2026.Api
{
    public class WhichStatistic
    {
        public enum VideoType
        {
            Movie,
            Series,
            Episode
        };

        public enum Statistic
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
        public static string FieldFor(Statistic whichStatistic, VideoType videoType)
        {
            string fieldName = "";
            switch (whichStatistic)
            {
                case WhichStatistic.Statistic.Largest:
                case WhichStatistic.Statistic.Smallest:
                    {
                        if (videoType == VideoType.Movie)
                            fieldName = "FileSize";
                        else if (videoType == VideoType.Series)
                            fieldName = "SUM(FileSize) AS FileSize";
                    }
                    break;
                case WhichStatistic.Statistic.Longest:
                case WhichStatistic.Statistic.Shortest:
                    {
                        if (videoType == VideoType.Movie)
                            fieldName = "RunTimeTicks";
                        else if (videoType == VideoType.Series)
                            fieldName = "SUM(RunTimeTicks) AS FileSize";
                    }
                    break;
                case WhichStatistic.Statistic.HighestRated:
                case WhichStatistic.Statistic.LowestRated:
                    fieldName = "Rating";
                    break;
                case WhichStatistic.Statistic.HighestBitrate:
                case WhichStatistic.Statistic.LowestBitrate:
                    {
                        if (videoType == VideoType.Movie)
                            fieldName = "TotalBitrate";
                        else if (videoType == VideoType.Series)
                            fieldName = "Sum(TotalBitrate)/Count(1) AS TotalBitrate";
                    }
                    break;
                case WhichStatistic.Statistic.LatestPremiereDate:
                case WhichStatistic.Statistic.OldestPremiereDate:
                    fieldName = "PremiereDate";
                    break;
                case WhichStatistic.Statistic.LatestAddition:
                case WhichStatistic.Statistic.OldestAddition:
                    fieldName = "DateAdded";
                    break;
                default:
                    break;
            }
            return fieldName;
        }

        public static string OrderClause(Statistic whichStatistic)
        {
            string orderClause = "";
            switch (whichStatistic)
            {
                case WhichStatistic.Statistic.Largest:
                    orderClause = "FileSize DESC";
                    break;
                case WhichStatistic.Statistic.Smallest:
                    orderClause = "FileSize ASC";
                    break;
                case WhichStatistic.Statistic.Longest:
                    orderClause = "RunTimeTicks DESC";
                    break;
                case WhichStatistic.Statistic.Shortest:
                    orderClause = "RunTimeTicks ASC";
                    break;
                case WhichStatistic.Statistic.HighestRated:
                    orderClause = "Rating DESC";
                    break;
                case WhichStatistic.Statistic.LowestRated:
                    orderClause = "Rating ASC";
                    break;
                case WhichStatistic.Statistic.HighestBitrate:
                    orderClause = "TotalBitrate DESC";
                    break;
                case WhichStatistic.Statistic.LowestBitrate:
                    orderClause = "TotalBitrate ASC";
                    break;
                case WhichStatistic.Statistic.LatestPremiereDate:
                    orderClause = "PremiereDate DESC";
                    break;
                case WhichStatistic.Statistic.OldestPremiereDate:
                    orderClause = "PremiereDate ASC";
                    break;
                case WhichStatistic.Statistic.LatestAddition:
                    orderClause = "DateAdded DESC";
                    break;
                case WhichStatistic.Statistic.OldestAddition:
                    orderClause = "DateAdded ASC";
                    break;
                default:
                    break;
            }
            return orderClause;
        }

        public static string WhereClause(Statistic whichStatistic)
        {
            string whereClause = "";
            switch (whichStatistic)
            {
                case WhichStatistic.Statistic.Largest:
                case WhichStatistic.Statistic.Smallest:
                case WhichStatistic.Statistic.Longest:
                case WhichStatistic.Statistic.Shortest:
                case WhichStatistic.Statistic.HighestBitrate:
                case WhichStatistic.Statistic.LowestBitrate:
                    break;
                case WhichStatistic.Statistic.HighestRated:
                case WhichStatistic.Statistic.LowestRated:
                    whereClause = "(Rating > 0)";
                    break;
                case WhichStatistic.Statistic.OldestPremiereDate:
                case WhichStatistic.Statistic.LatestPremiereDate:
                    whereClause = "(PremiereDate IS NOT NULL AND PremiereDate != '')";
                    break;
                case WhichStatistic.Statistic.LatestAddition:
                case WhichStatistic.Statistic.OldestAddition:
                    whereClause = "(DateAdded IS NOT NULL AND DateAdded != '')";
                    break;
                default:
                    break;
            }
            return whereClause;
        }

        public static string Title(Statistic whichStatistic, VideoType videoType )
        {
            string title = "";
            switch (whichStatistic)
            {
                case WhichStatistic.Statistic.Largest:
                    {
                        if (videoType == VideoType.Movie)
                            title = Constants.BiggestMovie;
                        else if (videoType == VideoType.Series)
                            title = Constants.BiggestSeries;
                    }
                    break;
                case WhichStatistic.Statistic.Smallest:
                    {
                        if (videoType == VideoType.Movie)
                            title = Constants.SmallestMovie;
                        else if (videoType == VideoType.Series)
                            title = Constants.SmallestSeries;
                    }
                    
                    break;
                case WhichStatistic.Statistic.Longest:
                    {
                        if (videoType == VideoType.Movie)
                            title = Constants.LongestMovie;
                        else if (videoType == VideoType.Series)
                            title = Constants.LongestSeries;
                    }
                    break;
                case WhichStatistic.Statistic.Shortest:
                    {
                        if (videoType == VideoType.Movie)
                            title = Constants.ShortestMovie;
                        else if (videoType == VideoType.Series)
                            title = Constants.ShortestSeries;
                    }
                    break;
                case WhichStatistic.Statistic.HighestRated:
                    {
                        if (videoType == VideoType.Movie)
                            title = Constants.HighestRatedMovie;
                        else if (videoType == VideoType.Series)
                            title = Constants.HighestRatedSeries;
                    }
                    break;
                case WhichStatistic.Statistic.LowestRated:
                    {
                        if (videoType == VideoType.Movie)
                            title = Constants.LowestRatedMovie;
                        else if (videoType == VideoType.Series)
                            title = Constants.LowestRatedSeries;
                    }
                    break;
                case WhichStatistic.Statistic.HighestBitrate:
                    {
                        if (videoType == VideoType.Movie)
                            title = Constants.HighestBitrateMovie;
                        else if (videoType == VideoType.Series)
                            title = Constants.HighestBitrateSeries;
                    }
                    break;
                case WhichStatistic.Statistic.LowestBitrate:
                    {
                        if (videoType == VideoType.Movie)
                            title = Constants.LowestBitrateMovie;
                        else if (videoType == VideoType.Series)
                            title = Constants.LowestBitrateSeries;
                    }
                    break;
                case WhichStatistic.Statistic.LatestPremiereDate:
                    {
                        if (videoType == VideoType.Movie)
                            title = Constants.LatestMoviePremiere;
                        else if (videoType == VideoType.Series)
                            title = Constants.LatestSeriesPremiere;
                    }
                    break;
                case WhichStatistic.Statistic.OldestPremiereDate:
                    {
                        if (videoType == VideoType.Movie)
                            title = Constants.OldestMoviePremiere;
                        else if (videoType == VideoType.Series)
                            title = Constants.OldestSeriesPremiere;
                    }
                    break;
                case WhichStatistic.Statistic.LatestAddition:
                    {
                        if (videoType == VideoType.Movie)
                            title = Constants.LatestMovieAddition;
                        else if (videoType == VideoType.Series)
                            title = Constants.LatestSeriesAddition;
                    }
                    
                    break;
                case WhichStatistic.Statistic.OldestAddition:
                    {
                        if (videoType == VideoType.Movie)
                            title = Constants.OldestMovieAddition;
                        else if (videoType == VideoType.Series)
                            title = Constants.LatestMovieAddition;
                    }
                    break;
                default:
                    break;
            }
            return title;
        }

        public static string Help(Statistic whichStatistic, VideoType videoType)
        {
            string help = "";
            switch (whichStatistic)
            {
                case WhichStatistic.Statistic.Largest:
                    break;
                case WhichStatistic.Statistic.Smallest:
                    break;
                case WhichStatistic.Statistic.Longest:
                    break;
                case WhichStatistic.Statistic.Shortest:
                    break;
                case WhichStatistic.Statistic.HighestRated:
                    break;
                case WhichStatistic.Statistic.LowestRated:
                    break;
                case WhichStatistic.Statistic.HighestBitrate:
                    break;
                case WhichStatistic.Statistic.LowestBitrate:
                    break;
                case WhichStatistic.Statistic.LatestPremiereDate:
                    break;
                case WhichStatistic.Statistic.OldestPremiereDate:
                    break;
                case WhichStatistic.Statistic.LatestAddition:
                    break;
                case WhichStatistic.Statistic.OldestAddition:
                    break;
                default:
                    break;
            }
            return help;
        }

        public static string Value(Statistic whichStatistic, IResultSet sqlResultValue, int index)
        {
            string value = "";
            switch (whichStatistic)
            {
                case WhichStatistic.Statistic.Smallest:
                case WhichStatistic.Statistic.Largest:
                    {
                        float maxSize = sqlResultValue.GetInt64(index);
                        maxSize /= (1024 * 1024 * 1024); // in GB;
                        value = $"{maxSize:F1} Gb";
                    }
                    break;
                case WhichStatistic.Statistic.Longest:
                case WhichStatistic.Statistic.Shortest:
                    {
                        long runTimeTicks = sqlResultValue.GetInt64(index);
                        value = new TimeSpan(runTimeTicks).ToString(@"hh\:mm\:ss");
                    }
                    break;
                case WhichStatistic.Statistic.HighestRated:
                case WhichStatistic.Statistic.LowestRated:
                    {
                        var rating = sqlResultValue.GetFloat(index).ToString("F1");
                        value = $"{rating} / 10";
                    }
                    break;
                case WhichStatistic.Statistic.HighestBitrate:
                case WhichStatistic.Statistic.LowestBitrate:
                    {
                        var bitrate = Math.Round((decimal)sqlResultValue.GetInt64(index) / 1000);
                        value = $"{bitrate:N0} Kbps";
                    }
                    break;
                case WhichStatistic.Statistic.OldestPremiereDate:
                case WhichStatistic.Statistic.LatestPremiereDate:
                    {
                        var premiereDate = DateTime.ParseExact(sqlResultValue.GetString(index), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                        value = premiereDate.ToShortDateString();
                    }
                    break;
                case WhichStatistic.Statistic.OldestAddition:
                case WhichStatistic.Statistic.LatestAddition:
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

        public static string SecondValue(Statistic whichStatistic, IResultSet sqlResultValue, int index)
        {
            string secondValue = "";
            switch (whichStatistic)
            {
                case WhichStatistic.Statistic.Smallest:
                case WhichStatistic.Statistic.Largest:
                case WhichStatistic.Statistic.Longest:
                case WhichStatistic.Statistic.Shortest:
                case WhichStatistic.Statistic.HighestRated:
                case WhichStatistic.Statistic.LowestRated:
                case WhichStatistic.Statistic.HighestBitrate:
                case WhichStatistic.Statistic.LowestBitrate:
                    break;
                case WhichStatistic.Statistic.OldestPremiereDate:
                case WhichStatistic.Statistic.LatestPremiereDate:
                    {
                        var premiereDate = DateTime.ParseExact(sqlResultValue.GetString(index), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                        secondValue = TimeSince(premiereDate);
                    }
                    break;
                case WhichStatistic.Statistic.OldestAddition:
                case WhichStatistic.Statistic.LatestAddition:
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
        private static string CheckForPlural(string value, decimal number, string starting = "", string ending = "", bool removeZero = true)
        {
            if (number == 1)
                return $" {starting} {number} {value} {ending}";
            if (number == 0 && removeZero)
                return "";
            return $" {starting} {number} {value}s {ending}";
        }

        public static string TimeSince(System.DateTime date)
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
