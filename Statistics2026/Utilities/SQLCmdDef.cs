using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Data;
using System;
using System.Collections.Generic;
using static ServiceStack.Diagnostics;

namespace Statistics2026.Utilities
{
    public class SQLCmdDef
    {
        public string Statement = string.Empty;
        public List<(string name, object? value)>? Parameters = null;
        public bool FailureAllowed { get; set; } = false;

        public bool HasParameters()
        {
            return !Parameters.IsNullOrEmpty();
        }

        public static string CreateCmd( string from, List<string> columns, List<string>? joins, List<string>? whereClauses, List<string>? groupByClauses, List<string>? orderBy )
        {
            var lines = new List<string>() { "SELECT" };
            lines.AddRange( joinWithComma( columns ) );
            lines.Add( $"FROM {from}" );
            if( joins != null )
            {
                lines.AddRange( joins.Select( join => "    " + join ) );
            }

            if( whereClauses != null )
            {
                lines.Add( $"WHERE" );
                lines.AddRange( whereClauses.Select( ( clause, index ) => $"    {( index == 0 ? "   " : "AND" )} ( " + clause + " )" ) );
            }

            lines.AddRange( joinWithComma( groupByClauses, "GROUP BY" ) );
            lines.AddRange( joinWithComma( orderBy, "ORDER BY" ) );

            return string.Join( "\n", lines );
        }

        public SQLCmdDef( string from, List<string> columns, List<string>? joins, List<string>? whereClauses, List<string>? groupByClauses, List<string>? orderBy, List<(string name, object? value)>? parameters = null, bool failureAllowed = false )
        {
            Statement = CreateCmd( from, columns, joins, whereClauses, groupByClauses, orderBy );
            Parameters = parameters ?? [];
            FailureAllowed = failureAllowed;
        }

        public SQLCmdDef( string sql, bool failureAllowed = false )
        {
            Statement = sql;
            FailureAllowed = failureAllowed;
        }

        public SQLCmdDef( SQLCmdDef rhs )
        {
            Statement = rhs.Statement;
            Parameters = rhs.Parameters;
            FailureAllowed = rhs.FailureAllowed;
        }

        public SQLCmdDef( string sql, List<(string name, object? value)>? _parameters, bool failureAllowed = false )
        {
            Statement = sql;
            Parameters = _parameters;
            FailureAllowed = failureAllowed;
        }

        public void Replace( string from, string to )
        {
            Statement = Statement.Replace( from, to );
        }

        public object? GetParamValue( string paramName )
        {
            if( Parameters == null )
                return string.Empty;

            foreach( var param in Parameters )
            {
                if( param.name == "@Key" )
                {
                    return param.value ?? null;
                }
            }
            return null;
        }

        private void _Execute( IDatabaseConnection connection, StatisticsDB db, Func<IStatement, bool>? onStatement )
        {
            using( var statement = connection.PrepareStatement( Statement ) )
            {
                db.DBLogger().Debug( $"{Statement.GetHashCode():X16} - Executing SQL Statement: - {Statement}" );
                if( HasParameters() )
                {
                    db.DBLogger().Debug( $"{Statement.GetHashCode():X16} -    Parameters:" );
                    foreach( var (name, value) in Parameters! )
                    {
                        _ = db.TryBind( statement, name, value );
                        db.DBLogger().Debug( $"{Statement.GetHashCode():X16} -     {name} = {value}" );
                    }
                }

                if( onStatement == null )
                {
                    _ = statement.MoveNext();
                }
                else
                {
                    while( statement.MoveNext() )
                    {
                        if( !onStatement( statement ) )
                            break;
                        db.CancellationToken?.ThrowIfCancellationRequested();
                    }
                }
            }
        }
        public void Execute( IDatabaseConnection connection, StatisticsDB db, Func<IStatement, bool>? onStatement )
        {
            if( FailureAllowed )
            {
                try
                {
                    _Execute( connection, db, onStatement );
                }
                catch
                {
                }
            }
            else
            {
                _Execute( connection, db, onStatement );
            }
        }

        public static List<string> joinWithComma( List<string>? values, string? prefix = null )
        {
            List<string> retVal = [];
            if( values == null )
                return retVal;

            if( prefix != null )
            {
                retVal.Add( prefix );
            }

            for( int ii = 0; ii < values.Count; ++ii )
            {
                var currLines = values[ ii ].Split( '\n' ).ToList();
                for( var jj = 1; jj < currLines.Count; ++jj )
                {
                    var line = currLines[ jj ];
                    line = "    " + line;
                    currLines[ jj ] = line;
                }
                values[ ii ] = string.Join( "\n", currLines );
            }
            retVal.AddRange( values.Select( ( value, index ) => $"    {( index == 0 ? " " : "," )} {value}" ) );
            return retVal;
        }

        static public SQLCmdDef CreateSQLCmdDef( string sql, string paramListName, List<object> paramList )
        {
            var placeHolderNames = string.Join( ", ", paramList.Select( ( _, index ) => $"@p{index}" ).ToArray() );

            var sqlDefParams = new List<(string name, object? value)>();
            for( int ii = 0; ii < paramList.Count; ++ii )
            {
                sqlDefParams.Add( ($"@p{ii}", paramList[ ii ]) );
            }

            sql = sql.Replace( paramListName, placeHolderNames );
            var retVal = new SQLCmdDef( sql, sqlDefParams );
            return retVal;
        }
    }
}
