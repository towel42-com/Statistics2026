using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Statistics2026.Api;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public async Task AddAllSeriesTaskImpl()
        {
            CheckIsValid( ECheckType.eUpdate );

            _embyInterfaces!.Logger().Debug( $"Starting Video Analysis" );

            _dbHelper!.Progress?.Report( 0 );
            var seriesList = EmbyHelperFuncs.GetLibraryItems<Series>( null, LibraryManager() );
            _dbHelper!.Progress?.Report( 100 );

            double count = seriesList.Count;
            var curr = 0.0;

            _dbHelper!.Progress?.Report( 0 );
            var sqlCmds = new List<SQLCmdDef>();
            sqlCmds.AddRange( UpdateSeriesFilter() );

            foreach( var series in seriesList )
            {
                _dbHelper!.Progress?.Report( 80.0 * ( ++curr ) / count );
                var cmds = await AddSeries( series );
                sqlCmds.AddRange( cmds );
                _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();

                _embyInterfaces!.Logger().Debug( $"    Processed Series ({curr} of {count}) - {series.Name}" );
            }

            _dbHelper!.Progress?.Report( 80 );
            _dbHelper.ExecuteCommands( sqlCmds );
            _dbHelper!.Progress?.Report( 100 );
            _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();
            _embyInterfaces!.Logger().Debug( $"Finished Video Analysis" );
        }

        private async Task<(int episodes, int specials)> GetCountForSeries( Series series )
        {
            CheckIsValid( ECheckType.eUpdate );

            var libraryOptions = LibraryManager().GetLibraryOptions( series );
            var allEpisodes = await ProviderManager().GetAllEpisodes( series, libraryOptions, _dbHelper!.CancellationToken!.Value ).ConfigureAwait( false );

            var episodes = allEpisodes.Where( e => !MediaInfo.isTVSpecial( e ) && ( e.PremiereDate <= DateTimeOffset.Now ) ).Count();
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

        public (int numSeasons, bool hasSpecials) GetSeasonCount( Series series )
        {
            var query = new InternalItemsQuery
            {
                Parent = series,
                IncludeItemTypes = new[] { nameof( Season ) },
                // DtoOptions(true) speeds up the DB query by skipping unneeded data fields
                DtoOptions = new DtoOptions( true )
            };

            var seasons = LibraryManager().GetItemList( query );
            var numSeasons = seasons.Count();
            bool hasSpecials = false;
            foreach( var season in seasons )
            {
                if( season.IndexNumber == 0 )
                {
                    hasSpecials = true;
                    break;
                }
            }
            if( hasSpecials )
            {
                numSeasons--;
            }
            return (numSeasons, hasSpecials);
        }

        private async Task<List<SQLCmdDef>> AddSeries( Series series )
        {
            CheckIsValid( ECheckType.eUpdate );

            var sqlCmds = new List<SQLCmdDef>();
            if( series.Id == null )
            {
                _embyInterfaces!.Logger().Error( $"AddSeries {series.SortName}: is missing ItemId" );
                return sqlCmds;
            }

            _embyInterfaces!.Logger().Debug( $"   AddSeries - Adding Series {series.Name}" );

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

            var seriesStatus = series.Status?.ToString() ?? string.Empty;
            var (numSeasons, hasSpecials) = GetSeasonCount( series );

            var (numEpisodes, numSpecials) = await GetCountForSeries( series );
            List<(string name, object? value)>? paramsList = null;
            sql = "INSERT INTO Series " +
            "(" +
                "  ItemId" +
                ", Name" +
                ", SortName" +
                ", PremiereDate" +
                ", NumEpisodes" +
                ", NumSpecials" +
                ", NumSeasons" +
                ", HasSpecials" +
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
                ", @NumSeasons" +
                ", @HasSpecials" +
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
                ", NumSeasons=@NumSeasons" +
                ", HasSpecials=@HasSpecials" +
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
                           ("@PremiereDate", HelperFuncs.ToDateTimeParamValue( series.PremiereDate.HasValue ? series.PremiereDate.Value : null )),
                           ("@NumEpisodes", numEpisodes),
                           ("@NumSpecials", numSpecials),
                           ("@NumSeasons", numSeasons),
                           ("@HasSpecials", hasSpecials),
                           ("@DateAdded", HelperFuncs.ToDateTimeParamValue( series.DateCreated )),
                           ("@ImageUrl", ItemImageUrl._ItemImageUrl(series)),
                           ("@FileSize", totalFileSize),
                           ("@RunTimeTicks", totalRuntime),
                           ("@Rating", averageRating),
                           ("@Status", seriesStatus),
                           ("@AverageBitrate", averageBitrate),
                       ];


            sqlCmds.Add( new SQLCmdDef( sql, paramsList ) );

            _embyInterfaces!.Logger().Debug( $"   AddSeries - Successfully Added Series {series.Name}" );
            return sqlCmds;
        }

        public List<GetTVSeriesProgressResponse> GetTVSeriesProgress( User? user )
        {
            CheckIsValid( ECheckType.eReport );

            if( user == null )
                throw new ArgumentNullException( "user" );

            var getSeriesSQL = "SELECT " +
                "  Series.Name" +
                ", strftime('%Y', Series.PremiereDate) AS PremiereDate" +
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
                if( curr.ItemUrl != null && curr.ItemUrl != string.Empty )
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

            List<(string name, object? value)> paramList = [ ("@UserId", user.Id.ToString()) ];

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
