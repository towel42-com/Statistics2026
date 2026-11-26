using Emby.Media.Common.Extensions;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Tasks;
using Statistics2026.Data;
using Statistics2026.Responses;
using Statistics2026.Utilities;
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using static Statistics2026.Data.Statistics2026DB;

namespace Statistics2026.Services
{
    public class TabulatorTableOptions
    {
        public int StartIndex { get; set; } = 0;
        public int Limit { get; set; } = 25;
        public string SortBy { get; set; } = string.Empty;
        public string SortOrder { get; set; } = string.Empty;
        public string Filters { get; set; } = string.Empty;

        public Dictionary<string, string> GetFilterMap()
        {
            if( string.IsNullOrWhiteSpace( Filters ) )
                return new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );

            try
            {
                Dictionary<string, string>? parsed = JsonSerializer.Deserialize<Dictionary<string, string>>( Filters );
                return parsed != null
                    ? new Dictionary<string, string>( parsed, StringComparer.OrdinalIgnoreCase )
                    : new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
            }
            catch( JsonException )
            {
                return new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
            }
        }
    }
}
