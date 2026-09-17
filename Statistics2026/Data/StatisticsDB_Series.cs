using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public void AddAllSeries()
        {
            CheckIsValid( ECheckType.eUpdate );

            _embyInterfaces!._logger?.Debug( $"AddAllSeries- Starting Video Analysis" );

            _dbHelper!.Progress?.Report( 0 );
            var seriesList = _dbHelper.GetLibraryItems<Series>().Cast<Series>().ToList();
            _dbHelper!.Progress?.Report( 100 );

            double count = seriesList.Count;
            var curr = 0.0;

            _dbHelper!.Progress?.Report( 0 );
            var sqlCmds = new List<SQLCmdDef>();

            foreach( var series in seriesList )
            {
                _dbHelper!.Progress?.Report( 80.0 * ( ++curr ) / count );
                sqlCmds.AddRange( AddSeries( series ) );
                _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();

                _embyInterfaces!._logger?.Debug( $"AddAllSeries -     Processed Series ({curr} of {count}) - {series.Name}" );
            }

            _dbHelper!.Progress?.Report( 80 );
            _dbHelper.ExecuteCommands( sqlCmds );
            _dbHelper!.Progress?.Report( 100 );
            _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();
            _embyInterfaces!._logger?.Debug( $"AddAllSeries - Finished Video Analysis" );
        }

        private (int episodes, int specials) GetCountForSeries( Series series )
        {
            CheckIsValid( ECheckType.eUpdate );

            var libraryOptions = _embyInterfaces!._libraryManager.GetLibraryOptions( series );
            var allEpisodes = _embyInterfaces!._providerManager.GetAllEpisodes( series, libraryOptions, _dbHelper!.CancellationToken!.Value ).ConfigureAwait( false ).GetAwaiter().GetResult();

            var episodes = allEpisodes.Where( e => !MediaInfo.isTVSpecial( e ) && ( e.PremiereDate <= DateTime.Now ) ).Count();
            var specials = allEpisodes.Where( e => MediaInfo.isTVSpecial( e ) ).Count();

            if( episodes == 0 && specials == 0 )// when providers are disabled
            {
                var cmd = new SQLCmdDef( $"SELECT NumEpisodes FROM Series WHERE ItemId=@SeriesId",
                            [
                            ("@SeriesId", series.Id.ToString())
                            ] );

                _dbHelper.ExecuteCommand( cmd, statement =>
                {
                    if( statement != null )
                    {
                        var row = statement.Current;
                        episodes = row.GetInt( 0 );
                    }

                    return false;
                } );

                cmd = new SQLCmdDef( $"SELECT NumSpecials FROM Series WHERE ItemId=@SeriesId",
                            [
                            ("@SeriesId", series.Id.ToString())
                            ] );

                _dbHelper.ExecuteCommand( cmd, statement =>
                {
                    if( statement != null )
                    {
                        var row = statement.Current;
                        specials = row.GetInt( 0 );
                    }

                    return false;
                } );
            }

            if( episodes == 0 && specials == 0 ) // Series hasnt been setup yet
            {
                var cmd = new SQLCmdDef( $"SELECT SUM(NumEpisodes) FROM Media WHERE SeriesId=@SeriesId AND IsEpisode AND NOT IsTVSpecial",
                            [
                            ("@SeriesId", series.Id.ToString())
                            ] );

                _dbHelper.ExecuteCommand( cmd, statement =>
                {
                    if( statement != null )
                    {
                        var row = statement.Current;
                        episodes = row.GetInt( 0 );
                    }

                    return false;
                } );

                cmd = new SQLCmdDef( $"SELECT SUM(NumEpisodes) FROM Media WHERE SeriesId=@SeriesId AND IsEpisode AND IsTVSpecial",
                            [
                            ("@SeriesId", series.Id.ToString())
                            ] );

                _dbHelper.ExecuteCommand( cmd, statement =>
                {
                    if( statement != null )
                    {
                        var row = statement.Current;
                        specials = row.GetInt( 0 );
                    }

                    return false;
                } );
            }

            return (episodes, specials);
        }

        private List<SQLCmdDef> AddSeries( Series series )
        {
            CheckIsValid( ECheckType.eUpdate );

            var sqlCmds = new List<SQLCmdDef>();
            if( series.Id == null )
            {
                _embyInterfaces!._logger?.Error( $"AddSeries {series.SortName}: is missing ItemId" );
                return sqlCmds;
            }

            _embyInterfaces!._logger?.Debug( $"AddAllSeries -    AddSeries - Adding Series {series.Name}" );

            long totalFileSize = 0;
            long totalRuntime = 0;
            var averageRating = 0.0;
            long averageBitrate = 0;

            var sql = "SELECT " +
                "  SUM(FileSize)" +
                ", SUM(RunTimeTicks)" +
                ", SUM(Rating)/Count(1)" +
                ", Sum(TotalBitrate)/Count(1) " +
                "FROM " +
                "Media " +
                "WHERE SeriesId=@SeriesId";

            var cmd = new SQLCmdDef( sql,
                        [
                            ("@SeriesId", series.Id.ToString())
                        ] );
            _dbHelper.ExecuteCommands( [ cmd ],
                statement =>
                {
                    if( statement != null )
                    {
                        var row = statement.Current;
                        totalFileSize = row.GetInt64( 0 );
                        totalRuntime = row.GetInt64( 1 );
                        averageRating = row.GetFloat( 2 );
                        averageBitrate = row.GetInt64( 3 );
                        return false;
                    }

                    return true;
                } );

            var seriesStatus = series.Status?.ToString() ?? "";

            cmd = new SQLCmdDef( "SELECT COUNT(*) FROM Series where ItemId=@ItemId", [ ("@ItemId", series.Id.ToString()) ] );
            var exists = false;
            _dbHelper.ExecuteCommand( cmd, statement =>
            {
                var row = statement.Current;
                exists = row.GetInt( 0 ) > 0;
                return false;
            } );

            var (numEpisodes, numSpecials) = GetCountForSeries( series );
            sql = string.Empty;
            List<(string name, object? value)>? paramsList = null;
            if( !exists )
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

                paramsList =
                       [
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
                       ];
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

                paramsList =
                       [
                           ("@ItemId", series.Id.ToString()),
                           ("@NumEpisodes", numEpisodes),
                           ("@NumSpecials", numSpecials),
                           ("@FileSize", totalFileSize),
                           ("@RunTimeTicks", totalRuntime),
                           ("@Rating", averageRating),
                           ("@Status", seriesStatus),
                       ];
            }

            sqlCmds.Add( new SQLCmdDef( sql, paramsList ) );

            _embyInterfaces!._logger?.Debug( $"AddAllSeries -    AddSeries - Successfully Added Series {series.Name}" );
            return sqlCmds;
        }

        public List<GetTVSeriesProgressResponse> GetTVSeriesProgress( User? user )
        {
            CheckIsValid( ECheckType.eReport );

            if( user == null )
                throw new ArgumentNullException( "user" );

            var getSeriesSQL = "SELECT " +
                "  Series.Name" +
                ", strftime('%Y', Series.PremiereDate) AS PremierDate" +
                ", Series.NumEpisodes" +
                ", Series.NumSpecials" +
                ", Series.Rating" +
                ", Series.Status" +
                ", Series.ItemId " +
                ", Series.ImageUrl " +
                " FROM " +
                "   Series "
                ;

            var series = new Dictionary<string, GetTVSeriesProgressResponse>();
            _dbHelper.ExecuteCommand( new SQLCmdDef( getSeriesSQL ), statement =>
            {
                var row = statement.Current;
                var col = 0;
                var name = row.GetString( col++ );
                var premiereYear = row.GetInt( col++ );
                var totalEpisodes = row.GetInt( col++ );
                var totalSpecials = row.GetInt( col++ );
                var score = row.GetDouble( col++ );
                var status = row.GetString( col++ );
                var seriesId = row.GetString( col++ );
                var imageUrl = row.GetString( col++ );

                var curr = new GetTVSeriesProgressResponse()
                {
                    SeriesId = seriesId,
                    Name = name,
                    PremiereYear = premiereYear,
                    Score = score,
                    SeriesStatus = status,
                    ItemUrl = imageUrl
                };
                if( curr.ItemUrl != null && curr.ItemUrl != "" )
                {
                    curr.ItemUrl = ItemImageUrl.ItemUrl( seriesId, curr.ItemUrl, curr.Name );
                    curr.Name = curr.ItemUrl;
                }

                curr.Episodes.Total = totalEpisodes;
                curr.Specials.Total = totalSpecials;

                series[ seriesId ] = curr;
                return true;
            } );

            var tableName = getUserTableName( user );
            var sqlBase = "SELECT " +
                $"  SUM({tableName}.NumEpisodes) " +
                $", {tableName}.SeriesId " +
                $" FROM " +
                $"   {tableName} " +
                $" WHERE " +
                $" {tableName}.IsEpisode AND " +
                $" <isTVSpecial> AND " +
                $" {tableName}.IsPlayed AND " +
                $" {tableName}.UserId=@UserId " +
                $" GROUP BY {tableName}.SeriesId "
                ;

            var sqlEpisodes = sqlBase.Replace( "<isTVSpecial>", $"NOT {tableName}.IsTVSpecial" );
            var sqlSpecials = sqlBase.Replace( "<isTVSpecial>", $"{tableName}.IsTVSpecial" );

            var paramList = new List<(string name, object? value)>() { ("@UserId", user.Id.ToString()) };

            _dbHelper.ExecuteCommand( new SQLCmdDef( sqlEpisodes, paramList ), statement =>
            {
                var row = statement.Current;
                var col = 0;
                var watchedCount = row.GetInt( col++ );
                var seriesId = row.GetString( col++ ); // should be true

                if( series.TryGetValue( seriesId, out var curr ) )
                {
                    curr.Episodes.Count = watchedCount;
                    series[ seriesId ] = curr;
                }

                return true;
            } );

            _dbHelper.ExecuteCommand( new SQLCmdDef( sqlSpecials, paramList ), statement =>
            {
                var row = statement.Current;
                var col = 0;
                var watchedCount = row.GetInt( col++ );
                var seriesId = row.GetString( col++ ); // should be true

                if( series.TryGetValue( seriesId, out var curr ) )
                {
                    curr.Specials.Count = watchedCount;
                    series[ seriesId ] = curr;
                }

                return true;
            } );

            var retVal = series.Values.ToList();

            return retVal;
        }
    }
}
