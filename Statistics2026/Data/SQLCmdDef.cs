using ServiceStack;
using System;
using System.Collections.Generic;

namespace Statistics2026.Data
{
    public class SQLCmdDef
    {
        public string Statement = String.Empty;
        public List<(string name, object? value)>? Parameters = null;

        public bool HasParameters()
        {
            return !Parameters.IsNullOrEmpty();
        }

        public SQLCmdDef(string sql)
        {
            Statement = sql;
        }

        public SQLCmdDef(string sql, List<(string name, object? value)>? _parameters)
        {
            Statement = sql;
            Parameters = _parameters;
        }
    }
}
