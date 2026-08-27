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
using RestSharp;
using ServiceStack;
using SQLitePCL.pretty;
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
using System.Xml.Linq;

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
