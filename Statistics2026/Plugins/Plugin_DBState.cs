using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using ServiceStack;
using Statistics2026.Configuration;
using Statistics2026.Data;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace Statistics2026
{
    public enum EDBState
    {
        eEmpty = 0x00,  // a tables created
        eSystemTablesCreated = 0x01,
        eUserTablesCreated = 0x02,
        eSystemDataInitialized = 0x04,
        eUserDataInitialized = 0x08,
        eFullyInitialized = eSystemTablesCreated | eUserTablesCreated | eSystemDataInitialized | eUserDataInitialized
    }

    public static class EDBStateToString
    {
        public static string ToPrettyString( this EDBState status )
        {
            var items = new List<string>();
            if( status == EDBState.eFullyInitialized )
            {
                items.Add( status.ToString() );
            }
            else
            {
                foreach( var curr in new[] { EDBState.eSystemTablesCreated, EDBState.eUserTablesCreated, EDBState.eSystemDataInitialized, EDBState.eUserDataInitialized } )
                {
                    if( ( curr & status ) != 0 )
                        items.Add( curr.ToString() );
                }
            }

            if( items.IsNullOrEmpty() )
                items.Add( "Empty" );

            return string.Join( "|", items );
        }
    }

    public partial class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        public string DBStateMessage()
        {
            if( ( DBState == null ) || ( DBState == EDBState.eEmpty ) )
            {
                return "The database has not been initialized or is not finalized, please run the 'Statistics 2026 - Run All Tasks' task on the Scheduled Tasks page and come back for the results.";
            }

            if( DBState == EDBState.eFullyInitialized )
            {
                return string.Empty;
            }

            using var db = Statistics2026DB.GetInstance( _embyInterfaces );

            List<string> msgs = [];

            if( ( ( DBState & EDBState.eSystemTablesCreated ) == 0 ) || ( ( DBState & EDBState.eSystemDataInitialized ) == 0 ) )
            {
                msgs.Add( "The System tables have not been fully created. Please run the 'Statistics 2026 - Run All Tasks' task on the Scheduled Tasks page and come back for the results." );
            }
            if( ( ( DBState & EDBState.eUserTablesCreated ) == 0 ) || ( ( DBState & EDBState.eUserDataInitialized ) == 0 ) )
            {
                msgs.Add( "The User watch tables have not been fully created. Please run the 'Statistics 2026 - Analyze User Watch Data'." );
            }

            var tasksRunning = db.TasksRunning( "All" );
            foreach( var task in tasksRunning )
            {
                msgs.Add( $"The '{task}' task is running.  Results may be inaccurate or incomplete." );
            }

            return msgs.Join( "<br>" );
        }

        public string DBStateString()
        {
            return DBState == null ? EDBState.eEmpty.ToPrettyString() : DBState.Value.ToPrettyString();
        }

        public EDBState CurrDBState()
        {
            var value = EDBState.eEmpty;

            if( Plugin.Instance != null && Plugin.Instance.DBState != null )
                value = Plugin.Instance?.DBState ?? EDBState.eEmpty;

            return value;
        }

        public void ResetDBState()
        {
            _SetDBState( EDBState.eEmpty );
        }

        public void SetDBState( EDBState dbState, bool enabled )
        {
            var value = CurrDBState();

            if( Plugin.Instance != null && Plugin.Instance.DBState != null )
                value = Plugin.Instance?.DBState ?? EDBState.eEmpty;

            if( enabled )
                value |= dbState;
            else
                value &= ~dbState;

            _SetDBState( value );
        }

        public bool IsDBStateSet( EDBState dbState )
        {
            var value = CurrDBState();

            return ( value & dbState ) == dbState;
        }

        public void ToggleDBState( EDBState dbState )
        {
            if( IsDBStateSet( dbState ) )
                RemoveDBState( dbState );
            else
                AddDBState( dbState );
        }

        public void AddDBState( EDBState dbState )
        {
            SetDBState( dbState, true );
        }

        public void RemoveDBState( EDBState dbState )
        {
            SetDBState( dbState, false );
        }

        private void _SetDBState( EDBState state )
        {
            if( Plugin.Instance == null )
                throw new ArgumentNullException( "Plugin.Instance is null" );

            _embyInterfaces.Logger().Debug( $"Setting DBState to {state.ToPrettyString()}" );
            Plugin.Instance.DBState = state;
            var config = Plugin.Instance.Configuration;
            Plugin.Instance.UpdateConfiguration( config );
        }

        public bool DBStateInitialized()
        {
            return DBState != null;
        }

        private EDBState? DBState
        {
            get
            {
                lock( _padlock )
                {
                    return field;
                }
            }
            set
            {
                lock( _padlock )
                {
                    field = value;
                }
            }
        } = null;
    }
}
