using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using ServiceStack;
using Statistics2026.Api;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public async Task AnalyzeMissingEpisodesTaskImpl( CancellationToken cancellationToken, IProgress<double> progress )
        {
            CheckIsValid( ECheckType.eUpdate );

            _embyInterfaces!._logger?.Debug( $"Starting Analysis" );
            progress.Report( 0 );
            var allSeries = EmbyHelperFuncs.GetLibraryItems<Series>( null, _embyInterfaces?._libraryManager );
            allSeries.Sort( ( a, b ) => StringComparer.OrdinalIgnoreCase.Compare( a.SortName, b.SortName ) );
            progress.Report( 100 );

            var reader = new TmdbCollectionReader( _embyInterfaces, cancellationToken );

            double count = allSeries.Count();
            var curr = 0.0;

            progress.Report( 0 );
            List<SQLCmdDef> sqlCmds = [];

            string paramString = string.Empty;
            List<object> paramList = [];
            foreach( var series in allSeries )
            {
                progress.Report( 80.0 * ( ++curr ) / count );
                try
                {
                    var cmds = await AnalyzeMissingEpisodes( reader, series, cancellationToken, progress );
                    foreach( var cmd in cmds )
                    {
                        var paramValue = cmd.GetParamValue( "@Key" );
                        if( paramValue != null )
                        {
                            paramList.Add( paramValue );
                        }
                    }
                    sqlCmds.AddRange( cmds );
                }
                catch( Exception )
                {
                }
                cancellationToken.ThrowIfCancellationRequested();
                _embyInterfaces!._logger?.Debug( $"    Processed Collection ({curr} of {count}) - {series.Name} items processed" );
            }

            sqlCmds.Insert( 0, DBHelper.CreateSQLCmdDef( "DELETE FROM Missing WHERE IsEpisode AND Key IN (<KEYLIST>)", "<KEYLIST>", paramList ) );

            cancellationToken.ThrowIfCancellationRequested();

            progress.Report( 80 );
            _dbHelper.ExecuteCommands( sqlCmds );
            progress.Report( 100 );
            _embyInterfaces!._logger?.Debug( $"Finished Analysis" );
        }

        private async Task<List<SQLCmdDef>> AnalyzeMissingEpisodes( TmdbCollectionReader reader, Series series, CancellationToken cancellationToken, IProgress<double> progress )
        {
            _embyInterfaces!._logger?.Info( $"     Analyzing Series '{series.Name}' - checking for missing episodes" );

            var seriesTmbdId = series.GetProviderId( MetadataProviders.Tmdb );
            if( string.IsNullOrEmpty( seriesTmbdId ) )
                return [];

            TmdbSeries? tmdbSeries = null;
            try
            {
                tmdbSeries = await reader.GetRemoteSeriesAsync( seriesTmbdId, cancellationToken ).ConfigureAwait( false );
            }
            catch( Exception ex )
            {
                _embyInterfaces._logger!.Error( $"Problem connecting to TMDB - {series.Name} - {seriesTmbdId}" );
                _embyInterfaces!._logger?.Error( ex.ToString() );
                throw ex;
            }

            if( tmdbSeries == null )
            {
                _embyInterfaces._logger!.Warn( $"Could not find TMDB series for {series.Name} - {seriesTmbdId}" );
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
                    _embyInterfaces!._logger?.Debug( $"         Checking for episode {episodeIdent} for {series.Name} on server" );
                    var embyEpisode = ( embySeason != null ) ? EmbyHelperFuncs.GetEpisodeFromTmdbId( _embyInterfaces._libraryManager, embySeason, episodeTmdbId, tmdbEpisode.EpisodeNumber ) : null;

                    if( embyEpisode == null )
                    {
                        _embyInterfaces!._logger?.Info( $"             {series.Name} - {episodeIdent} is missing" );

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
                            ("@EpisodeNum", tmdbEpisode.EpisodeNumber )
                        ] ) );
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            return retVal;
        }
    }
}
