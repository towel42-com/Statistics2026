using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using ServiceStack;
using Statistics2026.Api;
using Statistics2026.Utilities;
using System;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        private class SeriesInfo
        {
            public string id = string.Empty;
            public string name = string.Empty;
            public long totalFileSize = 0;
            public long totalRuntime = 0;
            public double averageRating = 0.0;
            public long averageBitrate = 0;
        }

        private (ConcurrentDictionary<string, SeriesInfo> seriesInfoMap, List<object> validSeriesIds) ComputeSeriesInfo( List<Series> seriesList )
        {
            var sql = "SELECT " +
                "  Media.SeriesId" +
                ", Series.Name" +
                ", SUM(Media.FileSize)" +
                ", SUM(Media.RunTimeTicks)" +
                ", SUM(Media.Rating)/Count(Media.Rating)" +
                ", Sum(Media.TotalBitrate)/Count(Media.TotalBitrate) " +
                "FROM Media " +
                "LEFT JOIN SERIES ON Series.ItemId=Media.SeriesId " +
                "WHERE Media.IsEpisode AND NOT Media.IsTVSpecial " +
                "GROUP BY Media.SeriesId"
                ;

            ConcurrentDictionary<string, SeriesInfo> seriesInfoMap = [];
            var cmd = new SQLCmdDef( sql );
            List<object> validSeriesIds = [];
            ExecuteCommand( cmd,
                statement =>
                {
                    var row = statement.Current;
                    var col = 0;
                    var id = row.GetString( col++ );
                    validSeriesIds.Add( id );
                    SeriesInfo seriesInfo = new()
                    {
                        id = id,
                        name = row.GetString( col++ ),
                        totalFileSize = row.GetInt64( col++ ),
                        totalRuntime = row.GetInt64( col++ ),
                        averageRating = row.GetFloat( col++ ),
                        averageBitrate = row.GetInt64( col++ ),
                    };

                    seriesInfoMap[ seriesInfo.id ] = seriesInfo;
                    return true;
                } );

            foreach( var series in seriesList )
            {
                if( !seriesInfoMap.TryGetValue( series.Id.ToString(), out _ ) )
                {
                    Logger().Error( $"Series '{series.Name}' is not in the Media table" );
                }
            }
            return (seriesInfoMap, validSeriesIds);
        }

        public async Task AddAllSeriesTaskImplAsync()
        {
            CheckIsValid( ECheckLevel.eUpdate );
            Logger().Debug( $"Starting Series Analysis" );

            Progress?.Report( 0 );
            var seriesList = EmbyHelperFuncs.GetAllSeries( null, LibraryManager(), false );
            Progress?.Report( 100 );
            if( seriesList == null )
                return;

            var (seriesInfoMap, validSeriesIds) = ComputeSeriesInfo( seriesList );

            var sqlCmds = new List<SQLCmdDef>();
            ConcurrentBag<object> validIds = [];
            sqlCmds.AddRange( UpdateSeriesFilter() );

            int completedCount = 0;

            using( var timer = new AutoTimer( $"    Analyze Series - Getting Commands", Logger() ) )
            {
                var pool = seriesList.Select( ( series, index ) => Task.Run( async () =>
                {
                    var retVal = (index: index, series: series, cmd: (SQLCmdDef?)null);
                    if( series == null )
                        return retVal;

                    if( seriesInfoMap.TryGetValue( series.Id.ToString(), out SeriesInfo seriesInfo ) )
                    {
                        retVal.cmd = await AddSeriesAsync( series, seriesInfo ).ConfigureAwait( false );
                        validIds.Add( series.Id.ToString() );
                    }
                    else
                    {
                        Logger().Error( $"Series '{series.Name}' is not in the Media table" );
                        return retVal;
                    }
                    CancellationToken?.ThrowIfCancellationRequested();
                    int currProgress = Interlocked.Increment( ref completedCount );
                    Progress?.Report( 80.0 * ( 1.0 * currProgress ) / seriesList.Count() );
                    Logger().Debug( $"    Processed Series ({currProgress} of {seriesList.Count()}) - {series.Name}" );

                    return retVal;
                } ) );

                CancellationToken?.ThrowIfCancellationRequested();
                var unsortedResults = await Task.WhenAll( pool );
                var results = unsortedResults.OrderBy( item => item.index ).ToList();
                sqlCmds.Add( SQLCmdDef.CreateSQLCmdDef( "DELETE FROM Series WHERE ItemId NOT IN (<KEYLIST>)", "<KEYLIST>", validIds.ToList() ) );
                foreach( var result in results )
                {
                    if( result.cmd != null )
                    {
                        sqlCmds.Add( result.cmd );
                    }
                    CancellationToken?.ThrowIfCancellationRequested();
                }
                CancellationToken?.ThrowIfCancellationRequested();
            }

            using( var timer = new AutoTimer( $"    Analyze Series - Executing Commands", Logger() ) )
            {
                Progress?.Report( 80 );
                ExecuteCommands( sqlCmds );
                Progress?.Report( 100 );
                CancellationToken?.ThrowIfCancellationRequested();
                Logger().Debug( $"Finished Series Analysis" );
            }
        }

        private async Task<(int episodes, int specials)> GetCountForSeriesAsync( Series series )
        {
            CheckIsValid( ECheckLevel.eUpdate );

            var libraryOptions = LibraryManager().GetLibraryOptions( series );
            var allEpisodes = await ProviderManager().GetAllEpisodes( series, libraryOptions, CancellationToken!.Value ).ConfigureAwait( false );

            var episodes = allEpisodes.Where( e => !Statistics2026.Utilities.MediaInfo.isTVSpecial( e ) && ( e.PremiereDate <= DateTimeOffset.Now ) ).Count();
            var specials = allEpisodes.Where( e => Statistics2026.Utilities.MediaInfo.isTVSpecial( e ) ).Count();

            if( episodes == 0 && specials == 0 )// when providers are disabled
            {
                var cmd = new SQLCmdDef( $"SELECT NumEpisodes FROM Series WHERE ItemId=@SeriesId",
                            [
                            ("@SeriesId", series.Id.ToString())
                            ] );

                ExecuteCommand( cmd, statement =>
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

                ExecuteCommand( cmd, statement =>
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

                ExecuteCommand( cmd, statement =>
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

                ExecuteCommand( cmd, statement =>
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

        private async Task<SQLCmdDef?> AddSeriesAsync( Series series, SeriesInfo seriesInfo )
        {
            CheckIsValid( ECheckLevel.eUpdate );

            if( series.Id == null )
            {
                Logger().Error( $"AddSeries {series.SortName}: is missing ItemId" );
                return null;
            }

            Logger().Debug( $"   AddSeries - Adding Series {series.Name}" );

            var seriesStatus = series.Status?.ToString() ?? string.Empty;
            var (numSeasons, hasSpecials) = GetSeasonCount( series );

            var tmdbService = new TmdbInterface();

            var (numEpisodes, numSpecials) = await GetCountForSeriesAsync( series ).ConfigureAwait( false );
            List<(string name, object? value)>? paramsList = null;
            var sql = "INSERT INTO Series " +
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
                ", AverageBitrate=@AverageBitrate" ;

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
                        ("@FileSize", seriesInfo.totalFileSize),
                        ("@RunTimeTicks", seriesInfo.totalRuntime),
                        ("@Rating", seriesInfo.averageRating),
                        ("@Status", seriesStatus),
                        ("@AverageBitrate", seriesInfo.averageBitrate),
                    ];

            Logger().Debug( $"   AddSeries - Successfully Added Series {series.Name}" );
            return new SQLCmdDef( sql, paramsList );
        }

        public List<GetTVSeriesProgressResponse> GetTVSeriesProgress( User? user )
        {
            CheckIsValid( ECheckLevel.eReport );

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
            ExecuteCommand( new SQLCmdDef( getSeriesSQL ), statement =>
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
                    curr.ItemUrl = ItemImageUrl.ItemUrlFromItemId( seriesId, curr.ItemUrl, curr.Name );
                    curr.Name = curr.ItemUrl;
                }

                curr.Episodes.Total = totalEpisodes;
                curr.Specials.Total = totalSpecials;

                series[ seriesId ] = curr;
                return true;
            } );

            var tableName = TableDefs().getUserTableName( user );
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

            ExecuteCommand( new SQLCmdDef( sqlEpisodes, paramList ), statement =>
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

            ExecuteCommand( new SQLCmdDef( sqlSpecials, paramList ), statement =>
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
