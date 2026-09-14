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
                "TotalTicksPlayed " +
                $"FROM {tableName} " +
                $"WHERE ItemId=@ItemId"
                ;
            var parameters = new List<(string, object?)>() { ("@ItemId", itemId) };

            long? totalTicksPlayed = null;

            _dbHelper.ExecuteCommand(new SQLCmdDef(sql, parameters), statement =>
            {
                var row = statement.Current;
                totalTicksPlayed = row.GetInt64(0);
                return true;
            });

            return totalTicksPlayed;
        }

        public void UpdateTotalTicksPlayed(string userId, string itemId, long totalTicksPlayed)
        {
            var tableName = getUserTableName(userId);

            string sql =
                $"UPDATE {tableName} " +
                "SET " +
                " TotalTicksPlayed=@TotalTicksPlayed " +
                "WHERE ItemId=@ItemId"
                ;
            var parameters = new List<(string, object?)>() {
                ("@TotalTicksPlayed", totalTicksPlayed ),
                ("@ItemId", itemId) };

            _dbHelper.ExecuteCommand(new SQLCmdDef(sql, parameters));
        }
    }
}
