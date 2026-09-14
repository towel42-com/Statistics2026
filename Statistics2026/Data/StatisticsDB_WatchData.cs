using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using ServiceStack;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using static ServiceStack.Diagnostics;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public (long? startPos, long? endPos, long? total) GetPlaybackState(string userId, string itemId)
        {
            var tableName = getUserTableName(userId);

            string sql =
                "SELECT " +
                "StartTickPos, " +
                "EndTickPos " +
                "TotalTicks " +
                $"FROM {tableName} " +
                $"WHERE ItemId=@ItemId"
                ;
            var parameters = new List<(string, object?)>() { ("@ItemId", itemId) };

            long? startPos = null;
            long? endPos = null;
            long? totalTicks = null;

            _dbHelper.ExecuteCommand(new SQLCmdDef(sql, parameters), statement =>
            {
                var row = statement.Current;
                startPos = row.GetInt64(0);
                endPos = row.GetInt64(1);
                totalTicks = row.GetInt64(2);
                return true;
            });

            return (startPos, endPos, totalTicks);
        }

        public void UpdatePlaybackState(string userId, string itemId, long startTicks, long endTicks, long totalTicks)
        {
            var tableName = getUserTableName(userId);

            string sql =
                $"UPDATE {tableName} " +
                "SET " +
                "  StartTickPos=@StartTickPos " +
                ", EndTickPos=@EndTickPos " +
                ", TotalTicks=TotalTicks+@TotalTicks " +
                "WHERE ItemId=@ItemId"
                ;
            var parameters = new List<(string, object?)>() {
                ("@StartTickPos", startTicks ),
                ("@EndTickPos", endTicks ),
                ("@TotalTicks", totalTicks ),
                ("@ItemId", itemId) };

            _dbHelper.ExecuteCommand(new SQLCmdDef(sql, parameters));
        }
    }
}
