#if DEBUG
//#define DEBUG_PLUGIN
#endif
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Configuration;
using Statistics2026.Utilities;
using WatchedMediaValueItemData = (string id, string name, long playCount, long denominator, double playCountPerUser);

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        public async Task AnalyzeWatchedMediaDataTaskImplAsync()
        {
            CheckIsValid( ECheckLevel.eUpdate );

            SetProgress( 0 );
            List<User> users = EmbyHelperFuncs.GetAllUsers( _embyInterfaces );
            if( users == null )
                return;
            SetProgress( 100 );

            Logger().Debug( $"Starting User Watch Data Analysis" );

            double count = users.Count;
            int completedCount = 0;
            int startedCount = 0;

            double progressWindow = 80.0 * 1.0 / count;

            SetProgress( 0 );
            using( var listTimer = new AutoTimer( $"    Analyze User Watch Data - Getting Commands", Logger() ) )
            {
                using SemaphoreSlim gatekeeper = HelperFuncs.CreateSemaphoreSlim();
                var pool = users.Select( ( user, index ) => Task.Run( async () =>
                {
                    await gatekeeper.WaitAsync();
                    try
                    {
                        HelperFuncs.MemoryUsage memoryReport = await HelperFuncs.RunAndReportMemory( ( ConfigManager().CommonConfiguration?.EnableDebugLevelLogging ?? false ), async () =>
                        {
                            await Task.Yield();
                            long runTimeMS = 0;
                            int userNum = Interlocked.Increment( ref startedCount );
                            List<SQLCmdDef> currCmds = [];
                            List<Video> allVideosForUser = [];

                            string? userTableName = TableDefs().getUserTableName( user );
                            if( userTableName == null )
                            {
                                throw new Exception( $"AddWatchedDataForUser: TableDef for {user.Name} is null" );
                            }

                            using( var currTimer = new AutoTimer( $"    Processed User {user.Name}", Logger() ) )
                            {
                                currTimer.Restart();

                                (currCmds, allVideosForUser) = await AddWatchedDataForUserAsync( user, userTableName, userNum, users.Count ).ConfigureAwait( false );
                                runTimeMS = currTimer.ElapsedMilliseconds();
                            }

                            CancellationToken?.ThrowIfCancellationRequested();

                            using( var timer = new AutoTimer( $"    Analyze User Watch Data - Executing Commands for user {user.Name}", Logger() ) )
                            {
                                await DeleteAllInvalid( allVideosForUser.ConvertAll( item => (BaseItem)item ), userTableName, "ItemId" );
                                await ExecuteCommands( currCmds, false );
                            }

                            int currProgress = Interlocked.Increment( ref completedCount );
                            SetProgress( progressWindow * currProgress );

                            Logger().Info( $"Processed User {user.Name} - {runTimeMS} ms {userNum} of {count}" );
                        } ).ConfigureAwait( false );
                        memoryReport.Report( Logger(), user.Name );
                        return true;
                    }
                    finally
                    {
                        gatekeeper.Release();
                    }
                } ) ).ToList();

                bool[] aOK = await Task.WhenAll( pool );

                CancellationToken?.ThrowIfCancellationRequested();
            }

            PluginConfiguration config = Statistics2026.Plugin.Instance!.Configuration;
            config.systemSettings.resetPlayCount = false;
            Statistics2026.Plugin.Instance.UpdateConfiguration( config );

            await ComputeUserDataDBState();
            if( !Plugin.Instance.IsDBStateSet( EDBState.eUserDataInitialized ) )
            {
                Logger().Error( "After initializing user watch data, user watch data still doesn't conform." );
            }

            Logger().Debug( $"Finished User Watch Data Analysis" );
        }

        private async Task<(List<SQLCmdDef>, List<Video>)> AddWatchedDataForUserAsync( User? user, string userTableName, int userIndex, int userCount )
        {
            CheckIsValid( ECheckLevel.eUpdate );

            if( user == null )
                throw new ArgumentNullException( "user" );

            PluginConfiguration config = Statistics2026.Plugin.Instance!.Configuration;

            List<Video> allVideosForUser = EmbyHelperFuncs.GetAllEpisodesAndMovies( user, LibraryManager() );
            if( allVideosForUser == null )
            {
                return ([], []);
            }

            //_tableList
            string sql =
                $"INSERT INTO {userTableName} " +
                "(" +
                    "  UserId" +
                    ", ItemId" +
                    ", IsPlayed" +
                    ", Name" +
                    ", PlayCount" +
                    ", LastPlayedDate" +
                    ", IsEpisode" +
                    ", NumEpisodes" +
                    ", IsTVSpecial" +
                    ", SeriesId" +
                ")" +
                " VALUES " +
                "(" +
                    "  @UserId" +
                    ", @ItemId" +
                    ", @IsPlayed" +
                    ", @Name" +
                    ", @PlayCount" +
                    ", @LastPlayedDate" +
                    ", @IsEpisode" +
                    ", @NumEpisodes" +
                    ", @IsTVSpecial" +
                    ", @SeriesId" +
                ") " +
                " ON CONFLICT(ItemId) " +
                " DO UPDATE " +
                " SET " +
                    "  IsPlayed=@IsPlayed" +
                    ", PlayCount=@PlayCount" +
                    ", LastPlayedDate=@LastPlayedDate" +
                    ", TotalTicksPlayed=@TotalTicksPlayed ";

            if( !config.systemSettings.resetPlayCount )
            {
                sql +=
                      " WHERE " +
                      " ItemId=@ItemId AND " +
                      "( ( TotalTicksPlayed IS NULL ) OR ( TotalTicksPlayed == 0 ) ) "
                      ;
            }

            int completedCount = 0;
            int totalVideos = allVideosForUser.Count;

            if( totalVideos == 0 )
            {
                return ([], allVideosForUser);
            }

            var orderedCmds = new SQLCmdDef?[ totalVideos ];
            string userIdStr = user.Id.ToString();
            bool isDebugEnabled = ConfigManager().CommonConfiguration?.EnableDebugLevelLogging ?? false;

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = CancellationToken ?? default
            };

            Parallel.For( 0, totalVideos, parallelOptions, index =>
            {
                Video video = allVideosForUser[ index ];
                if( video == null )
                    return;

                bool isPlayed = video.Played;
                int playCount = video.PlayCount;
                bool isFavorite = video.IsFavorite;

                if( config.systemSettings.resetPlayCount )
                    ResetPlayCount( user, video, ref isPlayed, ref playCount, ref isFavorite );

                DateTimeOffset? lastPlayedDate = video.LastPlayedDate;
                var mediaInfo = MediaInfo.CreateMinimal( Logger(), video );
                if( !mediaInfo.aOK )
                    return;

                if( !config.systemSettings.resetPlayCount )
                {
                    isPlayed = mediaInfo.IsPlayed ?? false;
                    playCount = mediaInfo.PlayCount ?? 0;
                }

                orderedCmds[ index ] = new SQLCmdDef( sql,
                [
                    ( "@UserId", userIdStr ),
                    ( "@ItemId", video.Id.ToString() ),
                    ( "@Name", mediaInfo.PrimaryName ),
                    ( "@IsEpisode", mediaInfo.IsEpisode ),
                    ( "@NumEpisodes", mediaInfo.NumEpisodes ),
                    ( "@IsTVSpecial", mediaInfo.IsTVSpecial ),
                    ( "@IsPlayed", isPlayed ),
                    ( "@PlayCount", playCount ),
                    ( "@LastPlayedDate", HelperFuncs.ToDateTimeParamValue( lastPlayedDate.HasValue ? lastPlayedDate.Value : null ) ),
                    ( "@SeriesId", mediaInfo.SeriesId ),
                    ( "@TotalTicksPlayed", mediaInfo.TotalTicksPlayed ),
                ] );

                int currProgress = Interlocked.Increment( ref completedCount );
                if( isDebugEnabled )
                {
                    Logger().Debug( $"User: {user.Name} {userIndex + 1} of {userCount} - Analyzed video '{mediaInfo.DescriptiveName}' - {currProgress} of {totalVideos}" );
                }
            } );

            var sqlCmds = new List<SQLCmdDef>( totalVideos + 1 );
            for( int ii = 0; ii < orderedCmds.Length; ii++ )
            {
                if( orderedCmds[ ii ] != null )
                {
                    sqlCmds.Add( orderedCmds[ ii ]! );
                }
            }

            if( config.systemSettings.resetPlayCount )
            {
                string sqlUpdateTicks =
                    $"UPDATE {userTableName} " +
                    $" SET " +
                    $"  TotalTicksPlayed=(IsPlayed*PlayCount)*Media.RunTimeTicks " +
                    $" FROM Media " +
                    $" WHERE Media.ItemId={userTableName}.ItemId"
                    ;
                sqlCmds.Add( new SQLCmdDef( sqlUpdateTicks ) );

                ValidateResetMapResults( user );
            }

            return (sqlCmds, allVideosForUser);
        }

        public async Task<long?> GetTotalTicksPlayed( string userId, string itemId )
        {
            string tableName = TableDefs().getUserTableName( userId );

            string sql =
                "SELECT " +
                "TotalTicksPlayed " +
                $"FROM {tableName} " +
                $"WHERE ItemId=@ItemId AND IsPlayed"
                ;
            List<(string, object?)> parameters = [ ("@ItemId", itemId) ];

            long? totalTicksPlayed = null;

            await ExecuteCommand( new SQLCmdDef( sql, parameters ), statement =>
            {
                IResultSet row = statement.Current;
                totalTicksPlayed = row.GetInt64( 0 );
                return Task.FromResult( true );
            } );

            return totalTicksPlayed;
        }

        public Task UpdateTotalTicksPlayed( string userId, string itemId, long totalTicksPlayed )
        {
            string tableName = TableDefs().getUserTableName( userId );

            string sql =
                $"UPDATE {tableName} " +
                "SET " +
                " TotalTicksPlayed=@TotalTicksPlayed " +
                "WHERE ItemId=@ItemId"
                ;
            List<(string, object?)> parameters =
                [
                    ("@TotalTicksPlayed", totalTicksPlayed ),
                    ("@ItemId", itemId)
                ];

            return ExecuteCommand( new SQLCmdDef( sql, parameters ) );
        }


