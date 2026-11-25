#if DEBUG
//#define DEBUG_DB
//#define DEBUG_DB_VEBOSE
#endif
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Utilities;
using System.Text;

namespace Statistics2026.Data
{
    public enum EMediaType
    {
        eMovie,
        eSeries,
        eEpisode
    }

    public sealed partial class Statistics2026DB : IAsyncDisposable, IHasEmbyInterface
    {
        public EmbyInterfaces _embyInterfaces { get; }
        private bool _disposed = false;
        ILogger? _DBConnLogger = null;
        private MediaBrowser.Model.Tasks.IScheduledTask? _currentTask = null;

        public bool AllTablesExisted { get; private set; } = false;
        public bool DataExists { get; private set; } = false;
        public bool UserDataExists { get; private set; } = false;

        private IDatabaseConnection? Connection { get; set; } = null;
        public CancellationToken? CancellationToken { get; set; } = null;
        public IProgress<double>? Progress { get; set; } = null;

        private class DBInitiator
        {
            public async Task<Statistics2026DB> InitAsync( EmbyInterfaces? embyInterfaces, string instanceLocation )
            {
                if( embyInterfaces == null )
                    throw new ArgumentNullException( "EmbyInterfaces is null." );

                Statistics2026DB? retVal = null;
                SQLCmdDef.setWarningLogger( ( msg ) => { embyInterfaces.Logger().Warn( msg ); } );

                await SQLCmdDef.LockDBAsync( null, async () =>
                {
                    retVal = new Statistics2026DB( embyInterfaces, instanceLocation );

                    retVal.dbStatusMessage( "New Instance Created" );
                    Plugin.Instance?.Configuration.UpdateBuildDateAndVersionInfo();
                    retVal.dbStatusMessage( "Creating Database" );
                    CreateConnection( retVal );
                    Plugin.Instance?._tableDefs.SetDatabase( retVal );
                    retVal.dbStatusMessage( "Finished Creating Database" );
                } ).ConfigureAwait( false );

                if( retVal == null )
                    throw new Exception( "Problem getting db instance" );

                await retVal.ComputeDBStateAsync().ConfigureAwait( false );

                return retVal;
            }

            private void CreateConnection( Statistics2026DB db )
            {
                string oldFileName = Path.Combine( db.ConfigManager().ApplicationPaths.DataPath, "Statistics2026.db" );
                string newFileName = Path.Combine( db.ConfigManager().ApplicationPaths.PluginConfigurationsPath, "Statistics2026.db" );

                if( File.Exists( oldFileName ) )
                {
                    try
                    {
                        db.Logger().Info( "DB exists in old location, moving to plugin/config" );
                        File.Move( oldFileName, newFileName );
                        db.Logger().Info( "   Moved successfully" );
                    }
                    catch( IOException ex )
                    {
                        // Handles scenarios like: file already exists at destination, or file is locked
                        // Handles scenarios like: file already exists at destination, or file is locked
                        db.Logger().Error( $"   Failed to be moved, removing and new db will be built - {ex.Message}" );
                    }
                    catch( UnauthorizedAccessException ex )
                    {
                        db.Logger().Error( $"   Failed to be moved, removing and new db will be built - Check Permissions - {ex.Message}" );
                    }
                }

                if( File.Exists( oldFileName ) ) // could not move, try deleting
                {
                    try
                    {
                        db.Logger().Info( "Deleting old DB file" );
                        File.Delete( oldFileName );
                        db.Logger().Info( "   Deleted successfully" );
                    }
                    catch( Exception ex )
                    {
                        db.Logger().Error( $"   Failed to be delete - {ex.Message}" );
                    }
                }

                CreateConnection( db, newFileName );
            }

