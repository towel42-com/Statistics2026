using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Services;
using ServiceStack;
using Statistics2026.Api;
using Statistics2026.ScheduledTasks;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

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
            List<(string key, string value)> dbStates = [];
            foreach( var curr in new[] { EDBState.eSystemTablesCreated, EDBState.eUserTablesCreated, EDBState.eSystemDataInitialized, EDBState.eUserDataInitialized } )
            {
                var currState = curr.ToString();
                currState = Regex.Replace( currState, "^e", "" ) + ":";
                if( ( curr & status ) != 0 )
                {
                    dbStates.Add( (currState, "<b>true</b>") );
                }
                else
                {
                    dbStates.Add( (currState, "<b>false</b>") );
                }
            }

            var dbStateHtml = HelperFuncs.MakeTableHtml( "Database State:", dbStates );
            retVal.html += dbStateHtml;

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
            var resultText = string.Empty;
            var depth = 0;

            resultText = HelperFuncs.addToHtml( depth++, "<br>\n<div>" );
            resultText += HelperFuncs.addToHtml( depth, $"<h3>Last Analysis:</h3>" );

            resultText += HelperFuncs.addToHtml( depth++, "<ul class=\"dynamic-list\">" );
            resultText += string.Join( "\n", results );
            resultText += HelperFuncs.addToHtml( --depth, "</ul>" );
            resultText += HelperFuncs.addToHtml( --depth, "</div>" );

            var errorText = HelperFuncs.MakeTableHtml( "Error Getting Last Run Information:", errors );
            return new HtmlJsonResponse() { html = resultText, error = errorText };
        }

        System.Type? TypeForAction( EAction action )
        {
            System.Type? retVal = null;
            switch( action )
            {
                case EAction.System:
                    retVal = typeof( RunAllTasksTask );
                    break;
                case EAction.LastMediaOrUserAdded:
                    retVal = null;
                    break;
                case EAction.CollectionsAnalysis:
                    retVal = typeof( AnalyzeCollectionsTask );
                    break;
                case EAction.MediaAnalysis:
                    retVal = typeof( AnalyzeMediaTask );
                    break;
                case EAction.MissingEpisodesAnalysis:
                    retVal = typeof( AnalyzeMissingEpisodesTask );
                    break;
                case EAction.MissingMoviesAnalysis:
                    retVal = typeof( AnalyzeMissingMoviesTask );
                    break;
                case EAction.SeriesAnalysis:
                    retVal = typeof( AnalyzeSeriesTask );
                    break;
                case EAction.UserAnalysis:
                    retVal = typeof( AnalyzeUsersTask );
                    break;
                case EAction.UserWatchDataAnalysis:
                    retVal = typeof( AnalyzeWatchedMediaDataTask );
                    break;
                case EAction.StreamingProviders:
                    retVal = typeof( AnalyzeStreamingProvidersTask );
                    break;
            }
            return retVal;
        }

        string TitleForAction( EAction action )
        {
            string retVal = string.Empty;
            switch( action )
            {
                case EAction.System:
                    retVal = "System Analysis";
                    break;
                case EAction.LastMediaOrUserAdded:
                    retVal = "Last Media or User Added";
                    break;
                case EAction.CollectionsAnalysis:
                    retVal = "Collection Analysis";
                    break;
                case EAction.MediaAnalysis:
                    retVal = "Media Analysis";
                    break;
                case EAction.MissingEpisodesAnalysis:
                    retVal = "Missing Episode Analysis";
                    break;
                case EAction.MissingMoviesAnalysis:
                    retVal = "Missing Movie Analysis";
                    break;
                case EAction.SeriesAnalysis:
                    retVal = "Series Analysis";
                    break;
                case EAction.UserAnalysis:
                    retVal = "User Analysis";
                    break;
                case EAction.UserWatchDataAnalysis:
                    retVal = "User Watch Data Analysis";
                    break;
                case EAction.StreamingProviders:
                    retVal = "Streaming Provider Analysis";
                    break;
            }
            return retVal;
        }

        public string GetLastRunInfo( EAction action )
        {
            CheckIsValid( ECheckLevel.eReport );

            var (lastUpdated, runtimeMS, reason) = GetLastUpdated( action );
            var updateText = string.Empty;
            bool run = ( lastUpdated != null && lastUpdated.HasValue && lastUpdated.Value != DateTimeOffset.MinValue );
            if( run )
            {
                updateText = $"Finished at <b>{lastUpdated!.Value.ToTZShortString( true )}</b>";
            }
            else if( TaskDef.IsTaskRunning( _embyInterfaces, TypeForAction( action ) ) )
            {
                updateText = "<b>Currently Running</b>";
            }
            else
            {
                updateText = "<b>Not Run</b>";
            }

            var titleText = TitleForAction( action );
            var rt = RunTime.CreateFromMS( runtimeMS );
            int depth = 2; // div and ul
            var retVal = string.Empty;

            retVal += HelperFuncs.addToHtml( depth++, "<li>" );
            retVal += HelperFuncs.addToHtml( depth++, "<div class=\"row\">" );
            retVal += HelperFuncs.addToHtml( depth, $"<span class=\"label\">{titleText}:</span>" );

            var data = updateText;
            if( Plugin.Instance!.Configuration.showDebugInfo )
            {
                if( run )
                {
                    data += $" taking <b>{rt.ToShortString( false )}ms</b>";
                    if( reason.Count > 0 )
                    {
                        data += "<div>Reasons for Run:</b></div>";
                        data += HelperFuncs.MakeTableHtml( reason, true, 0 );
                    }
                }
            }

            retVal += HelperFuncs.addToHtml( depth, $"<span class=\"value\">{data}</span>" );
            retVal += HelperFuncs.addToHtml( --depth, "</div>" );
            retVal += HelperFuncs.addToHtml( --depth, "</li>" );
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

        public void SetLastUpdated( EAction action, List<string> reasons, long runtimeMS )
        {
            SetLastUpdated( action, reasons, runtimeMS, DateTimeOffset.UtcNow );
        }

        public void SetLastUpdated( EAction action, List<string> reasons, long runtimeMS, DateTimeOffset updateTime )
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
