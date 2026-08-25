using Dapper;
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
using Microsoft.Data.Sqlite;
using Statistics2026;
using Statistics2026.Api;
using Statistics2026.Configuration;
using Statistics2026.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace Statistics2026.Data
{

    public class DBHelperFuncs : IDisposable
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

        private ILogger? _logger = null;
        public SQLConnection _connection = new SQLConnection();

        public async Task Execute(string sql)
        {
            _ = _connection.Execute(sql);
        }

        public DBHelperFuncs()
        {
        }


        public void Dispose()
        {
            _connection?.Dispose();
            _logger?.Debug("StatisticsData : DB Connection Closed");
        }

        public DBHelperFuncs(string db_path, ILogger logger)
        {
            _logger = logger;

            string db_file_name = Path.Combine(db_path, "Statistics2026.db");
            _logger.Debug("CreateConnection : " + db_file_name);
            _connection.CreateConnection(db_file_name);

            _logger.Debug("ConnectionCreated : " + GetHashCode());
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

        public IEnumerable<T> GetLibraryItems<T>(ILibraryManager libManager)
        {
            return GetUserItems<T>(null, libManager);
        }

        static public IEnumerable<T> GetUserItems<T>(User? user, ILibraryManager libraryManager)
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

        public SqliteTransaction BeginTransaction()
        {
            return _connection.BeginTransaction();
        }

        public Task WaitAsync()
        {
            return _connection.WaitAsync();
        }

        public int Release()
        {
            return _connection.Release();
        }

        public SqliteCommand CreateCommand()
        {
            return _connection.CreateCommand();
        }
        public override int GetHashCode()
        {
            return _connection.GetHashCode();
        }

    };

    public static class DBHelperExtFuncs
    {
        public static Task<int> ExecuteAsync(this SQLConnection sqlConnection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            if (sqlConnection == null || sqlConnection._connection == null)
                throw new ArgumentNullException("null sqlConnection");

            var commandDef = new CommandDefinition(sql, param, transaction, commandTimeout, commandType);

            return sqlConnection._connection.ExecuteAsync(commandDef);
        }

        public static Task<int> ExecuteAsync(this DBHelperFuncs dbHelper, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            if (dbHelper == null || dbHelper._connection == null)
                throw new ArgumentNullException("null dbHelper");

            return dbHelper._connection.ExecuteAsync(sql, param, transaction, commandTimeout, commandType);
        }

        public static Task<DbDataReader> ExecuteReaderAsync(this SQLConnection sqlConnection, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            if (sqlConnection == null || sqlConnection._connection == null)
                throw new ArgumentNullException("null sqlConnection");

            var commandDef = new CommandDefinition(sql, param, transaction, commandTimeout, commandType);

            return sqlConnection._connection.ExecuteReaderAsync(commandDef);
        }

        public static Task<DbDataReader> ExecuteReaderAsync(this DBHelperFuncs dbHelper, string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            if (dbHelper == null || dbHelper._connection == null)
                throw new ArgumentNullException("null dbHelper");

            return dbHelper._connection.ExecuteReaderAsync(sql, param, transaction, commandTimeout, commandType);
        }

    }
}
