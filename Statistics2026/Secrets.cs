using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Statistics2026.Secrets
{
    public static class TheMovieDB
    {
        private static string GetSecret( string keyName )
        {
            var attribute = typeof( TheMovieDB ).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault( a => a.Key == keyName );

            return attribute?.Value ?? string.Empty;
        }

        static public string getApiKeyV3()
        {
            return GetSecret( "TMDB_API_KEY_V3" );
        }

        static public string getApiKeyV4()
        {
            return GetSecret( "TMDB_API_KEY_V4" );
        }
    }
}
