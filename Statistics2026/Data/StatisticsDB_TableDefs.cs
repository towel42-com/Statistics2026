using MediaBrowser.Controller.Entities;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Statistics2026.Data
{
    public class TableDefs
    {
        public const string sUserMediaTablePrefix = "UserMedia_";
        private readonly Dictionary<string, TableDef> _tableMap = [];
        private List<TableDef> _tableList = [];
        private TableDef? _userMediaTemplate = null;
        private Statistics2026DB? _db = null;
        private readonly EmbyInterfaces? _embyInterfaces = null;

        public TableDefs( EmbyInterfaces embyInterfaces )
        {
            _embyInterfaces = embyInterfaces ?? throw new ArgumentNullException( "embyInterfaces is null." );
            ConstructTables();
        }

        public void SetDatabase( Statistics2026DB db )
        {
            _db = db ?? throw new ArgumentNullException( "db is null." );
            if ( !_db.HasConnection() )
                throw new ArgumentNullException( "db has no connection." );
        }

        private void ConstructTables()
        {
            _tableList =
            [
                new TableDef("LastUpdateTable",
                    [
                        new TableColDef( "Action", "TEXT", false, true, "System" ),
                        new TableColDef( "LastUpdated", "DATETIME", true ),
                        new TableColDef( "Reason", "TEXT", true ),
                        new TableColDef( "RunTimeMS", "INT", true ),
                        new TableColDef( "Version", "TEXT", true ),
                        new TableColDef( "BuildDate", "DATETIME", true )
                    ]
                ),
                new TableDef("StatusInfo",
                    [
                        new TableColDef( "LastUpdated", "DATETIME", true ),
                        new TableColDef( "Version", "TEXT", true ),
                        new TableColDef( "BuildDate", "DATETIME", true )
                    ]
                ){ DeprecatedTable = true },

                new TableDef("Media",
                    [
                        new TableColDef( "ItemId", "TEXT", false, true ),
                        new TableColDef( "PrimaryName", "TEXT", false ),
                        new TableColDef( "SortName", "TEXT", true ),
                        new TableColDef( "SecondaryName", "TEXT", true ),
                        new TableColDef( "StartYear", "INT", true ),
                        new TableColDef( "IsEpisode", "BOOLEAN", true ),
                        new TableColDef( "IsTVSpecial", "BOOLEAN", true ),
                        new TableColDef( "SeriesId", "TEXT", true ),
                        new TableColDef( "Season", "INT", true ),
                        new TableColDef( "Episode", "INT", true ),
                        new TableColDef( "NumEpisodes", "INT", true ),
                        new TableColDef( "ResolutionBase", "TEXT", true ),
                        new TableColDef( "ResolutionDetail", "TEXT", true ),
                        new TableColDef( "Codec", "TEXT", true ),
                        new TableColDef( "DolbyVisionProfile", "TEXT", true ),
                        new TableColDef( "StudioNames", "TEXT", true ),  // json array of text
                        new TableColDef( "Genres", "TEXT", true ), // json array of text
                        new TableColDef( "StreamingProviders", "TEXT", true ){FormerColumnName="StreamingServices"}, // json array of text
                        new TableColDef( "ServerLocation", "TEXT", true ),
                        new TableColDef( "FileSize", "INT", true),
                        new TableColDef( "ImageUrl", "TEXT", true ),
                        new TableColDef( "RunTimeTicks", "INT", true ),
                        new TableColDef( "Rating", "REAL", true ),
                        new TableColDef( "TotalBitrate", "INT", true ),
                        new TableColDef( "PremiereDate", "DATETIME", true ),
                        new TableColDef( "DateAdded", "DATETIME", true )
                    ]
                ),

                new TableDef("Series",
                    [
                        new TableColDef( "ItemId", "TEXT", false, true ),
                        new TableColDef( "Name", "TEXT", false ),
                        new TableColDef( "SortName", "TEXT", true ),
                        new TableColDef( "PremiereDate", "DATETIME", true ),
                        new TableColDef( "NumEpisodes", "INT", true ),
                        new TableColDef( "NumSpecials", "INT", true ),
                        new TableColDef( "NumSeasons", "INT", true ),
                        new TableColDef( "HasSpecials", "BOOLEAN", true ),
                        new TableColDef( "DateAdded", "DATETIME", true ),
                        new TableColDef( "ImageUrl", "TEXT", true ),
                        new TableColDef( "FileSize", "INT", true),
                        new TableColDef( "RunTimeTicks", "INT", true ),
                        new TableColDef( "Rating", "REAL", true ),
                        new TableColDef( "Status", "TEXT" , true ),
                        new TableColDef( "AverageBitrate", "INT", true ),
                        new TableColDef( "StreamingProviders", "TEXT", true ){FormerColumnName="StreamingServices"}, // json array

                    ]
                ),
                new TableDef("SeriesFilterList",
                    [
                        new TableColDef( "SeriesId", "TEXT", false, true ),
                        new TableColDef( "TrackSeries", "BOOLEAN", true ),
                        new TableColDef( "TrackSpecials", "BOOLEAN", true ),
                        new TableColDef( "MinSeason", "INT", true ),
                        new TableColDef( "MaxSeason", "INT", true ),
                    ]
                )
                {
                    ValidationRequiresData = false,
                },

                new TableDef("Users",
                    [
                        new TableColDef( "UserId", "TEXT", false, true ),
                        new TableColDef( "UserName", "TEXT", false ),
                        new TableColDef( "ConnectUserId", "TEXT", true ),
                        new TableColDef( "IsAdministrator", "BOOLEAN", true ),
                        new TableColDef( "TotalTimeWatched", "INT", true ){DeprecatedColumn=true },
                        new TableColDef( "TotalWatchableTime", "INT", true ){DeprecatedColumn=true },
                        new TableColDef( "MediaTableName", "TEXT", false)
                    ]
                ),
                new TableDef("Missing",
                    [
                        new TableColDef( "Key", "TEXT", false, true),
                        new TableColDef( "TmdbId", "TEXT", false ),
                        new TableColDef( "Title", "TEXT", true ),
                        new TableColDef( "OriginalTitle", "TEXT", true ),
                        new TableColDef( "ReleaseDate", "DATETIME", true ),
                        new TableColDef( "Overview", "TEXT", true ),
                        new TableColDef( "PosterPath", "TEXT", true ),
                        new TableColDef( "ParentId", "TEXT", true ), // always an emby item id, could be series or collection id
                        new TableColDef( "IsEpisode", "BOOLEAN", false),
                        new TableColDef( "SeasonNum", "INT", true ),
                        new TableColDef( "EpisodeNum", "INT", true ),
                        new TableColDef( "StudioNames", "TEXT", true ),
                        new TableColDef( "StreamingProviders", "TEXT", true ){FormerColumnName="StreamingServices"}, // json array of text
                    ]
                ){ ValidationRequiresData = false },
                new TableDef("Collections",
                    [
                            new TableColDef( "ItemId", "TEXT", false, true),
                            new TableColDef( "TmdbId", "TEXT", true ),
                            new TableColDef( "Name", "TEXT", false ),
                            new TableColDef( "SortName", "TEXT", false ),
                            new TableColDef( "ImageUrl", "TEXT", false ),
                    ]
                ),
                new TableDef("CollectionMembership",
                    [
                        new TableColDef( "CollectionId", "TEXT", false ),
                        new TableColDef( "ItemId", "TEXT", false ),
                        new TableColDef( "CollectionName", "TEXT", false ) // for debugging purposes
                        {
                            DeprecatedColumn = true
                        }
                    ]
                )
                {
                    CompositeIndex = new List<string>(){"CollectionId", "ItemId" }
                },
                new TableDef("CachedStats",
                    [
                        new TableColDef( "LongestSeries", "TEXT", true ),
                        new TableColDef( "ShortestSeries", "TEXT", true ),
                        new TableColDef( "LargestSeries", "TEXT", true ),
                        new TableColDef( "SmallestSeries", "TEXT", true ),
                        new TableColDef( "TotalTVStudioCount", "INT", true ),
                        new TableColDef( "LongestMovie", "TEXT", true ),
                        new TableColDef( "ShortestMovie", "TEXT", true ),
                        new TableColDef( "LargestMovie", "TEXT", true ),
                        new TableColDef( "SmallestMovie", "TEXT", true ),
                        new TableColDef( "TotalMovieStudioCount", "INT", true ),
                    ]
                ){ DeprecatedTable = true },
                new TableDef("CachedWatchedAnalysis",
                    [
                        new TableColDef( "ItemId", "TEXT", true ),
                        new TableColDef( "Name", "TEXT", true ),
                        new TableColDef( "ImageUrl", "TEXT", true ),
                        new TableColDef( "NumEpisodes", "INT", true ),
                        new TableColDef( "NumWatched", "INT", true ),
                        new TableColDef( "PercentWatchedPerUser", "DOUBLE", true ),
                    ]
                ){ DeprecatedTable = true },
                new TableDef("UserVideoList",
                    [
                        new TableColDef( "ItemId", "TEXT", true ),
                        new TableColDef( "Name", "TEXT", true ),
                        new TableColDef( "ImageUrl", "TEXT", true ),
                        new TableColDef( "NumEpisodes", "INT", true ),
                        new TableColDef( "NumWatched", "INT", true ),
                        new TableColDef( "PercentWatchedPerUser", "DOUBLE", true ),
                    ]
                ){ DeprecatedTable = true },
                new TableDef( "StreamingProviders",
                [
                    new TableColDef( "ProviderId", "INT", true, true ),
                    new TableColDef( "Name", "TEXT", false, false ),
                    new TableColDef( "LogoPath", "TEXT", true, false ),
                    new TableColDef( "DisplayPriority", "INT", false, false, "32767" ),
                    new TableColDef( "UrlTemplate", "TEXT", true )
                ])
            ];

            _userMediaTemplate = new TableDef( $"{sUserMediaTablePrefix}<USER_ID>",
                    [
                        new TableColDef( "UserId", "TEXT", false ), // user
                        new TableColDef( "ItemId", "TEXT", false, true ), // video item
                        new TableColDef( "Name", "TEXT", true ), // Name of the show to reduce joins
                        new TableColDef( "IsPlayed", "BOOLEAN", true ),
                        new TableColDef( "PlayCount", "INT", true ),
                        new TableColDef( "LastPlayedDate", "DATETIME", true ),
                        new TableColDef( "StartTickPos", "INT", true) { DeprecatedColumn = true},
                        new TableColDef( "EndTickPos", "INT", true ) { DeprecatedColumn = true},
                        new TableColDef( "TotalTicksPlayed", "INT", true ) { FormerColumnName = "TotalTicks"},
                        new TableColDef( "IsEpisode", "BOOLEAN", true ),
                        new TableColDef( "NumEpisodes", "INT", true ), // for multi episode media
                        new TableColDef( "IsTVSpecial", "BOOLEAN", true ),
                        new TableColDef( "SeriesId", "TEXT", true ) // if episode add seriesid
                    ]
                );

            foreach( var tableDef in _tableList )
            {
                if( tableDef.DeprecatedTable )
                    continue;

                _tableMap[ tableDef.Name ] = tableDef;
            }
        }

        public List<string> tableNames()
        {
            var retVal = new List<string>();
            foreach( var table in _tableList )
            {
                if( table.DeprecatedTable )
                    continue;
                retVal.Add( table.Name );
            }

            return retVal;
        }

        public List<string> UserMediaUsageColumnNames()
        {
            return _userMediaTemplate!.ColumnNames();
        }

        public void CreateTables( TableDef.EAction action, CancellationToken? cancellationToken, IProgress<double>? progress )
        {
            CheckIsValid();
            if( action != TableDef.EAction.eRecreate && action != TableDef.EAction.eCreate )
                throw new InvalidEnumArgumentException( $"Action must be {TableDef.EAction.eRecreate} or {TableDef.EAction.eCreate}" );

            if( Plugin.Instance == null )
                throw new NullReferenceException( $"Plugin.Instance is null" );

            if( !Plugin.Instance.IsDBStateSet( EDBState.eSystemTablesCreated ) )
            {
                var sqlCmds = new List<SQLCmdDef>();
                foreach( var tableDef in _tableList )
                {
                    sqlCmds.AddRange( tableDef.GetSQLCommands( action ) );
                }

                var config = Statistics2026.Plugin.Instance!.Configuration;
                if( config.resetPlayCount && action == TableDef.EAction.eRecreate && _userMediaTemplate != null )
                {
                    sqlCmds.AddRange( DropAllUserMediaCmds() );
                    Plugin.Instance.RemoveDBState( EDBState.eUserTablesCreated | EDBState.eUserDataInitialized );
                }

                _db.ExecuteCommands( sqlCmds );

                Plugin.Instance.AddDBState( EDBState.eSystemTablesCreated );
            }

            InitWatchedMediaTables( cancellationToken, progress );
        }

        public TableDef? getTableDef( string tableName, bool isUserMediaTable = false, User? user = null )
        {
            if( isUserMediaTable )
            {
                return getUserMediaTableDef( user );
            }

            if( _tableMap.TryGetValue( tableName, out var tableDef ) )
            {
                return tableDef;
            }
            return null;
        }

        public TableDef? getUserMediaTableDef( User? user = null )
        {
            if( user == null )
                return null;
            if( _userMediaTemplate == null )
                return null;

            var tableName = getUserTableName( user );
            var retVal = new TableDef( _userMediaTemplate );
            return retVal;
        }

        public void ClearTable( string tableName )
        {
            CheckIsValid();

            List<SQLCmdDef> sqlCmds = [];
            var tableDef = getTableDef( tableName );
            if( tableDef != null )
            {
                sqlCmds.AddRange( tableDef.GetSQLCommands( TableDef.EAction.eClear ) );
            }
            else
            {
                sqlCmds.Add( new SQLCmdDef( TableDef.clearTable( tableName ) ) );
            }

            _db.ExecuteCommands( sqlCmds );
        }

        public List<SQLCmdDef> DropTableCmds( string tableName )
        {
            var tableDef = getTableDef( tableName );
            if( tableDef != null )
            {
                var sqlCmds = tableDef.GetSQLCommands( TableDef.EAction.eDrop );
                return sqlCmds;
            }

            return [ new SQLCmdDef( TableDef.dropTable( tableName ) ) ];
        }

        public void DropTable( string tableName )
        {
            CheckIsValid();

            var cmds = DropTableCmds( tableName );
            _db.ExecuteCommands( cmds );
        }

        public List<string> allTables( (string regex, bool like)? regex = null )
        {
            CheckIsValid();

            var retVal = new List<string>();
            var clauses = new List<string>() { "type='table'", "name NOT LIKE 'sqlite_%'" };
            if( regex != null )
            {
                var clause = "name " + ( regex.Value.like ? "LIKE" : "NOT LIKE" ) + " '" + regex.Value.regex + "'";
                clauses.Add( clause );
            }

            var sql = "SELECT name FROM sqlite_master " + HelperFuncs.JoinClauses( clauses );

            _db.ExecuteCommand( new SQLCmdDef( sql ), statement =>
            {
                var row = statement.Current;
                retVal.Add( row.GetString( 0 ) );
                return true;
            } );
            return retVal;
        }

        public List<SQLCmdDef> DropAllUserMediaCmds()
        {
            var retVal = new List<SQLCmdDef>();
            if( _userMediaTemplate == null )
                return retVal;

            var tables = allUserMediaTables( false );

            foreach( var table in tables )
                retVal.AddRange( DropTableCmds( table ) );

            return retVal;
        }

        public List<string> allUserMediaTables( bool existingOnly = true )
        {
            CheckIsValid();

            if( existingOnly )
                return allTables( (regex: $"{sUserMediaTablePrefix}%", like: true) );
            else
            {
                var users = EmbyHelperFuncs.GetAllUsers( _embyInterfaces );

                HashSet<string> tableNameSet = [];
                foreach( var user in users ?? [] )
                {
                    var tableName = getUserTableName( user );
                    if( tableName != null )
                        tableNameSet.Add( tableName );
                }
                return tableNameSet.ToList();
            }
        }

        public User? getUserForTableName( string tableName )
        {
            CheckIsValid();
            if( !tableName.StartsWith( sUserMediaTablePrefix ) )
                return null;

            var idString = tableName.Substring( sUserMediaTablePrefix.Length );
            var guid = new Guid( idString );

            return _embyInterfaces.GetUserById( guid );

        }

        public string? getUserTableName( User? user )
        {
            CheckIsValid();

            return user == null ? null : getUserTableName( user.Id.ToString() );
        }

        public string getUserTableName( string userId )
        {
            CheckIsValid();

            userId = userId.ToString().Replace( "-", string.Empty );
            return $"{sUserMediaTablePrefix}{userId}";
        }

        public void InitWatchedMediaTables( CancellationToken? cancellationToken, IProgress<double>? progress )
        {
            CheckIsValid();

            if( Plugin.Instance == null )
                throw new NullReferenceException( $"Plugin.Instance is null" );

            if( !Plugin.Instance.IsDBStateSet( EDBState.eUserTablesCreated ) )
            {

                progress?.Report( 0 );
                var users = EmbyHelperFuncs.GetAllUsers( _embyInterfaces );
                if( users == null )
                {
                    progress?.Report( 100 );
                    return;
                }

                progress?.Report( 100 );

                var sqlCmds = new List<SQLCmdDef>();
                var curr = 0.0;
                double count = users.Count;

                using( var timer = new AutoTimer( $"    Analyze User Watch Data - Getting Init Table Commands", _embyInterfaces.Logger() ) )
                {
                    curr = 0;
                    foreach( var user in users )
                    {
                        progress?.Report( 80.0 * ( ++curr ) / count );
                        using( var userTimer = new AutoTimer( $"    Processed User ({curr} of {count}) - {user.Name}", _embyInterfaces.Logger() ) )
                        {
                            sqlCmds.AddRange( GetInitWatchedMediaTableCommands( user ) );
                            cancellationToken?.ThrowIfCancellationRequested();
                        }
                    }

                    cancellationToken?.ThrowIfCancellationRequested();
                }

                using( var timer = new AutoTimer( $"    Analyze User Watch Data - Executing Init Table Commands", _embyInterfaces.Logger() ) )
                {
                    progress?.Report( 80 );
                    _db.ExecuteCommands( sqlCmds );
                    progress?.Report( 100 );
                }

                Plugin.Instance.AddDBState( EDBState.eUserTablesCreated );
            }
        }


        [MemberNotNull( nameof( _userMediaTemplate ), nameof( _db ), nameof( _embyInterfaces ) )]
        public void CheckIsValid()
        {
            if( _userMediaTemplate == null )
                throw new Exception( $"_userMediaTemplate is null" );
            if( _db == null )
                throw new Exception( $"_db is null" );
            if( _embyInterfaces == null )
                throw new Exception( $"_embyInterfaces is null" );
        }

        public List<SQLCmdDef> GetInitWatchedMediaTableCommands( User? user )
        {
            CheckIsValid();

            if( user == null )
                throw new Exception( $"GetInitWatchedMediaTableCommands: User is null" );

            var userTableName = getUserTableName( user );
            if( userTableName == null )
                return [];

            var sqlCmds = new List<SQLCmdDef>();

            var cmds = _userMediaTemplate.GetSQLCommands( TableDef.EAction.eCreate );
            for( var ii = 0; ii < cmds.Count(); ++ii )
            {
                var cmd = new SQLCmdDef( cmds[ ii ] );
                cmd.Replace( $"{sUserMediaTablePrefix}<USER_ID>", userTableName );
                sqlCmds.Add( cmd );
            }

            return sqlCmds;
        }
    }
}
