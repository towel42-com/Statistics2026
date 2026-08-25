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
    public class SQLConnection : IDisposable
    {
        public SqliteConnection? _connection { get; private set; } = null;
        public SemaphoreSlim _lock { get; private set; } = new System.Threading.SemaphoreSlim(1, 1);

        public SQLConnection()
        {
        }
        public void Dispose()
        {
            _connection?.Dispose();
            _lock?.Dispose();
        }

        public void CreateConnection(string db_file)
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = db_file,
                Mode = SqliteOpenMode.ReadWriteCreate, // Matches Create | ReadWrite
                Cache = SqliteCacheMode.Private       // Matches PrivateCache
            };
            connectionStringBuilder.Pooling = false;

            _connection = new SqliteConnection(connectionStringBuilder.ConnectionString);

            try
            {
                var queries = new List<string>
                {
                    "PRAGMA synchronous=Normal",
                    "PRAGMA temp_store=file"
                };

                using (var command = _connection.CreateCommand())
                {

                    command.CommandText = string.Join(";", queries);
                    command.ExecuteNonQuery();
                }
            }
            catch
            {
                _connection.Dispose();
                throw;
            }
        }

        public async Task Execute(string sql)
        {
            if (_connection == null)
                throw new ArgumentNullException("_connection is null");

            await _lock.WaitAsync();
            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = sql;
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }

        public SqliteTransaction BeginTransaction()
        {
            if (_connection == null)
                throw new ArgumentNullException("_connection is null");

            return _connection.BeginTransaction();
        }

        public Task WaitAsync()
        {
            return _lock.WaitAsync();
        }

        public int Release()
        {
            return _lock.Release();
        }

        public SqliteCommand CreateCommand()
        {
            if (_connection == null)
                throw new ArgumentNullException("_connection is null");

            return _connection.CreateCommand();
        }
        public override int GetHashCode()
        {
            return _connection?.GetHashCode() ?? 0;
        }

    }
}
