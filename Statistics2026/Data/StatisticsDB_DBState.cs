using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        private void ComputeDBState()
        {
            CheckIsValid( ECheckType.eInit );

            if( Plugin.Instance!.DBStateInitialized() )
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
        }

        private void ComputeUserDataDBState()
        {
            CheckIsValid( ECheckType.eInit );

            if( !Plugin.Instance!.DBStateInitialized() )
            {
                throw new ArgumentNullException( "Plugin.Instance is null or Plugin.Instance.DBState is null" );
            }

            var users = _embyInterfaces?._userManager.GetUserList( new UserQuery() { EnableRemoteAccess = true } ).ToList();

            Plugin.Instance.RemoveDBState( EDBState.eUserTablesCreated );
            if( users == null )
            {
                return;
            }

            var tableNames = allUserMediaTables();
            _dbHelper.ValidateTables( tableNames,
                tableName =>
                {
                    return _userMediaTemplate!.ColumnNames();
                },
                tableName =>
                {
                    var sqlTicks = $"SELECT COUNT(*) FROM {tableName} WHERE TotalTicksPlayed IS NOT NULL AND TotalTicksPlayed != 0";
                    var sqlPlayed = $"SELECT COUNT(*) FROM {tableName} WHERE PlayCount>0 AND IsPlayed";

                    long? ticksPlayed = null;
                    long systemPlayed = 0;

                    List<SQLCmdDef> cmds =
                    [
                        new( sqlTicks ),
                        new( sqlPlayed )
                    ];

                    _dbHelper.ExecuteCommands( cmds, statement =>
                    {
                        var row = statement.Current;
                        var value = row.GetInt64( 0 );
                        if( ticksPlayed == null )
                            ticksPlayed = value;
                        else
                            systemPlayed = value;
                        return true;
                    }
                    );

                    ticksPlayed ??= 0;

                    _embyInterfaces!._logger.Debug( $"Table: {tableName} - # w/TicksPlayed {ticksPlayed} - # w/PlayCount {systemPlayed}" );
                    if( ticksPlayed! != systemPlayed )
                    {
                        _embyInterfaces._logger.Debug( "The following rows do not have TotalTicksPlayed data" );

                        var sql =
                            $"SELECT * FROM {tableName} WHERE TotalTicksPlayed IS NOT NULL AND TotalTicksPlayed != 0" +
                            $" EXCEPT " +
                            $"SELECT * FROM {tableName} WHERE PlayCount>0 AND IsPlayed"
                            ;

                        _dbHelper.ExecuteCommand( new SQLCmdDef( sql ), statement =>
                        {
                            var row = statement.Current;
                            var col = 0;
                            var userId = row.GetString( col++ );
                            var itemId = row.GetString( col++ );
                            var name = row.GetString( col++ );
                            var isPlayed = row.GetBoolean( col++ );
                            var playCount = row.GetInt64( col++ );
                            var lastPlayed = row.GetString( col++ );
                            var startTickPos = row.GetInt64( col++ );
                            var endTickPos = row.GetInt64( col++ );
                            var totalTicksPlayed = row.GetInt64( col++ );
                            var isEpisode = row.GetBoolean( col++ );
                            var numEpisodes = row.GetInt64( col++ );
                            var isTVSpecial = row.GetBoolean( col++ );
                            var seriesId = row.GetString( col++ );

                            _embyInterfaces._logger.Debug( $"{name} - {isPlayed} - {playCount} - {totalTicksPlayed}" );
                            return true;
                        } );
                    }

                    return ticksPlayed != systemPlayed;
                },
                ( createTableNeeded, initDataNeeded ) =>
                {
                    Plugin.Instance.SetDBState( EDBState.eUserTablesCreated, !createTableNeeded );
                    Plugin.Instance.SetDBState( EDBState.eUserDataInitialized, !initDataNeeded );
                }
            );
        }
    }
}
