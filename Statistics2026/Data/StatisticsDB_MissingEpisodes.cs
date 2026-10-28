using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using ServiceStack;
using Statistics2026.Api;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
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
        public async Task AnalyzeMissingEpisodesTaskImpl( CancellationToken cancellationToken, IProgress<double> progress )
        {
            CheckIsValid( ECheckType.eUpdate );

            _embyInterfaces!.Logger().Debug( $"Starting Analysis" );
            progress.Report( 0 );
            var allSeries = EmbyHelperFuncs.GetLibraryItems<Series>( null, LibraryManager() );
            allSeries.Sort( ( a, b ) => StringComparer.OrdinalIgnoreCase.Compare( a.SortName, b.SortName ) );
            progress.Report( 100 );

            var reader = new TmdbCollectionReader( _embyInterfaces, cancellationToken );

            double count = allSeries.Count();
            var curr = 0.0;

            progress.Report( 0 );
            List<SQLCmdDef> sqlCmds = [];

            List<object> paramList = [];
            foreach( var series in allSeries )
            {
                progress.Report( 80.0 * ( ++curr ) / count );
                try
                {
                    var cmds = await AnalyzeMissingEpisodes( reader, series, cancellationToken, progress );
                    paramList.Add( series.Id.ToString() );
                    sqlCmds.AddRange( cmds );
                }
                catch( Exception )
                {
                }
                cancellationToken.ThrowIfCancellationRequested();
                _embyInterfaces!.Logger().Info( $"    Processed Series ({curr} of {count}) - {series.Name}" );
            }

            sqlCmds.Insert( 0, DBHelper.CreateSQLCmdDef( "DELETE FROM Missing WHERE IsEpisode AND ParentId IN (<KEYLIST>)", "<KEYLIST>", paramList ) );

            cancellationToken.ThrowIfCancellationRequested();

            progress.Report( 80 );
            _dbHelper.ExecuteCommands( sqlCmds );
            progress.Report( 100 );
            _embyInterfaces!.Logger().Debug( $"Finished Analysis" );
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

        private async Task<List<SQLCmdDef>> AnalyzeMissingEpisodes( TmdbCollectionReader reader, Series series, CancellationToken cancellationToken, IProgress<double> progress )
        {
            _embyInterfaces!.Logger().Info( $"     Analyzing Series '{series.Name}' - checking for missing episodes" );

            var seriesTmbdId = series.GetProviderId( MetadataProviders.Tmdb );
            if( string.IsNullOrEmpty( seriesTmbdId ) )
                return [];

            TmdbSeries? tmdbSeries = null;
            try
            {
                tmdbSeries = await reader.GetRemoteSeriesAsync( seriesTmbdId ).ConfigureAwait( false );
            }
            catch( Exception ex )
            {
                _embyInterfaces.Logger().Error( $"Problem connecting to TMDB - {series.Name} - {seriesTmbdId}" );
                _embyInterfaces!.Logger().Error( ex.ToString() );
                throw ex;
            }

            if( tmdbSeries == null )
            {
                _embyInterfaces.Logger().Warn( $"Could not find TMDB series for {series.Name} - {seriesTmbdId}" );
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
                    _embyInterfaces!.Logger().Debug( $"         Checking for episode {episodeIdent} for {series.Name} on server" );
                    var embyEpisode = ( embySeason != null ) ? EmbyHelperFuncs.GetEpisodeFromTmdbId( LibraryManager(), embySeason, episodeTmdbId, tmdbEpisode.EpisodeNumber ) : null;

                    if( embyEpisode == null )
                    {
                        _embyInterfaces!.Logger().Info( $"             {series.Name} - {episodeIdent} is missing" );

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

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            return retVal;
        }
    }
}
