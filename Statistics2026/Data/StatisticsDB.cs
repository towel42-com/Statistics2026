using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace Statistics2026.Data
{
    public enum EMediaType
    {
        eMovie,
        eSeries,
        eEpisode
    }

    public sealed partial class StatisticsDB
    {
        private static readonly object _padlock = new object();
        private Dictionary<string, TableDef> _tableMap = new Dictionary<string, TableDef>();
        private List<TableDef> _tableList = new List<TableDef>();
        private TableDef? _userMediaTemplate = null;
        private EmbyInterfaces? _embyInterfaces = null;
        public bool AllTablesExisted { get; private set; } = false;
        public bool DataExists { get; private set; } = false;
        public bool UserDataExists { get; private set; } = false;

        DBHelper _dbHelper = new DBHelper();

        public static StatisticsDB GetInstance(EmbyInterfaces? embyInterfaces)
        {
            if (embyInterfaces == null)
                throw new ArgumentNullException("EmbyInterfaces is null.");

            lock (_padlock)
            {
                var retVal = new StatisticsDB(embyInterfaces);
                embyInterfaces._logger.Debug("Statistics2026 : New Instance Created : " + retVal.GetHashCode());
                return retVal;
            }
        }

        private StatisticsDB()
        {
            ConstructTableList();
        }

        private StatisticsDB(EmbyInterfaces embyInterfaces)
        {
            if (embyInterfaces == null)
                throw new ArgumentNullException("embyInterfaces is null.");

            ConstructTableList();

            embyInterfaces._logger?.Debug("Statistics2026 : Creating Database");

            _embyInterfaces = embyInterfaces;
            _dbHelper = new DBHelper(_embyInterfaces);

            embyInterfaces._logger?.Debug("Statistics2026 : Finished Creating Database");
            ComputeDBState();
        }

        ~StatisticsDB()
        {
        }

        private void CheckIsValid(ECheckType checkType)
        {
            if (Statistics2026.Plugin.Instance == null)
                throw new ArgumentNullException("Statistics2026.Plugin.Instance");

            if (Statistics2026.Plugin.Instance.Configuration == null)
                throw new ArgumentNullException("Statistics2026.Plugin.Instance.Configuration");

            if (checkType == ECheckType.eInit)
                _dbHelper.CheckIsValid(ECheckLevel.eInterfaces | ECheckLevel.eThrowOnFailure);
            else if (checkType == ECheckType.eReport)
                _dbHelper.CheckIsValid(ECheckLevel.eInterfaces | ECheckLevel.eConnection | ECheckLevel.eThrowOnFailure);
            else if (checkType == ECheckType.eUpdate)
                _dbHelper.CheckIsValid(ECheckLevel.eAllWithThrow);

            if (_embyInterfaces != null && (checkType == ECheckType.eReport) && _embyInterfaces.IsStatistics2026TaskRunning())
            {
                throw new Exception("Statistics 2026 task is running");
            }
        }

        public void Initialize(CancellationToken? cancellationToken, IProgress<double>? progress, bool reset = false)
        {
            SetCancellationToken(cancellationToken, progress);
            CreateTables(reset ? TableDef.EAction.eRecreate : TableDef.EAction.eCreate);
        }

        public void ResetCancellationToken()
        {
            if (_dbHelper != null)
            {
                _dbHelper.CancellationToken = null;
                _dbHelper.Progress = null;
            }
        }

        private void SetCancellationToken(CancellationToken? cancellationToken, IProgress<double>? progress)
        {
            if (_dbHelper != null)
            {
                _dbHelper.CancellationToken = cancellationToken;
                _dbHelper.Progress = progress;
            }
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
                        new TableColDef( "ItemId", "TEXT", false, true ),
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
                        new TableColDef( "ItemId", "TEXT", false, true ),
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
                        new TableColDef( "UserId", "TEXT", false, true ),
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
                            new TableColDef( "ItemId", "TEXT", false, true),
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
                )
            };

            _tableList.Add(
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
                ));
            _tableList[_tableList.Count - 1].DeprecatedTable = true;

            _tableList.Add(
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
                ));
            _tableList[_tableList.Count - 1].DeprecatedTable = true;

            _tableList.Add(
                new TableDef("UserVideoList",
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
            );
            _tableList[_tableList.Count - 1].DeprecatedTable = true;

            _userMediaTemplate = new TableDef("UserMedia_<USER_ID>",
                    new List<TableColDef>()
                    {
                        new TableColDef( "UserId", "TEXT", false ), // user
                        new TableColDef( "ItemId", "TEXT", false, true ), // video item
                        new TableColDef( "Name", "TEXT", true ), // Name of the show to reduce joins
                        new TableColDef( "IsPlayed", "BOOLEAN", true ),
                        new TableColDef( "PlayCount", "INT", true ),
                        new TableColDef( "LastPlayedDate", "DATETIME", true ),
                        new TableColDef( "StartTickPos", "INT", true){DeprecatedColumn= true},
                        new TableColDef( "EndTickPos", "INT", true ){DeprecatedColumn= true},
                        new TableColDef( "TotalTicksPlayed", "INT", true ){FormerColumnName="TotalTicks"},
                        new TableColDef( "IsEpisode", "BOOLEAN", true ),
                        new TableColDef( "NumEpisodes", "BOOLEAN", true ), // for multi episode media
                        new TableColDef( "IsTVSpecial", "BOOLEAN", true ),
                        new TableColDef( "SeriesId", "TEXT", true ) // if episode add seriesid
                    }
                );

            foreach (var tableDef in _tableList)
            {
                if (tableDef.DeprecatedTable)
                    continue;

                _tableMap[tableDef.Name] = tableDef;
            }
        }

        private void ComputeDBState()
        {
            if (Plugin.Instance == null)
                throw new ArgumentNullException("Plugin.Instance is null");

            if (Plugin.Instance.DBState != null)
                return;

            var aTableMissing = false;
            var aTableNeedsData = false;
            foreach (var tableDef in _tableList)
            {
                if (!tableDef.DeprecatedTable)
                {
                    var tableMissing = !_dbHelper.TableExists(tableDef.Name);
                    var tableNeedsData = true;
                    if (!tableMissing)
                    {
                        var sql = $"SELECT COUNT(*) FROM {tableDef.Name} LIMIT 1";

                        _dbHelper.ExecuteCommand(new SQLCmdDef(sql), statement =>
                        {
                            var row = statement.Current;
                            tableNeedsData = row.GetInt64(0) == 0;
                            return false;
                        }
                        );
                    }
                    aTableMissing = aTableMissing || tableMissing;
                    aTableNeedsData = aTableNeedsData || tableNeedsData;
                    if (aTableMissing && aTableNeedsData)
                        break;
                }
            }

            if (aTableMissing)
                UpdateDBState(~EDBState.eSystemTablesCreated, true);
            else
                UpdateDBState(EDBState.eSystemTablesCreated);

            if (aTableNeedsData)
                UpdateDBState(~EDBState.eSystemDataInitialized, true);
            else
                UpdateDBState(EDBState.eSystemDataInitialized);

            ComputeUserDataDBState();
        }

        public void UpdateDBState(EDBState? state, bool andValue = false)
        {
            EDBState? value = EDBState.eEmpty;

            if (Plugin.Instance != null && Plugin.Instance.DBState != null)
                value = Plugin.Instance.DBState;

            if (andValue)
                value = value & state;
            else
                value = value | state;
            SetDBState(value);
        }

        private void SetDBState(EDBState? state)
        {
            if (Plugin.Instance == null)
                throw new ArgumentNullException("Plugin.Instance is null");

            Plugin.Instance.DBState = state;
            var config = Plugin.Instance.Configuration;
            config.dbStateOK = (Plugin.Instance.DBState == EDBState.eFullyInitialized);
            Plugin.Instance.UpdateConfiguration(config);
        }

        private void CreateTables(TableDef.EAction action)
        {
            if (action != TableDef.EAction.eRecreate && action != TableDef.EAction.eCreate)
                throw new InvalidEnumArgumentException($"Action must be {TableDef.EAction.eRecreate} or {TableDef.EAction.eCreate}");

            if (Plugin.Instance == null)
                throw new NullReferenceException($"Plugin.Instance is null");

            if (Plugin.Instance.DBState == null)
            {
                SetDBState(EDBState.eEmpty);
            }

            var sqlCmds = new List<SQLCmdDef>();
            foreach (var tableDef in _tableList)
            {
                sqlCmds.AddRange(tableDef.GetSQLCommands(action));
            }

            var config = Statistics2026.Plugin.Instance!.Configuration;
            if (config.resetPlayCount && action == TableDef.EAction.eRecreate && _userMediaTemplate != null)
            {
                sqlCmds.AddRange(DropAllUserMediaCmds());
                UpdateDBState(~EDBState.eUserTablesCreated & ~EDBState.eUserDataInitialized, true);
            }

            _dbHelper.ExecuteCommands(sqlCmds);

            UpdateDBState(EDBState.eSystemTablesCreated);
            InitUserWatchData();
        }

        public void ClearTable(string tableName)
        {
            List<SQLCmdDef> sqlCmds = new List<SQLCmdDef>();
            if (_tableMap.TryGetValue(tableName, out var tableDef))
            {
                sqlCmds.AddRange(tableDef.GetSQLCommands(TableDef.EAction.eClear));
            }
            else
            {
                sqlCmds.Add(new SQLCmdDef(TableDef.clearTable(tableName)));
            }
            _dbHelper.ExecuteCommands(sqlCmds);
        }

        public string getUserTableName(User? user)
        {
            if (user == null)
                return string.Empty;
            return getUserTableName(user.Id.ToString());
        }

        public string getUserTableName(string userId)
        {
            userId = userId.ToString().Replace("-", "");

            return $"UserMedia_{userId}";
        }

        public List<SQLCmdDef> DropTableCmds(string tableName)
        {
            if (_tableMap.TryGetValue(tableName, out var tableDef))
            {
                var sqlCmds = tableDef.GetSQLCommands(TableDef.EAction.eDrop);
                return sqlCmds;
            }
            return new List<SQLCmdDef>() { new SQLCmdDef(TableDef.dropTable(tableName)) };
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
