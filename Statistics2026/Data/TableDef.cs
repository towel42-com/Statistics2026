using System;
using System.Collections.Generic;
using System.Linq;

namespace Statistics2026.Data
{
    public class TableColDef
    {
        public TableColDef( string columnName, string columnType, bool allowNull, bool isPrimaryIndex = false )
        {
            if( columnName == null || columnName == "" )
                throw new ArgumentException( "TableColDef: Must define the column name" );
            if( columnType == null || columnType == "" )
                throw new ArgumentException( "TableColDef: Must define the column type" );

            Name = columnName;
            Type = columnType;
            AllowNull = allowNull;
            IsPrimaryIndex = isPrimaryIndex;
        }

        public static string getIndexName( string tableName, string columnName )
        {
            return $"idx_{tableName}_{columnName}";
        }

        private string getIndexName( string tableName )
        {
            return getIndexName( tableName, Name );
        }

        public string createIndex( string tableName )
        {
            var idxName = getIndexName( tableName );
            var sql = $"CREATE INDEX IF NOT EXISTS {idxName} on {tableName} ({Name});";
            return sql;
        }

        public List<SQLCmdDef> AlterCmds( string tableName )
        {
            var retVal = new List<SQLCmdDef>();
            if( DeprecatedColumn )
            {
                var idxName = getIndexName( tableName );
                retVal.Add( new SQLCmdDef( $"DROP INDEX IF EXISTS {idxName}", true ) );
                retVal.Add( new SQLCmdDef( $"ALTER TABLE {tableName} DROP COLUMN {Name}", true ) );
            }
            else if( FormerColumnName != null )
            {
                retVal.Add( new SQLCmdDef( $"ALTER TABLE {tableName} RENAME COLUMN {FormerColumnName} TO {Name}", true ) );
            }
            else
            {
                retVal.Add( new SQLCmdDef( $"ALTER TABLE {tableName} ADD COLUMN {ToString( true )}", true ) );
            }

            return retVal;
        }

        public string ToString( bool alter )
        {
            var retVal = $"{Name} {Type}";
            if( !alter && !AllowNull )
                retVal += " NOT NULL";
            if( IsPrimaryIndex )
                retVal += " PRIMARY KEY";
            return retVal;
        }
        public override string ToString()
        {
            return ToString( false );
        }

        public string Name { get; private set; } = string.Empty;
        public string Type { get; private set; } = string.Empty;
        public bool AllowNull { get; private set; } = false;
        public bool IsPrimaryIndex { get; private set; } = false;
        public bool DeprecatedColumn { get; set; } = false;
        public string? FormerColumnName { get; set; } = null;
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

        public TableDef( string name, List<TableColDef> cols, List<string>? indexes = null )
        {
            Name = name;
            if( Name == null || Name == "" )
                throw new ArgumentException( "TableDef: Must define the table name" );
            Columns = cols;
            if( Columns.Count() == 0 )
                throw new ArgumentException( "TableDef: Must define columns" );

            if( indexes == null )
            {
                Indexes = [];
                Columns.ForEach( col =>
                {
                    if( !col.IsPrimaryIndex )
                        Indexes.Add( col.Name );
                } );

            }
            else
            {
                Indexes = indexes;
            }
        }

        private List<SQLCmdDef> createTable()
        {
            var tableName = Name;
            var sql = $"CREATE TABLE IF NOT EXISTS {tableName} (\n";
            var first = true;
            foreach( var col in Columns )
            {
                if( col.DeprecatedColumn )
                    continue;
                //if (!col.IsPrimaryIndex)
                //    continue;

                if( first )
                    sql += "      ";
                else
                    sql += "    , ";

                first = false;
                sql += col.ToString() + "\n";
            }

            sql += ");";

            List<SQLCmdDef> retVal = [ new( sql ) ];

            Columns.ForEach( column =>
            {
                retVal.AddRange( column.AlterCmds( Name ) );
            }
            );

            Indexes?.ForEach( columnName =>
                {
                    if( columnName == null || columnName == "" )
                        return;

                    TableColDef? column = null;
                    foreach( var col in Columns )
                    {
                        if( col.Name == columnName )
                        {
                            column = col;
                            break;
                        }
                    }

                    if( column == null )
                        throw new Exception( $"Index's column '{columnName}' does not exist" );

                    if( column.DeprecatedColumn )
                        return;

                    retVal.Add( new SQLCmdDef( column.createIndex( Name ) ) );
                }
                );

            return retVal;
        }

        public override string ToString()
        {
            List<string> tmp = [];
            createTable().ForEach( cmd => { tmp.Add( cmd.ToString() ); } );
            return string.Join( ";\n", tmp );
        }

        public static string clearTable( string tableName )
        {
            var sql = $"DELETE FROM {tableName}";
            return sql;
        }

        private string clearTable()
        {
            return clearTable( Name );
        }

        public static string dropTable( string tableName )
        {
            var sql = $"DROP TABLE IF EXISTS {tableName}";
            return sql;
        }

        private string dropTable()
        {
            return dropTable( Name );
        }

        public List<SQLCmdDef> GetSQLCommands( EAction action )
        {
            var retVal = new List<SQLCmdDef>();
            switch( action )
            {
                case EAction.eClear:
                    retVal.Add( new SQLCmdDef( clearTable() ) );
                    break;
                case EAction.eDrop:
                    retVal.Add( new SQLCmdDef( dropTable() ) );
                    break;
                case EAction.eCreate:
                {
                    if( DeprecatedTable )
                        retVal.Add( new SQLCmdDef( dropTable() ) );
                    else
                        retVal.AddRange( createTable() );
                }

                break;
                case EAction.eRecreate:
                {
                    retVal.AddRange( GetSQLCommands( EAction.eDrop ) );
                    retVal.AddRange( GetSQLCommands( EAction.eCreate ) );
                }

                break;
            }

            return retVal;
        }

        public List<string> ColumnNames()
        {
            var retVal = new List<string>();
            foreach( var col in Columns )
            {
                if( col.DeprecatedColumn )
                    continue;
                retVal.Add( col.Name );
            }

            return retVal;
        }

        public string Name { get; private set; }
        public List<TableColDef> Columns { get; private set; }
        public List<string> Indexes { get; private set; }
        public bool DeprecatedTable { get; set; } = false;
    }
}
