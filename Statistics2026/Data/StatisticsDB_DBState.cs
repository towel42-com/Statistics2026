using MediaBrowser.Model.Querying;
using ServiceStack.Text;

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

            var users = _embyInterfaces?._userManager.GetUserList( new UserQuery() { EnableRemoteAccess = true } ).ToList();

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
                    if ( missingRows != null )
                        missing.AddRange( missingRows );
                    return missingRows != null;
                },
                ( createTableNeeded, initDataNeeded ) =>
                {
                    Plugin.Instance.SetDBState( EDBState.eUserTablesCreated, !createTableNeeded );
                    Plugin.Instance.SetDBState( EDBState.eUserDataInitialized, !initDataNeeded );
                }
            );

            if ( missing != null && missing.Count > 0 )
                dumpTable( [ "User", "Item ID", "Name", "Runtime Ticks", "Is Played?", "Play Count", "Total Ticks Played", "Series Name", "Season#", "Episode#" ], missing, text => _embyInterfaces?._logger.Warn( text ) );
        }

        private List<List<object>>? CheckUserMediaTableHasData( string tableName )
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

            //_embyInterfaces!._logger.Debug( $"Table: {tableName} - # w/TicksPlayed {ticksPlayed} - # w/PlayCount {systemPlayed}" );
            List<List<object>>? missing = null;
            if( ticksPlayed != systemPlayed && ticksPlayed != 0 )
            {
                var user = getUserForTableName( tableName );
                if( user == null )
                {
                    _embyInterfaces?._logger.Warn( $"Invalid user table name {tableName}" );
                }
                else
                {
                    _embyInterfaces?._logger.Warn( $"User: {user.Name} has an invalid UserMedia table" );
                }

                var fields = $"Users.UserName, {tableName}.ItemID, {tableName}.Name, Media.RunTimeTicks, {tableName}.IsPlayed, {tableName}.PlayCount, {tableName}.TotalTicksPlayed, Series.Name, Media.Season, Media.Episode";
                var joinClause = $" LEFT JOIN Users ON Users.UserId={tableName}.UserId LEFT JOIN Media ON Media.ItemId = {tableName}.ItemId LEFT JOIN Series On Series.ItemId={tableName}.SeriesId";

                var pos = sqlTicks.IndexOf( "WHERE" );
                if( pos != -1 )
                    sqlTicks = sqlTicks.Insert( pos, joinClause + " " );

                pos = sqlPlayed.IndexOf( "WHERE" );
                if( pos != -1 )
                    sqlPlayed = sqlPlayed.Insert( pos, joinClause + " " );

                var sqlAtoB = sqlTicks + " EXCEPT " + sqlPlayed;
                sqlAtoB = sqlAtoB.Replace( "COUNT(*)", fields );

                var sqlBtoA = sqlPlayed + " EXCEPT " + sqlTicks;
                sqlBtoA = sqlBtoA.Replace( "COUNT(*)", fields );

                cmds =
                [
                    new( sqlAtoB ),
                    new( sqlBtoA )
                ];

                missing = [];
                _dbHelper.ExecuteCommands( cmds, statement =>
                {
                    var row = statement.Current;
                    var col = 0;

                    var userName = row.GetString( col++ );
                    var itemId = row.GetString( col++ );
                    var itemName = row.GetString( col++ );
                    var runtimeTicks = row.GetInt64( col++ );
                    var isPlayed = row.GetBoolean( col++ );
                    var playCount = row.GetInt64( col++ );
                    var totalTicksPlayed = row.GetInt64( col++ );
                    var seriesName = row.GetString( col++ );
                    var season = row.GetInt64( col++ );
                    var episode = row.GetInt64( col++ );

                    missing.Add( [ userName, itemId, itemName, runtimeTicks, isPlayed, playCount, totalTicksPlayed, seriesName, season, episode ] );

                    return true;
                } );
            }

            return ( ticksPlayed == systemPlayed ) ? null : missing;
        }

        public static void dumpTable( List<string> header, List<List<object>> rows, Action<string> dumpFunc )
        {
            List<int> columnWidths = [];
            foreach( var item in header )
            {
                columnWidths.Add( item.Length + 2 );
            }

            foreach( var currRow in rows )
            {
                if( currRow.Count() != header.Count() )
                {
                    throw new InvalidDataContractException( "Header column count is different than row column count" );
                }
                for( var ii = 0; ii < currRow.Count(); ++ii )
                {
                    var currItemString = currRow[ ii ]?.ToString() ?? string.Empty;

                    columnWidths[ ii ] = Math.Max( currItemString.Length + 2, columnWidths[ ii ] );
                }
            }

            dumpRow( columnWidths, null, dumpFunc );
            dumpRow( columnWidths, header.Cast<object>().ToList(), dumpFunc );
            dumpRow( columnWidths, null, dumpFunc );
            foreach( var currRow in rows )
            {
                dumpRow( columnWidths, currRow.Cast<object>().ToList(), dumpFunc );
            }
            dumpRow( columnWidths, null, dumpFunc );
        }

        public static void dumpRow( List<int> columnWidths, List<object>? items, Action<string> dumpFunc )
        {
            var rowString = string.Empty;
            if( items == null )
            {
                for( var ii = 0; ii < columnWidths.Count(); ++ii )
                {
                    if( ii == 0 )
                    {
                        rowString += "|";
                    }
                    rowString += new string( '-', columnWidths[ ii ] );
                    rowString += "|";
                }
            }
            else
            {
                if( columnWidths.Count() != items.Count() )
                {
                    throw new InvalidDataContractException( "Column width column count is different than row column count" );
                }

                for( var ii = 0; ii < items.Count(); ++ii )
                {
                    var currItem = items[ ii ]?.ToString() ?? string.Empty;
                    var totalPadding = columnWidths[ ii ] - currItem.Length;
                    var leftPadding = totalPadding / 2;

                    currItem = new string( ' ', leftPadding ) + currItem + new string( ' ', totalPadding - leftPadding );
                    if( ii == 0 )
                    {
                        rowString += "|";
                    }
                    rowString += currItem;
                    rowString += "|";
                }
            }
            dumpFunc( rowString );
        }
    }
}
