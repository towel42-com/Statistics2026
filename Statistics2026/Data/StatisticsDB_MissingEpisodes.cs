using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using ServiceStack;
using Statistics2026.Notifications;
using Statistics2026.Utilities;
using System.Text.Json;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        public async Task AnalyzeMissingEpisodesTaskImplAsync()
        {
            CheckIsValid( ECheckLevel.eUpdate );

            Logger().Debug( $"Starting Analysis" );
            SetProgress( 0 );
            List<Series> seriesList = EmbyHelperFuncs.GetAllSeries( null, LibraryManager(), false );
            seriesList.Sort( ( a, b ) => StringComparer.OrdinalIgnoreCase.Compare( a.SortName, b.SortName ) );
            SetProgress( 100 );

            TmdbInterface reader = await TmdbInterface.CreateAsync( _embyInterfaces, CancellationToken );

            double count = seriesList.Count();
            int completedCount = 0;

            List<SQLCmdDef> sqlCmds = [];
            List<string> nowAvailable = [];

            SetProgress( 0 );
            await DeleteAllInvalid( seriesList.ConvertAll( item => (BaseItem)item ), "Missing", "ParentId", whereClauses: [ "IsEpisode" ] );

            using( var timer = new AutoTimer( $"    Analyze Missing Episodes - Getting Commands", Logger() ) )
            {
                using SemaphoreSlim gatekeeper = HelperFuncs.CreateSemaphoreSlim();

                var pool = seriesList.Select( ( series, index ) => Task.Run( async () =>
                {
                    await gatekeeper.WaitAsync();
                    try
                    {
                        (int index, Series item, List<SQLCmdDef> sqlCmds, List<string> nowAvailable) retVal = (index: index, item: series, sqlCmds: new List<SQLCmdDef>(), nowAvailable: new List<string>());
                        if( series == null )
                            return retVal;

                        (retVal.sqlCmds, retVal.nowAvailable) = await AnalyzeMissingEpisodesAsync( reader, series ).ConfigureAwait( false );

                        CancellationToken?.ThrowIfCancellationRequested();


                        int currProgress = Interlocked.Increment( ref completedCount );
                        SetProgress( 80.0 * ( 1.0 * currProgress ) / count );

                        Logger().Info( $"    Processed Series ({currProgress} of {count}) - {series.Name}" );
                        return retVal;
                    }
                    finally
                    {
                        gatekeeper.Release();
                    }
                } ) ).ToList();

                (int index, Series item, List<SQLCmdDef> sqlCmds, List<string> nowAvailable)[] unsortedResults = await Task.WhenAll( pool );
                sqlCmds = unsortedResults
                            .Where( item => item.sqlCmds != null )
                            .OrderBy( item => item.index )
                            .SelectMany( item => item.sqlCmds! )
                            .Where( cmd => cmd != null )
                            .Select( cmd => cmd! )
                            .ToList();

                nowAvailable = unsortedResults
                                .Where( item => item.nowAvailable != null )
                                .OrderBy( item => item.index )
                                .SelectMany( item => item.nowAvailable! )
                                .Where( cmd => cmd != null )
                                .Select( cmd => cmd! )
                                .ToList();

                CancellationToken?.ThrowIfCancellationRequested();
            }

            using( var timer = new AutoTimer( $"    Analyze Missing Episodes - Executing Commands", Logger() ) )
            {
                SetProgress( 80 );
                await ExecuteCommands( sqlCmds );
                SetProgress( 100 );
                Logger().Debug( $"Finished Missing Episodes Analysis" );
            }

            if( nowAvailable.Count() != 0 )
            {
                DateTimeOffset? lastUpdated = ( await GetLastUpdated( EAction.NotifyAvailableToday ) ).when;
                if( lastUpdated == null || lastUpdated.Value.LocalDateTime.Date != DateTimeOffset.Now.LocalDateTime.Date )
                {
                    NotificationSender.SendEpisodesAvailableNotification( nowAvailable, _embyInterfaces );
                }
            }
            await SetLastUpdated( EAction.NotifyAvailableToday, [ "Task" ], 0 );
        }

        private async Task<(List<SQLCmdDef> cmds, List<string> episodesNowAvailable)> AnalyzeMissingEpisodesAsync( TmdbInterface reader, Series series )
        {
            Logger().Info( $"     Analyzing Series '{series.Name}' - checking for missing episodes" );

            string seriesTmbdId = series.GetProviderId( MetadataProviders.Tmdb );
            if( string.IsNullOrEmpty( seriesTmbdId ) )
                return ([], []);

            List<SQLCmdDef> sqlCmds = [];
            List<string> episodesNowAvailable = [];

            TmdbSeries? tmdbSeries = null;
            AddStreamingProvidersInfo? streamingProviders = null;
            try
            {
                tmdbSeries = await reader.GetRemoteSeriesAsync( seriesTmbdId ).ConfigureAwait( false );
                streamingProviders = await reader.GetStreamingProvidersAsync( Logger(), series ).ConfigureAwait( false );
                sqlCmds.AddRange( streamingProviders?.addProviderCmds ?? [] );
            }
            catch( Exception ex )
            {
                Logger().Error( $"Problem connecting to TMDB - {series.Name} - {seriesTmbdId}" );
                Logger().Error( ex.ToString() );
            }

            if( tmdbSeries == null )
            {
                Logger().Warn( $"Could not find TMDB series for {series.Name} - {seriesTmbdId}" );
                return ([], []);
            }

            foreach( TmdbSeason tmdbSeason in tmdbSeries.Seasons )
            {
                if( tmdbSeason.SeasonNumber == 0 && !Plugin.Instance!.Configuration.reportOnMissingSpecials )
                    continue;

                Season? embySeason = EmbyHelperFuncs.GetSeasonFromSeries( series, tmdbSeason.SeasonNumber );

                foreach( TmdbEpisode tmdbEpisode in tmdbSeason.Episodes )
                {
                    if( HelperFuncs.InvalidDateForMissingAnalysis( tmdbEpisode.AirDate() ) )
                        continue;

                    string episodeTmdbId = tmdbEpisode.Id;
                    string episodeIdent = $"S{tmdbSeason.SeasonNumber:D2}E{tmdbEpisode.EpisodeNumber:D2}";
                    Logger().Debug( $"         Checking for episode {episodeIdent} for {series.Name} on server" );
                    Episode? embyEpisode = ( embySeason != null ) ? EmbyHelperFuncs.GetEpisodeFromTmdbId( LibraryManager(), embySeason, episodeTmdbId, tmdbEpisode.EpisodeNumber ) : null;

                    if( embyEpisode == null )
                    {
                        Logger().Info( $"             {series.Name} - {episodeIdent} is missing - Series ID: {series.Id.ToString()}" );

                        string key = $"{tmdbSeries.Id}-{tmdbSeason.Id}-{tmdbEpisode.Id}";

                        string title = $"{tmdbSeries.Name} - S{tmdbSeason.SeasonNumber:D2}E{tmdbEpisode.EpisodeNumber:D2} - {tmdbEpisode.Name}";
                        if( tmdbEpisode.AirDate()?.LocalDateTime.Date == DateTimeOffset.Now.Date )
                        {
                            episodesNowAvailable.Add( title );
                        }
                        sqlCmds.Add( new SQLCmdDef( kSQLAddToMissing,
                        [
                            ("@Key", key ),
                            ("@TmdbId", tmdbEpisode.Id ),
                            ("@Title", title ),
                            ("@OriginalTitle", string.Empty ),
                            ("@ReleaseDate", HelperFuncs.ToDateTimeParamValue( tmdbEpisode.AirDate() ) ),
                            ("@Overview", tmdbEpisode.Overview ),
                            ("@PosterPath", tmdbEpisode.StillPath ),
                            ("@ParentId", series.Id.ToString() ),
                            ("@IsEpisode", true ),
                            ("@SeasonNum", tmdbSeason.SeasonNumber ),
                            ("@EpisodeNum", tmdbEpisode.EpisodeNumber ),
                            ("@StudioNames", JsonSerializer.Serialize(series.Studios)),
                            ("@StreamingProviders", JsonSerializer.Serialize(streamingProviders?.allProviders ?? new object()))
                        ] ) );
                    }

                    CancellationToken?.ThrowIfCancellationRequested();
                }
            }

            return (sqlCmds, episodesNowAvailable);
        }
    }
}
