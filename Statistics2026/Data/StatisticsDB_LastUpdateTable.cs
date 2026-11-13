using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Services;
using ServiceStack;
using Statistics2026.Api;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
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
            UserWatchDataAnalysis,
            LastMediaOrUserAdded,
            StreamingProviders
        }

        public (DateTimeOffset? when, long ms, List<string> reasons) GetLastUpdated( EAction action )
        {
            var sql = "SELECT LastUpdated, RunTimeMS, Reason FROM \n" +
                "LastUpdateTable \n" +
                $"WHERE Action = '{action.ToString()}'\n"
                ;
            DateTimeOffset? lastUpdated = DateTimeOffset.MinValue;
            long ms = 0;
            List<string> reasons = [];
            ExecuteCommand( new SQLCmdDef( sql ), statement =>
            {
                var row = statement.Current;
                var count = 0;
                lastUpdated = HelperFuncs.ReadDateTime( row.GetString( count++ ) );
                ms = row.GetInt64( count++ );
                reasons = HelperFuncs.JsonToStringArray( row.GetString( count++ ) );
                if( reasons.IsNullOrEmpty() )
                    reasons = [ row.GetString( count - 1 ) ];
                return false;
            } );
            return (lastUpdated, ms, reasons);
        }

        public string GetLastRunInfo( string actionString, ref bool ok )
        {
            if( Enum.TryParse( actionString, true, out Statistics2026DB.EAction whichRunEnum ) )
            {
                ok = true;
                return GetLastRunInfo( whichRunEnum );
            }
            else
            {
                ok = false;
                return $"Unknown run type <b>{actionString}</b>";
            }
        }


        public HtmlJsonResponse GetSystemStatus()
        {
            var retVal = GetLastRunHtml( "All" );

            var version = Plugin.Instance?.Configuration.Version ?? String.Empty;
            var buildDate = Plugin.Instance?.Configuration.BuildDate ?? String.Empty;

            var depth = 0;

            var systemHtml = HelperFuncs.addToHtml( depth++, "<ul>" );
            systemHtml += HelperFuncs.addToHtml( depth, $"<li>Version: {version}</li>" );
            systemHtml += HelperFuncs.addToHtml( depth, $"<li>Build Date: {buildDate}</li>" );
            systemHtml += HelperFuncs.addToHtml( --depth, "</ul>" );

            retVal.html = systemHtml + retVal.html;

            var status = Plugin.Instance?.CurrDBState() ?? EDBState.eEmpty;
            List<string> dbStates = [];
            foreach( var curr in new[] { EDBState.eSystemTablesCreated, EDBState.eUserTablesCreated, EDBState.eSystemDataInitialized, EDBState.eUserDataInitialized } )
            {
                var currState = curr.ToString();
                currState = Regex.Replace( currState, "^e", "" );
                if( ( curr & status ) != 0 )
                {
                    dbStates.Add( currState + " = <b>true</b>" );
                }
                else
                {
                    dbStates.Add( currState + " = <b>false</b>" );
                }
            }

            retVal.html += HelperFuncs.MakeTableHtml( "Database State:", dbStates );

            retVal.html = retVal.html.Replace( "</br>", "" );
            return retVal;
        }

        public HtmlJsonResponse GetLastRunHtml( string whichRuns ) // whichRuns comma separated list
        {
            var list = whichRuns.Split( ',' );
            List<string> results = [];
            List<string> errors = [];

            if( list.Length == 1 && list[ 0 ] == "All" )
            {
                list = new[]
                    {
                        EAction.UserAnalysis.ToString(),
                        EAction.MediaAnalysis.ToString(),
                        EAction.UserWatchDataAnalysis.ToString(),
                        EAction.CollectionsAnalysis.ToString(),
                        EAction.SeriesAnalysis.ToString(),
                        EAction.StreamingProviders.ToString(),
                        EAction.MissingEpisodesAnalysis.ToString(),
                        EAction.MissingMoviesAnalysis.ToString(),
                        EAction.LastMediaOrUserAdded.ToString(),
                        EAction.System.ToString(),
                    };
            }

            foreach( var whichRun in list )
            {
                bool ok = false;
                var currData = GetLastRunInfo( whichRun, ref ok );
                if( ok )
                    results.Add( currData );
                else
                    errors.Add( currData );
            }
            var resultText = HelperFuncs.MakeTableHtml( "Analsys Last Run:", results );

            var errorText = HelperFuncs.MakeTableHtml( "Error Getting Last Run Information:", errors );
            return new HtmlJsonResponse() { html = resultText, error = errorText };
        }

        public string GetLastRunInfo( EAction action )
        {
            CheckIsValid( ECheckLevel.eReport );

            var (lastUpdated, runtimeMS, reason) = GetLastUpdated( action );
            const string notRun = "Not Run";
            var lastUpdateText = notRun;
            if( lastUpdated != null && lastUpdated.HasValue && lastUpdated.Value != DateTimeOffset.MinValue )
            {
                lastUpdateText = lastUpdated.Value.ToTZShortString( true );
            }

            var titleText = string.Empty;
            switch( action )
            {
                case EAction.System:
                    titleText = "System Analysis";
                    break;
                case EAction.LastMediaOrUserAdded:
                    titleText = "Last Media or User Added";
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

            var rt = RunTime.CreateFromMS( runtimeMS );
            var retVal = $"Last {titleText} finished at <b>{lastUpdateText}</b>";

            if( Plugin.Instance!.Configuration.showDebugInfo )
            {
                retVal += $" taking <b>{rt.ToShortString( false )}</b>";
                if( reason.Count > 0 )
                {
                    var reasonHtml = HelperFuncs.MakeTableHtml( reason, true, 0 );
                    retVal += reasonHtml;
                }
            }
            return retVal;
        }

        public void ClearLastUpdated( EAction action )
        {
            CheckIsValid( ECheckLevel.eUpdate );

            var buildDate = BuildDateInfo.GetBuildDate();
            var version = Plugin.Instance!.Configuration.Version;

            var sql = "UPDATE \n" +
                "LastUpdateTable \n" +
                "SET \n" +
                "  LastUpdated = @LastUpdated\n" +
                ", RunTimeMS = @RunTimeMS\n" +
                ", Reason = @Reason\n" +
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
                            ("@Reason", null),
                            ("@BuildDate", null),
                            ("@Version", null)
                        ] )
            ];

            ExecuteCommands( sqlCmds );
        }

        public void UpdateLastUpdated( EAction action, List<string> reasons, long runtimeMS )
        {
            UpdateLastUpdated( action, reasons, runtimeMS, DateTimeOffset.UtcNow );
        }

        public void UpdateLastUpdated( EAction action, List<string> reasons, long runtimeMS, DateTimeOffset updateTime )
        {
            CheckIsValid( ECheckLevel.eUpdate );

            var buildDate = BuildDateInfo.GetBuildDate();
            var version = Plugin.Instance!.Configuration.Version;

            var sql = "INSERT INTO \n" +
                "LastUpdateTable \n" +
                "(\n" +
                "  Action\n" +
                ", LastUpdated\n" +
                ", Reason\n" +
                ", RunTimeMS\n" +
                ", BuildDate\n" +
                ", Version\n" +
                ")\n" +
                " VALUES \n" +
                "(\n" +
                "  @Action\n" +
                ", @LastUpdated\n" +
                ", @Reason\n" +
                ", @RunTimeMS\n" +
                ", @BuildDate\n" +
                ", @Version\n" +
                ")\n" +
                "ON CONFLICT(Action) " +
                "DO UPDATE " +
                "SET \n" +
                "  LastUpdated = @LastUpdated\n" +
                ", Reason = @Reason\n" +
                ", RunTimeMS = @RunTimeMS\n" +
                ", BuildDate = @BuildDate\n" +
                ", Version = @Version\n"
                ;

            List<SQLCmdDef> sqlCmds =
            [
                new( sql,
                        [
                            ("@Action", action.ToString()),
                            ("@LastUpdated", HelperFuncs.ToDateTimeParamValue( updateTime )),
                            ("@Reason", JsonSerializer.Serialize( reasons )),
                            ("@RunTimeMS", runtimeMS),
                            ("@BuildDate", HelperFuncs.ToDateTimeParamValue( buildDate )),
                            ("@Version", version)
                        ] )
            ];

            ExecuteCommands( sqlCmds );
        }
    }
}
