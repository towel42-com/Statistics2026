using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Providers;
using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Responses;
using Statistics2026.Utilities;
using System.Collections.Concurrent;

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

        private async Task<(ConcurrentDictionary<string, SeriesInfo> seriesInfoMap, List<object> validSeriesIds)> ComputeSeriesInfo( List<Series> seriesList )
        {
            string sql = "SELECT " +
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
            await ExecuteCommand( cmd,
                statement =>
                {
                    IResultSet row = statement.Current;
                    int col = 0;
                    string id = row.GetString( col++ );
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
                    return Task.FromResult( true );
                } );

            foreach( Series series in seriesList )
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

            SetProgress( 0 );
            List<Series> seriesList = EmbyHelperFuncs.GetAllSeries( null, LibraryManager(), false );
            SetProgress( 100 );
            if( seriesList == null )
                return;

            SetProgress( 0 );
            await DeleteAllInvalid( seriesList.ConvertAll( item => (BaseItem)item ), "Series", "ItemId" );

            (ConcurrentDictionary<string, SeriesInfo>? seriesInfoMap, List<object>? validSeriesIds) = await ComputeSeriesInfo( seriesList );

            var sqlCmds = new List<SQLCmdDef>();
            sqlCmds.AddRange( await UpdateSeriesFilter() );

            int completedCount = 0;

            using( var timer = new AutoTimer( $"    Analyze Series - Getting Commands", Logger() ) )
            {
                IEnumerable<Task<(int index, Series item, List<SQLCmdDef?> cmds)>> pool = seriesList.Select( ( series, index ) => Task.Run( async () =>
                {
                    (int index, Series item, List<SQLCmdDef?> cmds) retVal = (index: index, item: series, cmds: new List<SQLCmdDef?>());
                    if( series == null )
                        return retVal;

                    if( !seriesInfoMap.TryGetValue( series.Id.ToString(), out SeriesInfo seriesInfo ) )
                    {
                        Logger().Error( $"Series '{series.Name}' is not in the Media table" );
                        return retVal;
                    }

                    retVal.cmds.Add( await AddSeriesAsync( series, seriesInfo ).ConfigureAwait( false ) );

                    CancellationToken?.ThrowIfCancellationRequested();
                    int currProgress = Interlocked.Increment( ref completedCount );
                    SetProgress( 80.0 * ( 1.0 * currProgress ) / seriesList.Count() );
                    Logger().Debug( $"    Processed Series ({currProgress} of {seriesList.Count()}) - {series.Name}" );

                    return retVal;
                } ) );

                CancellationToken?.ThrowIfCancellationRequested();
                (int index, Series item, List<SQLCmdDef?> cmds)[] unsortedResults = await Task.WhenAll( pool );
                sqlCmds = unsortedResults
                            .Where( item => item.cmds != null )
                            .OrderBy( item => item.index )
                            .SelectMany( item => item.cmds! )
                            .Where( cmd => cmd != null )
                            .Select( cmd => cmd! )
                            .ToList();

                CancellationToken?.ThrowIfCancellationRequested();
            }

            using( var timer = new AutoTimer( $"    Analyze Series - Executing Commands", Logger() ) )
            {
                SetProgress( 80 );
                await ExecuteCommands( sqlCmds );
                SetProgress( 100 );
                CancellationToken?.ThrowIfCancellationRequested();
                Logger().Debug( $"Finished Series Analysis" );
            }
        }

        private async Task<(int episodes, int specials)> GetCountForSeriesAsync( Series series )
        {
            CheckIsValid( ECheckLevel.eUpdate );

            LibraryOptions libraryOptions = LibraryManager().GetLibraryOptions( series );
            RemoteSearchResult[] allEpisodes = await ProviderManager().GetAllEpisodes( series, libraryOptions, CancellationToken!.Value ).ConfigureAwait( false );

            int episodes = allEpisodes.Where( e => !Statistics2026.Utilities.MediaInfo.isTVSpecial( e ) && ( e.PremiereDate <= DateTimeOffset.Now ) ).Count();
            int specials = allEpisodes.Where( e => Statistics2026.Utilities.MediaInfo.isTVSpecial( e ) ).Count();

            if( episodes == 0 && specials == 0 )// when providers are disabled
            {
                var cmd = new SQLCmdDef( $"SELECT NumEpisodes FROM Series WHERE ItemId=@SeriesId",
                            [
                            ("@SeriesId", series.Id.ToString())
                            ] );

                await ExecuteCommand( cmd, statement =>
                {
                    if( statement != null )
                    {
                        IResultSet row = statement.Current;
                        episodes = row.GetInt( 0 );
                    }

                    return Task.FromResult( false );
                } );

                cmd = new SQLCmdDef( $"SELECT NumSpecials FROM Series WHERE ItemId=@SeriesId",
                            [
                            ("@SeriesId", series.Id.ToString())
                            ] );

                await ExecuteCommand( cmd, statement =>
                {
                    if( statement != null )
                    {
                        IResultSet row = statement.Current;
                        specials = row.GetInt( 0 );
                    }

                    return Task.FromResult( false );
                } );
            }

            if( episodes == 0 && specials == 0 ) // Series hasnt been setup yet
            {
                var cmd = new SQLCmdDef( $"SELECT SUM(NumEpisodes) FROM Media WHERE SeriesId=@SeriesId AND IsEpisode AND NOT IsTVSpecial",
                            [
                            ("@SeriesId", series.Id.ToString())
                            ] );

                await ExecuteCommand( cmd, statement =>
                {
                    if( statement != null )
                    {
                        IResultSet row = statement.Current;
                        episodes = row.GetInt( 0 );
                    }

                    return Task.FromResult( false );
                } );

                cmd = new SQLCmdDef( $"SELECT SUM(NumEpisodes) FROM Media WHERE SeriesId=@SeriesId AND IsEpisode AND IsTVSpecial",
                            [
                            ("@SeriesId", series.Id.ToString())
                            ] );

                await ExecuteCommand( cmd, statement =>
                {
                    if( statement != null )
                    {
                        IResultSet row = statement.Current;
                        specials = row.GetInt( 0 );
                    }

                    return Task.FromResult( false );
                } );
            }

            return (episodes, specials);
        }

        private (int numSeasons, bool hasSpecials) GetSeasonCount( Series series )
        {
            var query = new InternalItemsQuery
            {
                Parent = series,
                IncludeItemTypes = new[] { nameof( Season ) },
                // DtoOptions(true) speeds up the DB query by skipping unneeded data fields
                DtoOptions = new DtoOptions( true )
            };

            BaseItem[] seasons = LibraryManager().GetItemList( query );
            int numSeasons = seasons.Count();
            bool hasSpecials = false;
            foreach( BaseItem? season in seasons )
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

            string seriesStatus = series.Status?.ToString() ?? string.Empty;
            (int numSeasons, bool hasSpecials) = GetSeasonCount( series );

            var tmdbService = new TmdbInterface();

            (int numEpisodes, int numSpecials) = await GetCountForSeriesAsync( series ).ConfigureAwait( false );
            List<(string name, object? value)>? paramsList = null;
            string sql = "INSERT INTO Series " +
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
                ", ServerLocation" +
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
                ", @ServerLocation" +
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
                ", AverageBitrate=@AverageBitrate" +
                ", ServerLocation=@ServerLocation";

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
                        ("@ImageUrl", GetItemImageUrl.ItemImageUrlFromItem(series)),
                        ("@FileSize", seriesInfo.totalFileSize),
                        ("@RunTimeTicks", seriesInfo.totalRuntime),
                        ("@Rating", seriesInfo.averageRating),
                        ("@Status", seriesStatus),
                        ("@AverageBitrate", seriesInfo.averageBitrate),
                        ("@ServerLocation", series.Path),
                    ];

            Logger().Debug( $"   AddSeries - Successfully Added Series {series.Name}" );
            return new SQLCmdDef( sql, paramsList );
        }
    }
}
