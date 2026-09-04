using MediaBrowser.Controller.Entities;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        private static readonly object _padlock = new object();
        private Dictionary<string, TableDef> _tableMap = new Dictionary<string, TableDef>();
        private List<TableDef> _tableList = new List<TableDef>();
        private TableDef? _userMediaTemplate = null;
        private EmbyManagers? _embyManagers = null;

        DBHelper _dbHelper = new DBHelper();

        public static StatisticsDB GetInstance(EmbyManagers? embyManagers)
        {
            if (embyManagers == null)
                throw new ArgumentNullException("EmbyManagers is null.");

            lock (_padlock)
            {
                var retVal = new StatisticsDB(embyManagers);
                embyManagers._logger.Debug("StatisticsData : New Instance Created : " + retVal.GetHashCode());
                return retVal;
            }
        }

        private StatisticsDB()
        {
            ConstructTableList();
        }

        private StatisticsDB(EmbyManagers embyManagers)
        {
            if (embyManagers == null)
                throw new ArgumentNullException("embyManagers is null.");

            ConstructTableList();

            _embyManagers = embyManagers;
            _dbHelper = new DBHelper(_embyManagers);
            NumUsers(false, false);
            NumUsers(false, true);
            NumUsers(true, false);
            NumUsers(true, true);

            embyManagers._logger?.Debug("StatisticsData : Creating Database");
            embyManagers._logger?.Debug("StatisticsData : Finished Creating Database");
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
                        new TableColDef( "IsTVSpecial", "BOOLEAN", true ),
                        new TableColDef( "SeriesId", "TEXT", true ),
                        new TableColDef( "Season", "INT", true ),
                        new TableColDef( "Episode", "INT", true ),
                        new TableColDef( "NumEpisodes", "INT", true ),
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
                        new TableColDef( "NumEpisodes", "INT", true ),
                        new TableColDef( "NumSpecials", "INT", true ),
                        new TableColDef( "DateAdded", "DATETIME", true ),
                        new TableColDef( "ImageUrl", "TEXT", true ),
                        new TableColDef( "FileSize", "INT", true),
                        new TableColDef( "RunTimeTicks", "INT", true ),
                        new TableColDef( "Rating", "REAL", true ),
                        new TableColDef( "Status", "TEXT" , true ),
                        new TableColDef( "AverageBitrate", "INT", true ),
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
                        new TableColDef( "PercentWatchedPerUser", "DOUBLE", true ),
                    }
                )
            };

            _userMediaTemplate = new TableDef("UserMedia_<USER_ID>",
                    new List<TableColDef>()
                    {
                        new TableColDef( "UserId", "TEXT", false ), // user
                        new TableColDef( "ItemId", "TEXT", false ), // video item
                        new TableColDef( "Name", "TEXT", true ), // Name of the show to reduce joins
                        new TableColDef( "IsPlayed", "BOOLEAN", true ),
                        new TableColDef( "PlayCount", "INT", true ),
                        new TableColDef( "LastPlayedDate", "DATETIME", true ),
                        new TableColDef( "IsEpisode", "BOOLEAN", true ),
                        new TableColDef( "NumEpisodes", "BOOLEAN", true ), // for multi episode media
                        new TableColDef( "IsTVSpecial", "BOOLEAN", true ),
                        new TableColDef( "SeriesId", "TEXT", true ) // if episode add seriesid
                    }
                );

            foreach (var tableDef in _tableList)
            {
                _tableMap[tableDef.Name] = tableDef;
            }
        }

        private void CreateTables(TableDef.EAction action)
        {
            if (action != TableDef.EAction.eRecreate && action != TableDef.EAction.eCreate)
                throw new InvalidEnumArgumentException($"Action must be {TableDef.EAction.eRecreate} or {TableDef.EAction.eCreate}");

            var sqlCmds = new List<string>();
            foreach (var tableDef in _tableList)
            {
                sqlCmds.AddRange(tableDef.GetSQLCommands(action));
            }

            sqlCmds.Add($"DROP TABLE IF EXISTS UserVideoList"); // incase running an old schema

            if (action == TableDef.EAction.eRecreate && _userMediaTemplate != null)
            {
                sqlCmds.AddRange(DropAllUserMediaCmds());
            }

            _dbHelper.ExecuteCommands(sqlCmds);
        }

        public void ClearTable(string tableName)
        {
            List<string> sqlCmds = new List<string>();
            if (_tableMap.TryGetValue(tableName, out var tableDef))
            {
                sqlCmds.AddRange(tableDef.GetSQLCommands(TableDef.EAction.eClear));
            }
            else
            {
                sqlCmds.Add(TableDef.clearTable(tableName));
            }
            _dbHelper.ExecuteCommands(sqlCmds);
        }

        private string getUserTableName(User? user)
        {
            if (user == null)
                return string.Empty;
            var idString = user.Id.ToString().Replace("-", "");
            return $"UserMedia_{idString}";
        }

        public List<string> allUserMediaTables()
        {
            return allTables((regex: "UserMedia_%", like: true));
        }

        public void ClearAllUserMedia()
        {
            var retVal = new List<string>();
            if (_userMediaTemplate == null)
                return;

            var tables = allUserMediaTables();

            foreach (var table in tables)
                ClearTable(table);
        }

        public List<string> DropAllUserMediaCmds()
        {
            var retVal = new List<string>();
            if (_userMediaTemplate == null)
                return retVal;

            var tables = allUserMediaTables();

            foreach (var table in tables)
                retVal.AddRange(DropTableCmds(table));

            return retVal;
        }

        public List<string> DropTableCmds(string tableName)
        {
            if (_tableMap.TryGetValue(tableName, out var tableDef))
            {
                var sqlCmds = tableDef.GetSQLCommands(TableDef.EAction.eDrop);
                return sqlCmds;
            }
            return new List<string>() { TableDef.dropTable(tableName) };
        }

        public void DropTable(string tableName)
        {
            var cmds = DropTableCmds(tableName);
            _dbHelper.ExecuteCommands(cmds);
        }

        public List<string> allTables((string regex, bool like)? regex = null)
        {
            var retVal = new List<string>();
            var clauses = new List<string>() { "type='table'", "name NOT LIKE 'sqlite_%'" };
            if (regex != null)
            {
                var clause = "name " + (regex.Value.like ? "LIKE" : "NOT LIKE") + " '" + regex.Value.regex + "'";
                clauses.Add(clause);
            }
            var sql = "SELECT name FROM sqlite_master " + DBHelper.JoinClauses(clauses);

            _dbHelper.ExecuteCommand(new SQLCmdDef(sql), statement =>
            {
                var row = statement.Current;
                retVal.Add(row.GetString(0));
                return true;
            });
            return retVal;
        }
    }
}
