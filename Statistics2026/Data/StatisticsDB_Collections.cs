using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Utilities;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        public async Task AddAllCollectionsTaskImplAsync()
        {
            CheckIsValid( ECheckLevel.eUpdate );

            Logger().Debug( $"Starting Collection Analysis" );
            SetProgress( 0 );
            List<BoxSet> collections = EmbyHelperFuncs.GetAllBoxSets( null, LibraryManager() );
            SetProgress( 100 );

            double count = collections.Count();
            int completedCount = 0;


            SetProgress( 0 );

            await DeleteAllInvalid( collections.ConvertAll( item => (BaseItem)item ), "Collections", "ItemId" );

            var sqlCmds = new List<SQLCmdDef>();
            using( var listTimer = new AutoTimer( $"    Analyze Collection - Getting Commands", Logger() ) )
            {
                var pool = collections.Select( ( collection, index ) => Task.Run( async () =>
                {
                    List<SQLCmdDef> currCmds = [];
                    long runTimeMS = 0;

                    await Task.Yield();
                    using( var currTimer = new AutoTimer( $"    Processed Collection {collection.Name}", Logger() ) )
                    {
                        currTimer.Restart();
                        currCmds = await AddCollectionAsync( collection ).ConfigureAwait( false );
                        runTimeMS = currTimer.ElapsedMilliseconds();
                    }

                    CancellationToken?.ThrowIfCancellationRequested();
                    int currProgress = Interlocked.Increment( ref completedCount );
                    SetProgress( 80.0 * ( 1.0 * currProgress ) / count );
                    Logger().Info( $"Processed Collection {collection.Name} - {runTimeMS} ms" );

                    return (index: index, item: collection, cmds: currCmds);
                } ) ).ToList();

                (int index, BoxSet item, List<SQLCmdDef> cmds)[] unsortedResults = await Task.WhenAll( pool );
                sqlCmds = unsortedResults
                            .Where( item => item.cmds != null )
                            .OrderBy( item => item.index )
                            .SelectMany( item => item.cmds! )
                            .Where( subItem => subItem != null )
                            .Select( subItem => subItem! )
                            .ToList();
            }

            CancellationToken?.ThrowIfCancellationRequested();

            using( var timer = new AutoTimer( $"    Analyze Collection - Executing Commands", Logger() ) )
            {
                SetProgress( 80 );
                await ExecuteCommands( sqlCmds );
                SetProgress( 100 );
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

            string sql =
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

            BaseItem[] baseItems = LibraryManager().GetItemList( query );
            var videos = baseItems.OfType<Video>().ToList();

            double count = videos.Count;
            double curr = 0.0;

            var sqlCmds = new List<SQLCmdDef>();

            foreach( Video video in videos )
            { 
                SetProgress( 100.0 * ( ++curr ) / count );
                List<SQLCmdDef> childCmds = await AddChildToCollectionAsync( video, collection ).ConfigureAwait( false );
                sqlCmds.AddRange( childCmds );
                CancellationToken?.ThrowIfCancellationRequested();
            }
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

            string sql =
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

            string? tmdbId = collection.GetProviderId( MetadataProviders.Tmdb );
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

        public async Task<StatCard> TotalCollectionCount()
        {
            CheckIsValid( ECheckLevel.eReport );

            string sql = "SELECT COUNT( ItemId ) FROM Collections";

            return await ValueGroupForSingleItem( Constants.TotalCollections, Constants.HelpTotalCollections, sql );
        }

        public async Task<StatCard> MultiCollectionMovies()
        {
            CheckIsValid( ECheckLevel.eReport );
            string sql = "SELECT  "
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
            await ExecuteCommand( sqlCmd, statement =>
            {
                IResultSet row = statement.Current;
                int col = 0;

                string itemSortName = row.GetString( col++ );
                string itemId = row.GetString( col++ );
                string itemImageUrl = row.GetString( col++ );
                string collectionName = row.GetString( col++ );
                string collectionSortName = row.GetString( col++ );
                string collectionImageUrl = row.GetString( col++ );
                ;
                string collectionId = row.GetString( col++ );

                retVal.AddLine( (itemSortName, itemId, itemImageUrl, false), (collectionSortName, collectionId, collectionImageUrl, false) );

                return Task.FromResult( true );
            } );

            return retVal;
        }

        private async Task<SortByText?> GetCollectionName( string collectionId )
        {
            string sql = "SELECT " +
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

            await ExecuteCommand( sqlCmd, statement =>
            {
                IResultSet row = statement.Current;
                names.Add( row.GetString( 0 ) );
                sortByNames.Add( row.GetString( 1 ) );
                return Task.FromResult( true );
            }, false );

            if( names.Count == 0 || sortByNames.Count == 0 )
                return null;

            var retVal = new SortByText( string.Join( ", ", names ), string.Join( ", ", sortByNames ) );
            return retVal;
        }

        private async Task<SortByText?> GetCollectionsForMovie( string movieId, bool includeImage )
        {
            CheckIsValid( ECheckLevel.eReport );
            string sql = "SELECT " +
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

            await ExecuteCommand( sqlCmd, statement =>
            {
                IResultSet row = statement.Current;
                int col = 0;
                string name = row.GetString( col++ );
                string sortBy = row.GetString( col++ );
                names.Add( name );
                sortByNames.Add( sortBy );
                if( includeImage )
                {
                    string collectionId = row.GetString( col++ );
                    string collectionUrl = row.GetString( col++ );
                    images.Add( ItemImageUrl.ItemUrlFromItemId( collectionId, collectionUrl, name ) );
                }
                return Task.FromResult( true );
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
