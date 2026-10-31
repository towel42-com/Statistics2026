using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using ServiceStack;
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
    public static class DBLocation
    {
        private static string GetSecret( string keyName )
        {
            var attribute = typeof( DBLocation ).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault( a => a.Key == keyName );

            return attribute?.Value ?? string.Empty;
        }

        static string? sOldLocation = null;
        static public string getOldLocation()
        {
            if( sOldLocation == null )
            {
                sOldLocation = GetSecret( "OLD_SERVER_LOCATION" ); //@"\\mediabox";
            }
            return sOldLocation!;
        }

        static string? sNewLocation = null;
        static public string getNewLocation()
        {
            if( sNewLocation == null )
            {
                sNewLocation = GetSecret( "NEW_SERVER_LOCATION" ); //@"\\mediabox";
            }
            return sNewLocation!;
        }

        static public bool isOldLocation( string? path )
        {
            if( path == null )
            {
                return false;
            }

            var oldLocation = DBLocation.getOldLocation();
            if( oldLocation.IsEmpty() )
            {
                return false;
            }
            return path.StartsWith( oldLocation, StringComparison.OrdinalIgnoreCase );
        }

        static public string updateLocation( string path )
        {
            var oldLocation = DBLocation.getOldLocation();
            var newLocation = DBLocation.getNewLocation();
            if( oldLocation.IsEmpty() || newLocation.IsEmpty() )
            {
                return path;
            }

            return path.Replace( oldLocation, newLocation );
        }
    }

    public static class TheMovieDB
    {
        private static string GetSecret( string keyName )
        {
            var attribute = typeof( TheMovieDB ).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault( a => a.Key == keyName );

            return attribute?.Value ?? string.Empty;
        }


        static string? sApiKeyV3 = null;
        static public string getApiKeyV3()
        {
            if( sApiKeyV3 == null )
            {
                sApiKeyV3 = GetSecret( "TMDB_API_KEY_V3" ); 
            }
            return sApiKeyV3!;
        }

        static string? sApiKeyV4 = null;
        static public string getApiKeyV4()
        {
            if( sApiKeyV4 == null )
            {
                sApiKeyV4 = GetSecret( "TMDB_API_KEY_V4" ); 
            }
            return sApiKeyV4!;
        }
    }
}
