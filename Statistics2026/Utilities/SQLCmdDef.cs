using MediaBrowser.Model.Logging;
using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Data;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Utilities;

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

            if( whereClauses != null && whereClauses.Count() > 0 )
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

            foreach( (string name, object? value) param in Parameters )
            {
                if( param.name == "@Key" )
                {
                    return param.value ?? null;
                }
            }
            return null;
        }

        private static readonly TrackingSemaphoreSlim _dbLock = new TrackingSemaphoreSlim( 1, 1 );
        private static ConcurrentStack<StringBuilder> _dbLockStacks = new();

        public static void setWarningLogger( Action<string> warnLogger )
        {
            _dbLock.setWarningLogger( warnLogger );
        }

        public static void lockDBSynchronous( Statistics2026DB? db, Action onAction )
        {
            _dbLock.Wait();
            try
            {
                _dbLockStacks.Push( HelperFuncs.getStackTrace() );
                onAction();
                _dbLockStacks.TryPop( out _ );
            }
            finally
            {
                _dbLock.Release();
            }
        }

        public static async Task LockDBAsync( Statistics2026DB? db, Func<Task> onAction )
        {
            await _dbLock.WaitAsync().ConfigureAwait( false );
            try
            {
                await onAction();
            }
            finally
            {
                _dbLock.Release();
            }
        }

        private Task _ExecuteAsync( IDatabaseConnection connection, Statistics2026DB db, Func<IStatement, Task<bool>>? onStatement, bool locked )
        {
            async Task runCommandAsync( IDatabaseConnection connection, Statistics2026DB db, Func<IStatement, Task<bool>>? onStatement )
            {
                using( IStatement statement = connection.PrepareStatement( Statement ) )
                {
                    StringBuilder? msgs = null;
                    if( db.dbLoggerEnabled() )
                        msgs = new StringBuilder();
                    if( HasParameters() )
                    {
                        msgs?.AppendLine( $"{Statement.GetHashCode():X16} -    Parameters:" );
                        foreach( (string? name, object? value) in Parameters! )
                        {
                            _ = db.TryBind( statement, name, value );
                            msgs?.AppendLine( $"{Statement.GetHashCode():X16} -     {name} = {value}" );
                        }
                    }

                    db.dbLogMultiLine( $"{Statement.GetHashCode():X16} - Executing SQL Statement: - {Statement}", msgs, LogSeverity.Debug, true );

                    try
                    {
                        if( onStatement == null )
                        {
                            _ = statement.MoveNext();
                        }
                        else
                        {
                            while( statement.MoveNext() )
                            {
                                bool continueLoop = await onStatement( statement );
                                if( !continueLoop )
                                    break;
                                db.CancellationToken?.ThrowIfCancellationRequested();
                            }
                        }
                    }
                    catch( Exception ex )
                    {
                        db.Logger().Error( $"Exception thrown in Execute: {ex}" );
                    }
                }
            }

            if( locked )
            {
                return runCommandAsync( connection, db, onStatement );
            }
            else
            {
                return LockDBAsync( db, () => { return runCommandAsync( connection, db, onStatement ); }  );
            }
        }

        public async Task ExecuteAsync( IDatabaseConnection connection, Statistics2026DB db, Func<IStatement, Task<bool>>? onStatement, bool locked )
        {
            if( FailureAllowed )
            {
                try
                {
                    await _ExecuteAsync( connection, db, onStatement, locked );
                }
                catch
                {
                }
            }
            else
            {
                await _ExecuteAsync( connection, db, onStatement, locked );
            }
        }

        public static List<string> joinWithComma( List<string>? values, string? prefix = null )
        {
            List<string> retVal = [];
            if( values == null || values.Count() == 0 )
                return retVal;

            if( prefix != null )
            {
                retVal.Add( prefix );
            }

            for( int ii = 0; ii < values.Count; ++ii )
            {
                List<string> currLines = values[ ii ].Split( '\n' ).ToList();
                for( int jj = 1; jj < currLines.Count; ++jj )
                {
                    string line = currLines[ jj ];
                    line = "    " + line;
                    currLines[ jj ] = line;
                }
                values[ ii ] = string.Join( "\n", currLines );
            }
            retVal.AddRange( values.Select( ( value, index ) => $"    {( index == 0 ? " " : "," )} {value}" ) );
            return retVal;
        }

        static public SQLCmdDef CreateSQLCmdDef( string sql, string paramListName, List<object> paramList, List<(string name, object? value)>? parameters = null )
        {
            string subTable = "SELECT value FROM json_each(@JsonParamList)";
            sql = sql.Replace( paramListName, subTable );

            List<(string name, object? value)>? sqlDefParams = parameters;
            if ( sqlDefParams == null )
            {
                sqlDefParams = [];
            }

            sqlDefParams.Add( ("@JsonParamList", JsonSerializer.Serialize( paramList ) ) );

            var retVal = new SQLCmdDef( sql, sqlDefParams );
            return retVal;
        }
    }
}
