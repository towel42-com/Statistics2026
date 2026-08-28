using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using Statistics2026.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using SQLitePCL.pretty;
using System.ComponentModel;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        private static StatisticsDB? instance = null;
        private static readonly object _padlock = new object();
        private Dictionary<string, TableDef> _tableMap = new Dictionary<string, TableDef>();
        private List<TableDef> _tableList = new List<TableDef>();


        DBHelper _dbHelper = new DBHelper();

        public static StatisticsDB GetInstance(string db_file, ILogger log)
        {
            lock (_padlock)
            {
                if (instance == null)
                {
                    instance = new StatisticsDB(db_file, log);
                    log.Debug("StatisticsData : New Instance Created : " + instance.GetHashCode());
                }
                return instance;
            }
        }

        public static StatisticsDB GetExistingInstance()
        {
            lock (_padlock)
            {
                if (instance == null)
                {
                    throw new InvalidOperationException("No existing instance found.");
                }
                return instance;
            }
        }

        private StatisticsDB()
        {
            ConstructTableList();
        }

        private StatisticsDB(string db_path, ILogger l)
        {
            ConstructTableList();
            _dbHelper = new DBHelper(db_path, l);
            _dbHelper.Logger?.Debug("StatisticsData : Creating Database");
            _dbHelper.Logger?.Debug("StatisticsData : Finished Creating Database");

        }

        ~StatisticsDB()
        {
        }

        public void Initialize()
        {
            CreateTables(TableDef.EAction.eRecreate);
        }

        public void SetCancellationToken(CancellationToken? cancellationToken)
        {
            if (_dbHelper != null)
                _dbHelper.CancellationToken = cancellationToken;
        }


        private void ConstructTableList()
        {
            _tableList = new List<TableDef>()
            {
                new TableDef("LastUpdateTable",
                    new List<TableColDef>()
                    {
                        new TableColDef( "LastUpdated", "DATETIME", true ),
                        new TableColDef( "Version", "TEXT", true ),
                        new TableColDef( "BuildDate", "DATETIME", true )
                    }
                ),

                new TableDef("Media",
                    new List<TableColDef>()
                    {
                        new TableColDef( "ItemId", "TEXT", false ),
                        new TableColDef( "PrimaryName", "TEXT", false ),
                        new TableColDef( "SortName", "TEXT", true ),
                        new TableColDef( "SecondaryName", "TEXT", true ),
                        new TableColDef( "StartYear", "INT", true ),
                        new TableColDef( "IsEpisode", "BOOLEAN", true ),
                        new TableColDef( "SeriesId", "TEXT", true ),
                        new TableColDef( "Season", "INT", true ),
                        new TableColDef( "Episode", "INT", true ),
                        new TableColDef( "ResolutionBase", "TEXT", true ),
                        new TableColDef( "ResolutionDetail", "TEXT", true ),
                        new TableColDef( "Codec", "TEXT", true ),
                        new TableColDef( "DolbyVisionProfile", "TEXT", true ),
                        new TableColDef( "StudioNames", "TEXT", true ),
                        new TableColDef( "Genres", "TEXT", true ),
                        new TableColDef( "ServerLocation", "TEXT", true ),
                        new TableColDef( "FileSize", "INT", true),
                        new TableColDef( "ImageUrl", "TEXT", true ),
                        new TableColDef( "RunTimeTicks", "INT", true ),
                        new TableColDef( "Rating", "REAL", true ),
                        new TableColDef( "TotalBitrate", "INT", true ),
                        new TableColDef( "PremiereDate", "DATETIME", true ),
                        new TableColDef( "DateAdded", "DATETIME", true )
                    }
                ),

                new TableDef("Series",
                    new List<TableColDef>()
                    {
                        new TableColDef( "ItemId", "TEXT", false ),
                        new TableColDef( "Name", "TEXT", false ),
                        new TableColDef( "SortName", "TEXT", true ),
                        new TableColDef( "PremiereDate", "DATETIME", true ),
                        new TableColDef( "DateAdded", "DATETIME", true ),
                        new TableColDef( "ImageUrl", "TEXT", true ),
                        new TableColDef( "FileSize", "INT", true),
                        new TableColDef( "RunTimeTicks", "INT", true ),
                        new TableColDef( "Rating", "REAL", true ),
                        new TableColDef( "AverageBitrate", "INT", true )
                    }
                ),

                new TableDef("Users",
                    new List<TableColDef>()
                    {
                        new TableColDef( "UserId", "TEXT", false ),
                        new TableColDef( "UserName", "TEXT", false ),
                        new TableColDef( "ConnectUserId", "TEXT", true ),
                        new TableColDef( "IsAdministrator", "BOOLEAN", true ),
                        new TableColDef( "TotalTimeWatched", "INT", true ),
                        new TableColDef( "TotalWatchableTime", "INT", true )
                    }
                ),

                new TableDef("UserVideoList",
                    new List<TableColDef>()
                    {
                        new TableColDef( "UserId", "TEXT", false ), // user
                        new TableColDef( "ItemId", "TEXT", false ), // video item
                        new TableColDef( "IsPlayed", "BOOLEAN", false ),
                        new TableColDef( "LastPlayedDate", "DATETIME", true ),
                        new TableColDef( "IsEpisode", "BOOLEAN", true ),
                        new TableColDef( "SeriesId", "TEXT", true ) // if episode add seriesid
                    }
                ),

                //new TableDef("EpisodeProgress",
                //    new List<TableColDef>()
                //    {
                //        new TableColDef( "ItemId", "TEXT", false ),
                //        new TableColDef( "UserId", "TEXT", false ),
                //        new TableColDef( "Name", "TEXT", false ),
                //        new TableColDef( "SortName", "TEXT" , true ),
                //        new TableColDef( "StartYear", "INT" , true ),
                //        new TableColDef( "Watched", "INT" , true ),
                //        new TableColDef( "Score", "REAL" , true ),
                //        new TableColDef( "Status", "TEXT" , true ),
                //        new TableColDef( "TotalEpisodes", "INT" , true ),
                //        new TableColDef( "CollectedEpisodes", "INT" , true ),
                //        new TableColDef( "SeenEpisodes", "INT" , true ),
                //        new TableColDef( "TotalSpecials", "INT" , true ),
                //        new TableColDef( "CollectedSpecials", "INT" , true ),
                //        new TableColDef( "SeenSpecials", "INT" , true ),
                //        new TableColDef( "PercentSeen", "INT" , true ),
                //        new TableColDef( "PercentCollected", "INT" , true ),
                //    }
                //),

                new TableDef("Collections",
                    new List<TableColDef>()
                    {
                        new TableColDef( "ItemId", "TEXT", false ),
                        new TableColDef( "Name", "TEXT", false ),
                        new TableColDef( "SortName", "TEXT", false )
                    }
                ),

                new TableDef("CollectionMembership",
                    new List<TableColDef>()
                    {
                        new TableColDef( "CollectionId", "TEXT", false ),
                        new TableColDef( "ItemId", "TEXT", false ),
                        new TableColDef( "CollectionName", "TEXT", false ) // for debugging purposes
                    }
                ),

                new TableDef("CachedStats",
                    new List<TableColDef>()
                    {
                        new TableColDef( "LongestSeries", "TEXT", true ),
                        new TableColDef( "ShortestSeries", "TEXT", true ),
                        new TableColDef( "LargestSeries", "TEXT", true ),
                        new TableColDef( "SmallestSeries", "TEXT", true ),
                        new TableColDef( "TotalTVStudioCount", "INT", true ),
                        new TableColDef( "LongestMovie", "TEXT", true ),
                        new TableColDef( "ShortestMovie", "TEXT", true ),
                        new TableColDef( "LargestMovie", "TEXT", true ),
                        new TableColDef( "SmallestMovie", "TEXT", true ),
                        new TableColDef( "TotalMovieStudioCount", "INT", true ),
                    }
                ),

                new TableDef("CachedWatchedAnalysis",
                    new List<TableColDef>()
                    {
                        new TableColDef( "ItemId", "TEXT", true ),
                        new TableColDef( "Name", "TEXT", true ),
                        new TableColDef( "ImageUrl", "TEXT", true ),
                        new TableColDef( "NumEpisodes", "INT", true ),
                        new TableColDef( "NumWatched", "INT", true ),
                        new TableColDef( "PercentWatched", "DOUBLE", true ),
                        new TableColDef( "PercentWatchedPerUser", "DOUBLE", true ),
                    }
                )
            };
            foreach (var tableDef in _tableList)
            {
                _tableMap[tableDef.Name] = tableDef;
            }
        }

        private void CreateTables( TableDef.EAction action )
        {
            if (action != TableDef.EAction.eRecreate && action != TableDef.EAction.eCreate)
                throw new InvalidEnumArgumentException( $"Action must be {TableDef.EAction.eRecreate} or {TableDef.EAction.eCreate}");

            var sqlCmds = new List<string>();
            foreach (var tableDef in _tableList)
            {
                sqlCmds.AddRange(tableDef.GetSQLCommands(action));
            }

            _dbHelper.ExecuteCommands(sqlCmds);
        }

        public bool ClearTable(string tableName)
        {
            if (_tableMap.TryGetValue(tableName, out var tableDef))
            {
                var sqlCmds = tableDef.GetSQLCommands(TableDef.EAction.eClear);
                return _dbHelper.ExecuteCommands(sqlCmds);
            }
            else
            {
                return false;
            }
        }
    }
}
