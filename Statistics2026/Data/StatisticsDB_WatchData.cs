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
        public long? GetTotalTicksPlayed(string userId, string itemId)
        {
            var tableName = getUserTableName(userId);

            string sql =
                "SELECT " +
                "TotalTicks " +
                $"FROM {tableName} " +
                $"WHERE ItemId=@ItemId"
                ;
            var parameters = new List<(string, object?)>() { ("@ItemId", itemId) };

            long? totalTicks = null;

            _dbHelper.ExecuteCommand(new SQLCmdDef(sql, parameters), statement =>
            {
                var row = statement.Current;
                totalTicks = row.GetInt64(0);
                return true;
            });

            return totalTicks;
        }

        public void UpdateTotalTicksPlayed(string userId, string itemId, long totalTicks)
        {
            var tableName = getUserTableName(userId);

            string sql =
                $"UPDATE {tableName} " +
                "SET " +
                " TotalTicks=@TotalTicks " +
                "WHERE ItemId=@ItemId"
                ;
            var parameters = new List<(string, object?)>() {
                ("@TotalTicks", totalTicks ),
                ("@ItemId", itemId) };

            _dbHelper.ExecuteCommand(new SQLCmdDef(sql, parameters));
        }
    }
}
