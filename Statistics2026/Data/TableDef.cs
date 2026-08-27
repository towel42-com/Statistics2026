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
using SQLitePCL.pretty;

namespace Statistics2026.Data
{
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
        public TableDef(string name, List<TableColDef> cols, List<string>? indexes)
        {
            Name = name;
            if (Name == null || Name == "")
                throw new ArgumentException("TableDef: Must define the table name");
            Columns = cols;
            if (Columns.Count() == 0)
                throw new ArgumentException("TableDef: Must define columns");

            if (indexes == null)
            {
                Indexes = new List<string>();
                Columns.ForEach(col => Indexes.Add(col.Name));
            }
            else
                Indexes = indexes;
        }

        private List<string> GetSQLCreateCommands()
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

        private string dropTable()
        {
            string sql = $"DROP TABLE IF EXISTS {Name}";
            return sql;
        }

        private string getIndexName(string name)
        {
            return $"idx_{Name}_{name}";
        }

        private string dropIndex(string name)
        {
            var idxName = getIndexName(name);
            string sql = $"DROP INDEX IF EXISTS {idxName}";
            return sql;
        }

        private List< string > dropIndexes()
        {
            var retVal = new List<string>();
            Indexes.ForEach(colName => retVal.Add(dropIndex(colName)));
            return retVal;
        }

        public List<string> GetSQLCommands(bool clearFirst)
        {
            var retVal = new List<string>();
            if (clearFirst)
            {
                retVal.Add(dropTable());
                retVal.AddRange(dropIndexes());
            }

            retVal.AddRange(GetSQLCreateCommands());

            return retVal;
        }

        public string Name { get; private set; }
        public List<TableColDef> Columns { get; private set; }
        public List<string> Indexes { get; private set; }
    }
}
