using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace Statistics2026.Data
{
    public sealed class DBHelper
    {
        private static string[] _datetimeFormats = new string[] {
            "THHmmssK",
            "THHmmK",
            "HH:mm:ss.FFFFFFFK",
            "HH:mm:ssK",
            "HH:mmK",
            "yyyy-MM-dd HH:mm:ss.FFFFFFFK", /* NOTE: UTC default (5). */
            "yyyy-MM-dd HH:mm:ssK",
            "yyyy-MM-dd HH:mmK",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
            "yyyy-MM-ddTHH:mmK",
            "yyyy-MM-ddTHH:mm:ssK",
            "yyyyMMddHHmmssK",
            "yyyyMMddHHmmK",
            "yyyyMMddTHHmmssFFFFFFFK",
            "THHmmss",
            "THHmm",
            "HH:mm:ss.FFFFFFF",
            "HH:mm:ss",
            "HH:mm",
            "yyyy-MM-dd HH:mm:ss.FFFFFFF", /* NOTE: Non-UTC default (19). */
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyyMMddHHmmss",
            "yyyyMMddHHmm",
            "yyyyMMddTHHmmssFFFFFFF",
            "yyyy-MM-dd",
            "yyyyMMdd",
            "yy-MM-dd"
        };
        private static string _datetimeFormatUtc = _datetimeFormats[5];
        private static string _datetimeFormatLocal = _datetimeFormats[19];

        private EmbyManagers? _embyManagers;

        private IDatabaseConnection? Connection { get; set; } = null;
        public CancellationToken? CancellationToken { get; set; } = null;
        public DBHelper()
        {
            _embyManagers = null;
        }

        private void CheckIsValid()
        {
            if (_embyManagers == null)
                throw new ArgumentNullException("_embyManagers");
        }

        public DBHelper(EmbyManagers embyManagers)
        {
            if (embyManagers == null)
                throw new ArgumentNullException("embyManagers is null.");

            _embyManagers = embyManagers;
            string db_file_name = Path.Combine(_embyManagers._configManager.ApplicationPaths.DataPath, "Statistics2026.db");
            CreateConnection(db_file_name);
        }

        public bool isValid()
        {
            if (Connection == null)
                return false;
            if (_embyManagers == null || _embyManagers._logger == null)
                return false;
            return true;
        }

        ~DBHelper()
        {
            _embyManagers?._logger?.Debug("StatisticsData : Cleaning up");
            if (Connection != null)
            {
                Connection.Close();
                _embyManagers?._logger?.Debug("StatisticsData : DB Connection Closed");
            }
        }

        public bool TryBind<T>(IStatement statement, string name, T? value)
        {
            IBindParameter bindParam;
            if (!statement.BindParameters.TryGetValue(name, out bindParam))
            {
                _embyManagers!._logger?.Error($"Error Binding {name} to {value}");
                return false;
            }

            if (value == null)
            {
                bindParam.BindNull();
                return true;
            }


            switch (value)
            {
                case string s:
                    bindParam.Bind(s);
                    break;
                case int i:
                    bindParam.Bind(i);
                    break;
                case long l:
                    bindParam.Bind(l);
                    break;
                case double d:
                    bindParam.Bind(d);
                    break;
                case float f:
                    bindParam.Bind((double)f);
                    break;
                case short sh:
                    bindParam.Bind((int)sh);
                    break;
                case byte[] ba:
                    bindParam.Bind(ba);
                    break;
                case DateTime dt:
                    bindParam.Bind(dt.ToString("o", CultureInfo.InvariantCulture));
                    break;
                case bool b:
                    // store bool as integer 0/1
                    bindParam.Bind(b ? 1 : 0);
                    break;
                default:
                    // Fallback: convert to string (covers enums, GUID, etc.)
                    bindParam.Bind(value.ToString() ?? string.Empty);
                    break;
            }

            return true;
        }

        public bool ExecuteCommand(SQLCmdDef cmd, Func<IStatement, bool>? onStatement = null)
        {
            var cmds = new List<SQLCmdDef>() { cmd };
            return ExecuteCommands(cmds, onStatement);
        }

        public bool ExecuteCommands(List<SQLCmdDef> cmds, Func<IStatement, bool>? onStatement = null)
        {
            if (Connection == null)
                throw new ArgumentNullException("Connection");

            lock (Connection)
            {
                try
                {
                    Connection.RunInTransaction(connection =>
                    {
                        foreach (var cmd in cmds)
                        {
                            CancellationToken?.ThrowIfCancellationRequested();
                            using (var statement = connection.PrepareStatement(cmd.Statement))
                            {
                                if (cmd.HasParameters())
                                {
                                    foreach (var param in cmd.Parameters!)
                                    {
                                        TryBind(statement, param.name, param.value);
                                    }
                                }

                                if (onStatement == null)
                                {
                                    statement.MoveNext();
                                }
                                else
                                {
                                    while (statement.MoveNext())
                                    {
                                        if (!onStatement(statement))
                                            break;
                                        CancellationToken?.ThrowIfCancellationRequested();
                                    }
                                }
                            }
                        }
                    });
                }
                catch (Exception)
                {
                    throw;
                }
            }

            return true;
        }

        public bool ExecuteCommands(List<string> cmds)
        {
            List<SQLCmdDef> cmdDefs = new List<SQLCmdDef>();
            foreach (var cmd in cmds)
            {
                CancellationToken?.ThrowIfCancellationRequested();
                cmdDefs.Add(new SQLCmdDef(cmd));
            }
            return ExecuteCommands(cmdDefs);
        }

        private string GetDateTimeKindFormat(DateTimeKind kind)
        {
            return (kind == DateTimeKind.Utc) ? _datetimeFormatUtc : _datetimeFormatLocal;
        }

        public DateTime ReadDateTime(string dateText)
        {
            return DateTime.ParseExact(
                dateText,
                _datetimeFormats,
                DateTimeFormatInfo.InvariantInfo,
                DateTimeStyles.None).ToUniversalTime();
        }

        public string ToDateTimeParamValue(DateTime dateValue)
        {
            var kind = DateTimeKind.Utc;
            if (dateValue.Kind == DateTimeKind.Unspecified) // if Unspecified force UTC
            {
                return DateTime.SpecifyKind(dateValue, kind).ToString(GetDateTimeKindFormat(kind), CultureInfo.InvariantCulture);
            }
            else
            {
                return dateValue.ToString(GetDateTimeKindFormat(dateValue.Kind), CultureInfo.InvariantCulture);
            }
        }

        private void CreateConnection(string db_file)
        {
            CheckIsValid();

            _embyManagers!._logger?.Debug("CreateConnection : " + db_file);
            ConnectionFlags connectionFlags;

            //_embyManagers!._logger?.Debug("Opening write _connection");
            connectionFlags = ConnectionFlags.Create;
            connectionFlags |= ConnectionFlags.ReadWrite;
            connectionFlags |= ConnectionFlags.PrivateCache;
            connectionFlags |= ConnectionFlags.NoMutex;

            SQLiteDatabaseConnection db = SQLite3.Open(db_file, connectionFlags, null, true);

            try
            {
                var queries = new List<string>
                {
                    //"PRAGMA cache size=-10000"
                    //"PRAGMA read_uncommitted = true",
                    "PRAGMA synchronous=Normal",
                    "PRAGMA temp_store=file"
                };

                db.ExecuteAll(string.Join(";", queries.ToArray()));
            }
            catch
            {
                throw;
            }

            Connection = db;
            _embyManagers!._logger?.Debug("ConnectionCreated : " + Connection.GetHashCode());
        }

        public IEnumerable<T> GetLibraryItems<T>()
        {
            CheckIsValid();
            return GetUserItems<T>(null, _embyManagers!._libraryManager);
        }

        static public IEnumerable<T> GetUserItems<T>(User? user, ILibraryManager libManager)
        {
            var query = new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(T).Name },
                Recursive = true,
                IsVirtualItem = false,
                DtoOptions = new DtoOptions(true)
                {
                    ImageTypes = new[] { ImageType.Thumb, ImageType.Thumbnail },
                    EnableImages = true
                }
            };

            return libManager.GetItemList(query).OfType<T>();
        }

        public static string FormatTicks(long ticks)
        {
            var runtime = new RunTime(ticks);
            return runtime.ToLongString();
        }

        public static string JoinClauses(List<string> clauses)
        {
            if (clauses.IsNullOrEmpty())
                return String.Empty;

            for (int ii = 0; ii < clauses.Count; ++ii)
            {
                clauses[ii] = $"( {clauses[ii]} )";
            }

            return "WHERE " + string.Join(" AND ", clauses) + " ";
        }
    };
}
