using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Statistics2026.Data
{
    public enum ECheckLevel
    {
        eInterfaces = 0x0001,
        eConnection = 0x0002,
        eProgress = 0x0004,
        eAll = eInterfaces | eConnection | eProgress,
        eThrowOnFailure = 0x010000,
        eAllWithThrow = eAll | eThrowOnFailure
    }

    public enum ECheckType
    {
        eNone,
        eInit,
        eUpdate,
        eReport
    }

    public sealed class DBHelper
    {
        private static readonly string[] _datetimeFormats = new string[] {
            "THHmmssK",
            "THHmmK",
            "HH:mm:ss.FFFFFFFK",
            "HH:mm:ssK",
            "HH:mmK",
            "yyyy-MM-dd HH:mm:ss.FFFFFFFK", /* NOTE: UTC default (5). */
            "yyyy-MM-dd HH:mm:ssK",
            "yyyy-MM-dd HH:mmK",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
            "yyyy-MM-ddTHH:mmK",
            "yyyy-MM-ddTHH:mm:ssK",
            "yyyyMMddHHmmssK",
            "yyyyMMddHHmmK",
            "yyyyMMddTHHmmssFFFFFFFK",
            "THHmmss",
            "THHmm",
            "HH:mm:ss.FFFFFFF",
            "HH:mm:ss",
            "HH:mm",
            "yyyy-MM-dd HH:mm:ss.FFFFFFF", /* NOTE: Non-UTC default (19). */
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyyMMddHHmmss",
            "yyyyMMddHHmm",
            "yyyyMMddTHHmmssFFFFFFF",
            "yyyy-MM-dd",
            "yyyyMMdd",
            "yy-MM-dd",
            "o"
        };
        private static readonly string _datetimeFormatUtc = _datetimeFormats[ 5 ];
        private static readonly string _datetimeFormatLocal = _datetimeFormats[ 19 ];

        private readonly EmbyInterfaces? _embyInterfaces;

        private IDatabaseConnection? Connection { get; set; } = null;
        public CancellationToken? CancellationToken { get; set; } = null;
        public IProgress<double>? Progress { get; set; } = null;

        public DBHelper()
        {
            _embyInterfaces = null;
        }

        public DBHelper( EmbyInterfaces embyInterfaces )
        {
            _embyInterfaces = embyInterfaces ?? throw new ArgumentNullException( "embyInterfaces is null." );
            var db_file_name = Path.Combine( _embyInterfaces._configManager.ApplicationPaths.DataPath, "Statistics2026.db" );
            CreateConnection( db_file_name );
        }

        public bool CheckIsValid( ECheckLevel checkLevel )
        {
            List<string> msgs = [ $"DBHelper is not valid 0x{checkLevel:X}" ];
            var retVal = true;
            if( ( checkLevel & ECheckLevel.eInterfaces ) != 0 )
            {
                if( _embyInterfaces == null || _embyInterfaces._logger == null )
                {
                    retVal = false;
                    msgs.Add( "_embyInterfaces is null" );
                }
            }

            if( ( checkLevel & ECheckLevel.eConnection ) != 0 )
            {
                if( Connection == null )
                {
                    retVal = false;
                    msgs.Add( "Connection is null" );
                }
            }

            if( ( checkLevel & ECheckLevel.eProgress ) != 0 )
            {
                if( CancellationToken == null )
                {
                    retVal = false;
                    msgs.Add( "CancellationToken is null" );
                }

                if( Progress == null )
                {
                    retVal = false;
                    msgs.Add( "Progress is null" );
                }
            }

            if( retVal == false && ( ( checkLevel & ECheckLevel.eThrowOnFailure ) != 0 ) )
            {
                var msg = string.Join( "\n", msgs );

                throw new ArgumentNullException( msg );
            }

            return retVal;
        }

        ~DBHelper()
        {
            _embyInterfaces?._logger?.Debug( "Statistics2026 : Cleaning up" );
            if( Connection != null )
            {
                Connection.Close();
                _embyInterfaces?._logger?.Debug( "Statistics2026 : DB Connection Closed" );
            }
        }

        public bool TryBind<T>( IStatement statement, string name, T? value )
        {
            if( !statement.BindParameters.TryGetValue( name, out var bindParam ) )
            {
                _embyInterfaces!._logger?.Error( $"Error Binding {name} to {value}" );
                return false;
            }

            if( value == null )
            {
                bindParam.BindNull();
                return true;
            }

            switch( value )
            {
                case string s:
                    bindParam.Bind( s );
                    break;
                case int i:
                    bindParam.Bind( i );
                    break;
                case long l:
                    bindParam.Bind( l );
                    break;
                case double d:
                    bindParam.Bind( d );
                    break;
                case float f:
                    bindParam.Bind( (double)f );
                    break;
                case short sh:
                    bindParam.Bind( sh );
                    break;
                case byte[] ba:
                    bindParam.Bind( ba );
                    break;
                case DateTime dt:
                    bindParam.Bind( dt.ToString( "o", CultureInfo.InvariantCulture ) );
                    break;
                case bool b:
                    // store bool as integer 0/1
                    bindParam.Bind( b ? 1 : 0 );
                    break;
                default:
                    // Fallback: convert to string (covers enums, GUID, etc.)
                    bindParam.Bind( value.ToString() ?? string.Empty );
                    break;
            }

            return true;
        }

        public void ExecuteCommand( SQLCmdDef cmd, Func<IStatement, bool>? onStatement = null )
        {
            List<SQLCmdDef> cmds = [ cmd ];
            ExecuteCommands( cmds, onStatement );
        }

        public void ExecuteCommands( List<SQLCmdDef> cmds, Func<IStatement, bool>? onStatement = null )
        {
            _ = CheckIsValid( ECheckLevel.eConnection | ECheckLevel.eThrowOnFailure );

            var ii = 0;
            try
            {
                Connection.RunInTransaction( connection =>
                {
                    for( ii = 0; ii < cmds.Count; ++ii )
                    {
                        CancellationToken?.ThrowIfCancellationRequested();

                        var cmd = cmds[ ii ];
                        cmd.Execute( connection, this, onStatement );
                        var value = 80 + ( 20.0 * ii / cmds.Count );
                        Progress?.Report( value );
                    }
                } );
            }
            catch( Exception )
            {
                throw;
            }
        }

        public void ExecuteCommands( List<string> cmds )
        {
            List<SQLCmdDef> cmdDefs = [];
            foreach( var cmd in cmds )
            {
                CancellationToken?.ThrowIfCancellationRequested();
                cmdDefs.Add( new SQLCmdDef( cmd ) );
            }

            ExecuteCommands( cmdDefs );
        }

        public void ExecuteCommand( string cmd )
        {
            ExecuteCommand( new SQLCmdDef( cmd ) );
        }

        private string GetDateTimeKindFormat( DateTimeKind kind )
        {
            return ( kind == DateTimeKind.Utc ) ? _datetimeFormatUtc : _datetimeFormatLocal;
        }

        public static DateTime ReadDateTime( string dateText )
        {
            return DateTime.ParseExact(
                dateText,
                _datetimeFormats,
                DateTimeFormatInfo.InvariantInfo,
                DateTimeStyles.None ).ToUniversalTime();
        }

        public string? ToDateTimeParamValue( DateTime? dateValue )
        {
            if( dateValue == null )
                return null;

            var kind = DateTimeKind.Utc;
            return dateValue.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind( dateValue.Value, kind ).ToString( GetDateTimeKindFormat( kind ), CultureInfo.InvariantCulture )
                : dateValue.Value.ToString( GetDateTimeKindFormat( dateValue.Value.Kind ), CultureInfo.InvariantCulture );
        }

        private void CreateConnection( string db_file )
        {
            _ = CheckIsValid( ECheckLevel.eInterfaces | ECheckLevel.eThrowOnFailure );

            _embyInterfaces!._logger?.Debug( "CreateConnection : " + db_file );
            ConnectionFlags connectionFlags;

            //_embyInterfaces!._logger?.Debug("Opening write _connection");
            connectionFlags = ConnectionFlags.Create; // create if missing
            connectionFlags |= ConnectionFlags.ReadWrite; // open for read-write
            connectionFlags |= ConnectionFlags.PrivateCache;
            connectionFlags |= ConnectionFlags.NoMutex;

            var db = SQLite3.Open( db_file, connectionFlags, null, true );

            try
            {
                List<string> queries =
                    [
                        "PRAGMA journal_mode=WAL",
                        "PRAGMA busy_timeout=5000",
                        "PRAGMA synchronous=Normal",
                        "PRAGMA temp_store=file"
                    ];

                db.ExecuteAll( string.Join( ";", queries.ToArray() ) );
            }
            catch
            {
                throw;
            }

            Connection = db;
            _embyInterfaces!._logger?.Debug( "ConnectionCreated : " + Connection.GetHashCode() );
        }

        public IEnumerable<T> GetLibraryItems<T>()
        {
            _ = CheckIsValid( ECheckLevel.eInterfaces | ECheckLevel.eThrowOnFailure );
            return GetUserItems<T>( null, _embyInterfaces!._libraryManager );
        }

        public static IEnumerable<T> GetUserItems<T>( User? user, ILibraryManager libManager )
        {
            var query = new InternalItemsQuery( user )
            {
                IncludeItemTypes = new[] { typeof( T ).Name },
                Recursive = true,
                IsVirtualItem = false,
                DtoOptions = new DtoOptions( true )
                {
                    ImageTypes = new[] { ImageType.Thumb, ImageType.Thumbnail },
                    EnableImages = true
                }
            };

            return libManager.GetItemList( query ).OfType<T>();
        }

        public bool ColumnExists( string tableName, string columnName )
        {
            var sql = $"SELECT 1 FROM pragma_table_info(@TableName) WHERE name=@ColumnName";
            List<(string name, object? value)> parameters =
                [
                    ("@TableName", tableName),
                    ("@ColumnName", columnName)
                ];

            var exists = false;

            ExecuteCommand( new SQLCmdDef( sql, parameters ), statement =>
            {
                var row = statement.Current;
                var value = row.GetInt64( 0 );
                exists = value != 0;
                return true;
            } );

            return exists;
        }

        public bool TableExists( string tableName )
        {
            var sql = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@TableName";
            List<(string name, object? value)> parameters = [ ("@TableName", tableName) ];

            var exists = false;

            ExecuteCommand( new SQLCmdDef( sql, parameters ), statement =>
            {
                var row = statement.Current;
                var value = row.GetInt64( 0 );
                exists = value != 0;
                return true;
            } );

            return exists;
        }

        public static string FormatTicks( long ticks )
        {
            var runtime = new RunTime( ticks );
            return runtime.ToLongString();
        }

        public static string JoinClauses( List<string> clauses )
        {
            if( clauses.IsNullOrEmpty() )
                return string.Empty;

            for( var ii = 0; ii < clauses.Count; ++ii )
            {
                clauses[ ii ] = $"( {clauses[ ii ]} )";
            }

            return " WHERE " + string.Join( " AND ", clauses ) + " ";
        }

        public void ValidateTables( List<string> tables, Func<string, List<string>> getColumnsFunc, Func<string, bool>? additionalNeedsDataFunc, Action<bool, bool> updateDBState )
        {
            if( tables.IsNullOrEmpty() )
            {
                updateDBState( true, true );
                return;
            }

            var aTableMissing = false;
            var aColumnMissing = false;
            var aTableNeedsData = false;
            foreach( var tableName in tables )
            {
                var tableMissing = !TableExists( tableName );
                var columnMissing = false;
                var tableNeedsData = false;
                if( !tableMissing )
                {
                    (columnMissing, tableNeedsData) = ValidateTable( tableName, getColumnsFunc( tableName ), additionalNeedsDataFunc );
                }
                else
                {
                    tableNeedsData = columnMissing = true;
                }

                aTableMissing = aTableMissing || tableMissing;
                aColumnMissing = aColumnMissing || columnMissing;
                aTableNeedsData = aTableNeedsData || tableNeedsData;
                if( aTableMissing && aTableNeedsData && aColumnMissing )
                    break;
            }

            updateDBState( aTableMissing || aColumnMissing, aTableNeedsData );
        }

        public (bool columnMissing, bool dataMissing) ValidateTable( string tableName, List<string> columns, Func<string, bool>? additionalNeedsDataFunc )
        {
            var columnMissing = false;
            foreach( var columnName in columns )
            {
                columnMissing = columnMissing || !ColumnExists( tableName, columnName );
            }

            var tableNeedsData = true;
            if( !columnMissing )
            {
                var sql = $"SELECT COUNT(*) FROM {tableName} LIMIT 1";

                tableNeedsData = false;
                ExecuteCommand( new SQLCmdDef( sql ), statement =>
                {
                    var row = statement.Current;
                    tableNeedsData = row.GetInt64( 0 ) == 0;
                    return false;
                }
                );

                tableNeedsData = tableNeedsData || ( ( additionalNeedsDataFunc != null ) && additionalNeedsDataFunc( tableName ) );
            }

            return (columnMissing, tableNeedsData);
        }
    }
}