            private void CreateConnection( Statistics2026DB db, string db_file )
            {
                _ = db.CheckIsValid( ECheckLevel.eLogger | ECheckLevel.eThrowOnFailure );

                db.Logger().Debug( $"CreateConnection - ThreadID: {Environment.CurrentManagedThreadId}" );
                ConnectionFlags connectionFlags;

                //_embyInterfaces!.Logger().Debug("Opening write _connection");
                connectionFlags = ConnectionFlags.Create; // create if missing
                connectionFlags |= ConnectionFlags.ReadWrite; // open for read-write
                connectionFlags |= ConnectionFlags.PrivateCache;
                connectionFlags |= ConnectionFlags.NoMutex;

                try
                {
                    SQLiteDatabaseConnection sqliteDb = SQLite3.Open( db_file, connectionFlags, null, true );
                    db.Connection = sqliteDb;
                    db.dbStatusMessage( "DB Opened" );

                    List<string> queries =
                        [
                            "PRAGMA busy_timeout = 5000",
                            "PRAGMA journal_mode=WAL",
                            "PRAGMA busy_timeout=5000",
                            "PRAGMA synchronous=Normal",
                            "PRAGMA temp_store=file"
                        ];

                    foreach( string query in queries )
                    {
                        db.Connection.Execute( query );
                    }
                    db.dbStatusMessage( "Pragmas Set" );
                }
                catch
                {
                    throw;
                }

                db.dbStatusMessage( "Connection Created" );
            }
        };

        public async static Task<Statistics2026DB> GetInstanceAsync( EmbyInterfaces? embyInterfaces, string instanceLocation )
        {
            DBInitiator dbInit = new();
            Statistics2026DB retVal = await dbInit.InitAsync( embyInterfaces, instanceLocation ).ConfigureAwait( false );
            return retVal;
        }

        private Statistics2026DB( EmbyInterfaces embyInterfaces, string instanceLocation )
        {
            _embyInterfaces = embyInterfaces ?? throw new ArgumentNullException( "embyInterfaces is null." );
#if DEBUG_DB
            _DBConnLogger = embyInterfaces.LogManager().GetLogger( "Statistics2026 - DBMonitor" );
#endif
            _instanceLocation = instanceLocation;
        }

        public async ValueTask DisposeAsync()
        {
            if( _disposed )
                return;

            await CloseConnectionAsync().ConfigureAwait( false );

            GC.SuppressFinalize( this );
        }

        void Dispose()
        {
            if( _disposed )
                return;

            CloseConnectionSynchronous();

            _disposed = true;
        }

        private string? _instanceLocation = null;
        private string? _DBLogPrefix = null;

        private string getDBLogPrefix()
        {
            if( _DBLogPrefix == null )
            {
                if( _instanceLocation == null )
                    throw new Exception( "_instanceLocation not set" );

                if( Connection == null )
                    return $"0x{(long)-1:X16} - {_instanceLocation!}";

                _DBLogPrefix = $"0x{GetDBHashCode():X16} - {_instanceLocation!}";
            }
            return _DBLogPrefix;
        }

        public void dbLog( string msg, LogSeverity severity = LogSeverity.Info, bool verbose = false, bool includeStack = false )
        {
            if( !includeStack )
            {
                _DBConnLogger?.Log( severity, $"{getDBLogPrefix()}: {msg}" );
                return;
            }

            dbLogMultiLine( msg, null, severity, verbose, includeStack );

        }

        public void dbLogMultiLine( string title, StringBuilder? msgs, LogSeverity severity, bool verbose, bool includeStack = false )
        {
            if( msgs == null && !includeStack )
                return;

            if( verbose == true )
            {
#if !DEBUG_DB_VEBOSE
                return;
#endif
            }

            if( includeStack )
            {
                if( msgs == null )
                    msgs = new();
                else
                    msgs.AppendLine( "======================================" );
                msgs.Append( HelperFuncs.getStackTrace( [ "Statistics2026DB.dbStatusMessage", "Statistics2026DB..ctor", "Statistics2026DB.GetInstance", "Statistics2026DB.dbLog", "Statistics2026DB.dbLogMultiLine" ] ) );
                msgs.AppendLine( "======================================" );
            }
            _DBConnLogger?.LogMultiline( $"{getDBLogPrefix()}: {title}", LogSeverity.Info, msgs! );
        }

        public bool dbLoggerEnabled()
        {
            return _DBConnLogger != null;
        }

