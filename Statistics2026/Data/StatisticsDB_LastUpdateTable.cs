using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public void UpdateLastUpdated(DateTime lastUpdate, DateTime buildDate, string version)
        {
            CheckIsValid();

            var sqlCmds = new List<SQLCmdDef>();
            sqlCmds.Add(new SQLCmdDef("delete from LastUpdateTable"));
            sqlCmds.Add(new SQLCmdDef("INSERT INTO LastUpdateTable (LastUpdated, BuildDate, Version) values (@LastUpdated, @BuildDate, @Version)",
                        new List<(string name, object? value)>()
                        {
                            ("@LastUpdated", _dbHelper.ToDateTimeParamValue(lastUpdate)),
                            ("@BuildDate", _dbHelper.ToDateTimeParamValue(buildDate)),
                            ("@Version", version)
                        }));

            _dbHelper.ExecuteCommands(sqlCmds);
        }
    }
}
