using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using ServiceStack;
using Statistics2026.Api;
using Statistics2026.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        public async Task AnalyzeMissingEpisodesTaskImplAsync()
        {
            CheckIsValid( ECheckLevel.eUpdate );

            Logger().Debug( $"Starting Analysis" );
            Progress?.Report( 0 );
            var seriesList = EmbyHelperFuncs.GetAllSeries( null, LibraryManager(), true );
            //Dictionary<string, List<(Series series, int episodeCount)>> seriesToEpisodeMap = [];
            //foreach( var series in seriesList )
            //{
            //    var currValue = (series, EmbyHelperFuncs.GetAllEpisodesForSeries( null, LibraryManager(), series ).Count());

            //    var key = series.GetProviderId( MetadataProviders.Tmdb );
            //    if( key.IsEmpty() )
            //    {

            //        key = $"{series.SortName}";
            //        if (series.PremiereDate != null )
            //            key += $"_{series.PremiereDate}";
            //    }

            //    if( seriesToEpisodeMap.TryGetValue( key, out var curr ) )
            //    {
            //        curr.Add( currValue );
            //    }
            //    else
            //    {
            //        curr = new List<(Series series, int episodeCount)>() { currValue };
            //        seriesToEpisodeMap.Add( key, curr );
            //    }
            //}
            //seriesToEpisodeMap = seriesToEpisodeMap.Where( kvp => kvp.Value.Count() >= 2 ).ToDictionary( kvp => kvp.Key, kvp => kvp.Value );

            seriesList.Sort( ( a, b ) => StringComparer.OrdinalIgnoreCase.Compare( a.SortName, b.SortName ) );
            Progress?.Report( 100 );

            var reader = new TmdbInterface( _embyInterfaces, CancellationToken );

            double count = seriesList.Count();
            var completedCount = 0;

            List<SQLCmdDef> sqlCmds = [];
            ConcurrentBag<object> validIds = [];

            Progress?.Report( 0 );
            using( var timer = new AutoTimer( $"    Analyze Missing Episodes - Getting Commands", Logger() ) )
            {
                using var gatekeeper = HelperFuncs.CreateSemaphoreSlim();

                var pool = seriesList.Select( ( series, index ) => Task.Run( async () =>
                {
                    await gatekeeper.WaitAsync();
                    try
                    {
                        var retVal = (index: index, series: series, cmds: new List<SQLCmdDef>());
                        if( series == null )
                            return retVal;

                        retVal.cmds = await AnalyzeMissingEpisodesAsync( reader, series ).ConfigureAwait( false );
                        CancellationToken?.ThrowIfCancellationRequested();
                        validIds.Add( series.Id.ToString() );


                        var currProgress = Interlocked.Increment( ref completedCount );
                        Progress?.Report( 80.0 * ( 1.0 * currProgress ) / count );

                        Logger().Info( $"    Processed Series ({currProgress} of {count}) - {series.Name}" );
                        return retVal;
                    }
                    finally
                    {
                        gatekeeper.Release();
                    }
                } ) ).ToList();

                var unsortedResults = await Task.WhenAll( pool );
                sqlCmds.Add( CreateSQLCmdDef( "DELETE FROM Missing WHERE IsEpisode AND ParentId IN (<KEYLIST>)", "<KEYLIST>", validIds.ToList() ) );
                var results = unsortedResults.OrderBy( item => item.index ).ToList();
                foreach( var result in results )
                {
                    sqlCmds.AddRange( result.cmds );
                    CancellationToken?.ThrowIfCancellationRequested();
                }
            }
            using( var timer = new AutoTimer( $"    Analyze Missing Episodes - Executing Commands", Logger() ) )
            {
                Progress?.Report( 80 );
                ExecuteCommands( sqlCmds );
                Progress?.Report( 100 );
                Logger().Debug( $"Finished Missing Episodes Analysis" );
            }
        }

        bool InvalidDateForMissingAnalysis( DateTimeOffset? dto, bool ignoreConfig = false )
        {
            if( dto == null )
                return true;

            var numDaysToAdd = ignoreConfig ? 0 : Plugin.Instance!.Configuration.numDaysFutureForMissing;
            var maxDate = DateTimeOffset.Now.Date.AddDays( numDaysToAdd );
            if( dto.Value.Date > maxDate )
                return true;

            if( dto == DateTimeOffset.MinValue )
                return true;

            return false;
        }

        private async Task<List<SQLCmdDef>> AnalyzeMissingEpisodesAsync( TmdbInterface reader, Series series )
        {
            Logger().Info( $"     Analyzing Series '{series.Name}' - checking for missing episodes" );

            var seriesTmbdId = series.GetProviderId( MetadataProviders.Tmdb );
            if( string.IsNullOrEmpty( seriesTmbdId ) )
                return [];

            List<SQLCmdDef> retVal = [];

            TmdbSeries? tmdbSeries = null;
            AddStreamingProvidersInfo? streamingProviders = null;
            try
            {
                tmdbSeries = await reader.GetRemoteSeriesAsync( seriesTmbdId ).ConfigureAwait( false );
                streamingProviders = await reader.GetStreamingProvidersAsync( Logger(), series ).ConfigureAwait( false );
                retVal.AddRange( streamingProviders?.addProviderCmds ?? [] );
            }
            catch( Exception ex )
            {
                Logger().Error( $"Problem connecting to TMDB - {series.Name} - {seriesTmbdId}" );
                Logger().Error( ex.ToString() );
            }

            if( tmdbSeries == null )
            {
                Logger().Warn( $"Could not find TMDB series for {series.Name} - {seriesTmbdId}" );
                return [];
            }

            foreach( var tmdbSeason in tmdbSeries.Seasons )
            {
                if( tmdbSeason.SeasonNumber == 0 && !Plugin.Instance!.Configuration.reportOnMissingSpecials )
                    continue;

                var embySeason = EmbyHelperFuncs.GetSeasonFromSeries( series, tmdbSeason.SeasonNumber );

                foreach( var tmdbEpisode in tmdbSeason.Episodes )
                {
                    if( InvalidDateForMissingAnalysis( tmdbEpisode.AirDate() ) )
                        continue;

                    var episodeTmdbId = tmdbEpisode.Id;
                    var episodeIdent = $"S{tmdbSeason.SeasonNumber:D2}E{tmdbEpisode.EpisodeNumber:D2}";
                    Logger().Debug( $"         Checking for episode {episodeIdent} for {series.Name} on server" );
                    var embyEpisode = ( embySeason != null ) ? EmbyHelperFuncs.GetEpisodeFromTmdbId( LibraryManager(), embySeason, episodeTmdbId, tmdbEpisode.EpisodeNumber ) : null;

                    if( embyEpisode == null )
                    {
                        Logger().Info( $"             {series.Name} - {episodeIdent} is missing - Series ID: {series.Id.ToString()}" );

                        var key = $"{tmdbSeries.Id}-{tmdbSeason.Id}-{tmdbEpisode.Id}";

                        var title = $"{tmdbSeries.Name} - S{tmdbSeason.SeasonNumber:D2}E{tmdbEpisode.EpisodeNumber:D2} - {tmdbEpisode.Name}";
                        retVal.Add( new SQLCmdDef( kSQLAddToMissing,
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

            return retVal;
        }
    }
}
