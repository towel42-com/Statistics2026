using MediaBrowser.Model.Querying;
using Statistics2026.Utilities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        private void ComputeDBState()
        {
            CheckIsValid( ECheckType.eInit );

            if( !Plugin.Instance!.Configuration.resetDBState && Plugin.Instance!.DBStateInitialized() )
                return;

            Plugin.Instance.ResetDBState();

            var tableNames = this.tableNames();

            _dbHelper.ValidateTables( tableNames,
                tableName =>
                {
                    if( _tableMap.TryGetValue( tableName, out var tableDef ) )
                    {
                        return tableDef.ColumnNames();
                    }
                    else
                    {
                        return [];
                    }
                },
                null,
                ( createTableNeeded, initDataNeeded ) =>
                {
                    Plugin.Instance.SetDBState( EDBState.eSystemTablesCreated, !createTableNeeded );
                    Plugin.Instance.SetDBState( EDBState.eSystemDataInitialized, !initDataNeeded );
                }
            );

            ComputeUserDataDBState();

            Plugin.Instance!.Configuration.resetDBState = false;
            Statistics2026.Plugin.Instance.UpdateConfiguration( Plugin.Instance!.Configuration );
        }

        private void ComputeUserDataDBState()
        {
            CheckIsValid( ECheckType.eInit );

            if( !Plugin.Instance!.DBStateInitialized() )
            {
                throw new ArgumentNullException( "Plugin.Instance is null or Plugin.Instance.DBState is null" );
            }

            var users = _embyInterfaces?._userManager!.GetUserList( new UserQuery() { EnableRemoteAccess = true } ).ToList();

            Plugin.Instance.RemoveDBState( EDBState.eUserTablesCreated );
            if( users == null )
            {
                return;
            }

            var tableNames = allUserMediaTables();
            var missing = new List<List<object>>();
            _dbHelper.ValidateTables( tableNames,
                tableName =>
                {
                    return _userMediaTemplate!.ColumnNames();
                },
                tableName =>
                {
                    var missingRows = CheckUserMediaTableHasData( tableName );
                    if( missingRows != null )
                        missing.AddRange( missingRows );
                    return missingRows != null;
                },
                ( createTableNeeded, initDataNeeded ) =>
                {
                    Plugin.Instance.SetDBState( EDBState.eUserTablesCreated, !createTableNeeded );
                    Plugin.Instance.SetDBState( EDBState.eUserDataInitialized, !initDataNeeded );
                }
            );

            if( missing != null && missing.Count > 0 )
                DumpTable.dumpTable( [ "User", "Item ID", "Name", "Runtime Ticks", "Is Played?", "Play Count", "Total Ticks Played", "Series Name", "Season#", "Episode#" ], missing, text => _embyInterfaces?._logger!.Warn( text ) );
        }
    }
}
