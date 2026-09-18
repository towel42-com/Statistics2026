using Emby.Naming.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using RestSharp;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using WatchedMediaValueItemData = (string id, string name, long playCount, long denominator, double playCountPerUser);

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public class WatchedMediaValue
        {
            public string ItemId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string ImageUrl { get; set; } = string.Empty;
            public long PlayCount { get; set; } = 0;
            public long Denominator { get; set; } = 0;
            public double PlayCountPerUser { get; set; } = 0.0;
            public EMediaType MediaType { get; set; } = EMediaType.eEpisode;

            public string Title()
            {
                var title = Name;
                if( MediaType == EMediaType.eMovie )
                {
                    title += $" - Watched {PlayCount} time";

                    if( PlayCount != 1 )
                        title += "s";
                }
                else
                {
                    if( Denominator == PlayCount )
                    {
                        title += $" - {Denominator} Episodes played 1 time each";
                    }
                    else
                    {
                        title += $" - For {Denominator} Episodes, a total of {PlayCount} play";
                        if( PlayCount != 1 )
                            title += "s";
                    }
                }

                return title;
            }
        }

        public void InitUserWatchData()
        {
            CheckIsValid( ECheckType.eInit );

            if( Plugin.Instance == null )
                throw new NullReferenceException( $"Plugin.Instance is null" );

            if( !Plugin.Instance.IsDBStateSet( EDBState.eUserTablesCreated ) )
            {

                _dbHelper!.Progress?.Report( 0 );
                var users = _embyInterfaces?._userManager.GetUserList( new UserQuery() { EnableRemoteAccess = true } ).ToList();
                if( users == null )
                {
                    _dbHelper!.Progress?.Report( 100 );
                    return;
                }

                _dbHelper!.Progress?.Report( 100 );

                var sqlCmds = new List<SQLCmdDef>();
                var curr = 0.0;
                double count = users.Count;

                using( var timer = new AutoTimer( $"    Analyze User Watch Data - Getting Init Table Commands", _embyInterfaces?._logger ) )
                {
                    curr = 0;
                    foreach( var user in users )
                    {
                        _dbHelper!.Progress?.Report( 80.0 * ( ++curr ) / count );
                        using( var userTimer = new AutoTimer( $"AnalyzeUserWatchData -     Processed User ({curr} of {count}) - {user.Name}", _embyInterfaces?._logger ) )
                        {
                            sqlCmds.AddRange( GetUserWatchInitTableCommands( user ) );
                            _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();
                        }
                    }

                    _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();
                }

                using( var timer = new AutoTimer( $"    Analyze User Watch Data - Executing Init Table Commands", _embyInterfaces?._logger ) )
                {
                    _dbHelper!.Progress?.Report( 80 );
                    _dbHelper.ExecuteCommands( sqlCmds );
                    _dbHelper!.Progress?.Report( 100 );
                }

                Plugin.Instance.AddDBState( EDBState.eUserTablesCreated );
            }
        }

        public long? GetTotalTicksPlayed( string userId, string itemId )
        {
            var tableName = getUserTableName( userId );

            var sql =
                "SELECT " +
                "TotalTicksPlayed " +
                $"FROM {tableName} " +
                $"WHERE ItemId=@ItemId"
                ;
            List<(string, object?)> parameters = [ ("@ItemId", itemId) ];

            long? totalTicksPlayed = null;

            _dbHelper.ExecuteCommand( new SQLCmdDef( sql, parameters ), statement =>
            {
                var row = statement.Current;
                totalTicksPlayed = row.GetInt64( 0 );
                return true;
            } );

            return totalTicksPlayed;
        }

        public void UpdateTotalTicksPlayed( string userId, string itemId, long totalTicksPlayed )
        {
            var tableName = getUserTableName( userId );

            var sql =
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

            _dbHelper.ExecuteCommand( new SQLCmdDef( sql, parameters ) );
        }

        public void AnalyzeUserWatchData()
        {
            CheckIsValid( ECheckType.eUpdate );

            _dbHelper!.Progress?.Report( 0 );
            var users = _embyInterfaces?._userManager.GetUserList( new UserQuery() { EnableRemoteAccess = true } ).ToList();
            if( users == null )
                return;
            _dbHelper!.Progress?.Report( 100 );

            _embyInterfaces?._logger?.Debug( $"AnalyzeUserWatchData - Starting User Watch Data Analysis" );

            double count = users.Count;
            double curr = 0;

            _dbHelper!.Progress?.Report( 0 );
            var sqlCmds = new List<SQLCmdDef>();
            using( var timer = new AutoTimer( $"    Analyze User Watch Data - Getting Commands", _embyInterfaces?._logger ) )
            {
                curr = 0;
                foreach( var user in users )
                {
                    _dbHelper!.Progress?.Report( 80.0 * ( ++curr ) / count );
                    using( var userTimer = new AutoTimer( $"AnalyzeUserWatchData -     Processed User ({curr} of {count}) - {user.Name}", _embyInterfaces?._logger ) )
                    {
                        sqlCmds.AddRange( AddUserWatchData( user ) );
                        _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();
                    }
                }

                _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();
            }

            var config = Statistics2026.Plugin.Instance!.Configuration;
            config.resetPlayCount = false;
            Statistics2026.Plugin.Instance.UpdateConfiguration( config );

            using( var timer = new AutoTimer( $"    Analyze User Watch Data - Executing Commands", _embyInterfaces?._logger ) )
            {
                _dbHelper!.Progress?.Report( 80 );
                _dbHelper.ExecuteCommands( sqlCmds );
                _dbHelper!.Progress?.Report( 100 );
            }

            ComputeUserDataDBState();
            if( !Plugin.Instance.IsDBStateSet( EDBState.eUserDataInitialized ) )
            {
                _embyInterfaces?._logger?.Error( "After initializing user watch data, user watch data still doesn't conform." );
            }

            Plugin.Instance.AddDBState( EDBState.eUserDataInitialized );
            _embyInterfaces?._logger?.Debug( $"AnalyzeUserWatchData - Finished User Watch Data Analysis" );
        }

        public List<SQLCmdDef> DropAllUserMediaCmds()
        {
            var retVal = new List<SQLCmdDef>();
            if( _userMediaTemplate == null )
                return retVal;

            var tables = allUserMediaTables();

            foreach( var table in tables )
                retVal.AddRange( DropTableCmds( table ) );

            return retVal;
        }

        public List<string> allUserMediaTables()
        {
            return allTables( (regex: "UserMedia_%", like: true) );
        }

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

        private void ValidateResetMapResults()
        {
            _ = Statistics2026.Plugin.Instance!.Configuration;
            foreach( var curr in _baseCount )
            {
                if( curr.Value.hit == false )
                {
                    _embyInterfaces!._logger!.Warn( $"Video {curr.Key} not hit for scott" );
                }
            }
        }

        private void ResetPlayCount( User user, Video video, ref bool isPlayed, ref int playCount )
        {
            CheckIsValid( ECheckType.eUpdate );

            FixResetMap();

            bool? newIsPlayed = null;
            int? newPlayCount = null;
            DateTimeOffset? newLastPlayedDate = null;
            bool? newHideFromResume = null;
            bool? newFavorite = null;
            var updateLastPlayedDate = false;

            var userData = _embyInterfaces!._userDataManager.GetUserData( user, video );
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
            else if( user.Name == "scott" )
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
            }
            else
            {
                if( playCount > 0 && !isPlayed )
                {
                    newIsPlayed = true;
                    newPlayCount = 1;
                }
            }

            if( user.Name == "amy" )
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
                    return;
                }
            }

            var update = false;
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
                var wasIsNull = userData.LastPlayedDate == null;
                var nowIsNull = newLastPlayedDate == null;
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

            _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();

            if( !update )
                return;

            if( _dbHelper.CancellationToken == null )
                return;

            var token = _dbHelper!.CancellationToken.Value;

            _embyInterfaces._userDataManager.SaveUserData( user, video, userData, UserDataSaveReason.Import, token );
        }

        public List<SQLCmdDef> GetUserWatchInitTableCommands( User? user )
        {
            if( _userMediaTemplate == null )
                throw new Exception( $"GetUserWatchInitTableCommands: TableDef for UserMedia_<USER_ID> is null" );

            if( user == null )
                throw new Exception( $"GetUserWatchInitTableCommands: User is null" );

            var userTableName = getUserTableName( user );

            var sqlCmds = new List<SQLCmdDef>();

            var cmds = _userMediaTemplate.GetSQLCommands( TableDef.EAction.eCreate );
            for( var ii = 0; ii < cmds.Count(); ++ii )
            {
                var cmd = new SQLCmdDef( cmds[ ii ] );
                cmd.Replace( "UserMedia_<USER_ID>", userTableName );
                sqlCmds.Add( cmd );
            }

            return sqlCmds;
        }

        private List<SQLCmdDef> AddUserWatchData( User? user )
        {
            CheckIsValid( ECheckType.eUpdate );

            if( user == null )
                throw new ArgumentNullException( "user" );

            var userTableName = getUserTableName( user );
            if( _userMediaTemplate == null )
                throw new Exception( $"AddUserWatchData: TableDef for UserMedia_<USER_ID> is null" );

            var sqlCmds = new List<SQLCmdDef>();

            var config = Statistics2026.Plugin.Instance!.Configuration;
            var resetPlayCount = config.resetPlayCount || !Plugin.Instance.IsDBStateSet( EDBState.eUserDataInitialized );

            var allVideosForUser = Statistics2026API.GetAllEpisodesAndMovies( user, _embyInterfaces!._libraryManager, false ).forUser;
            //_tableList
            var sql =
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
                " WHERE " +
                " ItemId=@ItemId"
                ;

            foreach( var video in allVideosForUser )
            {
                if( video == null )
                    continue;

                var isPlayed = video.Played;
                var playCount = video.PlayCount;

                if( resetPlayCount )
                    ResetPlayCount( user, video, ref isPlayed, ref playCount );
                var lastPlayedDate = video?.LastPlayedDate ?? null;

                using( var mediaInfo = new MediaInfo( video! ) )
                {
                    if( !mediaInfo.aOK )
                        continue;

                    sqlCmds.Add( new SQLCmdDef( sql,
                        [
                            ( "@UserId", user.Id.ToString()),
                            ( "@ItemId", video?.Id.ToString() ?? string.Empty),
                            ( "@Name", mediaInfo.PrimaryName),
                            ( "@IsEpisode", mediaInfo.IsEpisode),
                            ( "@NumEpisodes", mediaInfo.NumEpisodes),
                            ( "@IsTVSpecial", mediaInfo.IsTVSpecial),
                            ( "@IsPlayed", isPlayed),
                            ( "@PlayCount", playCount),
                            ( "@LastPlayedDate", _dbHelper.ToDateTimeParamValue( lastPlayedDate.HasValue ? lastPlayedDate.Value.DateTime : null )),
                            ( "@SeriesId", mediaInfo.SeriesId)
                        ] ) );
                }
            }

            if( resetPlayCount )
            {
                var sqlUpdateTicks =
                    $"UPDATE {userTableName} " +
                    $" SET " +
                    $"  TotalTicksPlayed=(IsPlayed*PlayCount)*Media.RunTimeTicks " +
                    $" FROM Media " +
                    $" WHERE Media.ItemId={userTableName}.ItemId"
                    ;
                sqlCmds.Add( new SQLCmdDef( sqlUpdateTicks ) );

                if( user.Name == "scott" )
                    ValidateResetMapResults();
            }

            return sqlCmds;
        }

        public StatCard TotalFinishedSeries( User? user )
        {
            CheckIsValid( ECheckType.eReport );

            if( user == null )
                throw new ArgumentNullException( "user" );

            var tableName = getUserTableName( user );
            var sql =
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

            _dbHelper.ExecuteCommand( new SQLCmdDef( sql, parameters ), statement =>
            {
                var row = statement.Current;
                var seriesName = row.GetString( 0 );
                var seriesId = row.GetString( 1 );
                var numPlayed = row.GetInt64( 2 );
                var numEpisodes = row.GetInt64( 3 );
                if( numPlayed == numEpisodes )
                    seriesInfo[ seriesId ] = (seriesName, 0, numEpisodes);
                return true;
            } );

            var retVal = new TextBasedStatCard( Constants.TotalSeriesFinished, Constants.HelpTotalSeriesFinished, EStatCardSize.eSmall );
            retVal.AddLine( seriesInfo.Count().ToString() );
            return retVal;
        }

        public List<List<WatchedMediaValue>> WatchedMediaValues( User? user, bool leastWatched, EMediaType mediaType )
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

                var excludeAdmin = Statistics2026.Plugin.Instance!.Configuration.excludeAdmin;
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
            CheckIsValid( ECheckType.eReport );

            var excludeAdmin = Statistics2026.Plugin.Instance!.Configuration.excludeAdmin;
            var numUsers = ( user == null ) ? NumUsers( false, excludeAdmin ) : 1;

            var tableNames = new List<string>();
            if( user != null )
            {
                tableNames.Add( getUserTableName( user ) );
            }
            else
            {
                tableNames = allUserMediaTables();
            }

            var playMap = new Dictionary<string, WatchedMediaValueItemData>();
            foreach( var tableName in tableNames )
            {
                var sql = string.Empty;
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

                    var clauses = getClauses( tableName, true, user );
                    sql += DBHelper.JoinClauses( clauses );

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

                    var clauses = getClauses( tableName, false, user );
                    sql += DBHelper.JoinClauses( clauses );

                    sql += $"GROUP BY {tableName}.ItemId " +
                           $"ORDER BY PerUser ";
                    if( leastWatched )
                        sql += "ASC ";
                    else
                        sql += "DESC ";
                }

                _dbHelper.ExecuteCommand( new SQLCmdDef( sql ), statement =>
                {
                    var row = statement.Current;
                    var col = 0;
                    var id = row.GetString( col++ );
                    var name = row.GetString( col++ );
                    var playCount = row.GetInt64( col++ );
                    var numEpisodes = row.GetInt64( col++ );
                    var playCountPerUser = row.GetDouble( col++ );
                    if( playMap.TryGetValue( id, out var currentValue ) )
                    {
                        // Safely updates based on the current value
                        playMap[ id ] = (id, name, currentValue.playCount + playCount, currentValue.denominator + numEpisodes, currentValue.playCountPerUser + playCountPerUser);
                    }
                    else
                    {
                        playMap[ id ] = (id, name, playCount, numEpisodes, playCountPerUser);
                    }

                    return true;
                } );
            }

            var sortedMapping = new SortedDictionary<long, List<WatchedMediaValue>>();

            var numResultsToGet = Statistics2026.Plugin.Instance!.Configuration.numWatchedToReport;

            foreach( var curr in playMap.Values )
            {
                var (id, name, playCount, denominator, playCountPerUser) = curr;
                var key = (long)playCountPerUser;

                var item = new WatchedMediaValue()
                {
                    ItemId = id,
                    Name = name,
                    PlayCount = playCount,
                    Denominator = denominator,
                    PlayCountPerUser = playCountPerUser,
                    MediaType = mediaType
                };

                if( sortedMapping.TryGetValue( key, out var value ) )
                {
                    value.Add( item );
                    sortedMapping[ key ] = value;
                }
                else
                    sortedMapping[ key ] = [ item ];
            }

            var retVal = new List<List<WatchedMediaValue>>();

            if( leastWatched )
            {
                foreach( var curr in sortedMapping.Values )
                {
                    if( retVal.Count >= numResultsToGet )
                        break;
                    for( int ii = 0; ii < curr.Count; ++ii )
                    {
                        var item = curr[ ii ];
                        item.ImageUrl = ItemImageUrl._ItemImageUrl( item.ItemId, _embyInterfaces!._libraryManager );
                        curr[ ii ] = item;
                    }
                    retVal.Add( curr );
                }
            }
            else
            {
                foreach( var curr in sortedMapping.Reverse() )
                {
                    if( retVal.Count >= numResultsToGet )
                        break;
                    for( int ii = 0; ii < curr.Value.Count; ++ii )
                    {
                        var item = curr.Value[ ii ];
                        item.ImageUrl = ItemImageUrl._ItemImageUrl( item.ItemId, _embyInterfaces!._libraryManager );
                        curr.Value[ ii ] = item;
                    }
                    retVal.Add( curr.Value );
                }
            }

            return retVal;
        }

        public StatCard WatchedMedia( User? user, bool leastWatched, EMediaType mediaType )
        {
            var watchedMedia = WatchedMediaValues( user, leastWatched, mediaType );
            var title = string.Empty;
            var help = string.Empty;

            if( ( mediaType == EMediaType.eSeries ) || ( mediaType == EMediaType.eEpisode ) )
            {
                title = leastWatched ? Constants.LeastWatchedShows : Constants.MostWatchedShows;
                help = leastWatched ? Constants.HelpLeastWatchedShows : Constants.HelpMostWatchedShows;
            }
            else if( mediaType == EMediaType.eMovie )
            {
                title = leastWatched ? Constants.LeastWatchedMovies : Constants.MostWatchedMovies;
                help = leastWatched ? Constants.HelpLeastWatchedMovies : Constants.HelpMostWatchedMovies;
            }

            var retVal = new TextBasedStatCard( title, help, EStatCardSize.eMedium )
            {
                SubTitle = ( user == null ) ? "(Weighted Watched across Users)" : "",
                ListType = TextBasedStatCard.EListType.eNumberedGroupByKey
            };

            foreach( var currList in watchedMedia )
            {
                foreach( var curr in currList )
                {
                    retVal.AddLine( $"{curr.Title()}", curr.ItemId, curr.ImageUrl );
                    retVal.AddKey( curr.PlayCount.ToString() );
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
                retVal.AddLine( $"Watch some {name} already!" );
            }

            return retVal;
        }

        public StatCard TotalTime( User? user, bool? episodesOnly, bool played )
        {
            CheckIsValid( ECheckType.eReport );

            if( user == null )
                throw new ArgumentNullException( "user" );

            var tableName = getUserTableName( user );
            var sql = string.Empty;

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
            var title = string.Empty;

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

            sql += DBHelper.JoinClauses( clauses );

            return ValueGroupForSingleItem( title, null, sql, [ ("@UserId", user.Id.ToString()) ], DBHelper.FormatTicks );
        }

        public StatCard TotalTimeWatched( User? user, bool? episodesOnly )
        {
            return TotalTime( user, episodesOnly, true );
        }

        public StatCard TotalWatchableTime( User? user, bool? episodesOnly )
        {
            return TotalTime( user, episodesOnly, false );
        }

        public List<(string name, DateTime lastPlayed)> LastSeenValues( User? user, bool movies )
        {
            CheckIsValid( ECheckType.eReport );

            if( user == null )
                throw new ArgumentNullException( "user" );

            var tableName = getUserTableName( user );

            var sql = "SELECT ";
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

            var retVal = new List<(string genre, DateTime lastPlayed)>();
            _dbHelper.ExecuteCommand( sqlCmd, statement =>
            {
                var row = statement.Current;
                var name = row.GetString( 0 );
                var date = row.GetString( 1 );
                var lastPlayedDate = DBHelper.ReadDateTime( date );
                retVal.Add( (name, lastPlayedDate) );
                return true;
            } );

            return retVal;
        }

        public StatCard LastSeen( User? user, bool movies )
        {
            CheckIsValid( ECheckType.eReport );

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

            var retVal = new TextBasedStatCard( title, help, EStatCardSize.eMedium )
            {
                ListType = TextBasedStatCard.EListType.eNumbered,
                IgnoreLength = true
            };
            var values = LastSeenValues( user, movies );

            foreach( var (name, lastPlayed) in values )
            {
                retVal.AddLine( $"{name} - {lastPlayed:d}" );
            }

            if( values.Count == 0 )
            {
                string name = movies ? "Movies" : "TV Shows";
                retVal.AddLine( $"Watch some {name} already!" );
            }

            return retVal;
        }
    }
}
