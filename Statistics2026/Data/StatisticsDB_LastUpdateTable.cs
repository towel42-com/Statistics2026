using System;
using System.Collections.Generic;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public void UpdateLastUpdated( DateTime lastUpdate, DateTime buildDate, string version )
        {
            CheckIsValid( ECheckType.eUpdate );

            var sqlCmds = new List<SQLCmdDef>
            {
                new( "delete from LastUpdateTable" ),
                new( "INSERT INTO LastUpdateTable (LastUpdated, BuildDate, Version) values (@LastUpdated, @BuildDate, @Version)",
                        [
                            ("@LastUpdated", _dbHelper.ToDateTimeParamValue(lastUpdate)),
                            ("@BuildDate", _dbHelper.ToDateTimeParamValue(buildDate)),
                            ("@Version", version)
                        ] )
            };

            _dbHelper.ExecuteCommands( sqlCmds );
        }
    }
}
