using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
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
        private readonly EmbyInterfaces? _embyInterfaces = null;
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
                embyInterfaces._logger!.Debug( "Statistics2026 : New Instance Created : " + retVal.GetHashCode() );
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

            embyInterfaces._logger?.Debug( "Statistics2026 : Creating Database" );

            _embyInterfaces = embyInterfaces;
            _dbHelper = new DBHelper( _embyInterfaces );

            embyInterfaces._logger?.Debug( "Statistics2026 : Finished Creating Database" );
            lock( _padlock )
            {
                ComputeDBState();
            }
        }

        ~StatisticsDB()
        {
        }

        private void CheckIsValid( ECheckType checkType )
        {
            if( Statistics2026.Plugin.Instance == null )
                throw new ArgumentNullException( "Statistics2026.Plugin.Instance" );

            if( Statistics2026.Plugin.Instance.Configuration == null )
                throw new ArgumentNullException( "Statistics2026.Plugin.Instance.Configuration" );

            if( checkType == ECheckType.eInit )
                _ = _dbHelper.CheckIsValid( ECheckLevel.eInterfaces | ECheckLevel.eThrowOnFailure );
            else if( checkType == ECheckType.eReport )
                _ = _dbHelper.CheckIsValid( ECheckLevel.eInterfaces | ECheckLevel.eConnection | ECheckLevel.eThrowOnFailure );
            else if( checkType == ECheckType.eUpdate )
                _ = _dbHelper.CheckIsValid( ECheckLevel.eAllWithThrow );

            if( ( checkType != ECheckType.eInit ) && ( checkType != ECheckType.eReport ) && Plugin.Instance != null && Plugin.Instance.IsStatistics2026TaskRunning( _currentTask ) )
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
    }
}
