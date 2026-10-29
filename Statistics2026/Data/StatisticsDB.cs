using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Logging;
using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Configuration;
using Statistics2026.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Statistics2026.Data
{
    public enum EMediaType
    {
        eMovie,
        eSeries,
        eEpisode
    }

    public sealed partial class StatisticsDB : IDisposable
    {
        private bool _disposed = false;
        EmbyInterfaces? _embyInterfaces = null;
        private static readonly object _padlock = new();
        private readonly Dictionary<string, TableDef> _tableMap = [];
        private List<TableDef> _tableList = [];
        private TableDef? _userMediaTemplate = null;
        private MediaBrowser.Model.Tasks.IScheduledTask? _currentTask = null;

        public bool AllTablesExisted { get; private set; } = false;
        public bool DataExists { get; private set; } = false;
        public bool UserDataExists { get; private set; } = false;

        private IDatabaseConnection? Connection { get; set; } = null;
        public CancellationToken? CancellationToken { get; set; } = null;
        public IProgress<double>? Progress { get; set; } = null;

        public static StatisticsDB GetInstance( EmbyInterfaces? embyInterfaces )
        {
            lock( _padlock )
            {
                if( embyInterfaces == null )
                    throw new ArgumentNullException( "EmbyInterfaces is null." );

                var retVal = new StatisticsDB( embyInterfaces );
                embyInterfaces.Logger().Debug( "New Instance Created : " + retVal.GetHashCode() );
                Plugin.Instance?.Configuration.UpdateBuildDateAndVersionInfo();
                return retVal;
            }
        }

        private StatisticsDB( EmbyInterfaces embyInterfaces )
        {
            _embyInterfaces = embyInterfaces ?? throw new ArgumentNullException( "embyInterfaces is null." );

            ConstructTableList();

            Logger().Debug( "Creating Database" );

            CreateConnection();

            embyInterfaces.Logger().Debug( "Finished Creating Database" );
            lock( _padlock )
            {
                ComputeDBState();
            }
        }

        public void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        ~StatisticsDB()
        {
            Dispose( false );
        }

        void Dispose( bool disposing )
        {
            if( _disposed )
                return;
            if( disposing )
            {
                CloseConnection();
            }
            _disposed = true;
        }

        public ILogger Logger()
        {
            CheckIsValid( ECheckLevel.eLogger | ECheckLevel.eThrowOnFailure );
            return _embyInterfaces!._logger!;
        }

        public ILibraryManager LibraryManager()
        {
            CheckIsValid( ECheckLevel.eEmbyInterfaceOnly | ECheckLevel.eLibraryManager | ECheckLevel.eThrowOnFailure );
            return _embyInterfaces!._libraryManager!;
        }

        public IUserDataManager UserDataManager()
        {
            CheckIsValid( ECheckLevel.eEmbyInterfaceOnly | ECheckLevel.eUserDataManager | ECheckLevel.eThrowOnFailure );
            return _embyInterfaces!._userDataManager!;
        }

        public IUserManager UserManager()
        {
            CheckIsValid( ECheckLevel.eEmbyInterfaceOnly | ECheckLevel.eUserManager | ECheckLevel.eThrowOnFailure );
            return _embyInterfaces!._userManager!;
        }

        public IProviderManager ProviderManager()
        {
            CheckIsValid( ECheckLevel.eEmbyInterfaceOnly | ECheckLevel.eProviderManager | ECheckLevel.eThrowOnFailure );
            return _embyInterfaces!._providerManager!;
        }

        public IServerConfigurationManager ConfigManager()
        {
            CheckIsValid( ECheckLevel.eEmbyInterfaceOnly | ECheckLevel.eConfigManager | ECheckLevel.eThrowOnFailure );

            return _embyInterfaces!._configManager!;
        }

        private void CreateConnection()
        {
            var oldFileName = Path.Combine( ConfigManager().ApplicationPaths.DataPath, "Statistics2026.db" );
            var newFileName = Path.Combine( ConfigManager().ApplicationPaths.PluginConfigurationsPath, "Statistics2026.db" );

            if( File.Exists( oldFileName ) )
            {
                try
                {
                    Logger().Info( "DB exists in old location, moving to plugin/config" );
                    File.Move( oldFileName, newFileName );
                    Logger().Info( "   Moved successfully" );
                }
                catch( IOException ex )
                {
                    // Handles scenarios like: file already exists at destination, or file is locked
                    // Handles scenarios like: file already exists at destination, or file is locked
                    Logger().Error( $"   Failed to be moved, removing and new db will be built - {ex.Message}" );
                }
                catch( UnauthorizedAccessException ex )
                {
                    Logger().Error( $"   Failed to be moved, removing and new db will be built - Check Permissions - {ex.Message}" );
                }
            }

            if( File.Exists( oldFileName ) ) // could not move, try deleting
            {
                try
                {
                    Logger().Info( "Deleting old DB file" );
                    File.Delete( oldFileName );
                    Logger().Info( "   Deleted successfully" );
                }
                catch( Exception ex )
                {
                    Logger().Error( $"   Failed to be delete - {ex.Message}" );
                }
            }

            CreateConnection( newFileName );
        }

        private void CreateConnection( string db_file )
        {
            _ = CheckIsValid( ECheckLevel.eLogger | ECheckLevel.eThrowOnFailure );

            _embyInterfaces!.Logger().Debug( "CreateConnection : " + db_file );
            ConnectionFlags connectionFlags;

            //_embyInterfaces!.Logger().Debug("Opening write _connection");
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
            _embyInterfaces!.Logger().Debug( "Connection Created : " + Connection.GetHashCode() );
        }

        public void CloseConnection()
        {
            Logger().Debug( "Cleaning up" );
            if( Connection != null )
            {
                Connection.Close();
                Logger().Debug( $"DB Connection Closed : {Connection.GetHashCode()}" );
            }
        }

        public void Initialize( MediaBrowser.Model.Tasks.IScheduledTask? runningTask, CancellationToken? cancellationToken, IProgress<double>? progress, bool reset = false )
        {
            _currentTask = runningTask;
            SetCancellationToken( cancellationToken, progress );
            CreateTables( reset ? TableDef.EAction.eRecreate : TableDef.EAction.eCreate );
        }

        public void ResetCancellationToken()
        {
            CancellationToken = null;
            Progress = null;
        }

        private void SetCancellationToken( CancellationToken? cancellationToken, IProgress<double>? progress )
        {
            CancellationToken = cancellationToken;
            Progress = progress;
        }

        public bool TryBind<T>( IStatement statement, string name, T? value )
        {
            if( !statement.BindParameters.TryGetValue( name, out var bindParam ) )
            {
                _embyInterfaces!.Logger().Error( $"Error Binding {name} to {value}" );
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
                    bindParam.Bind( HelperFuncs.ToDateTimeParamValue( dt )! ); /*dt.ToString( "o", CultureInfo.InvariantCulture ) )*/
                    break;
                case DateTimeOffset dto:
                    bindParam.Bind( HelperFuncs.ToDateTimeParamValue( dto )! );
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

        public void ExecuteCommand( SQLCmdDef cmd, Func<IStatement, bool>? onStatement = null, bool useTransaction = true )
        {
            List<SQLCmdDef> cmds = [ cmd ];
            ExecuteCommands( cmds, onStatement, useTransaction );
        }

        public void ExecuteCommands( List<SQLCmdDef> cmds, Func<IStatement, bool>? onStatement = null, bool useTransaction = true )
        {
            _ = CheckIsValid( ECheckLevel.eConnection | ECheckLevel.eThrowOnFailure );

            void runCommands( IDatabaseConnection connection, List<SQLCmdDef> cmds, Func<IStatement, bool>? onStatement )
            {
                for( var ii = 0; ii < cmds.Count; ++ii )
                {
                    CancellationToken?.ThrowIfCancellationRequested();

                    var cmd = cmds[ ii ];
                    cmd.Execute( connection, this, onStatement );
                    var value = 80 + ( 20.0 * ii / cmds.Count );
                    Progress?.Report( value );
                }
            }

            try
            {
                if( useTransaction && ( cmds.Count > 1 ) )
                {
                    Connection.RunInTransaction( connection =>
                    {
                        runCommands( connection, cmds, onStatement );
                    } );
                }
                else
                {
                    runCommands( Connection!, cmds, onStatement );
                }
            }
            catch( Exception )
            {
                throw;
            }
        }

        public void ExecuteCommands( List<string> cmds, bool useTransaction = true )
        {
            List<SQLCmdDef> cmdDefs = [];
            foreach( var cmd in cmds )
            {
                CancellationToken?.ThrowIfCancellationRequested();
                cmdDefs.Add( new SQLCmdDef( cmd ) );
            }

            ExecuteCommands( cmdDefs, null, useTransaction );
        }

        public void ExecuteCommand( string cmd )
        {
            ExecuteCommand( new SQLCmdDef( cmd ), null, false );
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

        public void ValidateTables( List<string> tables, Func<string, List<string>> getColumnsFunc, Func<string, bool> validationRequiresDataFunc, Func<string, bool>? additionalNeedsDataFunc, Action<bool, bool> updateDBState )
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
                    (columnMissing, tableNeedsData) = ValidateTable( tableName, getColumnsFunc( tableName ), validationRequiresDataFunc, additionalNeedsDataFunc );
                }
                else
                {
                    tableNeedsData = columnMissing = true;
                }

                if( tableMissing )
                {
                    _embyInterfaces?.Logger().Info( $"Table '${tableName}' does not exist." );
                }

                if( columnMissing )
                {
                    _embyInterfaces?.Logger().Info( $"Table '${tableName}' is missing a column." );
                }

                if( tableNeedsData )
                {
                    _embyInterfaces?.Logger().Info( $"Table '${tableName}' is missing data." );
                }
                aTableMissing = aTableMissing || tableMissing;
                aColumnMissing = aColumnMissing || columnMissing;
                aTableNeedsData = aTableNeedsData || tableNeedsData;
                if( aTableMissing && aTableNeedsData && aColumnMissing )
                    break;
            }

            updateDBState( aTableMissing || aColumnMissing, aTableNeedsData );
        }

        public (bool columnMissing, bool dataMissing) ValidateTable( string tableName, List<string> columns, Func<string, bool> validationRequiresDataFunc, Func<string, bool>? additionalNeedsDataFunc )
        {
            var columnMissing = false;
            foreach( var columnName in columns )
            {
                columnMissing = columnMissing || !ColumnExists( tableName, columnName );
            }

            var validationRequiresData = validationRequiresDataFunc( tableName );
            var tableNeedsData = columnMissing;
            if( !columnMissing && validationRequiresData )
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

        static public SQLCmdDef CreateSQLCmdDef( string sql, string paramListName, List<object> paramList )
        {
            var placeHolderNames = string.Join( ", ", paramList.Select( ( _, index ) => $"@p{index}" ).ToArray() );

            var sqlDefParams = new List<(string name, object? value)>();
            for( int ii = 0; ii < paramList.Count; ++ii )
            {
                sqlDefParams.Add( ($"@p{ii}", paramList[ ii ]) );
            }

            sql = sql.Replace( paramListName, placeHolderNames );
            var retVal = new SQLCmdDef( sql, sqlDefParams );
            return retVal;
        }

        public async Task AnalyzeData<T>( string dataType, List<T> items, Func<T, Task<List<SQLCmdDef>>> getSqlCmdsFunc, Func<T, string> itemNameFunc, List<SQLCmdDef?> initCmds )
        {
            double count = items.Count;
            var completedCount = 0;
            Progress?.Report( 0 );
            List<SQLCmdDef?> nullableCmds = initCmds ?? [];

            using( var timer = new AutoTimer( $"    Analyze {dataType} - Getting Commands", Logger() ) )
            {
                var pool = items.Select( ( item, index ) => Task.Run( async () =>
                {
                    var retVal = (index: index, item: item, cmds: new List<SQLCmdDef?>());
                    if( item == null )
                        return retVal;

                    retVal.cmds.AddRange( await getSqlCmdsFunc( item ) );

                    CancellationToken?.ThrowIfCancellationRequested();
                    int currProgress = Interlocked.Increment( ref completedCount );
                    Progress?.Report( 80.0 * ( 1.0 * currProgress ) / count );
                    Logger().Debug( $"    Processed {dataType} ({currProgress} of {count}) - {itemNameFunc( item )}" );

                    return retVal;
                } ) );

                var unsortedResults = await Task.WhenAll( pool );
                CancellationToken?.ThrowIfCancellationRequested();
                var results = unsortedResults.OrderBy( item => item.index ).ToList();
                foreach( var result in results )
                {
                    nullableCmds.AddRange( result.cmds );
                    CancellationToken?.ThrowIfCancellationRequested();
                }
            }

            List<SQLCmdDef> sqlCmds = nullableCmds.Where( cmd => cmd != null ).Select( cmd => cmd! ).ToList();

            CancellationToken?.ThrowIfCancellationRequested();
            using( var timer = new AutoTimer( $"    Analyze {dataType} - Executing Commands", Logger() ) )
            {
                Progress?.Report( 80 );
                ExecuteCommands( sqlCmds );
                Progress?.Report( 100 );
                CancellationToken?.ThrowIfCancellationRequested();
                Logger().Debug( $"Finished Series Analysis" );
            }
        }
    }
}
