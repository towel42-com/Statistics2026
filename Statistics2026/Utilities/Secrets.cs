#if DEBUG
//#define DEBUG_PLUGIN
#endif

using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
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

#if DEBUG_PLUGIN
        static List<string>? sOldLocations = null;
#endif
        static public List<string> getOldLocations()
        {
#if DEBUG_PLUGIN
            if( sOldLocations == null )
            {
                var oldLocation = GetSecret( "OLD_SERVER_LOCATION" ); //@"\\mediabox";
                sOldLocations = oldLocation.Split( ';' ).ToList();
            }
            return sOldLocations!;
#else
            return [];
#endif
        }

#if DEBUG_PLUGIN
        static string? sNewLocation = null;
#endif
        static public string getNewLocation()
        {
#if DEBUG_PLUGIN
            if( sNewLocation == null )
            {
                sNewLocation = GetSecret( "NEW_SERVER_LOCATION" );
            }
            return sNewLocation!;
#else
            return string.Empty;
#endif
        }

        static public bool isOldLocation( string? path )
        {
#if DEBUG_PLUGIN
            if( path == null )
            {
                return false;
            }

            var oldLocations = DBLocation.getOldLocations();
            if( oldLocations.All( loc => loc.IsEmpty() ) )
            {
                return false;
            }
            return oldLocations.Any( loc => path.StartsWith( loc, StringComparison.OrdinalIgnoreCase ) );
#else
            return false;
#endif
        }

        static public (string path, bool success) updateLocation( string path, bool copyIfExists, ILogger logger )
        {
#if DEBUG_PLUGIN
            if( path == null )
            {
                return ( string.Empty, false );
            }

            var oldLocations = DBLocation.getOldLocations();
            var newLocation = DBLocation.getNewLocation();
            if( oldLocations.All( loc => loc.IsEmpty() ) || newLocation.IsEmpty() )
            {
                return (path, true);
            }

            bool copyOK = true;
            var oldPath = path;
            var newPath = path;
            foreach( var oldLocation in oldLocations )
            {
                if( oldLocation.IsEmpty() )
                    continue;

                if( !path.StartsWith( oldLocation, StringComparison.OrdinalIgnoreCase ) )
                {
                    continue;
                }

                var pattern = Regex.Escape( oldLocation );
                newPath = Regex.Replace( path, pattern, newLocation, RegexOptions.IgnoreCase );
                if( copyIfExists && File.Exists( oldPath ) && !File.Exists( newPath ) )
                {
                    //logger.Debug( $"Copying file '{oldPath}' to '{newPath}'" );
                    try
                    {
                        string destDir = Path.GetDirectoryName( newPath );
                        if( !Directory.Exists( destDir ) )
                        {
                            //logger.Debug( $"Creating Directory '{destDir}'" );
                            Directory.CreateDirectory( destDir );
                        }
                        File.Copy( oldPath, newPath );
                        //logger.Debug( $"Successfully copied file '{oldPath}' to '{newPath}'" );
                    }
                    catch( Exception ex )
                    {
                        logger.Debug( ex.ToString() );
                        logger.Debug( $"Error Copying file '{oldPath}' to '{newPath}'" );
                        copyOK = false;
                    }
                }
            }

            return ( newPath, copyOK );
#else
            return (path, true);
#endif
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
