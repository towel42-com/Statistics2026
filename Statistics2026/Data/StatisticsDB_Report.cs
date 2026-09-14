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
    using WatchedMediaValueItemData = (string id, string name, long playCount, long denominator, double playCountPerUser);

    public sealed partial class StatisticsDB
    {
        private string GetSingleValueFromSQL(string sql, List<(string name, object? value)>? parameters = null, Func<long, string>? formatter = null)
        {
            CheckIsValid();

            var cmd = new SQLCmdDef(sql, parameters);

            var retVal = String.Empty;
            _dbHelper.ExecuteCommand(cmd, statement =>
                {
                    var row = statement.Current;
                    var count = row.GetInt64(0);
                    retVal = formatter?.Invoke(count) ?? count.ToString();
                    return false;
                });
            return retVal;
        }

        private TextBasedStatCard ValueGroupForSingleItem(string title, string? help, string sql, List<(string name, object? value)>? parameters = null, Func<long, string>? formatter = null)
        {
            CheckIsValid();

            var retVal = new TextBasedStatCard(title, help, EStatCardSize.eSmall);
            var value = GetSingleValueFromSQL(sql, parameters, formatter);
            retVal.AddLine(value);
            return retVal;
        }

        private TextBasedStatCard ValueGroupForSingleValue(string title, string? help, Object value)
        {
            CheckIsValid();

            var retVal = new TextBasedStatCard(title, help, EStatCardSize.eSmall);
            retVal.AddLine(value.ToString());
            return retVal;
        }

        public StatCard StatisticFor(User? user, StatGen.EStatisticType whichStatistic, StatGen.EVideoType videoType)
        {
            CheckIsValid();

            var statGen = new StatGen(whichStatistic, videoType, _dbHelper);
            return statGen.GetStatCard();
        }

        public StatGen.StatCardValues StatCardValuesFor(User? user, StatGen.EStatisticType whichStatistic, StatGen.EVideoType videoType)
        {
            CheckIsValid();

            var statGen = new StatGen(whichStatistic, videoType, _dbHelper);
            return statGen.GetStatCardValues();
        }
    }
}
