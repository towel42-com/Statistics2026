using System;
using System.Collections.Generic;
using System.Linq;

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
        public enum EAction
        {
            eRecreate, // drops first then recreates
            eCreate,   // only calls create
            eDrop,      // only drops the table and indexes
            eClear,      // deletes all from table
        }

        public TableDef(string name, List<TableColDef> cols, List<string>? indexes = null)
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

        private List<string> createTable()
        {
            var tableName = Name;
            string sql = $"CREATE TABLE IF NOT EXISTS {tableName} (\n";
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

            var retVal = new List<string>() { sql };

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
            return string.Join(";\n", createTable());
        }

        public static string clearTable(string tableName)
        {
            string sql = $"DELETE FROM {tableName}";
            return sql;
        }

        private string clearTable()
        {
            return clearTable(Name);
        }

        public static string dropTable(string tableName)
        {
            string sql = $"DROP TABLE IF EXISTS {tableName}";
            return sql;
        }

        private string dropTable()
        {
            return dropTable(Name);
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

        public List<string> GetSQLCommands(EAction action)
        {
            var retVal = new List<string>();
            switch (action)
            {
                case EAction.eClear:
                    retVal.Add(clearTable());
                    break;
                case EAction.eDrop:
                    retVal.Add(dropTable());
                    break;
                case EAction.eCreate:
                    retVal.AddRange(createTable());
                    break;
                case EAction.eRecreate:
                    retVal.AddRange(GetSQLCommands(EAction.eDrop));
                    retVal.AddRange(GetSQLCommands(EAction.eCreate));
                    break;
            }
            return retVal;
        }

        public string Name { get; private set; }
        public List<TableColDef> Columns { get; private set; }
        public List<string> Indexes { get; private set; }
    }
}
