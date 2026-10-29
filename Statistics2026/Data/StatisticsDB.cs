using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Logging;
using Statistics2026.Utilities;
using System;
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

    public sealed partial class StatisticsDB
    {
        private static readonly object _padlock = new();
        private readonly Dictionary<string, TableDef> _tableMap = [];
        private List<TableDef> _tableList = [];
        private TableDef? _userMediaTemplate = null;
        private MediaBrowser.Model.Tasks.IScheduledTask? _currentTask = null;

        public bool AllTablesExisted { get; private set; } = false;
        public bool DataExists { get; private set; } = false;
        public bool UserDataExists { get; private set; } = false;

        private readonly DBHelper _dbHelper = new();

        public static StatisticsDB GetInstance( EmbyInterfaces? embyInterfaces )
        {
            if( embyInterfaces == null )
                throw new ArgumentNullException( "EmbyInterfaces is null." );

            lock( _padlock )
            {
                var retVal = new StatisticsDB( embyInterfaces );
                embyInterfaces.Logger().Debug( "Statistics2026 : New Instance Created : " + retVal.GetHashCode() );
                return retVal;
            }
        }

        private StatisticsDB()
        {
            ConstructTableList();
        }

        private StatisticsDB( EmbyInterfaces embyInterfaces )
        {
            if( embyInterfaces == null )
                throw new ArgumentNullException( "embyInterfaces is null." );

            ConstructTableList();

            embyInterfaces.Logger().Debug( "Statistics2026 : Creating Database" );

            _dbHelper = new DBHelper( embyInterfaces );

            embyInterfaces.Logger().Debug( "Statistics2026 : Finished Creating Database" );
            lock( _padlock )
            {
                ComputeDBState();
            }
        }

        ~StatisticsDB()
        {
        }

        public EmbyInterfaces EmbyInterfaces()
        {
            _dbHelper.CheckIsValid( ECheckLevel.eEmbyInterfaceOnly | ECheckLevel.eThrowOnFailure );
            return _dbHelper._embyInterfaces!;
        }

        public ILogger Logger()
        {
            _dbHelper.CheckIsValid( ECheckLevel.eLogger | ECheckLevel.eThrowOnFailure );
            return EmbyInterfaces()._logger!;
        }

        public ILibraryManager LibraryManager()
        {
            _dbHelper.CheckIsValid( ECheckLevel.eEmbyInterfaceOnly | ECheckLevel.eThrowOnFailure );
            return EmbyInterfaces()._libraryManager!;
        }

        public IUserDataManager UserDataManager()
        {
            _dbHelper.CheckIsValid( ECheckLevel.eEmbyInterfaceOnly | ECheckLevel.eUserDataManager | ECheckLevel.eThrowOnFailure );
            return EmbyInterfaces()._userDataManager!;
        }

        public IUserManager UserManager()
        {
            _dbHelper.CheckIsValid( ECheckLevel.eEmbyInterfaceOnly | ECheckLevel.eUserManager | ECheckLevel.eThrowOnFailure );
            return EmbyInterfaces()._userManager!;
        }

        public IProviderManager ProviderManager()
        {
            _dbHelper.CheckIsValid( ECheckLevel.eEmbyInterfaceOnly | ECheckLevel.eProviderManager | ECheckLevel.eThrowOnFailure );
            return EmbyInterfaces()._providerManager!;
        }

        private void CheckIsValid( ECheckLevel checkType )
        {
            _ = _dbHelper.CheckIsValid( checkType | ECheckLevel.eThrowOnFailure ); // always throw

            if( ( ( checkType & ECheckLevel.eInit ) == 0 ) && ( ( checkType & ECheckLevel.eReport ) != 0 ) && TaskDef.IsStatistics2026TaskRunning( EmbyInterfaces(), _currentTask, true ) )
            {
                throw new Exception( "Statistics 2026 task is running" );
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
            if( _dbHelper != null )
            {
                _dbHelper.CancellationToken = null;
                _dbHelper.Progress = null;
            }
        }

        private void SetCancellationToken( CancellationToken? cancellationToken, IProgress<double>? progress )
        {
            if( _dbHelper != null )
            {
                _dbHelper.CancellationToken = cancellationToken;
                _dbHelper.Progress = progress;
            }
        }

        public async Task AnalyzeData<T>( string dataType, List<T> items, Func<T, Task<List<SQLCmdDef>>> getSqlCmdsFunc, Func<T, string> itemNameFunc, List<SQLCmdDef?> initCmds )
        {
            double count = items.Count;
            var completedCount = 0;
            _dbHelper.Progress?.Report( 0 );
            List<SQLCmdDef?> nullableCmds = initCmds ?? [];

            using( var timer = new AutoTimer( $"    Analyze {dataType} - Getting Commands", Logger() ) )
            {
                var pool = items.Select( ( item, index ) => Task.Run( async () =>
                {
                    var retVal = (index: index, item: item, cmds: new List<SQLCmdDef?>());
                    if( item == null )
                        return retVal;

                    retVal.cmds.AddRange( await getSqlCmdsFunc( item ) );

                    _dbHelper.CancellationToken?.ThrowIfCancellationRequested();
                    int currProgress = Interlocked.Increment( ref completedCount );
                    EmbyInterfaces().Logger().Debug( $"    Processed {dataType} ({currProgress} of {count}) - {itemNameFunc( item )}" );

                    return retVal;
                } ) );

                var unsortedResults = await Task.WhenAll( pool );
                _dbHelper.CancellationToken?.ThrowIfCancellationRequested();
                var results = unsortedResults.OrderBy( item => item.index ).ToList();
                foreach( var result in results )
                {
                    nullableCmds.AddRange( result.cmds );
                    _dbHelper.CancellationToken?.ThrowIfCancellationRequested();
                }
            }

            List<SQLCmdDef> sqlCmds = nullableCmds.Where( cmd => cmd != null ).Select( cmd => cmd! ).ToList();

            _dbHelper.CancellationToken?.ThrowIfCancellationRequested();
            using( var timer = new AutoTimer( $"    Analyze {dataType} - Executing Commands", Logger() ) )
            {
                _dbHelper.Progress?.Report( 80 );
                _dbHelper.ExecuteCommands( sqlCmds );
                _dbHelper.Progress?.Report( 100 );
                _dbHelper.CancellationToken?.ThrowIfCancellationRequested();
                EmbyInterfaces().Logger().Debug( $"Finished Series Analysis" );
            }
        }
    }
}
