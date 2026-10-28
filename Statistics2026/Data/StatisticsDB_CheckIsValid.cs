using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Logging;
using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Configuration;
using Statistics2026.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Statistics2026.Data
{
    public enum ECheckLevel
    {
        eEmbyInterfaceOnly = 0x00000000 | eThrowOnFailure,
        eLogger = 0x00000001,
        eConnection = 0x00000002,
        eProgress = 0x00000004,
        eLibraryManager = 0x00000010,
        eUserDataManager = 0x00000020,
        eUserManager = 0x00000040,
        eProviderManager = 0x00000080,
        eConfigManager = 0x00000100,
        eAll = 0x000001FF,
        eAllWithThrow = eAll | eThrowOnFailure,
        eInit = eLogger | eThrowOnFailure,
        eUpdate = eAllWithThrow,
        eReport = eLogger | eConnection | eThrowOnFailure,
        eThrowOnFailure = 0x00010000,
    }

    public sealed partial class StatisticsDB : IDisposable
    {
        public string ToString( ECheckLevel checkLevel )
        {
            List<string> values = [];
            values.Add( "EmbyInterfaces" );

            foreach( ECheckLevel curr in Enum.GetValues( typeof( ECheckLevel ) ) )
            {
                if(
                       ( ( curr & ECheckLevel.eAll ) != 0 )
                    || ( ( curr & ECheckLevel.eInit ) != 0 )
                    || ( ( curr & ECheckLevel.eUpdate ) != 0 )
                    || ( ( curr & ECheckLevel.eReport ) != 0 )
                )
                    continue;

                if( ( checkLevel & curr ) != 0 )
                {
                    values.Add( curr.ToString() );
                }
            }
            return values.Join( "|" );
        }


        [MemberNotNull( nameof( _embyInterfaces ) )]
        public bool CheckIsValid( ECheckLevel checkLevel )
        {
            List<string> msgs = [ $"DBHelper is not valid {ToString( checkLevel )}" ];
            var retVal = true;
            if( Statistics2026.Plugin.Instance == null )
            {
                retVal = false;
                msgs.Add( "Statistics2026.Plugin.Instance is null" );
            }

            if( Statistics2026.Plugin.Instance!.Configuration == null )
            {
                retVal = false;
                msgs.Add( "Statistics2026.Plugin.Instance.Configuration is null" );
            }

            if( ( checkLevel & ECheckLevel.eLogger ) != 0 )
            {
                if( _embyInterfaces == null || _embyInterfaces._logger == null )
                {
                    retVal = false;
                    msgs.Add( "Logger is null" );
                }
            }

            if( ( checkLevel & ECheckLevel.eConnection ) != 0 )
            {
                if( Connection == null )
                {
                    retVal = false;
                    msgs.Add( "Connection is null" );
                }
            }

            if( ( checkLevel & ECheckLevel.eProgress ) != 0 )
            {
                if( CancellationToken == null )
                {
                    retVal = false;
                    msgs.Add( "CancellationToken is null" );
                }

                if( Progress == null )
                {
                    retVal = false;
                    msgs.Add( "Progress is null" );
                }
            }

            if( retVal == false && ( ( checkLevel & ECheckLevel.eThrowOnFailure ) != 0 ) )
            {
                var msg = string.Join( "\n", msgs );
                throw new ArgumentNullException( msg );
            }

            if( _embyInterfaces == null )
            {
                throw new ArgumentNullException( "_embyInterfaces is null" );
            }

            return retVal;
        }
    }
}
