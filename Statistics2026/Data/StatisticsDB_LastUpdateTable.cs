using Statistics2026.Api;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public enum EAction
        {
            System,
            CollectionsAnalysis,
            MediaAnalysis,
            MissingEpisodesAnalysis,
            MissingMoviesAnalysis,
            SeriesAnalysis,
            UserAnalysis,
            UserWatchDataAnalysis
        }

        public HtmlJsonResponse GetLastRunInfo( EAction action )
        {
            CheckIsValid( ECheckType.eReport );

            var sql = "SELECT LastUpdated FROM \n" +
                "LastUpdateTable \n" +
                $"WHERE Action = '{action.ToString()}'\n"
                ;

            var cmd = new SQLCmdDef( sql );
                //,
                //[
                //    ("@Action", action.ToString())
                //] );

            const string notRun = "Not Run";
            DateTime? lastUpdated = DateTime.MinValue;
            _dbHelper.ExecuteCommand( new SQLCmdDef( sql ), statement =>
            {
                var row = statement.Current;
                lastUpdated = DBHelper.ReadDateTime( row.GetString( 0 ) );
                return false;
            } );

            var lastUpdateText = notRun;
            if( lastUpdated.HasValue && lastUpdated.Value != DateTime.MinValue )
            {
                lastUpdateText = lastUpdated.Value.ToShortDateString() + " " + lastUpdated.Value.ToString( "HH:mm UTC" );
            }

            var titleText = string.Empty;
            switch( action )
            {
                case EAction.System:
                    titleText = "System Analysis";
                    break;
                case EAction.CollectionsAnalysis:
                    titleText = "Collection Analysis";
                    break;
                case EAction.MediaAnalysis:
                    titleText = "Media Analysis";
                    break;
                case EAction.MissingEpisodesAnalysis:
                    titleText = "Missing Episode Analysis";
                    break;
                case EAction.MissingMoviesAnalysis:
                    titleText = "Missing Movie Analysis";
                    break;
                case EAction.SeriesAnalysis:
                    titleText = "Series Analysis";
                    break;
                case EAction.UserAnalysis:
                    titleText = "User Analysis";
                    break;
                case EAction.UserWatchDataAnalysis:
                    titleText = "User Watch Data Analysis";
                    break;
            }
            var retVal = new HtmlJsonResponse
            {
                html = $"Last {titleText} finished at <b>{lastUpdateText}</b>"
            };

            return retVal;
        }

        public void ClearLastUpdated( EAction action )
        {
            CheckIsValid( ECheckType.eUpdate );

            var buildDate = BuildDateInfo.GetBuildDate();
            var version = Plugin.Instance!.Configuration.Version;

            var sql = "UPDATE \n" +
                "LastUpdateTable \n" +
                "SET \n" +
                "  LastUpdated = @LastUpdated\n" +
                ", RunTimeMS = @RunTimeMS\n" +
                ", BuildDate = @BuildDate\n" +
                ", Version = @Version\n" +
                "WHERE Action = @Action\n"
                ;

            List<SQLCmdDef> sqlCmds =
            [
                new( sql,
                        [
                            ("@Action", action.ToString()),
                            ("@LastUpdated", null),
                            ("@RunTimeMS", null),
                            ("@BuildDate", null),
                            ("@Version", null)
                        ] )
            ];

            _dbHelper.ExecuteCommands( sqlCmds );
        }

        public void UpdateLastUpdated( EAction action, long runtimeMS )
        {
            CheckIsValid( ECheckType.eUpdate );

            var buildDate = BuildDateInfo.GetBuildDate();
            var version = Plugin.Instance!.Configuration.Version;

            var sql = "INSERT INTO \n" +
                "LastUpdateTable \n" +
                "(\n" +
                "  Action\n" +
                ", LastUpdated\n" +
                ", RunTimeMS\n" +
                ", BuildDate\n" +
                ", Version\n" +
                ")\n" +
                " VALUES \n" +
                "(\n" +
                "  @Action\n" +
                ", @LastUpdated\n" +
                ", @RunTimeMS\n" +
                ", @BuildDate\n" +
                ", @Version\n" +
                ")\n" +
                "ON CONFLICT(Action) DO UPDATE SET \n" +
                "  LastUpdated = @LastUpdated\n" +
                ", RunTimeMS = @RunTimeMS\n" +
                ", BuildDate = @BuildDate\n" +
                ", Version = @Version\n"
                ;

            List<SQLCmdDef> sqlCmds =
            [
                new( sql,
                        [
                            ("@Action", action.ToString()),
                            ("@LastUpdated", _dbHelper.ToDateTimeParamValue( DateTime.UtcNow )),
                            ("@RunTimeMS", runtimeMS),
                            ("@BuildDate", _dbHelper.ToDateTimeParamValue( buildDate )),
                            ("@Version", version)
                        ] )
            ];

            _dbHelper.ExecuteCommands( sqlCmds );
        }
    }
}
