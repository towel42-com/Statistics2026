using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Responses;
using Statistics2026.ScheduledTasks;
using Statistics2026.Utilities;
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
            StreamingProviders,
            NotifyAvailableToday,
            PeopleAnalysis
        }

        public async Task<(DateTimeOffset? when, long ms, List<string> reasons)> GetLastUpdated( EAction action )
        {
            string sql = "SELECT LastUpdated, RunTimeMS, Reason FROM \n" +
                "LastUpdateTable \n" +
                $"WHERE Action = '{action.ToString()}'\n"
                ;
            DateTimeOffset? lastUpdated = DateTimeOffset.MinValue;
            long ms = 0;
            List<string> reasons = [];
            await ExecuteCommand( new SQLCmdDef( sql ), statement =>
            {
                IResultSet row = statement.Current;
                int count = 0;
                lastUpdated = HelperFuncs.ReadDateTime( row.GetString( count++ ) );
                ms = row.GetInt64( count++ );
                reasons = HelperFuncs.JsonToStringArray( row.GetString( count++ ) );
                if( reasons.IsNullOrEmpty() )
                    reasons = [ row.GetString( count - 1 ) ];
                return Task.FromResult( false );
            } );
            return (lastUpdated, ms, reasons);
        }

        public async Task<HtmlJsonResponse> GetSystemStatus()
        {
            string version = Plugin.Instance?.Configuration.Version ?? String.Empty;
            string buildDate = Plugin.Instance?.Configuration.BuildDate ?? String.Empty;
            string dbState = Plugin.Instance?.DBStateString() ?? String.Empty;

            int depth = 0;

            string systemHtml = HelperFuncs.addToHtml( depth++, "<ul>" );
            systemHtml += HelperFuncs.addToHtml( depth, $"<li>Version: {version}</li>" );
            systemHtml += HelperFuncs.addToHtml( depth, $"<li>Build Date: {buildDate}</li>" );
            systemHtml += HelperFuncs.addToHtml( depth, $"<li>Database Status: {dbState}</li>" );
            systemHtml += HelperFuncs.addToHtml( --depth, "</ul>" );

            HtmlJsonResponse retVal = await GetLastRunHtml( "All" );

            retVal.html = systemHtml + retVal.html;

            EDBState status = Plugin.Instance?.CurrDBState() ?? EDBState.eEmpty;
            List<(string key, string value)> dbStates = [];
            foreach( EDBState curr in new[] { EDBState.eSystemTablesCreated, EDBState.eUserTablesCreated, EDBState.eSystemDataInitialized, EDBState.eUserDataInitialized } )
            {
                string currState = curr.ToString();
                currState = Regex.Replace( currState, "^e", "" ) + ":";
                string value = ( ( curr & status ) != 0 ).ToString();
                dbStates.Add( (currState, $"<b>{value}</b>") );
            }

            string dbStateHtml = HelperFuncs.MakeTableHtml( "Database State:", dbStates );
            retVal.html += dbStateHtml;

            return retVal;
        }

        List<EAction> ActionsFromStringList( string whichRuns )
        {
            string[] list = whichRuns.Split( ',' );
            if( list.Length == 1 && list[ 0 ].ToLower() == "all" )
            {
                list = new[]
                    {
                        EAction.UserAnalysis.ToString(),
                        EAction.MediaAnalysis.ToString(),
                        EAction.PeopleAnalysis.ToString(),
                        EAction.UserWatchDataAnalysis.ToString(),
                        EAction.CollectionsAnalysis.ToString(),
                        EAction.SeriesAnalysis.ToString(),
                        EAction.StreamingProviders.ToString(),
                        EAction.MissingEpisodesAnalysis.ToString(),
                        EAction.NotifyAvailableToday.ToString(),
                        EAction.MissingMoviesAnalysis.ToString(),
                        EAction.LastMediaOrUserAdded.ToString(),
                        EAction.System.ToString(),
                    };
            }
            List<EAction> retVal = [];
            foreach( string? whichRun in list )
            {
                if( Enum.TryParse( whichRun, true, out Statistics2026DB.EAction whichRunEnum ) )
                {
                    retVal.Add( whichRunEnum );
                }
                else
                    throw new Exception( $"Unknown run type <b>{whichRun}</b>" );
            }

            return retVal;
        }

        public List<string> TasksRunning( string whichRuns )
        {
            List<EAction> actions = ActionsFromStringList( whichRuns );
            return TasksRunning( actions );
        }

        public List<string> TasksRunning( List<EAction> whichRuns )
        {
            List<string> retVal = [];
            foreach( EAction curr in whichRuns )
            {
                if( TaskDef.IsTaskRunning( _embyInterfaces, TypeForAction( curr ) ) )
                {
                    retVal.Add( TitleForAction( curr ) );
                }
            }
            return retVal;
        }

        public async Task<HtmlJsonResponse> GetLastRunHtml( string whichRuns ) // whichRuns comma separated list
        {
            List<string> results = [];
            List<string> errors = [];

            try
            {
                List<EAction> actions = ActionsFromStringList( whichRuns );
                foreach( EAction whichRun in actions )
                {
                    string curr = await GetLastRunInfo( whichRun );
                    results.Add( curr );
                }
            }
            catch( Exception ex )
            {
                errors.Add( ex.ToString() );
            }

            string resultText = string.Empty;
            int depth = 0;

            resultText = HelperFuncs.addToHtml( depth++, "<br>\n<div>" );
            resultText += HelperFuncs.addToHtml( depth, $"<h3>Last Analysis:</h3>" );

            resultText += HelperFuncs.addToHtml( depth++, "<ul class=\"dynamic-list\">" );
            resultText += string.Join( "\n", results );
            resultText += HelperFuncs.addToHtml( --depth, "</ul>" );
            resultText += HelperFuncs.addToHtml( --depth, "</div>" );

            string errorText = HelperFuncs.MakeTableHtml( "Error Getting Last Run Information:", errors );
            return new HtmlJsonResponse() { html = resultText, error = errorText };
        }

        private System.Type? TypeForAction( EAction action )
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
                case EAction.NotifyAvailableToday:
                    retVal = null;
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
                case EAction.PeopleAnalysis:
                    retVal = typeof( AnalyzePeopleTask );
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

        private string TitleForAction( EAction action )
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
                case EAction.PeopleAnalysis:
                    retVal = "People Analysis";
                    break;
                case EAction.UserWatchDataAnalysis:
                    retVal = "User Watch Data Analysis";
                    break;
                case EAction.StreamingProviders:
                    retVal = "Streaming Provider Analysis";
                    break;
                case EAction.NotifyAvailableToday:
                    retVal = "Notify Episodes are Available Today";
                    break;

            }
            return retVal;
        }

        private async Task<string> GetLastRunInfo( EAction action )
        {
            CheckIsValid( ECheckLevel.eReport );

            (DateTimeOffset? lastUpdated, long runtimeMS, List<string>? reason) = await GetLastUpdated( action );
            string updateText = string.Empty;
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

            string titleText = TitleForAction( action );
            var rt = RunTime.CreateFromMS( runtimeMS );
            int depth = 2; // div and ul
            string retVal = string.Empty;

            retVal += HelperFuncs.addToHtml( depth++, "<li>" );
            retVal += HelperFuncs.addToHtml( depth++, "<div class=\"row\">" );
            retVal += HelperFuncs.addToHtml( depth, $"<span class=\"label\">{titleText}:</span>" );

            string data = updateText;
            if( Plugin.Instance!.Configuration.ShowDebugInfo( _embyInterfaces ) )
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

        public Task ClearLastUpdated( EAction action )
        {
            CheckIsValid( ECheckLevel.eUpdate );

            DateTimeOffset buildDate = BuildDateInfo.GetBuildDate();
            string version = Plugin.Instance!.Configuration.Version;

            string sql = "UPDATE \n" +
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

            return ExecuteCommands( sqlCmds );
        }

        public Task SetLastUpdated( EAction action, List<string> reasons, long runtimeMS )
        {
            return SetLastUpdated( action, reasons, runtimeMS, DateTimeOffset.UtcNow );
        }

        public Task SetLastUpdated( EAction action, List<string> reasons, long runtimeMS, DateTimeOffset updateTime )
        {
            CheckIsValid( ECheckLevel.eUpdate );

            DateTimeOffset buildDate = BuildDateInfo.GetBuildDate();
            string version = Plugin.Instance!.Configuration.Version;

            string sql = "INSERT INTO \n" +
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

            return ExecuteCommands( sqlCmds );
        }
    }
}
