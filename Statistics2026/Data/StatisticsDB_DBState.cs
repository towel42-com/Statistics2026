using MediaBrowser.Model.Querying;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        private void ComputeDBState()
        {
            CheckIsValid( ECheckLevel.eInit );

            if( !Plugin.Instance!.Configuration.resetDBState && Plugin.Instance!.DBStateInitialized() )
                return;

            Plugin.Instance.ResetDBState();

            var tableNames = TableDefs().tableNames();

            ValidateTables( tableNames,
                tableName =>
                {
                    return TableDefs().getTableDef( tableName )?.ColumnNames() ?? [];
                },
                tableName =>
                {
                    return TableDefs().getTableDef( tableName )?.ValidationRequiresData ?? false;
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
            CheckIsValid( ECheckLevel.eInit );

            if( !Plugin.Instance!.DBStateInitialized() )
            {
                throw new ArgumentNullException( "Plugin.Instance is null or Plugin.Instance.DBState is null" );
            }

            var users = EmbyHelperFuncs.GetAllUsers( _embyInterfaces );
            Plugin.Instance.RemoveDBState( EDBState.eUserTablesCreated );
            if( users == null )
            {
                return;
            }

            var tableNames = TableDefs().allUserMediaTables( false );
            var missing = new List<List<object>>();
            ValidateTables( tableNames,
                tableName =>
                {
                    return TableDefs().UserMediaUsageColumnNames();
                },
                tableName =>
                {
                    return true;
                },
                tableName =>
                {
                    var user = TableDefs().getUserForTableName( tableName );
                    if( ( user != null ) && ( user.Policy.IsAdministrator ) )
                        return false;

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
                DumpTable.dumpTable( [ "User", "Item ID", "Name", "Runtime Ticks", "Is Played?", "Play Count", "Total Ticks Played", "Series Name", "Season#", "Episode#" ], missing, text => Logger().Warn( text ) );
        }
    }
}
