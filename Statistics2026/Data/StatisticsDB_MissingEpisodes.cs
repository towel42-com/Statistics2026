using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using ServiceStack;
using Statistics2026.Api;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public async Task AnalyzeMissingEpisodesTaskImplAsync()
        {
            var allSeries = EmbyHelperFuncs.GetAllSeries( null, LibraryManager() );

            List<object> paramList = [];
            foreach( var series in allSeries )
            {
                if( series == null )
                    continue;

                var seriesTmbdId = series.GetProviderId( MetadataProviders.Tmdb );
                if( string.IsNullOrEmpty( seriesTmbdId ) )
                    continue;

                paramList.Add( series.Id.ToString() );
            }

            //List<SQLCmdDef?> preCmds = new()
            //{
            //    SQLCmdDef.CreateSQLCmdDef( "DELETE FROM Missing WHERE IsEpisode AND TmdbId NOT IN (<KEYLIST>)", "<KEYLIST>", paramList )
            //};

            var reader = new TmdbInterface( EmbyInterfaces(), _dbHelper.CancellationToken );

            await AnalyzeData( "Missing Episode", "Missing Episodes",
                () =>
                {
                    var allSeries = EmbyHelperFuncs.GetAllSeries( null, LibraryManager() );
                    allSeries.Sort( ( a, b ) => StringComparer.OrdinalIgnoreCase.Compare( a.SortName, b.SortName ) );
                    return allSeries;
                },
                async series =>
                {
                    var cmds = await AnalyzeMissingEpisodesAsync( reader, series ).ConfigureAwait( false );
                    return cmds;
                },
                async series =>
                {
                    return series?.Name ?? string.Empty;
                },
                () => { },
                [],
                10
                );
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
            EmbyInterfaces().Logger().Info( $"     Analyzing Series '{series.Name}' - checking for missing episodes" );

            var seriesTmbdId = series.GetProviderId( MetadataProviders.Tmdb );
            if( string.IsNullOrEmpty( seriesTmbdId ) )
                return [];

            TmdbSeries? tmdbSeries = null;
            try
            {
                tmdbSeries = await reader.GetRemoteSeriesAsync( seriesTmbdId ).ConfigureAwait( false );
                if( tmdbSeries == null )
                {
                    EmbyInterfaces().Logger().Warn( $"Could not find TMDB series for {series.Name} - {seriesTmbdId}" );
                    return [];
                }
            }
            catch( Exception ex )
            {
                EmbyInterfaces().Logger().Error( $"Problem connecting to TMDB - {series.Name} - {seriesTmbdId}" );
                EmbyInterfaces().Logger().Error( ex.ToString() );
                return [];
            }

            List<SQLCmdDef> retVal = [];
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
                    EmbyInterfaces().Logger().Debug( $"         Checking for episode {episodeIdent} for {series.Name} on server" );
                    var embyEpisode = ( embySeason != null ) ? EmbyHelperFuncs.GetEpisodeFromTmdbId( LibraryManager(), embySeason, episodeTmdbId, tmdbEpisode.EpisodeNumber ) : null;

                    if( embyEpisode == null )
                    {
                        EmbyInterfaces().Logger().Info( $"             {series.Name} - {episodeIdent} is missing" );

                        var key = $"{tmdbSeries.Id}-{tmdbSeason.Id}-{tmdbEpisode.Id}";
                        //return $"{primaryName} - S{season:D2}E{episode:D2} - {secondaryName}";

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
                            ("@StudioNames", JsonSerializer.Serialize(series.Studios))
                        ] ) );
                    }

                    _dbHelper.CancellationToken?.ThrowIfCancellationRequested();
                }
            }

            return retVal;
        }
    }
}