#if DEBUG_PLUGIN
        private readonly Dictionary<string, (bool hit, int playCount)> _baseCount = new()
                        {
                            { "Rocky", ( false, 100 ) },
                            { "Star Wars", ( false, 200) },
                            { "The Empire Strikes Back", ( false, 100) },
                            { "Return of the Jedi", ( false, 100) },
                            { "Captain America: The First Avenger", ( false, 100) },
                            { "Captain America: Civil War", ( false, 20) },
                            { "Avengers: Endgame", ( false, 30) },
                            { "Top Gun: Maverick", ( false, 30) },
                            { "Caddyshack", ( false, 30) },
                            { "Wonder Woman", ( false, 20) },
                            { "Avengers: Infinity War", ( false, 10) },
                            { "The Avengers", ( false, 10) },
                            { "Fight Club", ( false, 10) },
                            { "Harry Potter and The Sorcerer's Stone", ( false, 10 ) },
                            { "Harry Potter and The Philosopher's Stone", ( false, 10 ) },
                            { "Harry Potter and The Chamber of Secrets", ( false, 10 ) },
                            { "Harry Potter and The Goblet of Fire", ( false, 10 ) },
                            { "Harry Potter and The Prisoner of Azkaban", ( false, 10 ) },
                            { "Harry Potter and The Order of the Phoenix", ( false, 10 ) },
                            { "Harry Potter and The Half-Blood Prince", ( false, 10 ) },
                            { "Harry Potter and The Deathly Hallows: Part 1", ( false, 10 ) },
                            { "Harry Potter and The Deathly Hallows: Part 2", ( false, 10 ) },
                            { "Tropic Thunder", ( false, 8) },
                            { "Ready Player One", ( false, 5) },
                            { "Rudy", ( false, 5) },
                            { "Free Guy", ( false, 4) },
                            { "Baby Driver", ( false, 3) },

                            { "The Sopranos", ( false, 20) },
                            { "Band of Brothers", ( false, 15) },
                            { "Sons of Anarchy", ( false, 10) },
                            { "Seinfeld", ( false, 5) },
                            { "South Park", ( false, 5) },
                            { "Better Call Saul", ( false, 5) },
                            { "Entourage", ( false, 5) },
                            { "Reacher", ( false, 2) },
                            { "Silicon Valley", ( false, 2) },
                            { "House", ( false, 2) },
                            { "Mr. Robot", ( false, 2) },
                            { "Schoolhouse Rock!", ( false, 2) },
                        };
        private static bool ResetMapFixed = false;

        private void FixResetMap()
        {
            if( ResetMapFixed )
                return;
            var tmp = new Dictionary<string, (bool, int)>( _baseCount );
            _baseCount.Clear();
            foreach( var curr in tmp )
            {
                _baseCount[ curr.Key.ToLower() ] = curr.Value;
            }

            ResetMapFixed = true;
        }
