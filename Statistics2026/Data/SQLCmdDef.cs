using ServiceStack;
using SQLitePCL.pretty;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Statistics2026.Data
{
    public class SQLCmdDef
    {
        public string Statement = String.Empty;
        public List<(string name, object? value)>? Parameters = null;
        public bool FailureAllowed { get; set; } = false;

        public bool HasParameters()
        {
            return !Parameters.IsNullOrEmpty();
        }

        public SQLCmdDef(string sql, bool failureAllowed = false)
        {
            Statement = sql;
            FailureAllowed = failureAllowed;
        }

        public SQLCmdDef(SQLCmdDef rhs)
        {
            Statement = rhs.Statement;
            Parameters = rhs.Parameters;
            FailureAllowed = rhs.FailureAllowed;
        }

        public SQLCmdDef(string sql, List<(string name, object? value)>? _parameters, bool failureAllowed = false)
        {
            Statement = sql;
            Parameters = _parameters;
            FailureAllowed = failureAllowed;
        }

        public void Replace(string from, string to)
        {
            Statement = Statement.Replace(from, to);
        }

        private void _Execute(IDatabaseConnection connection, DBHelper dbHelper, Func<IStatement, bool>? onStatement, CancellationToken? cancellationToken)
        {
            using (var statement = connection.PrepareStatement(Statement))
            {
                if (HasParameters())
                {
                    foreach (var param in Parameters!)
                    {
                        dbHelper.TryBind(statement, param.name, param.value);
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
                        cancellationToken?.ThrowIfCancellationRequested();
                    }
                }
            }

        }
        public void Execute(IDatabaseConnection connection, DBHelper dbHelper, Func<IStatement, bool>? onStatement, CancellationToken? cancellationToken)
        {
            if (FailureAllowed)
            {
                try
                {
                    _Execute(connection, dbHelper, onStatement, cancellationToken);
                }
                catch
                { 
                }
            }
            else
            {
                _Execute(connection, dbHelper, onStatement, cancellationToken);
            }

        }
    }
}
