using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack;
using Statistics2026.Api;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public async Task AddAllCollectionsTaskImplAsync()
        {
            CheckIsValid( ECheckLevel.eUpdate );

            Logger().Debug( $"Starting Collection Analysis" );
            Progress?.Report( 0 );
            var collections = EmbyHelperFuncs.GetAllBoxSets( null, LibraryManager() );
            Progress?.Report( 100 );

            double count = collections.Count();
            var completedCount = 0;

            Progress?.Report( 0 );
            var sqlCmds = new List<SQLCmdDef>();

            using( var timer = new AutoTimer( $"    Analyze Collection - Getting Commands", Logger() ) )
            {
                var pool = collections.Select( ( collection, index ) => Task.Run( async () =>
                {
                    List<SQLCmdDef> currCmds = [];
                    long runTimeMS = 0;
                    using( var userTimer = new AutoTimer( $"    Processed Collection {collection.Name}", Logger() ) )
                    {
                        currCmds = await AddCollectionAsync( collection ).ConfigureAwait( false );
                        runTimeMS = timer.ElapsedMilliseconds();
                    }

                    CancellationToken?.ThrowIfCancellationRequested();
                    int currProgress = Interlocked.Increment( ref completedCount );
                    Progress?.Report( 80.0 * ( 1.0 * currProgress ) / count );
                    Logger().Info( $"Processed Collection {collection.Name} - {runTimeMS} ms" );

                    return (index: index, collection: collection, cmds: currCmds, runTimeMS: runTimeMS);
                } ) ).ToList();

                var unsortedResults = await Task.WhenAll( pool );
                var results = unsortedResults.OrderBy( item => item.index ).ToList();

                foreach( var result in results )
                {
                    sqlCmds.AddRange( result.cmds );
                }
            }

            CancellationToken?.ThrowIfCancellationRequested();

            using( var timer = new AutoTimer( $"    Analyze Collection - Executing Commands", Logger() ) )
            {
                Progress?.Report( 80 );
                ExecuteCommands( sqlCmds );
                Progress?.Report( 100 );
            }
            Logger().Debug( $"Finished Collection Analysis" );
        }

        private async Task<List<SQLCmdDef>> AddChildToCollectionAsync( Video video, BoxSet collection )
        {
            CheckIsValid( ECheckLevel.eUpdate );

            var sqlCmds = new List<SQLCmdDef>();

            if( video == null || collection == null )
            {
                Logger().Error( $"AddChildToCollection video, collection must be set" );
                return sqlCmds;
            }

            var sql =
                "INSERT INTO CollectionMembership " +
                "(" +
                    "  CollectionId" +
                    ", ItemId" +
                ")" +
                " VALUES " +
                "(" +
                "  @CollectionId" +
                ", @ItemId" +
                ")" +
                " ON CONFLICT(CollectionId,ItemId) " +
                " DO NOTHING "
                ;


            sqlCmds.Add( new SQLCmdDef( sql,
            [
                ("@CollectionId", collection.Id.ToString()),
                ("@ItemId", video.Id.ToString()),
            ] ) );
            return sqlCmds;
        }

        private async Task<List<SQLCmdDef>> AddCollectionMembersAsync( BoxSet collection )
        {
            CheckIsValid( ECheckLevel.eUpdate );

            Logger().Debug( $"AddCollectionMembers -     Adding members of Collection - {collection.Name}" );

            var query = new InternalItemsQuery
            {
                CollectionIds = new[] { collection.InternalId },
                Recursive = true
            };

            var baseItems = LibraryManager().GetItemList( query );
            var videos = baseItems.OfType<Video>().ToList();

            double count = videos.Count;
            var curr = 0.0;

            var sqlCmds = new List<SQLCmdDef>();

            videos.ForEach( async video =>
            {
                Progress?.Report( 100.0 * ( ++curr ) / count );
                sqlCmds.AddRange( await AddChildToCollectionAsync( video, collection ).ConfigureAwait( false ) );
                CancellationToken?.ThrowIfCancellationRequested();
            } );
            Logger().Debug( $"AddCollectionMembers -     Finished Adding {videos.Count} members of Collection {collection.Name} " );
            return sqlCmds;
        }

        public async Task<List<SQLCmdDef>> AddCollectionAsync( BoxSet collection )
        {
            CheckIsValid( ECheckLevel.eUpdate );

            var sqlCmds = new List<SQLCmdDef>();
            if( collection.Id == null )
            {
                Logger().Error( $"AddCollection {collection.SortName}: is missing ItemId" );
                return sqlCmds;
            }

            Logger().Debug( $"AddCollection - Adding Collection {collection.Name}" );

            var sql =
                "INSERT INTO Collections " +
                "(" +
                    "  ItemId" +
                    ", TmdbId" +
                    ", Name" +
                    ", SortName" +
                    ", ImageUrl" +
                ")" +
                " VALUES " +
                "(" +
                "  @ItemId" +
                ", @TmdbId" +
                ", @Name" +
                ", @SortName" +
                ", @ImageUrl" +
                ")" +
                " ON CONFLICT(ItemId) " +
                " DO UPDATE " +
                " SET " +
                    "  Name=@Name" +
                    ", SortName=@SortName" +
                    ", TmdbId=@TmdbId" +
                    ", ImageUrl=@ImageUrl"
                    ;

            var tmdbId = collection.GetProviderId( MetadataProviders.Tmdb );
            if( ( tmdbId != null ) && !long.TryParse( tmdbId, out _ ) )
            {
                tmdbId = null;
            }

            sqlCmds.Add( new SQLCmdDef( sql,
                [
                    ("@ItemId", collection.Id.ToString()),
                    ("@TmdbId", tmdbId),
                    ("@Name", collection.Name),
                    ("@SortName", collection.SortName),
                    ("@ImageUrl", ItemImageUrl._ItemImageUrl(collection)),
                ] ) );
            Logger().Debug( $"    AddCollection - Successfully Added Collection" );

            sqlCmds.AddRange( await AddCollectionMembersAsync( collection ).ConfigureAwait( false ) );
            return sqlCmds;
        }

        public StatCard TotalCollectionCount()
        {
            CheckIsValid( ECheckLevel.eReport );

            var sql = "SELECT COUNT( ItemId ) FROM Collections";

            return ValueGroupForSingleItem( Constants.TotalCollections, Constants.HelpTotalCollections, sql );
        }

        public StatCard MultiCollectionMovies()
        {
            CheckIsValid( ECheckLevel.eReport );
            var sql = "SELECT  "
                + "  Media.SortName "
                + ", Media.ItemId "
                + ", Media.ImageUrl "
                + ", Collections.Name "
                + ", Collections.SortName "
                + ", Collections.ImageUrl "
                + ", CollectionMembership.CollectionId  "
                + "FROM "
                + " CollectionMembership "
                + " INNER JOIN Media ON Media.ItemId=CollectionMembership.ItemId"
                + " INNER JOIN Collections ON Collections.ItemId=CollectionMembership.CollectionId "
                + "WHERE NOT Media.IsEpisode"
                + "  AND CollectionMembership.ItemId IN ("
                + "      SELECT ItemId "
                + "      FROM CollectionMembership "
                + "      GROUP BY ItemId "
                + "      HAVING COUNT(*) > 1"
                + "  )"
                + "ORDER BY Media.SortName ASC, Collections.SortName ASC"
                ;

            var retVal = new GroupedTextBasedStatCard( Constants.MoviesInMultipleCollections, null, EStatCardStyle.eDetailed )
            {
                ListType = EListType.eUnordered
            };

            var sqlCmd = new SQLCmdDef( sql );
            ExecuteCommand( sqlCmd, statement =>
            {
                var row = statement.Current;
                var col = 0;

                var itemSortName = row.GetString( col++ );
                var itemId = row.GetString( col++ );
                var itemImageUrl = row.GetString( col++ );
                var collectionName = row.GetString( col++ );
                var collectionSortName = row.GetString( col++ );
                var collectionImageUrl = row.GetString( col++ );
                ;
                var collectionId = row.GetString( col++ );

                retVal.AddLine( (itemSortName, itemId, itemImageUrl, false), (collectionSortName, collectionId, collectionImageUrl, false) );

                return true;
            } );

            return retVal;
        }

        public SortByText? GetCollectionName( string collectionId )
        {
            var sql = "SELECT " +
                "  Collections.Name" +
                ", Collections.SortName" +
                " FROM Collections" +
                " WHERE " +
                " Collections.ItemId=@ItemId"
                ;

            var sqlCmd = new SQLCmdDef( sql,
            [
                ("@ItemId", collectionId)
            ] );

            List<string> names = [];
            List<string> sortByNames = [];

            ExecuteCommand( sqlCmd, statement =>
            {
                var row = statement.Current;
                names.Add( row.GetString( 0 ) );
                sortByNames.Add( row.GetString( 1 ) );
                return true;
            }, false );

            if( names.Count == 0 || sortByNames.Count == 0 )
                return null;

            var retVal = new SortByText( string.Join( ", ", names ), string.Join( ", ", sortByNames ) );
            return retVal;
        }

        public SortByText? GetCollectionsForMovie( string movieId, bool includeImage )
        {
            CheckIsValid( ECheckLevel.eReport );
            var sql = "SELECT " +
                "  Collections.Name" +
                ", Collections.SortName" +
                ", Collections.ItemId" +
                ", Collections.ImageUrl" +
                " FROM CollectionMembership" +
                " LEFT JOIN Collections" +
                "    ON Collections.Itemid=CollectionMembership.CollectionId" +
                " WHERE " +
                " CollectionMembership.ItemId=@ItemId" +
                " ORDER BY Collections.SortName ASC"
                ;

            var sqlCmd = new SQLCmdDef( sql,
            [
                ("@ItemId", movieId)
            ] );

            List<string> names = [];
            List<string> sortByNames = [];
            List<string> images = [];

            ExecuteCommand( sqlCmd, statement =>
            {
                var row = statement.Current;
                var col = 0;
                var name = row.GetString( col++ );
                var sortBy = row.GetString( col++ );
                names.Add( name );
                sortByNames.Add( sortBy );
                if( includeImage )
                {
                    var collectionId = row.GetString( col++ );
                    var collectionUrl = row.GetString( col++ );
                    images.Add( ItemImageUrl.ItemUrl( collectionId, collectionUrl, name ) );
                }
                return true;
            }, false );

            if( names.Count == 0 || sortByNames.Count == 0 )
                return null;

            if( includeImage )
            {
                if( images.Count != names.Count )
                    return null;
                for( int ii = 0; ii < names.Count; ++ii )
                {
                    if( images[ ii ] != null && images[ ii ] != String.Empty )
                    {
                        names[ ii ] = images[ ii ];
                    }
                }
            }

            var retVal = new SortByText( string.Join( ", ", names ), string.Join( ", ", sortByNames ) );
            return retVal;
        }
    }
}