#endif

        private void ValidateResetMapResults( User user )
        {
#if DEBUG_PLUGIN
            if( user == null || user.Name != "scott" )
                return;

            _ = Statistics2026.Plugin.Instance!.Configuration;
            foreach( var curr in _baseCount )
            {
                if( curr.Value.hit == false )
                {
                    Logger().Warn( $"Video {curr.Key} not hit for scott" );
                }
            }
#endif
        }

        private (bool isCustomUser, bool justReturn) CustomResetForUser( Video video, User user, int playCount, ref int? newPlayCount, bool isPlayed, ref bool? newIsPlayed, bool isFavorite, ref bool? newFavorite )
        {
#if DEBUG_PLUGIN
            if( user.Name == "scott" )
            {
                var name = video!.Name;
                if( video is Episode episode )
                {
                    name = episode.Series.Name;
                }

                if( _baseCount.TryGetValue( name.ToLower(), out var pc ) )
                {
                    newPlayCount = pc.playCount;
                    _baseCount[ name.ToLower() ] = (true, pc.playCount);
                    newFavorite = true;
                }
                else
                {
                    if( playCount > 0 && !isPlayed )
                    {
                        newIsPlayed = true;
                        newPlayCount = 1;
                    }

                    newFavorite = false;
                }
                return (true, false);
            }
            else if( user.Name == "amy" )
            {
                var name = video!.Name;
                if( video is Episode episode )
                {
                    name = episode.Series.Name;
                }

                if( !isPlayed && playCount > 0 )
                {
                    newIsPlayed = true;
                    newPlayCount = playCount;
                }
                else if( name.StartsWith( "Outlander" ) )
                {
                    return (true, true);
                }
                return (true, false);
            }
#endif
            return (false, false);
        }

        private void ResetPlayCount( User user, Video video, ref bool isPlayed, ref int playCount, ref bool isFavorite )
        {
            CheckIsValid( ECheckLevel.eUpdate );

#if DEBUG_PLUGIN
            FixResetMap();
#endif

            bool? newIsPlayed = null;
            int? newPlayCount = null;
            DateTimeOffset? newLastPlayedDate = null;
            bool? newHideFromResume = null;
            bool? newFavorite = null;
            bool updateLastPlayedDate = false;

            UserItemData userData = UserDataManager().GetUserData( user, video );
            if( userData == null )
                return;

            if( user.Policy.IsAdministrator )
            {
                newIsPlayed = true;
                newPlayCount = 0;
                newLastPlayedDate = null;
                updateLastPlayedDate = true;
                newHideFromResume = true;
                newFavorite = false;
            }
            else
            {
                (bool isCustom, bool justReturn) = CustomResetForUser( video, user, playCount, ref newPlayCount, isPlayed, ref newIsPlayed, isFavorite, ref newFavorite );

                if( justReturn )
                {
                    return;
                }

                if( !isCustom )
                {
                    if( playCount > 0 && !isPlayed )
                    {
                        newIsPlayed = true;
                        newPlayCount = 1;
                    }
                }
            }

            bool update = false;
            if( newIsPlayed != null && ( userData.Played != newIsPlayed.Value ) )
            {
                userData.Played = newIsPlayed.Value;
                isPlayed = newIsPlayed.Value;
                update = true;
            }

            if( newPlayCount != null && ( userData.PlayCount != newPlayCount.Value ) )
            {
                userData.PlayCount = newPlayCount.Value;
                playCount = newPlayCount.Value;
                update = true;
            }

            if( updateLastPlayedDate )
            {
                bool wasIsNull = userData.LastPlayedDate == null;
                bool nowIsNull = newLastPlayedDate == null;
                if( wasIsNull != nowIsNull )
                {
                    userData.LastPlayedDate = newLastPlayedDate;
                    update = true;
                }
            }

            if( newHideFromResume != null && ( userData.HideFromResume != newHideFromResume.Value ) )
            {
                userData.HideFromResume = newHideFromResume.Value;
                update = true;
            }

            if( newFavorite != null && ( userData.IsFavorite != newFavorite.Value ) )
            {
                userData.IsFavorite = newFavorite.Value;
                update = true;
            }

            CancellationToken?.ThrowIfCancellationRequested();

            if( !update )
                return;

            if( CancellationToken == null )
                return;

            CancellationToken token = CancellationToken.Value;

            UserDataManager().SaveUserData( user, video, userData, UserDataSaveReason.Import, token );
        }

        public async Task<StatCard> TotalFinishedSeries( User? user )
        {
            CheckIsValid( ECheckLevel.eReport );

            if( user == null )
                throw new ArgumentNullException( "user" );

            string? tableName = TableDefs().getUserTableName( user );
            string sql =
                "SELECT " +
                    "  PrimaryName" +
                    ", Media.SeriesId " +
                    $", SUM({tableName}.NumEpisodes) " +
                    ", Series.NumEpisodes " +
                $"FROM {tableName} " +
                $"LEFT JOIN Media ON {tableName}.ItemId=Media.ItemId " +
                "LEFT JOIN Series ON Series.ItemId=Media.SeriesId " +
                $"WHERE Media.IsEpisode AND NOT Media.IsTVSpecial AND {tableName}.IsPlayed " +
                $"AND {tableName}.UserId=@UserId " +
                "GROUP BY Media.SeriesId"
                ;

            List<(string, object?)> parameters = [ ("@UserId", user.Id.ToString()) ];
            var seriesInfo = new Dictionary<string, (string name, long watched, long total)>();

            await ExecuteCommand( new SQLCmdDef( sql, parameters ), statement =>
            {
                IResultSet row = statement.Current;
                string seriesName = row.GetString( 0 );
                string seriesId = row.GetString( 1 );
                long numPlayed = row.GetInt64( 2 );
                long numEpisodes = row.GetInt64( 3 );
                if( numPlayed == numEpisodes )
                    seriesInfo[ seriesId ] = (seriesName, 0, numEpisodes);
                return Task.FromResult( true );
            } );

            var retVal = new TextBasedStatCard( Constants.TotalSeriesFinished, Constants.HelpTotalSeriesFinished, EStatCardStyle.eCompact );
            retVal.AddLine( seriesInfo.Count().ToString(), false );
            return retVal;
        }

        private async Task<List<List<WatchedMediaValue>>> WatchedMediaValues( User? user, bool leastWatched, EMediaType mediaType )
        {
            List<string> getClauses( string tableName, bool episodes, User? user )
            {
                List<string> clauses =
                    [
                        "PlayCount > 0",
                        $"{tableName}.TotalTicksPlayed > 0"
                    ];

                if( episodes )
                    clauses.Add( $"{tableName}.IsEpisode" );
                else
                    clauses.Add( $"NOT {tableName}.IsEpisode" );

                bool excludeAdmin = Statistics2026.Plugin.Instance!.Configuration.userReportSettings.excludeAdmin;
                if( ( user == null ) && excludeAdmin )
                {
                    clauses.Add( "NOT Users.IsAdministrator" );
                }

                if( user != null )
                {
                    clauses.Add( $"{tableName}.UserId = '{user.Id}'" );
                }

                return clauses;
            }

            CheckIsValid( ECheckLevel.eReport );

            bool excludeAdmin = Statistics2026.Plugin.Instance!.Configuration.userReportSettings.excludeAdmin;
            long numUsers = ( user == null ) ? ( await NumUsers( false, excludeAdmin ) ) : 1;

            var tableNames = new List<string>();
            if( user != null )
            {
                string? name = TableDefs().getUserTableName( user );
                if( name != null )
                    tableNames.Add( name );
            }
            else
            {
                tableNames = await TableDefs().allUserMediaTables();
            }

            var playMap = new Dictionary<string, WatchedMediaValueItemData>();
            foreach( string tableName in tableNames )
            {
                string sql = string.Empty;
                if( mediaType == EMediaType.eSeries )
                {
                    sql = "SELECT " +
                    $"  Series.ItemId" +
                    $", Series.Name" +
                    $", SUM(PlayCount) AS PlayCount " +
                    $", Series.NumEpisodes AS NumEpisodes" +
                    $", ((1.0 * SUM(PlayCount)) / (1.0 * Series.NumEpisodes)) AS PerUser " +
                    $"FROM Series " +
                    $"LEFT OUTER JOIN {tableName} ON Series.ItemId = {tableName}.SeriesId " +
                    $"LEFT OUTER JOIN Users ON {tableName}.UserId = Users.UserId ";

                    List<string> clauses = getClauses( tableName, true, user );
                    sql += HelperFuncs.JoinWhereClauses( clauses );

                    sql += $"GROUP BY SeriesId " +
                           $"ORDER BY PerUser ";
                    if( leastWatched )
                        sql += "ASC ";
                    else
                        sql += "DESC ";
                }
                else if( mediaType == EMediaType.eMovie )
                {
                    sql = "SELECT " +
                        $"  {tableName}.ItemId" +
                        $", {tableName}.Name" +
                        $", SUM({tableName}.PlayCount) AS PlayCount " +
                        $", 1 AS NumEpisodes" +
                        $", (1.0 * SUM({tableName}.PlayCount)) AS PerUser " +
                        $"FROM {tableName} " +
                        $"LEFT OUTER JOIN Users ON {tableName}.UserId = Users.UserId "
                        ;

                    List<string> clauses = getClauses( tableName, false, user );
                    sql += HelperFuncs.JoinWhereClauses( clauses );

                    sql += $"GROUP BY {tableName}.ItemId " +
                           $"ORDER BY PerUser ";
                    if( leastWatched )
                        sql += "ASC ";
                    else
                        sql += "DESC ";
                }

                await ExecuteCommand( new SQLCmdDef( sql ), statement =>
                {
                    IResultSet row = statement.Current;
                    int col = 0;
                    string id = row.GetString( col++ );
                    string name = row.GetString( col++ );
                    long playCount = row.GetInt64( col++ );
                    long numEpisodes = row.GetInt64( col++ );
                    double playCountPerUser = row.GetDouble( col++ );
                    if( playMap.TryGetValue( id, out (string id, string name, long playCount, long denominator, double playCountPerUser) currentValue ) )
                    {
                        // Safely updates based on the current value
                        playMap[ id ] = (id, name, currentValue.playCount + playCount, currentValue.denominator + numEpisodes, currentValue.playCountPerUser + playCountPerUser);
                    }
                    else
                    {
                        playMap[ id ] = (id, name, playCount, numEpisodes, playCountPerUser);
                    }

                    return Task.FromResult( true );
                } );
            }

            var sortedMapping = new SortedDictionary<long, List<WatchedMediaValue>>();

            int numResultsToGet = Statistics2026.Plugin.Instance!.Configuration.NumWatchedToReport( ( mediaType == EMediaType.eMovie ) ? EReportType.eMovieReport : EReportType.eTVReport );
            int maxPerGroup = Statistics2026.Plugin.Instance!.Configuration.NumTiedToReport( ( mediaType == EMediaType.eMovie ) ? EReportType.eMovieReport : EReportType.eTVReport );

            foreach( (string id, string name, long playCount, long denominator, double playCountPerUser) curr in playMap.Values )
            {
                (string? id, string? name, long playCount, long denominator, double playCountPerUser) = curr;
                long key = (long)( 100 * playCountPerUser );

                var item = new WatchedMediaValue()
                {
                    ItemId = id,
                    Name = name,
                    PlayCount = playCount,
                    Denominator = denominator,
                    PlayCountPerUser = playCountPerUser,
                    MediaType = mediaType
                };

                if( sortedMapping.TryGetValue( key, out List<WatchedMediaValue>? value ) )
                {
                    value.Add( item );
                    sortedMapping[ key ] = value;
                }
                else
                {
                    sortedMapping[ key ] = [ item ];
                }
            }

            var retVal = new List<List<WatchedMediaValue>>();

            List<WatchedMediaValue> cleanCurrList( List<WatchedMediaValue> inList )
            {
                int numToKeep = Math.Min( inList.Count, maxPerGroup );
                var subList = inList.Take( numToKeep ).ToList();

                for( int ii = 0; ii < numToKeep; ++ii )
                {
                    WatchedMediaValue item = subList[ ii ];
                    item.ImageUrl = GetItemImageUrl.ItemImageUrlFromItemId( item.ItemId, LibraryManager() );
                    subList[ ii ] = item;
                }

                return subList;
            }

            if( leastWatched )
            {
                foreach( List<WatchedMediaValue> curr in sortedMapping.Values )
                {
                    if( retVal.Count >= numResultsToGet )
                        break;

                    retVal.Add( cleanCurrList( curr ) );
                }
            }
            else
            {
                foreach( KeyValuePair<long, List<WatchedMediaValue>> curr in sortedMapping.Reverse() )
                {
                    if( retVal.Count >= numResultsToGet )
                        break;

                    retVal.Add( cleanCurrList( curr.Value ) );
                }
            }

            return retVal;
        }

        public async Task<StatCard> WatchedMedia( User? user, bool leastWatched, EMediaType mediaType )
        {
            List<List<WatchedMediaValue>> watchedMedia = await WatchedMediaValues( user, leastWatched, mediaType );
            string title = string.Empty;
            string help = string.Empty;

            bool isForTV = ( ( mediaType == EMediaType.eSeries ) || ( mediaType == EMediaType.eEpisode ) );
            if( isForTV )
            {
                title = leastWatched ? Constants.LeastWatchedShows : Constants.MostWatchedShows;
                help = leastWatched ? Constants.HelpLeastWatchedShows : Constants.HelpMostWatchedShows;
            }
            else
            {
                title = leastWatched ? Constants.LeastWatchedMovies : Constants.MostWatchedMovies;
                help = leastWatched ? Constants.HelpLeastWatchedMovies : Constants.HelpMostWatchedMovies;
            }

            var retVal = new GroupedTextBasedStatCard( title, help, EStatCardStyle.eDetailed )
            {
                SubTitle = ( user == null ) ? "(Watched across Users)" : string.Empty,
                ListType = EListType.eNumbered
            };

            EReportType reportType = ( user != null ) ? EReportType.eUserReport : ( isForTV ? EReportType.eTVReport : EReportType.eMovieReport );
            bool showImages = Plugin.Instance!.Configuration.ShowImages( reportType );
            foreach( List<WatchedMediaValue> currList in watchedMedia )
            {
                foreach( WatchedMediaValue curr in currList )
                {
                    long key = (long)( 100 * curr.PlayCountPerUser );
                    retVal.AddLine( key.ToString(), ($"{curr.Title()}", curr.ItemId, showImages ? curr.ImageUrl : string.Empty, false) );
                }
            }

            if( watchedMedia.Count == 0 )
            {
                string? name;
                if( mediaType == EMediaType.eSeries )
                    name = "TV Shows";
                else if( mediaType == EMediaType.eEpisode )
                    name = "TV Episodes";
                else // mediaType == EMediaType.eMovies
                    name = "Movies";
                retVal.AddLine( $"Watch some {name} already!", false );
            }

            return retVal;
        }

        private async Task<StatCard> TotalTime( User? user, bool? episodesOnly, bool played )
        {
            CheckIsValid( ECheckLevel.eReport );

            if( user == null )
                throw new ArgumentNullException( "user" );

            string? tableName = TableDefs().getUserTableName( user );
            string sql = string.Empty;

            if( played )
            {
                sql = "SELECT SUM(TotalTicksPlayed) " +
                    $"FROM {tableName} "
                ;
            }
            else
            {
                sql = "SELECT SUM(RunTimeTicks) " +
                    $"FROM {tableName} " +
                    $"LEFT JOIN Media ON {tableName}.ItemId=Media.ItemId "
                    ;
            }
            List<string> clauses = [ $"{tableName}.UserId=@UserId" ];
            string title = string.Empty;

            if( episodesOnly == null )
            {
                title = played ? Constants.UserTotalTimeWatched : Constants.UserTotalWatchableTime;
            }
            else if( episodesOnly.Value )
            {
                title = played ? Constants.UserTotalEpisodeTimeWatched : Constants.UserTotalEpisodeWatchableTime;
                clauses.Add( $"{tableName}.IsEpisode" );
            }
            else
            {
                title = played ? Constants.UserTotalMovieTimeWatched : Constants.UserTotalMovieWatchableTime;
                clauses.Add( $"NOT {tableName}.IsEpisode" );
            }

            if( played )
                clauses.Add( $"{tableName}.IsPlayed" );

            sql += HelperFuncs.JoinWhereClauses( clauses );

            return await ValueGroupForSingleItem( title, null, sql, [ ("@UserId", user.Id.ToString()) ], FormatTicks );
        }

        public Task<StatCard> TotalTimeWatched( User? user, bool? episodesOnly )
        {
            return TotalTime( user, episodesOnly, true );
        }

        public Task<StatCard> TotalWatchableTime( User? user, bool? episodesOnly )
        {
            return TotalTime( user, episodesOnly, false );
        }

        private async Task<List<(string name, DateTimeOffset lastPlayed)>> LastSeenValues( User? user, bool movies )
        {
            CheckIsValid( ECheckLevel.eReport );

            if( user == null )
                throw new ArgumentNullException( "user" );

            string? tableName = TableDefs().getUserTableName( user );

            string sql = "SELECT ";
            if( movies )
                sql += "PrimaryName ";
            else
                sql += "PrimaryName || ' - S' || printf('%02d', Season ) || 'E' || printf('%02d', Episode) || ' - ' || SecondaryName ";
            sql += "AS Name " +
                   ", LastPlayedDate " +
                   $"FROM {tableName} " +
                   $"LEFT JOIN Media ON Media.ItemId={tableName}.ItemId " +
                   $"WHERE {tableName}.IsPlayed " +
                   $"AND " + StatGen.validDateClause( $"{tableName}.LastPlayedDate" ) +
                   $"AND {tableName}.UserId = @UserId " +
                   "AND "
                   ;
            if( movies )
                sql += "NOT";
            sql += $" {tableName}.IsEpisode " +
               $"ORDER BY {tableName}.LastPlayedDate DESC " +
               "LIMIT 10 "
               ;

            var sqlCmd = new SQLCmdDef( sql,
            [
                ( "@UserId", user.Id.ToString())
            ] );

            var retVal = new List<(string genre, DateTimeOffset lastPlayed)>();
            await ExecuteCommand( sqlCmd, statement =>
            {
                IResultSet row = statement.Current;
                string name = row.GetString( 0 );
                string date = row.GetString( 1 );
                DateTimeOffset? lastPlayedDate = HelperFuncs.ReadDateTime( date );
                if( lastPlayedDate == null )
                    lastPlayedDate = DateTimeOffset.MinValue;
                retVal.Add( (name, lastPlayedDate.Value) );
                return Task.FromResult( true );
            } );

            return retVal;
        }

        public async Task<StatCard> LastSeen( User? user, bool movies )
        {
            CheckIsValid( ECheckLevel.eReport );

            if( user == null )
                throw new ArgumentNullException( "user" );

            string? title;

            string? help;
            if( movies )
            {
                title = Constants.LastSeenMovies;
                help = Constants.HelpLastSeenMovies;
            }
            else
            {
                title = Constants.LastSeenTVSeries;
                help = Constants.HelpLastSeenTVSeries;
            }

            var retVal = new TextBasedStatCard( title, help, EStatCardStyle.eDetailed )
            {
                ListType = EListType.eNumbered,
                IgnoreLength = true
            };
            List<(string name, DateTimeOffset lastPlayed)> values = await LastSeenValues( user, movies );

            foreach( (string? name, DateTimeOffset lastPlayed) in values )
            {
                retVal.AddLine( $"{name} - {lastPlayed:d}", false );
            }

            if( values.Count == 0 )
            {
                string name = movies ? "Movies" : "TV Shows";
                retVal.AddLine( $"Watch some {name} already!", false );
            }

            return retVal;
        }

        private async Task<List<List<object>>?> CheckUserMediaTableHasData( string tableName )
        {
            string sqlTicks = $"SELECT COUNT(*) FROM {tableName} WHERE TotalTicksPlayed IS NOT NULL AND iif( {tableName}.IsPlayed AND ( {tableName}.PlayCount > 0 ), {tableName}.TotalTicksPlayed, 0 ) != 0";
            string sqlPlayed = $"SELECT COUNT(*) FROM {tableName} WHERE PlayCount>0 AND IsPlayed";

            long? ticksPlayed = null;
            long systemPlayed = 0;

            List<SQLCmdDef> cmds =
            [
                new( sqlTicks ),
                new( sqlPlayed )
            ];

            await ExecuteCommands( cmds, false, statement =>
                {
                    IResultSet row = statement.Current;
                    long value = row.GetInt64( 0 );
                    if( ticksPlayed == null )
                        ticksPlayed = value;
                    else
                        systemPlayed = value;
                    return Task.FromResult( true );
                }
            );

            ticksPlayed ??= 0;

            //Logger().Debug( $"Table: {tableName} - # w/TicksPlayed {ticksPlayed} - # w/PlayCount {systemPlayed}" );
            List<List<object>>? missing = null;
            if( ticksPlayed != systemPlayed && ticksPlayed != 0 )
            {
                User? user = TableDefs().getUserForTableName( tableName );
                if( user == null )
                {
                    Logger().Warn( $"Invalid user table name {tableName}" );
                    return [];
                }
                else
                {
                    Logger().Warn( $"User: {user.Name} has an invalid UserMedia table" );
                }

                string fields = $"Users.UserName, {tableName}.ItemID, {tableName}.Name, Media.RunTimeTicks, {tableName}.IsPlayed, {tableName}.PlayCount"
                    + $", iif( {tableName}.IsPlayed,  {tableName}.TotalTicksPlayed, 0 )"
                    + $", Series.Name, Media.Season, Media.Episode";
                string joinClause = $" LEFT JOIN Users ON Users.UserId={tableName}.UserId LEFT JOIN Media ON Media.ItemId = {tableName}.ItemId LEFT JOIN Series On Series.ItemId={tableName}.SeriesId";

                int pos = sqlTicks.IndexOf( "WHERE" );
                if( pos != -1 )
                    sqlTicks = sqlTicks.Insert( pos, joinClause + " " );

                pos = sqlPlayed.IndexOf( "WHERE" );
                if( pos != -1 )
                    sqlPlayed = sqlPlayed.Insert( pos, joinClause + " " );

                string ticksNotPlayed = sqlTicks + " EXCEPT " + sqlPlayed;
                ticksNotPlayed = ticksNotPlayed.Replace( "COUNT(*)", fields );

                string playedNotTicks = sqlPlayed + " EXCEPT " + sqlTicks;
                playedNotTicks = playedNotTicks.Replace( "COUNT(*)", fields );

                List<List<object>> ticksNotPlayedList = [];
                await ExecuteCommand( new SQLCmdDef( ticksNotPlayed ), statement =>
                {
                    IResultSet row = statement.Current;
                    int col = 0;

                    string userName = row.GetString( col++ );
                    string itemId = row.GetString( col++ );
                    string itemName = row.GetString( col++ );
                    long runtimeTicks = row.GetInt64( col++ );
                    bool isPlayed = row.GetBoolean( col++ );
                    long playCount = row.GetInt64( col++ );
                    long totalTicksPlayed = row.GetInt64( col++ );
                    string seriesName = row.GetString( col++ );
                    long season = row.GetInt64( col++ );
                    long episode = row.GetInt64( col++ );

                    ticksNotPlayedList.Add( [ userName, itemId, itemName, runtimeTicks, isPlayed, playCount, totalTicksPlayed, seriesName, season, episode ] );

                    return Task.FromResult( true );
                } );

                List<List<object>> playedNotTicksList = [];
                await ExecuteCommand( new SQLCmdDef( playedNotTicks ), statement =>
                {
                    IResultSet row = statement.Current;
                    int col = 0;

                    string userName = row.GetString( col++ );
                    string itemId = row.GetString( col++ );
                    string itemName = row.GetString( col++ );
                    long runtimeTicks = row.GetInt64( col++ );
                    bool isPlayed = row.GetBoolean( col++ );
                    long playCount = row.GetInt64( col++ );
                    long totalTicksPlayed = row.GetInt64( col++ );
                    string seriesName = row.GetString( col++ );
                    long season = row.GetInt64( col++ );
                    long episode = row.GetInt64( col++ );

                    playedNotTicksList.Add( [ userName, itemId, itemName, runtimeTicks, isPlayed, playCount, totalTicksPlayed, seriesName, season, episode ] );

                    return Task.FromResult( true );
                } );

                missing = [];
                missing.AddRange( ticksNotPlayedList );
                missing.AddRange( playedNotTicksList );
            }

            return ( ticksPlayed == systemPlayed ) ? null : missing;
        }

        public async Task<StatCard?> UserWatchMediaIssues()
        {
            CheckIsValid( ECheckLevel.eReport );

            string fields =
                "  Users.UserName\n" +
                ", Media.PrimaryName\n" +
                ", Media.SecondaryName\n" +
                ", Media.Season\n" +
                ", Media.Episode\n" +
                ", Media.RunTimeTicks\n" +
                ", <TABLE_NAME>.IsPlayed\n" +
                ", <TABLE_NAME>.PlayCount\n" +
                ", <TABLE_NAME>.TotalTicksPlayed\n"
                ;

            string joinClause =
                "LEFT JOIN Users ON Users.UserId=<TABLE_NAME>.UserId\n" +
                "LEFT JOIN Media ON Media.ItemId = <TABLE_NAME>.ItemId\n"
                ;

            string sqlTicks =
                "SELECT\n" +
                fields +
                "FROM <TABLE_NAME>\n" +
                joinClause +
                "WHERE <TABLE_NAME>.TotalTicksPlayed IS NOT NULL AND iif( <TABLE_NAME>.IsPlayed, <TABLE_NAME>.TotalTicksPlayed, 0 ) != 0\n";

            string sqlPlayed =
                "SELECT\n" +
                fields +
                "FROM <TABLE_NAME>\n" +
                joinClause +
                "WHERE PlayCount>0 AND IsPlayed\n";

            string sqlAtoB = sqlTicks + " EXCEPT \n" + sqlPlayed;

            string sqlBtoA = sqlPlayed + " EXCEPT \n" + sqlTicks;

            List<SQLCmdDef> sqlCmds = [];
            List<string> tables = await TableDefs().allUserMediaTables();
            foreach( string tableName in tables )
            {
                User? user = TableDefs().getUserForTableName( tableName );
                if( ( user == null ) || ( user.Policy.IsAdministrator ) )
                {
                    continue;
                }
                string curr = sqlAtoB;
                curr = curr.Replace( "<TABLE_NAME>", tableName );
                sqlCmds.Add( new SQLCmdDef( curr ) );

                curr = sqlBtoA;
                curr = curr.Replace( "<TABLE_NAME>", tableName );
                sqlCmds.Add( new SQLCmdDef( curr ) );
            }

            var groupData = new TableBasedStatCard( Constants.WatchedUserMediaIssues, Constants.HelpWatchedUserMediaIssues,
                [ "User Name", "Media Name", "Played", "Play Count", "Run Time", "Total Played" ], EStatCardStyle.eDetailed )
            {
                UseSeparators = true,
                ShowCategory = false
            };

            SortedDictionary<string, List<object>> items = [];

            await ExecuteCommands( sqlCmds, true, statement =>
            {
                IResultSet row = statement.Current;
                int col = 0;

                string userName = row.GetString( col++ );
                string primaryName = row.GetString( col++ );
                string secondaryName = row.GetString( col++ );
                int season = row.GetInt( col++ );
                int episode = row.GetInt( col++ );
                long runTimeTicks = row.GetInt64( col++ );
                bool isPlayed = row.GetBoolean( col++ );
                long playCount = row.GetInt64( col++ );
                long totalTicksPlayed = row.GetInt64( col++ );

                string mediaName = MediaInfo.GetDisplayName( primaryName, secondaryName, season, episode );
                if( mediaName == null || mediaName == "" )
                {
                    return Task.FromResult( true );
                }
                var rtTicks = new RunTime( runTimeTicks );
                var totalTicks = new RunTime( totalTicksPlayed );

                items[ userName + "-" + mediaName ] = [ userName, mediaName, isPlayed ? "Yes" : "No", playCount, rtTicks.ToShortString(), totalTicks.ToShortString() ];
                return Task.FromResult( true );
            } );

            foreach( KeyValuePair<string, List<object>> kvp in items )
            {
                groupData.addRow( kvp.Key, kvp.Value );
            }
            if( groupData.IsEmpty() )
            {
                return null;
            }
            return groupData;
        }

        public async Task<StatCard> PlayedUserMedia()
        {
            CheckIsValid( ECheckLevel.eReport );

            List<string> tables = await TableDefs().allUserMediaTables();

            string sqlBase =
                $"SELECT \n" +
                $"    Users.UserName\n" +
                $"  , Media.PrimaryName\n" +
                $"  , Media.SecondaryName\n" +
                $"  , Media.Season\n" +
                $"  , Media.Episode\n" +
                $"  , <TABLE_NAME>.TotalTicksPlayed\n" +
                $"  , (IsPlayed*PlayCount)*Media.RunTimeTicks AS PlayCountBasedTotalTicksPlayed\n" +
                $" FROM <TABLE_NAME>\n" +
                $" LEFT JOIN Users ON <TABLE_NAME>.UserId=Users.UserId\n" +
                $" LEFT JOIN Media ON <TABLE_NAME>.ItemId=Media.ItemId\n" +
                $" WHERE iif( <TABLE_NAME>.IsPlayed, <TABLE_NAME>.TotalTicksPlayed, 0 )  != (IsPlayed*PlayCount)*Media.RunTimeTicks\n"
                ;

            List<string> sqlCmds = [];
            foreach( string tableName in tables )
            {
                string curr = sqlBase;
                curr = curr.Replace( "<TABLE_NAME>", tableName );
                sqlCmds.Add( curr );
            }

            string sql = sqlCmds.Join( "\nUNION\n\n" );
            var groupData = new TableBasedStatCard( Constants.MediaTimePlayedPerUser, Constants.HelpMediaTimePlayedPerUser,
                    [
                        [ "", "", new HeaderDef( "Time Played", 2 ), "" ],
                        [ "User Name", "Media Name", "Play Count * Runtime", "Tracked", "Difference" ]
                    ], EStatCardStyle.eDetailed )
            {
                UseSeparators = true,
                ShowCategory = false
            };
            groupData.SetDataColumnAlignment( 2, StatCard.EAlignment.eRight );
            groupData.SetDataColumnAlignment( 3, StatCard.EAlignment.eRight );
            groupData.SetDataColumnAlignment( 4, StatCard.EAlignment.eRight );
            groupData.SetClassForColumnFunc(
                ( int column, string value ) =>
                {
                    if( ( column >= 2 && column <= 4 ) && value.TrimStart().StartsWith( "-" ) )
                    {
                        return [ "override-red" ];
                    }
                    return [];
                } );

            SortedDictionary<string, List<object>> items = [];

            await ExecuteCommand( new SQLCmdDef( sql ), statement =>
            {
                IResultSet row = statement.Current;

                int col = 0;
                string userName = row.GetString( col++ );
                string primaryName = row.GetString( col++ );
                string secondaryName = row.GetString( col++ );
                int season = row.GetInt( col++ );
                int episode = row.GetInt( col++ );
                long totalTicksPlayed = row.GetInt64( col++ );
                long playCountBasedTotalTicksPlayed = row.GetInt64( col++ );

                string mediaName = MediaInfo.GetDisplayName( primaryName, secondaryName, season, episode );
                var rtTotal = new RunTime( totalTicksPlayed );
                var playCountRT = new RunTime( playCountBasedTotalTicksPlayed );
                var diffRT = new RunTime( playCountBasedTotalTicksPlayed - totalTicksPlayed );

                items[ userName + "-" + mediaName ] = [ userName, mediaName, rtTotal.ToShortString(), playCountRT.ToShortString(), diffRT.ToShortString() ];
                return Task.FromResult( true );
            } );

            string prevUser = string.Empty;
            foreach( KeyValuePair<string, List<object>> kvp in items )
            {
                if( prevUser != kvp.Value[ 0 ].ToString() )
                {
                    groupData.addRow( kvp.Value[ 0 ].ToString(), [ kvp.Value[ 0 ].ToString() ], true );
                }

                List<object> values = kvp.Value;
                values[ 0 ] = string.Empty;
                groupData.addRow( kvp.Key, values );
            }

            return groupData;
        }
    }
}
