using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using ServiceStack;
using Statistics2026.Api;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public async Task AnalyzeMissingMoviesTaskImplAsync()
        {

            var allBoxSets = EmbyHelperFuncs.GetAllBoxSets( null, LibraryManager() );

            List<object> paramList = [];
            foreach( var collection in allBoxSets )
            {
                if( collection == null )
                    continue;
                var collectionTmbdId = collection.GetProviderId( MetadataProviders.Tmdb );
                if( string.IsNullOrEmpty( collectionTmbdId ) )
                    continue;

                paramList.Add( collectionTmbdId );
            }

            //List<SQLCmdDef?> preCmds = new()
            //{
            //    SQLCmdDef.CreateSQLCmdDef( "DELETE FROM Missing WHERE NOT IsEpisode AND TmdbId NOT IN (<KEYLIST>)", "<KEYLIST>", paramList )
            //};

            var reader = new TmdbInterface( EmbyInterfaces(), _dbHelper.CancellationToken );

            await AnalyzeData( "Missing Movie", "Missing Movies",
                () =>
                {
                    var collections = EmbyHelperFuncs.GetAllBoxSets( null, LibraryManager() );
                    collections.Sort( ( a, b ) => StringComparer.OrdinalIgnoreCase.Compare( a.SortName, b.SortName ) );
                    return collections;
                },
                async collection =>
                {
                    var cmds = await AnalyzeMissingMoviesInCollectionAsync( reader, collection ).ConfigureAwait( false );
                    return cmds;
                },
                async collection =>
                {
                    return collection?.Name ?? string.Empty;
                },
                () => { },
                [],
                10
                );
        }

        private async Task<List<SQLCmdDef>> AnalyzeMissingMoviesInCollectionAsync( TmdbInterface reader, BoxSet collection )
        {
            EmbyInterfaces().Logger().Info( $"    Analyzing Collection '{collection.Name}' - checking for missing movies" );

            var collectionTmbdId = collection.GetProviderId( MetadataProviders.Tmdb );
            if( string.IsNullOrEmpty( collectionTmbdId ) )
                return [];

            TmdbCollection? tmdbCollection = null;

            try
            {
                tmdbCollection = await reader.GetRemoteCollectionMembersAsync( collectionTmbdId ).ConfigureAwait( false );
                if( tmdbCollection == null )
                {
                    EmbyInterfaces().Logger().Warn( $"Could not find TMDB collection for {collection.Name} - {collectionTmbdId}" );
                    return [];
                }
            }
            catch( Exception ex )
            {
                EmbyInterfaces().Logger().Error( $"Problem connecting to TMDB - {collection.Name} - {collectionTmbdId}" );
                EmbyInterfaces().Logger().Error( ex.ToString() );
                return [];
            }


            List<SQLCmdDef> retVal = [];
            foreach( var tmdbMovie in tmdbCollection.Movies )
            {
                if( InvalidDateForMissingAnalysis( tmdbMovie.ReleaseDate() ) )
                    continue;

                var movieTmdbId = tmdbMovie.Id;
                EmbyInterfaces().Logger().Debug( $"        Checking for movie {tmdbMovie.Title} for {collection.Name} on server" );
                var embyMovie = EmbyHelperFuncs.GetMovieByTmdbId( LibraryManager(), movieTmdbId );

                if( embyMovie == null )
                {
                    EmbyInterfaces().Logger().Info( $"            {tmdbMovie.Title} is missing" );
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