        public TableDefs TableDefs()
        {
            if( ( Plugin.Instance?._tableDefs ?? null ) == null )
                throw new ArgumentNullException( "TableDefs is null" );
            return Plugin.Instance!._tableDefs;
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

        public int GetDBHashCode()
        {
            return Connection?.GetHashCode() ?? 0;
        }

        private void dbStatusMessage( string message, bool includeStack = false )
        {
            StringBuilder builder = new();
            dbLog( $"Thread ID: {Environment.CurrentManagedThreadId:D4} - Statistics2026DB Hash: 0x{GetHashCode():X16}: '{message}'", includeStack: includeStack );
        }

        public void CloseConnectionSynchronous()
        {
            SQLCmdDef.lockDBSynchronous( this, () =>
            {
                try
                {
                    dbStatusMessage( "Cleaning up" );
                    if( Connection != null )
                    {
                        Connection.Close();
                        dbStatusMessage( "DB Connection Closed" );
                        Connection = null;
                    }
                }
                catch( Exception )
                {
                }
            } );
        }

        public Task CloseConnectionAsync()
        {
            return SQLCmdDef.LockDBAsync( this, async () =>
            {
                try
                {
                    dbStatusMessage( "Cleaning up" );
                    if( Connection != null )
                    {
                        Connection.Close();
                        dbStatusMessage( "DB Connection Closed" );
                        Connection = null;
                    }
                }
                catch( Exception )
                {
                }
            } );
        }

        public Task InitializeAsync( MediaBrowser.Model.Tasks.IScheduledTask? runningTask, CancellationToken? cancellationToken, IProgress<double>? progress, bool reset = false )
        {
            _currentTask = runningTask;
            SetCancellationToken( cancellationToken, progress );
            return TableDefs().CreateTablesAsync( reset ? TableDef.EAction.eRecreate : TableDef.EAction.eCreate, cancellationToken, value => { progress?.Report( value ); } );
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
            if( !statement.BindParameters.TryGetValue( name, out IBindParameter? bindParam ) )
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

        public Task ExecuteCommandAsync( SQLCmdDef cmd, Func<IStatement, Task<bool>>? onStatement = null, bool useTransaction = true, bool locked = false )
        {
            List<SQLCmdDef> cmds = [ cmd ];
            return ExecuteCommandsAsync( cmds, true, onStatement, useTransaction, locked );
        }

        private void SetProgress( double value )
        {
            Progress?.Report( value );
        }

        public async Task ExecuteCommandsAsync( List<SQLCmdDef> cmds, bool useProgress = true, Func<IStatement, Task<bool>>? onStatement = null, bool useTransaction = true, bool locked = false )
        {
            _ = CheckIsValid( ECheckLevel.eConnection | ECheckLevel.eThrowOnFailure );

            async Task runCommandsAsync( IDatabaseConnection? connection, List<SQLCmdDef> cmds, Func<IStatement, Task<bool>>? onStatement, bool locked, bool withTransaction )
            {
                if( connection == null )
                    return;

                if( withTransaction )
                {
                    connection.Execute( "BEGIN TRANSACTION;" );
                }

                try
                {
                    for( int ii = 0; ii < cmds.Count; ++ii )
                    {
                        CancellationToken?.ThrowIfCancellationRequested();

                        SQLCmdDef cmd = cmds[ ii ];
                        await cmd.ExecuteAsync( connection, this, onStatement, locked ).ConfigureAwait( false );
                        if( useProgress && ( cmds.Count > 1 ) )
                        {
                            double value = 80 + ( 20.0 * ii / cmds.Count );
                            SetProgress( value );
                        }
                    }

                    if( withTransaction )
                    {
                        connection.Execute( "COMMIT;" );
                    }
                }
                catch
                {
                    if( withTransaction )
                    {
                        try
                        { 
                            connection.Execute( "ROLLBACK;" );
                        }
                        catch { }
                    }
                    throw;
                }
            }

            try
            {
                bool runInTx = useTransaction && ( cmds.Count > 1 );

                if( runInTx && !locked )
                {
                    await SQLCmdDef.LockDBAsync( this, () => runCommandsAsync( Connection, cmds, onStatement, true, withTransaction: runInTx ) ).ConfigureAwait( false );
                }
                else
                {
                    await runCommandsAsync( Connection, cmds, onStatement, locked, withTransaction: runInTx ).ConfigureAwait( false );
                }
            }
            catch( Exception )
            {
                throw;
            }
        }

        public Task ExecuteCommandsAsync( List<string> cmds, bool useTransaction = true )
        {
            List<SQLCmdDef> cmdDefs = [];
            foreach( string cmd in cmds )
            {
                CancellationToken?.ThrowIfCancellationRequested();
                cmdDefs.Add( new SQLCmdDef( cmd ) );
            }

            return ExecuteCommandsAsync( cmdDefs, true, null, useTransaction );
        }

        public Task ExecuteCommandAsync( string cmd )
        {
            return ExecuteCommandAsync( new SQLCmdDef( cmd ), null, false );
        }

        public async Task<bool> ColumnExistsAsync( string tableName, string columnName )
        {
            string sql = $"SELECT 1 FROM pragma_table_info(@TableName) WHERE name=@ColumnName";
            List<(string name, object? value)> parameters =
                [
                    ("@TableName", tableName),
                    ("@ColumnName", columnName)
                ];

            bool exists = false;

            await ExecuteCommandAsync( new SQLCmdDef( sql, parameters ), statement =>
            {
                IResultSet row = statement.Current;
                long value = row.GetInt64( 0 );
                exists = value != 0;
                return Task.FromResult( true );
            } ).ConfigureAwait( false );

            return exists;
        }

        public async Task<bool> TableExistsAsync( string tableName )
        {
            string sql = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@TableName";
            List<(string name, object? value)> parameters = [ ("@TableName", tableName) ];

            bool exists = false;

            await ExecuteCommandAsync( new SQLCmdDef( sql, parameters ), statement =>
            {
                IResultSet row = statement.Current;
                long value = row.GetInt64( 0 );
                exists = value != 0;
                return Task.FromResult( true );
            } ).ConfigureAwait( false );

            return exists;
        }

        public static string FormatTicks( long ticks )
        {
            var runtime = new RunTime( ticks );
            return runtime.ToLongString();
        }

        public async Task ValidateTablesAsync( List<string> tables, Func<string, List<string>> getColumnsFunc, Func<string, bool> validationRequiresDataFunc, Func<string, bool>? additionalNeedsDataFunc, Action<bool, bool> updateDBState )
        {
            if( tables.IsNullOrEmpty() )
            {
                updateDBState( true, true );
                return;
            }

            bool aTableMissing = false;
            bool aColumnMissing = false;
            bool aTableNeedsData = false;
            foreach( string tableName in tables )
            {
                bool tableMissing = !( await TableExistsAsync( tableName ).ConfigureAwait( false ) );
                bool columnMissing = false;
                bool tableNeedsData = false;
                if( !tableMissing )
                {
                    (columnMissing, tableNeedsData) = await ValidateTableAsync( tableName, getColumnsFunc( tableName ), validationRequiresDataFunc, additionalNeedsDataFunc ).ConfigureAwait( false );
                }
                else
                {
                    tableNeedsData = columnMissing = true;
                }

                if( tableMissing )
                {
                    _embyInterfaces?.Logger().Info( $"Table '{tableName}' does not exist." );
                }

                if( columnMissing )
                {
                    _embyInterfaces?.Logger().Info( $"Table '{tableName}' is missing a column." );
                }

                if( tableNeedsData )
                {
                    _embyInterfaces?.Logger().Info( $"Table '{tableName}' is missing data." );
                }
                aTableMissing = aTableMissing || tableMissing;
                aColumnMissing = aColumnMissing || columnMissing;
                aTableNeedsData = aTableNeedsData || tableNeedsData;
                if( aTableMissing && aTableNeedsData && aColumnMissing )
                    break;
            }

            updateDBState( aTableMissing || aColumnMissing, aTableNeedsData );
        }

        public async Task<(bool columnMissing, bool dataMissing)> ValidateTableAsync( string tableName, List<string> columns, Func<string, bool> validationRequiresDataFunc, Func<string, bool>? additionalNeedsDataFunc )
        {
            bool columnMissing = false;
            foreach( string columnName in columns )
            {
                columnMissing = columnMissing || !( await ColumnExistsAsync( tableName, columnName ).ConfigureAwait( false ) );
            }

            bool validationRequiresData = validationRequiresDataFunc( tableName );
            bool tableNeedsData = columnMissing;
            if( !columnMissing && validationRequiresData )
            {
                string sql = $"SELECT COUNT(*) FROM {tableName} LIMIT 1";

                tableNeedsData = false;
                await ExecuteCommandAsync( new SQLCmdDef( sql ), statement =>
                {
                    IResultSet row = statement.Current;
                    tableNeedsData = row.GetInt64( 0 ) == 0;
                    return Task.FromResult( false );
                }
                ).ConfigureAwait( false );

                tableNeedsData = tableNeedsData || ( ( additionalNeedsDataFunc != null ) && additionalNeedsDataFunc( tableName ) );
            }

            return (columnMissing, tableNeedsData);
        }

        static public SQLCmdDef CreateSQLCmdDef( string sql, string paramListName, List<object> paramList )
        {
            string placeHolderNames = string.Join( ", ", paramList.Select( ( _, index ) => $"@p{index}" ).ToArray() );

            var sqlDefParams = new List<(string name, object? value)>();
            for( int ii = 0; ii < paramList.Count; ++ii )
            {
                sqlDefParams.Add( ($"@p{ii}", paramList[ ii ]) );
            }

            sql = sql.Replace( paramListName, placeHolderNames );
            var retVal = new SQLCmdDef( sql, sqlDefParams );
            return retVal;
        }

        public Task DeleteAllInvalidAsync( List<string> ids, string tableName, string idColName, List<string>? whereClauses = null, List<(string name, object? value)>? parameters = null )
        {
            whereClauses = whereClauses ?? [];
            whereClauses.Add( $"{idColName} NOT IN (<KEYLIST>)" );
            string whereClause = HelperFuncs.JoinWhereClauses( whereClauses );

            SQLCmdDef cmd = SQLCmdDef.CreateSQLCmdDef( $"DELETE FROM {tableName} {whereClause}", "<KEYLIST>", ids.ConvertAll( item => (object)item ), parameters );
            return ExecuteCommandAsync( cmd );
        }

        public Task DeleteAllInvalidAsync( List<BaseItem> items, string tableName, string idColName, List<string>? whereClauses = null, List<(string name, object? value)>? parameters = null )
        {
            List<string> ids = EmbyHelperFuncs.GetAllIds( items );
            return DeleteAllInvalidAsync( ids, tableName, idColName, whereClauses, parameters );
        }

        //public async Task AnalyzeData<T>( string dataType, List<T> items, Func<T, Task<List<SQLCmdDef>>> getSqlCmdsFunc, Func<T, string> itemNameFunc, List<SQLCmdDef?> initCmds )
        //{
        //    double count = items.Count;
        //    var completedCount = 0;
        //    SetProgress( 0 );
        //    List<SQLCmdDef> sqlCmds = initCmds?.Where( item => item != null ).Select( item => item! ).ToList() ?? [];

        //    using( var timer = new AutoTimer( $"    Analyze {dataType} - Getting Commands", Logger() ) )
        //    {
        //        var pool = items.Select( ( item, index ) => Task.Run( async () =>
        //        {
        //            (int index, T item, List<SQLCmdDef?>? cmds) retVal = (index, item, new List<SQLCmdDef?>());
        //            if( item == null )
        //                return retVal;

        //            await Task.Yield();
        //            retVal.cmds!.AddRange( await getSqlCmdsFunc( item ) );

        //            CancellationToken?.ThrowIfCancellationRequested();
        //            int currProgress = Interlocked.Increment( ref completedCount );
        //            SetProgress( 80.0 * ( 1.0 * currProgress ) / count );
        //            Logger().Debug( $"    Processed {dataType} ({currProgress} of {count}) - {itemNameFunc( item )}" );

        //            return retVal;
        //        } ) );

        //        var unsortedResults = await Task.WhenAll( pool ).ConfigureAwait( false );
        //        sqlCmds.AddRange( unsortedResults
        //                    .Where( item => item.cmds != null )
        //                    .OrderBy( item => item.index )
        //                    .SelectMany( item => item.cmds! )
        //                    .Where( subItem => subItem != null )
        //                    .Select( subItem => subItem! )
        //                    .ToList()
        //                    );

        //        CancellationToken?.ThrowIfCancellationRequested();
        //    }

        //    CancellationToken?.ThrowIfCancellationRequested();
        //    using( var timer = new AutoTimer( $"    Analyze {dataType} - Executing Commands", Logger() ) )
        //    {
        //        SetProgress( 80 );
        //        ExecuteCommands( sqlCmds );
        //        SetProgress( 100 );
        //        CancellationToken?.ThrowIfCancellationRequested();
        //        Logger().Debug( $"Finished Series Analysis" );
        //    }
        //}

        public bool HasConnection()
        {
            return Connection != null;
        }
    }
}
