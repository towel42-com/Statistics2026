using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using SQLitePCL.pretty;
using Statistics2026.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;


namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        private static StatisticsDB instance = null;
        private static readonly object _padlock = new object();

        private ILogger _logger = null;
        private IDatabaseConnection _connection = null;
        DBHelperFuncs _dbHelper = null;


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
            _dbHelper = new DBHelperFuncs();
        }

        private StatisticsDB(string db_path, ILogger l)
        {
            _logger = l;
            _logger.Debug("StatisticsData : Creating Database");
            _dbHelper = new DBHelperFuncs(db_path, _logger);
            _connection = _dbHelper.SQLConnection;
            _logger.Debug("StatisticsData : Finished Creating Database");

        }

        ~StatisticsDB()
        {
            _logger.Debug("StatisticsData : Cleaning up");
            if (_connection != null)
            {
                _connection.Close();
                _logger.Debug("StatisticsData : DB Connection Closed");
            }
        }

        public void Initialize()
        {
            CreateTables();
        }


        private void CreateTables()
        {
            lock (_connection)
            {
                bool clearFirst = true;
                new TableDef("LastUpdateTable",
                    new List<TableColDef>()
                    {
                        new TableColDef( "LastUpdated", "DATETIME", true ),
                        new TableColDef( "Version", "TEXT", true ),
                        new TableColDef( "BuildDate", "TEXT", true )
                    },
                    null
                ).Execute(clearFirst, _connection);

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
                    },
                    null
                ).Execute(clearFirst, _connection);

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
                    },
                    null
                ).Execute(clearFirst, _connection);

                new TableDef("Users",
                    new List<TableColDef>()
                    {
                        new TableColDef( "UserId", "TEXT", false ),
                        new TableColDef( "UserName", "TEXT", false ),
                        new TableColDef( "ConnectUserId", "TEXT", true ),
                        new TableColDef( "IsAdministrator", "BOOLEAN", true ),
                        new TableColDef( "TotalTimeWatched", "INT", true ),
                        new TableColDef( "TotalWatchableTime", "INT", true ),
                        new TableColDef( "TotalMovies", "INT", true ),
                        new TableColDef( "TotalCollections", "INT", true ),
                        new TableColDef( "TotalMoviesWatched", "INT", true ),
                        new TableColDef( "FavoriteMovieYears", "TEXT", true ),
                        new TableColDef( "FavoriteMovieGenres", "TEXT", true ),
                        new TableColDef( "TotalMovieTimeWatched", "INT", true ),
                        new TableColDef( "TotalMovieWatchableTime", "INT", true ),
                        new TableColDef( "LastSeenMovies", "TEXT", true ),
                        new TableColDef( "TotalTVSeries", "INT", true ),
                        new TableColDef( "TotalEpisodes", "INT", true ),
                        new TableColDef( "TotalEpisodesWatched", "INT", true ),
                        new TableColDef( "TotalSeriesFinished", "INT", true ),
                        new TableColDef( "FavoriteShowGenres", "TEXT", true ),
                        new TableColDef( "TotalTVTimeWatched", "INT", true ),
                        new TableColDef( "TotalTVWatchableTime", "INT", true ),
                        new TableColDef( "LastSeenShows", "TEXT", true ),
                    },
                    new List<string>() { "UserId", "UserName", "ConnectUserId" }
                ).Execute(clearFirst, _connection);

                new TableDef("UserVideoList",
                    new List<TableColDef>()
                    {
                        new TableColDef( "UserId", "TEXT", false ), // user
                        new TableColDef( "ItemId", "TEXT", false ), // video item
                        new TableColDef( "IsPlayed", "BOOLEAN", false ),
                        new TableColDef( "IsEpisode", "BOOLEAN", true ),
                        new TableColDef( "SeriesId", "TEXT", true ) // if episode add seriesid
                    },
                    null
                ).Execute(clearFirst, _connection);

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
                //    },
                //    new List<string>() { "ItemId", "UserId", "Name", "SortName" }
                //).Execute(clearFirst, _connection);


                new TableDef("Collections",
                    new List<TableColDef>()
                    {
                        new TableColDef( "ItemId", "TEXT", false ),
                        new TableColDef( "Name", "TEXT", false ),
                        new TableColDef( "SortName", "TEXT", false )
                    },
                    null
                ).Execute(clearFirst, _connection);

                new TableDef("CollectionMembership",
                    new List<TableColDef>()
                    {
                        new TableColDef( "CollectionId", "TEXT", false ),
                        new TableColDef( "ItemId", "TEXT", false ),
                        new TableColDef( "CollectionName", "TEXT", false ) // for debugging purposes
                   },
                    null
                ).Execute(clearFirst, _connection);

            }
        }
    }
}
