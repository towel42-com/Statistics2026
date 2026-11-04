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
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public async Task AnalyzeMissingMoviesTaskImplAsync()
        {
            CheckIsValid( ECheckLevel.eUpdate );

            Logger().Debug( $"Starting Analysis" );
            Progress?.Report( 0 );
            var collections = EmbyHelperFuncs.GetAllBoxSets( null, LibraryManager() );
            collections.Sort( ( a, b ) => StringComparer.OrdinalIgnoreCase.Compare( a.SortName, b.SortName ) );
            Progress?.Report( 100 );

            var reader = new TmdbInterface( _embyInterfaces, CancellationToken );

            double count = collections.Count();
            var completedCount = 0;

            List<SQLCmdDef> sqlCmds = [];
            ConcurrentBag<object> validIds = [];

            Progress?.Report( 0 );
            using( var timer = new AutoTimer( $"    Analyze Missing Movies - Getting Commands", Logger() ) )
            {
                using var gatekeeper = new SemaphoreSlim( 10, 10 );

                var pool = collections.Select( ( collection, index ) => Task.Run( async () =>
                {
                    await gatekeeper.WaitAsync();
                    try
                    {
                        var retVal = (index: index, collection: collection, cmds: new List<SQLCmdDef>());
                        if( collection == null )
                            return retVal;

                        retVal.cmds = await AnalyzeMissingMoviesInCollectionAsync( reader, collection ).ConfigureAwait( false );
                        validIds.Add( collection.Id.ToString() );

                        CancellationToken?.ThrowIfCancellationRequested();

                        var currProgress = Interlocked.Increment( ref completedCount );
                        Progress?.Report( 80.0 * ( 1.0 * currProgress ) / count );

                        Logger().Info( $"    Processed Collection ({currProgress} of {count}) - {collection.Name}" );
                        return retVal;
                    }
                    finally
                    {
                        gatekeeper.Release();
                    }
                } ) ).ToList();

                var unsortedResults = await Task.WhenAll( pool );
                sqlCmds.Add( CreateSQLCmdDef( "DELETE FROM Missing WHERE NOT IsEpisode AND ParentId IN (<KEYLIST>)", "<KEYLIST>", validIds.ToList() ) );
                var results = unsortedResults.OrderBy( item => item.index ).ToList();
                foreach( var result in results )
                {
                    sqlCmds.AddRange( result.cmds );
                    CancellationToken?.ThrowIfCancellationRequested();
                }
            }

            using( var timer = new AutoTimer( $"    Analyze Missing Movies - Executing Commands", Logger() ) )
            {
                Progress?.Report( 80 );
                ExecuteCommands( sqlCmds );
                Progress?.Report( 100 );
                Logger().Debug( $"Finished Missing Movies Analysis" );
            }
        }

        private async Task<List<SQLCmdDef>> AnalyzeMissingMoviesInCollectionAsync( TmdbInterface reader, BoxSet collection )
        {
            Logger().Info( $"    Analyzing Collection '{collection.Name}' - checking for missing movies" );

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
                Logger().Error( $"Problem connecting to TMDB - {collection.Name} - {collectionTmbdId}" );
                Logger().Error( ex.ToString() );
            }

            if( tmdbCollection == null )
            {
                Logger().Warn( $"Could not find TMDB collection for {collection.Name} - {collectionTmbdId}" );
                return [];
            }

            List<SQLCmdDef> retVal = [];
            foreach( var tmdbMovie in tmdbCollection.Movies )
            {
                if( InvalidDateForMissingAnalysis( tmdbMovie.ReleaseDate() ) )
                    continue;

                var movieTmdbId = tmdbMovie.Id;
                Logger().Debug( $"        Checking for movie {tmdbMovie.Title} for {collection.Name} on server" );
                var embyMovie = EmbyHelperFuncs.GetMovieByTmdbId( LibraryManager(), movieTmdbId );

                if( embyMovie == null )
                {
                    Logger().Info( $"            {tmdbMovie.Title} is missing" );
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
