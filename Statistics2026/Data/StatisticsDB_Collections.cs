using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using ServiceStack;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using static ServiceStack.Diagnostics;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public void AddAllCollections(CancellationToken cancellationToken, IProgress<double> progress)
        {
            CheckIsValid( ECheckType.eUpdate );

            _embyInterfaces!._logger?.Debug($"AddAllCollections - Starting Collection Analysis");
            progress.Report(0);
            var collections = _dbHelper.GetLibraryItems<BoxSet>();
            progress.Report(100);

            double count = collections.Count();
            double curr = 0.0;

            progress.Report(0);
            var sqlCmds = new List<SQLCmdDef>();

            foreach (var collection in collections)
            {
                progress.Report(80.0 * (++curr) / count);
                sqlCmds.AddRange(AddCollection(collection, cancellationToken, progress));
                cancellationToken.ThrowIfCancellationRequested();
                _embyInterfaces!._logger?.Debug($"AddAllCollections -     Processed Collection ({curr} of {count}) - {collection.Name} items processed");
            }
            cancellationToken.ThrowIfCancellationRequested();

            progress.Report(80);
            _dbHelper.ExecuteCommands(sqlCmds);
            progress.Report(100);
            _embyInterfaces!._logger?.Debug($"AddAllCollections - Finished Collection Analysis");
        }

        private List<SQLCmdDef> AddChildToCollection(Video video, BoxSet collection)
        {
            CheckIsValid(ECheckType.eUpdate);

            var sqlCmds = new List<SQLCmdDef>();

            if (video == null || collection == null)
            {
                _embyInterfaces!._logger?.Error($"AddChildToCollection video, collection must be set");
                return sqlCmds;
            }

            string sql =
                "INSERT INTO CollectionMembership " +
                "(" +
                    "  CollectionId" +
                    ", ItemId" +
                    ", CollectionName" +
                ")" +
                " VALUES " +
                "(" +
                "  @CollectionId" +
                ", @ItemId" +
                ", @CollectionName" +
                ")";

            sqlCmds.Add(new SQLCmdDef(sql, new List<(string name, object? value)>
            {
                ("@CollectionId", collection.Id.ToString()),
                ("@ItemId", video.Id.ToString()),
                ("@CollectionName", collection.Name),
            }));
            return sqlCmds;
        }

        private List<SQLCmdDef> AddCollectionMembers(BoxSet collection, CancellationToken cancellationToken, IProgress<double> progress)
        {
            CheckIsValid(ECheckType.eUpdate);

            _embyInterfaces!._logger?.Debug($"AddAllCollections - AddCollectionMembers -     Adding members of Collection - {collection.Name}");

            var query = new InternalItemsQuery
            {
                CollectionIds = new[] { collection.InternalId },
                Recursive = true
            };

            var baseItems = _embyInterfaces._libraryManager.GetItemList(query);
            var videos = baseItems.OfType<Video>().ToList();

            double count = videos.Count;
            double curr = 0.0;

            var sqlCmds = new List<SQLCmdDef>();

            videos.ForEach(video =>
            {
                progress.Report(100.0 * (++curr) / count);
                sqlCmds.AddRange(AddChildToCollection(video, collection));
                cancellationToken.ThrowIfCancellationRequested();
            });
            _embyInterfaces!._logger?.Debug($"AddAllCollections - AddCollectionMembers -     Finished Adding {videos.Count} members of Collection {collection.Name} ");
            return sqlCmds;
        }

        public List<SQLCmdDef> AddCollection(BoxSet collection, CancellationToken cancellationToken, IProgress<double> progress)
        {
            CheckIsValid(ECheckType.eUpdate);

            var sqlCmds = new List<SQLCmdDef>();
            if (collection.Id == null)
            {
                _embyInterfaces!._logger?.Error($"AddCollection {collection.SortName}: is missing ItemId");
                return sqlCmds;
            }

            _embyInterfaces!._logger?.Debug($"AddAllCollections - AddCollection - Adding Collection {collection.Name}");

            string sql =
                "INSERT INTO Collections " +
                "(" +
                    "  ItemId" +
                    ", Name" +
                    ", SortName" +
                ")" +
                " VALUES " +
                "(" +
                "  @ItemId" +
                ", @Name" +
                ", @SortName" +
                ")";
            sqlCmds.Add(new SQLCmdDef(sql, new List<(string name, object? value)>()
            {
                ("@ItemId", collection.Id.ToString()),
                ("@Name", collection.Name),
                ("@SortName", collection.SortName),
            }));
            _embyInterfaces!._logger?.Debug($"AddAllCollections -     AddCollection - Successfully Added Collection");

            sqlCmds.AddRange(AddCollectionMembers(collection, cancellationToken, progress));
            return sqlCmds;
        }

        public StatCard TotalCollectionCount()
        {
            CheckIsValid(ECheckType.eReport);

            string sql = "SELECT COUNT( ItemId ) FROM Collections";

            return ValueGroupForSingleItem(Constants.TotalCollections, Constants.HelpTotalCollections, sql);
        }
    }
}
