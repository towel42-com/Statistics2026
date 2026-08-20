using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Users;
using SQLitePCL.pretty;
using Statistics2026;
using Statistics2026.Api;
using Statistics2026.Configuration;
using Statistics2026.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;


namespace Statistics2026.Data
{
    public sealed class DBHelperFuncs
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

        private ILogger _logger = null;
        public IDatabaseConnection SQLConnection = null;

        public DBHelperFuncs()
        {
        }

        public DBHelperFuncs(string db_path, ILogger logger)
        {
            _logger = logger;

            string db_file_name = Path.Combine(db_path, "Statistics2026.db");
            SQLConnection = CreateConnection(db_file_name);
        }

        public void TryBind(IStatement statement, string name, int value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                _logger.Error($"Error Binding {name} to {value}");
            }
        }

        public void TryBind(IStatement statement, string name, long value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                _logger.Error($"Error Binding {name} to {value}");
            }
        }

        public void TryBind(IStatement statement, string name, bool value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                bindParam.Bind(value);
            }
            else
            {
                _logger.Error($"Error Binding {name} to {value}");
            }
        }

        public void TryBind(IStatement statement, string name, string value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                if (value == null)
                {
                    bindParam.BindNull();
                }
                else
                {
                    bindParam.Bind(value);
                }
            }
            else
            {
                _logger.Error($"Error Binding {name} to {value}");
            }
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

        private IDatabaseConnection CreateConnection(string db_file)
        {
            _logger.Info("CreateConnection : " + db_file);
            ConnectionFlags connectionFlags;

            //Logger.Info("Opening write _connection");
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

            _logger.Info("ConnectionCreated : " + db.GetHashCode());
            return db;
        }
        public IEnumerable<T> GetLibraryItems<T>(ILibraryManager libManager)
        {
            return GetUserItems<T>(null, libManager);
        }

        static public IEnumerable<T> GetUserItems<T>(User user, ILibraryManager libraryManager)
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

            return libraryManager.GetItemList(query).OfType<T>();
        }
    };

    public class TableColDef
    {
        public TableColDef(string columnName, string columnType, bool allowNull)
        {
            if (columnName == null || columnName == "")
                throw new ArgumentException("TableColDef: Must define the column name");
            if (columnType == null || columnType == "")
                throw new ArgumentException("TableColDef: Must define the column type");

            Name = columnName;
            Type = columnType;
            AllowNull = allowNull;
        }

        public override string ToString()
        {
            string retVal = $"{Name} {Type}";
            if (!AllowNull)
                retVal += " NOT NULL";
            return retVal;
        }

        public string Name { get; private set; }
        public string Type { get; private set; }
        public bool AllowNull { get; private set; }
    }

    public class TableDef
    {
        public TableDef(string name, List<TableColDef> cols, List<string> indexes, bool allColumnsIndexed = false)
        {
            Name = name;
            if (Name == null || Name == "")
                throw new ArgumentException("TableDef: Must define the table name");
            Columns = cols;
            if (Columns.Count() == 0)
                throw new ArgumentException("TableDef: Must define columns");

            if (allColumnsIndexed && indexes != null)
                throw new ArgumentException("TableDef: Can not set indexes with allColumnsIndexed set");

            if (indexes == null)
                Indexes = new List<string>();
            else
                Indexes = indexes;

            if (allColumnsIndexed)
            {
                Indexes.Clear();
                Columns.ForEach(col => Indexes.Add(col.Name));
            }

        }
        public string Name { get; private set; }
        public List<TableColDef> Columns { get; private set; }
        public List<string> Indexes { get; private set; }

        public List<string> GetSQLCreateCommands()
        {
            string sql = $"CREATE TABLE IF NOT EXISTS {Name} (\n";
            bool first = true;
            foreach (var col in Columns)
            {
                if (first)
                    sql += "      ";
                else
                    sql += "    , ";

                first = false;
                sql += col.ToString() + "\n";
            }

            sql += ");";

            var retVal = new List<string>();
            retVal.Add(sql);

            if (Indexes != null)
            {
                Indexes.ForEach(columnName =>
                {
                    if (columnName == null || columnName == "")
                        return;

                    var idxName = getIndexName(columnName);
                    sql = $"CREATE INDEX IF NOT EXISTS {idxName} on {Name} ({columnName});";
                    retVal.Add(sql);
                }
                );
            }

            return retVal;
        }

        public override string ToString()
        {
            return string.Join(";\n", GetSQLCreateCommands());
        }

        public void dropTable(IDatabaseConnection _connection)
        {
            string sql = $"DROP TABLE IF EXISTS {Name}";
            _connection.Execute(sql);
        }

        public string getIndexName(string name)
        {
            return $"idx_{Name}_{name}";
        }

        public void dropIndex(string name, IDatabaseConnection _connection)
        {
            var idxName = getIndexName(name);
            string sql = $"DROP INDEX IF EXISTS {idxName}";
            _connection.Execute(sql);
        }

        public void dropIndexes(IDatabaseConnection _connection)
        {
            Indexes.ForEach(colName => dropIndex(colName, _connection));
        }

        public void Execute(bool clearFirst, IDatabaseConnection _connection)
        {
            if (clearFirst)
            {
                dropTable(_connection);
                dropIndexes(_connection);
            }

            var cmds = GetSQLCreateCommands();
            foreach (var cmd in cmds)
            {
                _connection.Execute(cmd);
            }
        }
    }
}
