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
        public void AddAllCollectionsTaskImpl( CancellationToken cancellationToken, IProgress<double> progress )
        {
            CheckIsValid( ECheckType.eUpdate );

            _embyInterfaces!._logger?.Debug( $"Starting Collection Analysis" );
            progress.Report( 0 );
            var collections = EmbyHelperFuncs.GetLibraryItems<BoxSet>( null, _embyInterfaces?._libraryManager );
            progress.Report( 100 );

            double count = collections.Count();
            var curr = 0.0;

            progress.Report( 0 );
            var sqlCmds = new List<SQLCmdDef>();

            foreach( var collection in collections )
            {
                progress.Report( 80.0 * ( ++curr ) / count );
                sqlCmds.AddRange( AddCollection( collection, cancellationToken, progress ) );
                cancellationToken.ThrowIfCancellationRequested();
                _embyInterfaces!._logger?.Debug( $"    Processed Collection ({curr} of {count}) - {collection.Name} items processed" );
            }

            cancellationToken.ThrowIfCancellationRequested();

            progress.Report( 80 );
            _dbHelper.ExecuteCommands( sqlCmds );
            progress.Report( 100 );
            _embyInterfaces!._logger?.Debug( $"Finished Collection Analysis" );
        }

        private List<SQLCmdDef> AddChildToCollection( Video video, BoxSet collection )
        {
            CheckIsValid( ECheckType.eUpdate );

            var sqlCmds = new List<SQLCmdDef>();

            if( video == null || collection == null )
            {
                _embyInterfaces!._logger?.Error( $"AddChildToCollection video, collection must be set" );
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

        private List<SQLCmdDef> AddCollectionMembers( BoxSet collection, CancellationToken cancellationToken, IProgress<double> progress )
        {
            CheckIsValid( ECheckType.eUpdate );

            _embyInterfaces!._logger?.Debug( $"AddCollectionMembers -     Adding members of Collection - {collection.Name}" );

            var query = new InternalItemsQuery
            {
                CollectionIds = new[] { collection.InternalId },
                Recursive = true
            };

            var baseItems = _embyInterfaces._libraryManager!.GetItemList( query );
            var videos = baseItems.OfType<Video>().ToList();

            double count = videos.Count;
            var curr = 0.0;

            var sqlCmds = new List<SQLCmdDef>();

            videos.ForEach( video =>
            {
                progress.Report( 100.0 * ( ++curr ) / count );
                sqlCmds.AddRange( AddChildToCollection( video, collection ) );
                cancellationToken.ThrowIfCancellationRequested();
            } );
            _embyInterfaces!._logger?.Debug( $"AddCollectionMembers -     Finished Adding {videos.Count} members of Collection {collection.Name} " );
            return sqlCmds;
        }

        public List<SQLCmdDef> AddCollection( BoxSet collection, CancellationToken cancellationToken, IProgress<double> progress )
        {
            CheckIsValid( ECheckType.eUpdate );

            var sqlCmds = new List<SQLCmdDef>();
            if( collection.Id == null )
            {
                _embyInterfaces!._logger?.Error( $"AddCollection {collection.SortName}: is missing ItemId" );
                return sqlCmds;
            }

            _embyInterfaces!._logger?.Debug( $"AddCollection - Adding Collection {collection.Name}" );

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
            if( ( tmdbId != null ) && !long.TryParse( tmdbId, out long result ) )
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
            _embyInterfaces!._logger?.Debug( $"    AddCollection - Successfully Added Collection" );

            sqlCmds.AddRange( AddCollectionMembers( collection, cancellationToken, progress ) );
            return sqlCmds;
        }

        public StatCard TotalCollectionCount()
        {
            CheckIsValid( ECheckType.eReport );

            var sql = "SELECT COUNT( ItemId ) FROM Collections";

            return ValueGroupForSingleItem( Constants.TotalCollections, Constants.HelpTotalCollections, sql );
        }

        public StatCard MultiCollectionMovies()
        {
            CheckIsValid( ECheckType.eReport );
            var sql = "SELECT  "
                + "  Media.SortName "
                + ", Media.ItemId "
                + ", Collections.Name "
                + ", Collections.SortName "
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

            Dictionary<string, string> urlMap = [];

            var sqlCmd = new SQLCmdDef( sql );
            _dbHelper.ExecuteCommand( sqlCmd, statement =>
            {
                var row = statement.Current;
                var col = 0;

                var itemSortName = row.GetString( col++ );
                var itemId = row.GetString( col++ );
                var collectionName = row.GetString( col++ );
                var collectionSortName = row.GetString( col++ );
                var collectionId = row.GetString( col++ );

                if( !urlMap.TryGetValue( itemId, out string itemImageUrl ) )
                {
                    itemImageUrl = ItemImageUrl._ItemImageUrl( itemId, _embyInterfaces!._libraryManager );
                    urlMap.Add( itemId, itemImageUrl );
                }

                if( !urlMap.TryGetValue( collectionId, out string collectionImageUrl ) )
                {
                    collectionImageUrl = ItemImageUrl._ItemImageUrl( collectionId, _embyInterfaces!._libraryManager );
                    urlMap.Add( collectionId, collectionImageUrl );
                }

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

            _dbHelper.ExecuteCommand( sqlCmd, statement =>
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
            CheckIsValid( ECheckType.eReport );
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

            _dbHelper.ExecuteCommand( sqlCmd, statement =>
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
