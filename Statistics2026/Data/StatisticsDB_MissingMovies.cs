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

            _embyInterfaces!.Logger().Debug( $"Starting Analysis" );
            progress.Report( 0 );
            var collections = EmbyHelperFuncs.GetLibraryItems<BoxSet>( null, LibraryManager() );
            collections.Sort( ( a, b ) => StringComparer.OrdinalIgnoreCase.Compare( a.SortName, b.SortName ) );

            progress.Report( 100 );

            var reader = new TmdbCollectionReader( _embyInterfaces, cancellationToken );

            double count = collections.Count();
            var curr = 0.0;

            progress.Report( 0 );
            List<SQLCmdDef> sqlCmds = [];

            List<object> paramList = [];
            foreach( var collection in collections )
            {
                progress.Report( 80.0 * ( ++curr ) / count );
                try
                {
                    var cmds = await AnalyzeMissingMoviesInCollection( reader, collection, progress ).ConfigureAwait( false );
                    paramList.Add( collection.Id.ToString() );
                    sqlCmds.AddRange( cmds );
                }
                catch( Exception )
                {
                }
                cancellationToken.ThrowIfCancellationRequested();
                _embyInterfaces!.Logger().Info( $"    Processed Collection ({curr} of {count}) - {collection.Name}" );

            }

            sqlCmds.Insert( 0, DBHelper.CreateSQLCmdDef( "DELETE FROM Missing WHERE NOT IsEpisode AND ParentId IN (<KEYLIST>)", "<KEYLIST>", paramList ) );

            cancellationToken.ThrowIfCancellationRequested();

            progress.Report( 80 );
            _dbHelper.ExecuteCommands( sqlCmds );
            progress.Report( 100 );
            _embyInterfaces!.Logger().Debug( $"Finished Analysis" );
        }

        private async Task<List<SQLCmdDef>> AnalyzeMissingMoviesInCollection( TmdbCollectionReader reader, BoxSet collection, IProgress<double> progress )
        {
            _embyInterfaces!.Logger().Info( $"    Analyzing Collection '{collection.Name}' - checking for missing movies" );

            var collectionTmbdId = collection.GetProviderId( MetadataProviders.Tmdb );
            if( string.IsNullOrEmpty( collectionTmbdId ) )
                return [];

            TmdbCollection? tmdbCollection = null;

            try
            {
                tmdbCollection = await reader.GetRemoteCollectionMembersAsync( collectionTmbdId ).ConfigureAwait( false );
            }
            catch( Exception ex )
            {
                _embyInterfaces.Logger().Error( $"Problem connecting to TMDB - {collection.Name} - {collectionTmbdId}" );
                _embyInterfaces!.Logger().Error( ex.ToString() );
                throw ex;
            }

            if( tmdbCollection == null )
            {
                _embyInterfaces.Logger().Warn( $"Could not find TMDB collection for {collection.Name} - {collectionTmbdId}" );
                return [];
            }

            List<SQLCmdDef> retVal = [];
            foreach( var tmdbMovie in tmdbCollection.Movies )
            {
                if( InvalidDateForMissingAnalysis( tmdbMovie.ReleaseDate() ) )
                    continue;

                var movieTmdbId = tmdbMovie.Id;
                _embyInterfaces!.Logger().Debug( $"        Checking for movie {tmdbMovie.Title} for {collection.Name} on server" );
                var embyMovie = EmbyHelperFuncs.GetMovieByTmdbId( LibraryManager(), movieTmdbId );

                if( embyMovie == null )
                {
                    _embyInterfaces!.Logger().Info( $"            {tmdbMovie.Title} is missing" );
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
                        ("@IsEpisode", false ),
                        ("@StudioNames", tmdbMovie.StudioNames() )
                    ] ) );
                }
            }

            return retVal;
        }
    }
}
