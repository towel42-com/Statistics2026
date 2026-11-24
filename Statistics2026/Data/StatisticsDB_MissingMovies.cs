using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using ServiceStack;
using Statistics2026.Utilities;
using System.Text.Json;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        public async Task AnalyzeMissingMoviesTaskImplAsync()
        {
            CheckIsValid( ECheckLevel.eUpdate );

            Logger().Debug( $"Starting Analysis" );
            SetProgress( 0 );
            List<BoxSet> collections = EmbyHelperFuncs.GetAllBoxSets( null, LibraryManager() );
            collections.Sort( ( a, b ) => StringComparer.OrdinalIgnoreCase.Compare( a.SortName, b.SortName ) );
            SetProgress( 100 );

            TmdbInterface reader = await TmdbInterface.CreateAsync( _embyInterfaces, CancellationToken );

            double count = collections.Count();
            int completedCount = 0;

            List<SQLCmdDef> sqlCmds = [];
            SetProgress( 0 );

            using( var timer = new AutoTimer( $"    Analyze Missing Movies - Getting Commands", Logger() ) )
            {
                using SemaphoreSlim gatekeeper = HelperFuncs.CreateSemaphoreSlim();

                var pool = collections.Select( ( collection, index ) => Task.Run( async () =>
                {
                    await gatekeeper.WaitAsync();
                    try
                    {
                        (int index, BoxSet collection, List<SQLCmdDef> cmds, List<string> missingTMDBids) retVal = (index: index, collection: collection, cmds: new List<SQLCmdDef>(), missingTMDBids: new List<string>());
                        if( collection == null )
                            return retVal;

                        (retVal.cmds, retVal.missingTMDBids) = await AnalyzeMissingMoviesInCollectionAsync( reader, collection ).ConfigureAwait( false );

                        CancellationToken?.ThrowIfCancellationRequested();

                        int currProgress = Interlocked.Increment( ref completedCount );
                        SetProgress( 80.0 * ( 1.0 * currProgress ) / count );

                        Logger().Info( $"    Processed Collection ({currProgress} of {count}) - {collection.Name}" );
                        return retVal;
                    }
                    finally
                    {
                        gatekeeper.Release();
                    }
                } ) ).ToList();

                (int index, BoxSet collection, List<SQLCmdDef> cmds, List<string> missingTMDBids)[] sortedResults =
                    ( await Task.WhenAll( pool ) )
                            .Where( item => item.cmds != null )
                            .OrderBy( item => item.index )
                            .ToArray();

                List<object> missingTMDBids =
                            sortedResults
                                .SelectMany( item => item.missingTMDBids! )
                                .Where( cmd => cmd != null )
                                .Cast<object>()
                                .ToList();
                SQLCmdDef cmd = SQLCmdDef.CreateSQLCmdDef( $"DELETE FROM Missing WHERE NOT IsEpisode AND TmdbId NOT IN (<KEYLIST>)", "<KEYLIST>", missingTMDBids );
                sqlCmds.Add( cmd );

                sqlCmds.AddRange( 
                    sortedResults
                            .SelectMany( item => item.cmds! )
                            .Where( cmd => cmd != null )
                            .Select( cmd => cmd! )
                            .ToList()
                            );

            }

            using( var timer = new AutoTimer( $"    Analyze Missing Movies - Executing Commands", Logger() ) )
            {
                SetProgress( 80 );
                await ExecuteCommandsAsync( sqlCmds );
                SetProgress( 100 );
                Logger().Debug( $"Finished Missing Movies Analysis" );
            }
        }

        private async Task<(List<SQLCmdDef> cmds, List<string> missingTMDBids)> AnalyzeMissingMoviesInCollectionAsync( TmdbInterface reader, BoxSet collection )
        {
            Logger().Info( $"    Analyzing Collection '{collection.Name}' - checking for missing movies" );

            string collectionTmbdId = collection.GetProviderId( MetadataProviders.Tmdb );
            if( string.IsNullOrEmpty( collectionTmbdId ) )
                return ([],[]);

            string isoCode = await HelperFuncs.GetCountryCodeAsync( _embyInterfaces!.HttpClient(), _cancellationToken: CancellationToken );

            TmdbCollection? tmdbCollection = null;
            try
            {
                tmdbCollection = await reader.GetRemoteCollectionMembersAsync( collectionTmbdId, Plugin.Instance!.Configuration.movieSettings.numDaysForMovieStreaming ).ConfigureAwait( false );
            }
            catch( Exception ex )
            {
                Logger().Error( $"Problem connecting to TMDB - {collection.Name} - {collectionTmbdId}" );
                Logger().Error( ex.ToString() );
            }

            if( tmdbCollection == null )
            {
                Logger().Warn( $"Could not find TMDB collection for {collection.Name} - {collectionTmbdId}" );
                return ([], []);
            }

            List<SQLCmdDef> cmds = [];
            List<string> missingTMDBids = [];
            DateTimeOffset daysAgo = DateTimeOffset.UtcNow.AddDays( -1 * Plugin.Instance!.Configuration.movieSettings.numDaysForMovieStreaming );
            foreach( TmdbMovie tmdbMovie in tmdbCollection.Movies )
            {
                if( HelperFuncs.InvalidDateForMissingAnalysis( tmdbMovie.ReleaseDate(), Plugin.Instance!.Configuration.tvSettings.numDaysFutureForMissing ) )
                    continue;

                if( ( tmdbMovie.ReleaseDate() != null ) && ( tmdbMovie.ReleaseDate() >= daysAgo ) && !tmdbMovie.HasWatchProvider( isoCode ) )
                    continue;

                string movieTmdbId = tmdbMovie.Id;

                Logger().Debug( $"        Checking for movie {tmdbMovie.Title} for {collection.Name} on server" );
                Movie? embyMovie = EmbyHelperFuncs.GetMovieByTmdbId( LibraryManager(), movieTmdbId );

                if( embyMovie == null )
                {
                    Logger().Info( $"            {tmdbMovie.Title} is missing from collection {collection.Name}" );
                    AddStreamingProvidersInfo? addStreamingProviderInfo = null;
                    try
                    {
                        addStreamingProviderInfo = await reader.GetStreamingProvidersAsync( Logger(), movieTmdbId, false, tmdbMovie.Title ).ConfigureAwait( false );
                        cmds.AddRange( addStreamingProviderInfo?.addProviderCmds ?? [] );
                    }
                    catch( Exception ex )
                    {
                        Logger().Error( ex.ToString() );
                    }

                    string key = $"{tmdbCollection.Id}-{tmdbMovie.Id}";
                    cmds.Add( new SQLCmdDef( kSQLAddToMissing,
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
                        ("@StudioNames", tmdbMovie.StudioNames() ),
                        ("@StreamingProviders", JsonSerializer.Serialize(addStreamingProviderInfo?.allProviders ?? new object()))

                    ] ) );
                    missingTMDBids.Add( tmdbMovie.Id );
                }
            }

            return (cmds, missingTMDBids);
        }
    }
}
