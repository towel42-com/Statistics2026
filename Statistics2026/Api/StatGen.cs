using SQLitePCL.pretty;
using Statistics2026.Data;
using System;
using System.Collections.Generic;

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
            FirstAdditionToServer,
            LatestAdditionToServer,
            HighestBitrate,
            LowestBitrate
        }

        private EStatisticType WhichStatistic { get; set; }
        private EVideoType VideoType { get; set; }

        private DBHelper _dbHelper { get; set; }
        public StatGen( EStatisticType statType, EVideoType videoType, DBHelper dbHelper )
        {
            WhichStatistic = statType;
            VideoType = videoType;
            _dbHelper = dbHelper;
        }

        public class StatCardValues
        {
            public string Value = "";
            public string SecondValue = "";
            public string Name = "";
            public string ItemId = "";
            public string ImageUrl = "";
        };

        public StatCardValues GetStatCardValues()
        {
            var sql = SQL();

            var retVal = new StatCardValues();

            var sqlCmd = new SQLCmdDef( sql );

            var cmd = new SQLCmdDef( sql );
            _dbHelper.ExecuteCommand( new SQLCmdDef( sql ), statement =>
            {
                var row = statement.Current;
                retVal.ItemId = row.GetString( 0 );
                retVal.Name = row.GetString( 1 );
                retVal.ImageUrl = row.GetString( 2 );
                retVal.Value = Value( row, 3 );
                retVal.SecondValue = SecondValue( row, 3 );
                return false;
            } );
            return retVal;
        }

        public StatCard GetStatCard()
        {
            var title = Title();
            var help = Help();
            var retVal = new TextBasedStatCard( title, help, EStatCardSize.eHalf );

            var statCardValues = GetStatCardValues();

            retVal.AddLine( statCardValues.Value );
            if( string.IsNullOrEmpty( statCardValues.SecondValue ) )
            {
                retVal.AddLine( statCardValues.Name );
            }
            else
            {
                retVal.AddLine( statCardValues.SecondValue );
                retVal.AddLine( statCardValues.Name );
            }

            retVal.ImageUrl = statCardValues.ImageUrl;
            retVal.MediaItemId = statCardValues.ItemId;
            return retVal;
        }

        private string SQL()
        {
            var nameField = NameField();
            var fieldName = FieldFor();
            var orderClause = OrderClause();
            var tableName = TableName();
            var whereClause = WhereClause();
            var retVal = "SELECT "
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
            return VideoType == EVideoType.Movie ? "PrimaryName" : VideoType == EVideoType.Episode ? "PrimaryName" : "Name";
        }

        private string TableName()
        {
            return VideoType == EVideoType.Movie ? "Media" : VideoType == EVideoType.Episode ? "Media" : "Series";
        }

        private string FieldFor()
        {
            var fieldName = "";
            switch( WhichStatistic )
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
                    fieldName = VideoType == EVideoType.Movie
                        ? "TotalBitrate"
                        : VideoType == EVideoType.Series ? "AverageBitrate AS TotalBitrate" : "TotalBitrate";
                }

                break;
                case EStatisticType.LatestPremiereDate:
                case EStatisticType.OldestPremiereDate:
                    fieldName = "PremiereDate";
                    break;
                case EStatisticType.LatestAdditionToServer:
                case EStatisticType.FirstAdditionToServer:
                    fieldName = "DateAdded";
                    break;
                default:
                    break;
            }

            return fieldName;
        }

        private string OrderClause()
        {
            var orderClause = "";
            switch( WhichStatistic )
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
                case EStatisticType.LatestAdditionToServer:
                    orderClause = "DateAdded DESC";
                    break;
                case EStatisticType.FirstAdditionToServer:
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

        public static string validDateClause( string columnName )
        {
            return $"{columnName} IS NOT NULL AND {columnName} != '' AND {columnName} != '0001-01-01T00:00:00.0000000'";
        }

        private string WhereClause()
        {
            var whereClauseList = new List<string>();
            switch( WhichStatistic )
            {
                case EStatisticType.Largest:
                case EStatisticType.Smallest:
                    whereClauseList.Add( "FileSize > 0" );
                    break;

                case EStatisticType.Longest:
                case EStatisticType.Shortest:
                    whereClauseList.Add( "RunTimeTicks > 0" );
                    break;

                case EStatisticType.HighestBitrate:
                case EStatisticType.LowestBitrate:
                    whereClauseList.Add( "TotalBitrate > 0" );
                    break;
                case EStatisticType.HighestRated:
                case EStatisticType.LowestRated:
                    whereClauseList.Add( "Rating > 0" );
                    break;
                case EStatisticType.OldestPremiereDate:
                case EStatisticType.LatestPremiereDate:
                    whereClauseList.Add( validDateClause( "PremiereDate" ) );
                    break;
                case EStatisticType.LatestAdditionToServer:
                case EStatisticType.FirstAdditionToServer:
                    whereClauseList.Add( validDateClause( "DateAdded" ) );
                    break;
                default:
                    break;
            }

            if( VideoType == EVideoType.Movie )
                whereClauseList.Add( "NOT IsEpisode" );
            else if( VideoType == EVideoType.Episode )
                whereClauseList.Add( "IsEpisode" );

            return whereClauseList.Count > 0 ? DBHelper.JoinClauses( whereClauseList ) + " " : "";
        }

        private string Title()
        {
            var title = "";
            switch( WhichStatistic )
            {
                case EStatisticType.Largest:
                {
                    if( VideoType == EVideoType.Movie )
                        title = Constants.BiggestMovie;
                    else if( VideoType == EVideoType.Series )
                        title = Constants.BiggestSeries;
                    else
                        throwHandleEpisode();
                }

                break;
                case EStatisticType.Smallest:
                {
                    if( VideoType == EVideoType.Movie )
                        title = Constants.SmallestMovie;
                    else if( VideoType == EVideoType.Series )
                        title = Constants.SmallestSeries;
                    else
                        throwHandleEpisode();
                }

                break;
                case EStatisticType.Longest:
                {
                    if( VideoType == EVideoType.Movie )
                        title = Constants.LongestMovie;
                    else if( VideoType == EVideoType.Series )
                        title = Constants.LongestSeries;
                    else
                        throwHandleEpisode();
                }

                break;
                case EStatisticType.Shortest:
                {
                    if( VideoType == EVideoType.Movie )
                        title = Constants.ShortestMovie;
                    else if( VideoType == EVideoType.Series )
                        title = Constants.ShortestSeries;
                    else
                        throwHandleEpisode();
                }

                break;
                case EStatisticType.HighestRated:
                {
                    if( VideoType == EVideoType.Movie )
                        title = Constants.HighestRatedMovie;
                    else if( VideoType == EVideoType.Series )
                        title = Constants.HighestRatedSeries;
                    else
                        throwHandleEpisode();
                }

                break;
                case EStatisticType.LowestRated:
                {
                    if( VideoType == EVideoType.Movie )
                        title = Constants.LowestRatedMovie;
                    else if( VideoType == EVideoType.Series )
                        title = Constants.LowestRatedSeries;
                    else
                        throwHandleEpisode();
                }

                break;
                case EStatisticType.HighestBitrate:
                {
                    if( VideoType == EVideoType.Movie )
                        title = Constants.HighestBitrateMovie;
                    else if( VideoType == EVideoType.Series )
                        title = Constants.HighestBitrateSeries;
                    else
                        throwHandleEpisode();
                }

                break;
                case EStatisticType.LowestBitrate:
                {
                    if( VideoType == EVideoType.Movie )
                        title = Constants.LowestBitrateMovie;
                    else if( VideoType == EVideoType.Series )
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
                            title = Constants.LatestEpisodePremiere;
                }

                break;
                case EStatisticType.OldestPremiereDate:
                {
                        if (VideoType == EVideoType.Movie)
                            title = Constants.OldestMoviePremiere;
                        else if (VideoType == EVideoType.Series)
                            title = Constants.OldestSeriesPremiere;
                        else
                            title = Constants.OldestEpisodePremiere;
                }

                break;
                case EStatisticType.LatestAdditionToServer:
                {
                        if (VideoType == EVideoType.Movie)
                            title = Constants.LatestMovieAddition;
                        else if (VideoType == EVideoType.Series)
                            title = Constants.LatestSeriesAddition;
                        else
                            title = Constants.LatestEpisodeAddition;
                }

                break;
                case EStatisticType.FirstAdditionToServer:
                {
                        if (VideoType == EVideoType.Movie)
                            title = Constants.FirstMovieAddition;
                        else if (VideoType == EVideoType.Series)
                            title = Constants.LatestMovieAddition;
                        else
                            title = Constants.FirstEpisodeAddition;
                }

                break;
                default:
                    break;
            }

            return title;
        }

        private string Help()
        {
            var help = "";
            switch( WhichStatistic )
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
                case EStatisticType.LatestAdditionToServer:
                    break;
                case EStatisticType.FirstAdditionToServer:
                    break;
                default:
                    break;
            }

            return help;
        }

        private string Value( IResultSet sqlResultValue, int index )
        {
            string? value;
            switch( WhichStatistic )
            {
                case EStatisticType.Smallest:
                case EStatisticType.Largest:
                {
                    float maxSize = sqlResultValue.GetInt64( index );
                    maxSize /= 1024 * 1024 * 1024; // in GB;
                    value = $"{maxSize:F1} Gb";
                }

                break;
                case EStatisticType.Longest:
                case EStatisticType.Shortest:
                {
                    var runTimeTicks = sqlResultValue.GetInt64( index );
                    value = new TimeSpan( runTimeTicks ).ToString( @"hh\:mm\:ss" );
                }

                break;
                case EStatisticType.HighestRated:
                case EStatisticType.LowestRated:
                {
                    var rating = sqlResultValue.GetFloat( index ).ToString( "F1" );
                    value = $"{rating} / 10";
                }

                break;
                case EStatisticType.HighestBitrate:
                case EStatisticType.LowestBitrate:
                {
                    var bitrate = Math.Round( (decimal)sqlResultValue.GetInt64( index ) / 1000 );
                    value = $"{bitrate:N0} Kbps";
                }

                break;
                case EStatisticType.OldestPremiereDate:
                case EStatisticType.LatestPremiereDate:
                {
                    var premiereDate = DBHelper.ReadDateTime( sqlResultValue.GetString( index ) );
                    value = premiereDate.ToShortDateString();
                }

                break;
                case EStatisticType.FirstAdditionToServer:
                case EStatisticType.LatestAdditionToServer:
                {
                    var premiereDate = DBHelper.ReadDateTime( sqlResultValue.GetString( index ) );
                    value = premiereDate.ToShortDateString();
                }

                break;

                default:
                    value = "";
                    break;
            }

            return value;
        }

        private string SecondValue( IResultSet sqlResultValue, int index )
        {
            var secondValue = "";
            switch( WhichStatistic )
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
                    var premiereDate = DBHelper.ReadDateTime( sqlResultValue.GetString( index ) );
                    secondValue = TimeSince( premiereDate );
                }

                break;
                case EStatisticType.FirstAdditionToServer:
                case EStatisticType.LatestAdditionToServer:
                {
                    var premiereDate = DBHelper.ReadDateTime( sqlResultValue.GetString( index ) );
                    secondValue = TimeSince( premiereDate );
                }

                break;

                default:
                    break;
            }

            return secondValue;
        }
        private string CheckForPlural( string value, decimal number, string starting = "", string ending = "", bool removeZero = true )
        {
            if (number == 1)
                return $" {starting} {number} {value} {ending}";
            if (number == 0 && removeZero)
                return "";
            return $" {starting} {number} {value}s {ending}";
        }

        private string TimeSince( System.DateTime date )
        {
            var yearDiff = DateTime.Now.Year - date.Year;
            var monthDiff = DateTime.Now.Month - date.Month;

            var numberOfTotalMonths = ( yearDiff * 12 ) + monthDiff;
            if( numberOfTotalMonths > 3 )
            {
                var numberOfYears = Math.Floor( numberOfTotalMonths / (decimal)12 );
                var numberOfMonth = Math.Floor( ( ( numberOfTotalMonths / (decimal)12 ) - numberOfYears ) * 12 );
                return $"{CheckForPlural( "year", numberOfYears, "", "", false )} {CheckForPlural( "month", numberOfMonth, "and" )} ago";
            }
            else
            {
                var numberOfDays = DateTime.Now.DayOfYear - date.DayOfYear;
                if( numberOfDays < 0 )
                    numberOfDays *= -1;
                if (numberOfDays == 0)
                {
                    return $"Today";
                }
                else if (numberOfDays == 1)
                {
                    return $"Yesterday";
                }
                else
                    return $"{CheckForPlural("day", numberOfDays, "", "", false)} ago";
            }
        }
    };
}
