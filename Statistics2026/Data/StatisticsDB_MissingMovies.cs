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
        public async Task AnalyzeMissingMoviesTaskImpl( CancellationToken cancellationToken, IProgress<double> progress )
        {
            CheckIsValid( ECheckType.eUpdate );

            _embyInterfaces!._logger?.Debug( $"Starting Analysis" );
            progress.Report( 0 );
            var collections = EmbyHelperFuncs.GetLibraryItems<BoxSet>( null, _embyInterfaces?._libraryManager );
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
                _embyInterfaces!._logger?.Debug( $"    Processed Collection ({curr} of {count}) - {collection.Name} items processed" );
            }

            cancellationToken.ThrowIfCancellationRequested();

            progress.Report( 80 );
            _dbHelper.ExecuteCommands( sqlCmds );
            progress.Report( 100 );
            _embyInterfaces!._logger?.Debug( $"Finished Analysis" );
        }

        private async Task<List<SQLCmdDef>> AnalyzeMissingMoviesInCollection( TmdbCollectionReader reader, BoxSet collection, CancellationToken cancellationToken, IProgress<double> progress )
        {
            _embyInterfaces!._logger?.Info( $"    Analyzing Collection '{collection.Name}' - checking for missing movies" );

            var collectionTmbdId = collection.GetProviderId( MetadataProviders.Tmdb );
            if( string.IsNullOrEmpty( collectionTmbdId ) )
                return [];

            var tmdbCollection = await reader.GetRemoteCollectionMembersAsyncViaCustom( collectionTmbdId, cancellationToken ).ConfigureAwait( false );
            if( tmdbCollection == null )
            {
                _embyInterfaces._logger!.Warn( $"Could not find TMDB collection for {collection.Name} - {collectionTmbdId}" );
                return [];
            }

            List<SQLCmdDef> retVal = [];
            foreach( var tmdbMovie in tmdbCollection.Movies )
            {
                if( InvalidDateForMissingAnalysis( tmdbMovie.ReleaseDate() ) )
                    continue;

                var movieTmdbId = tmdbMovie.Id;
                _embyInterfaces!._logger?.Debug( $"        Checking for movie {tmdbMovie.Title} for {collection.Name} on server" );
                var embyMovie = EmbyHelperFuncs.GetMovieByTmdbId( _embyInterfaces._libraryManager, movieTmdbId );

                if( embyMovie == null )
                {
                    _embyInterfaces!._logger?.Info( $"            {tmdbMovie.Title} is missing" );
                    var key = $"{tmdbCollection.Id}-{tmdbMovie.Id}";
                    retVal.Add( new SQLCmdDef( kSQLAddToMissing,
                    [
                        ("@Key", key ),
                        ("@TmdbId", tmdbMovie.Id ),
                        ("@Title", tmdbMovie.Title ),
                        ("@OriginalTitle", tmdbMovie.OriginalTitle ),
                        ("@ReleaseDate", HelperFuncs.ToDateTimeParamValue( tmdbMovie.ReleaseDate() ) ),
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
    }
}
