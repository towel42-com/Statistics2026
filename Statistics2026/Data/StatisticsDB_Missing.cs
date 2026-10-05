using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using ServiceStack;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        private const string kSQLAddToMissing =
            "INSERT INTO Missing" +
            "(" +
                "  Key" +
                ", TmdbId" +
                ", Title" +
                ", OriginalTitle" +
                ", ReleaseDate" +
                ", Overview" +
                ", PosterPath" +
                ", ParentId" +
                ", IsEpisode" +
                ", SeasonNum" +
                ", EpisodeNum" +
            ")" +
            " VALUES " +
            "(" +
                "  @Key" +
                ", @TmdbId" +
                ", @Title" +
                ", @OriginalTitle" +
                ", @ReleaseDate" +
                ", @Overview" +
                ", @PosterPath" +
                ", @ParentId" +
                ", @IsEpisode" +
                ", @SeasonNum" +
                ", @EpisodeNum" +
            ")" +
            " ON CONFLICT(Key) " +
            " DO UPDATE " +
            " SET " +
                "  TmdbId=@TmdbId" +
                ", Title=@Title" +
                ", OriginalTitle=@OriginalTitle" +
                ", ReleaseDate=@ReleaseDate" +
                ", Overview=@Overview" +
                ", PosterPath=@PosterPath" +
                ", ParentId=@ParentId" +
                ", IsEpisode=@IsEpisode" +
                ", SeasonNum=@SeasonNum" +
                ", EpisodeNum=@EpisodeNum"
                ;

        public async Task AnalyzeMissingMoviesTaskImpl( CancellationToken cancellationToken, IProgress<double> progress )
        {
            CheckIsValid( ECheckType.eUpdate );

            _embyInterfaces!._logger?.Debug( $"AnalyzeMissingMovies - Starting Analysis" );
            progress.Report( 0 );
            var collections = _dbHelper.GetLibraryItems<BoxSet>().ToList();
            collections.Sort( ( a, b ) => StringComparer.OrdinalIgnoreCase.Compare( a.SortName, b.SortName ) );

            progress.Report( 100 );

            var reader = new TmdbCollectionReader( _embyInterfaces, cancellationToken );

            double count = collections.Count();
            var curr = 0.0;

            progress.Report( 0 );
            var sqlCmds = new List<SQLCmdDef>
            {
                new( "DELETE FROM Missing WHERE NOT IsEpisode" )
            };

            foreach( var collection in collections )
            {
                progress.Report( 80.0 * ( ++curr ) / count );
                var cmds = await AnalyzeMissingMoviesInCollection( reader, collection, cancellationToken, progress ).ConfigureAwait( false );
                sqlCmds.AddRange( cmds );
                cancellationToken.ThrowIfCancellationRequested();
                _embyInterfaces!._logger?.Debug( $"AnalyzeMissingMovies -     Processed Collection ({curr} of {count}) - {collection.Name} items processed" );
            }

            cancellationToken.ThrowIfCancellationRequested();

            progress.Report( 80 );
            _dbHelper.ExecuteCommands( sqlCmds );
            progress.Report( 100 );
            _embyInterfaces!._logger?.Debug( $"AnalyzeMissingMovies - Finished Analysis" );
        }

        private bool InvalidDate( DateTime? dt )
        {
            if( dt == null )
                return true;

            if( dt.Value.Date > DateTime.Today.Date )
                return true;

            if( dt == DateTime.MinValue )
                return true;

            return false;
        }

        private async Task<List<SQLCmdDef>> AnalyzeMissingMoviesInCollection( TmdbCollectionReader reader, BoxSet collection, CancellationToken cancellationToken, IProgress<double> progress )
        {
            _embyInterfaces!._logger?.Info( $"AnalyzeMissingMovies -     Analyzing Collection '{collection.Name}' - checking for missing movies" );

            var collectionTmbdId = collection.GetProviderId( MetadataProviders.Tmdb );
            if( string.IsNullOrEmpty( collectionTmbdId ) )
                return [];

            var tmdbCollection = await reader.GetRemoteCollectionMembersAsyncViaCustom( collectionTmbdId, cancellationToken ).ConfigureAwait( false );
            if( tmdbCollection == null )
            {
                _embyInterfaces._logger!.Warn( $"Could not find TMDB collection for {collection.Name} - {collectionTmbdId}" );
                return [];
            }

            //var tmdbCollectionProvider = await reader.GetRemoteCollectionMembersAsyncViaProviders( collectionTmbdId, collection.InternalId, cancellationToken ).ConfigureAwait( false );
            //if( tmdbCollectionProvider == null )
            //{
            //    _embyInterfaces._logger!.Warn( $"Could not find TMDB collection for {collection.Name} - {collectionTmbdId}" );
            //    return [];
            //}

            List<SQLCmdDef> retVal = [];
            foreach( var tmdbMovie in tmdbCollection.Movies )
            {
                if( InvalidDate( tmdbMovie.ReleaseDate() ) )
                    continue;

                var movieTmdbId = tmdbMovie.Id;
                _embyInterfaces!._logger?.Debug( $"AnalyzeMissingMovies -         Checking for movie {tmdbMovie.Title} for {collection.Name} on server" );
                var embyMovie = _dbHelper.GetMovieByTmdbId( _embyInterfaces._libraryManager, movieTmdbId );

                if( embyMovie == null )
                {
                    _embyInterfaces!._logger?.Info( $"AnalyzeMissingMovies -             {tmdbMovie.Title} is missing" );
                    var key = $"{tmdbCollection.Id}-{tmdbMovie.Id}";
                    retVal.Add( new SQLCmdDef( kSQLAddToMissing,
                    [
                        ("@Key", key ),
                        ("@TmdbId", tmdbMovie.Id ),
                        ("@Title", tmdbMovie.Title ),
                        ("@OriginalTitle", tmdbMovie.OriginalTitle ),
                        ("@ReleaseDate", _dbHelper.ToDateTimeParamValue( tmdbMovie.ReleaseDate() ) ),
                        ("@Overview", tmdbMovie.Overview ),
                        ("@PosterPath", tmdbMovie.PosterPath ),
                        ("@ParentId", collection.Id.ToString() ),
                        ("@IsEpisode", false )
                    ] ) );
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            return retVal;
        }

        public async Task AnalyzeMissingEpisodesTaskImpl( CancellationToken cancellationToken, IProgress<double> progress )
        {
            CheckIsValid( ECheckType.eUpdate );

            _embyInterfaces!._logger?.Debug( $"AnalyzeMissingEpisodes - Starting Analysis" );
            progress.Report( 0 );
            var allSeries = _dbHelper.GetLibraryItems<Series>().Cast<Series>().ToList();
            allSeries.Sort( ( a, b ) => StringComparer.OrdinalIgnoreCase.Compare( a.SortName, b.SortName ) );
            progress.Report( 100 );

            var reader = new TmdbCollectionReader( _embyInterfaces, cancellationToken );

            double count = allSeries.Count();
            var curr = 0.0;

            progress.Report( 0 );
            var sqlCmds = new List<SQLCmdDef>
            {
                new( "DELETE FROM Missing WHERE IsEpisode" )
            };

            foreach( var series in allSeries )
            {
                progress.Report( 80.0 * ( ++curr ) / count );
                var cmds = await AnalyzeMissingEpisodes( reader, series, cancellationToken, progress );
                sqlCmds.AddRange( cmds );
                cancellationToken.ThrowIfCancellationRequested();
                _embyInterfaces!._logger?.Debug( $"AnalyzeMissingMovies -     Processed Collection ({curr} of {count}) - {series.Name} items processed" );
            }

            cancellationToken.ThrowIfCancellationRequested();

            progress.Report( 80 );
            _dbHelper.ExecuteCommands( sqlCmds );
            progress.Report( 100 );
            _embyInterfaces!._logger?.Debug( $"AnalyzeMissingEpisodes - Finished Analysis" );
        }

        private async Task<List<SQLCmdDef>> AnalyzeMissingEpisodes( TmdbCollectionReader reader, Series series, CancellationToken cancellationToken, IProgress<double> progress )
        {
            _embyInterfaces!._logger?.Info( $"AnalyzeMissingEpisodes -     Analyzing Series '{series.Name}' - checking for missing episodes" );

            var seriesTmbdId = series.GetProviderId( MetadataProviders.Tmdb );
            if( string.IsNullOrEmpty( seriesTmbdId ) )
                return [];

            var tmdbSeries = await reader.GetRemoteSeriesAsync( seriesTmbdId, cancellationToken ).ConfigureAwait( false );
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

                var embySeason = _dbHelper.GetSeasonFromSeries( series, tmdbSeason.SeasonNumber );
                if( embySeason == null )
                    continue;

                foreach( var tmdbEpisode in tmdbSeason.Episodes )
                {
                    if( InvalidDate( tmdbEpisode.AirDate() ) )
                        continue;

                    var episodeTmdbId = tmdbEpisode.Id;
                    var episodeIdent = $"S{tmdbSeason.SeasonNumber:D2}E{tmdbEpisode.EpisodeNumber:D2}";
                    _embyInterfaces!._logger?.Debug( $"AnalyzeMissingEpisodes -         Checking for episode {episodeIdent} for {series.Name} on server" );
                    var embyEpisode = _dbHelper.GetEpisodeFromTmdbId( _embyInterfaces._libraryManager, embySeason, episodeTmdbId, tmdbEpisode.EpisodeNumber );

                    if( embyEpisode == null )
                    {
                        _embyInterfaces!._logger?.Info( $"AnalyzeMissingEpisodes -             episode {episodeIdent} is missing" );

                        var key = $"{tmdbSeries.Id}-{tmdbSeason.Id}-{tmdbEpisode.Id}";
                        //return $"{primaryName} - S{season:D2}E{episode:D2} - {secondaryName}";

                        var title = $"{tmdbSeries.Name} - S{tmdbSeason.SeasonNumber:D2}E{tmdbEpisode.EpisodeNumber:D2} - {tmdbEpisode.Name}";
                        retVal.Add( new SQLCmdDef( kSQLAddToMissing,
                        [
                            ("@Key", key ),
                            ("@TmdbId", tmdbEpisode.Id ),
                            ("@Title", title ),
                            ("@OriginalTitle", string.Empty ),
                            ("@ReleaseDate", _dbHelper.ToDateTimeParamValue( tmdbEpisode.AirDate() ) ),
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

        private StatCard TotalMissingMedia( bool episodes )
        {
            CheckIsValid( ECheckType.eReport );

            var whereClause = episodes ? "IsEpisode" : "NOT IsEpisode";

            var sql = $"SELECT COUNT( * ) FROM Missing WHERE {whereClause}";
            var value = GetSingleValueFromSQL( sql );
            var numMissing = value.ToInt64();

            sql = $"SELECT COUNT( DISTINCT ParentId ) FROM Missing WHERE {whereClause}";
            value = GetSingleValueFromSQL( sql );
            var numCollections = value.ToInt64();

            var title = episodes ? Constants.MissingEpisodes : Constants.MissingMovies;
            var subTitle = episodes ? Constants.MissingEpisodesSubTitle : Constants.MissingMoviesSubTitle;
            var help = episodes ? Constants.MissingEpisodesHelp : Constants.MissingMoviesHelp;

            var retVal = new TextBasedStatCard( title, help, EStatCardStyle.eCompact );

            retVal.AddLine( numMissing.ToString(), true );
            retVal.AddLine( subTitle, true );
            retVal.AddLine( numCollections.ToString(), true );
            return retVal;
        }

        public StatCard TotalMissingMovies()
        {
            return TotalMissingMedia( false );
        }

        public StatCard TotalMissingEpisodes()
        {
            return TotalMissingMedia( true );
        }
    }
}
