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
        public void LoadPlaybackState(ref PlaybackInfo info)
        {
            var tableName = getUserTableName(info.UserId);

            string sql =
                "SELECT " +
                "StartTickPos, " +
                "EndTickPos " +
                $"FROM {tableName} " +
                $"WHERE ItemId=@ItemId"
                ;
            var parameters = new List<(string, object?)>() { ("@ItemId", info.ItemId) };

            long? startPos = null;
            long? endPos = null;

            _dbHelper.ExecuteCommand(new SQLCmdDef(sql, parameters), statement =>
            {
                var row = statement.Current;
                startPos = row.GetInt64(0);
                endPos = row.GetInt64(1);
                return true;
            });

            info.StartPlaybackPositionTicks = startPos ?? long.MaxValue;
            info.EndPlaybackPositionTicks = endPos ?? long.MinValue;
        }

        public void UpdatePlaybackState(PlaybackInfo info)
        {
            var tableName = getUserTableName(info.UserId);

            string sql =
                $"UPDATE {tableName} " +
                "SET " +
                "StartTickPos=@StartTickPos, " +
                "EndTickPos=@EndTickPos " +
                "WHERE ItemId=@ItemId"
                ;
            var parameters = new List<(string, object?)>() {
                ("@StartTickPos", info.StartPlaybackPositionTicks ),
                ("@EndTickPos", info.EndPlaybackPositionTicks ),
                ("@ItemId", info.ItemId) };

            _dbHelper.ExecuteCommand(new SQLCmdDef(sql, parameters));
        }
    }
}
