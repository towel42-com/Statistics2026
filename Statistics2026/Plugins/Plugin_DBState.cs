using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using ServiceStack;
using Statistics2026.Configuration;
using Statistics2026.Api;
using Statistics2026.ScheduledTasks;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;

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
        public static string ToPrettyString(this EDBState status)
        {
            var items = new List<string>();
            if (status == EDBState.eFullyInitialized)
                items.Add(status.ToString());
            else
            {

                foreach (var curr in new[] { EDBState.eSystemTablesCreated, EDBState.eUserTablesCreated, EDBState.eSystemDataInitialized, EDBState.eUserDataInitialized })
                {
                    if ((curr & status) != 0)
                        items.Add(curr.ToString());
                }
            }

            if (items.IsNullOrEmpty())
                items.Add("Empty");

            return string.Join("|", items);
        }
    }

    public partial class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {
        public EDBState CurrDBState()
        {
            EDBState value = EDBState.eEmpty;

            if (Plugin.Instance != null && Plugin.Instance.DBState != null)
                value = Plugin.Instance?.DBState ?? EDBState.eEmpty;

            return value;
        }

        public void ResetDBState()
        {
            _SetDBState(EDBState.eEmpty);
        }

        public void SetDBState(EDBState dbState, bool enabled)
        {
            var value = CurrDBState();

            if (Plugin.Instance != null && Plugin.Instance.DBState != null)
                value = Plugin.Instance?.DBState ?? EDBState.eEmpty;

            if (enabled)
                value = value | dbState;
            else
                value = value & ~dbState;

            _SetDBState(value);
        }

        public bool IsDBStateSet(EDBState dbState)
        {
            var value = CurrDBState();

            return ((value & dbState) == dbState);
        }

        public void ToggleDBState(EDBState dbState)
        {
            if (IsDBStateSet(dbState))
                RemoveDBState(dbState);
            else
                AddDBState(dbState);
        }

        public void AddDBState(EDBState dbState)
        {
            SetDBState(dbState, true);
        }

        public void RemoveDBState(EDBState dbState)
        {
            SetDBState(dbState, false);
        }

        public string DBStateAsString()
        {
            if (DBState == null)
                return EDBState.eEmpty.ToPrettyString();
            return DBState.Value.ToPrettyString();
        }
        private void _SetDBState(EDBState state)
        {
            if (Plugin.Instance == null)
                throw new ArgumentNullException("Plugin.Instance is null");

            _logger.Debug($"Setting DBState to {state.ToPrettyString()}");
            Plugin.Instance.DBState = state;
            var config = Plugin.Instance.Configuration;
            config.dbStateOK = (Plugin.Instance.DBState == EDBState.eFullyInitialized);
            Plugin.Instance.UpdateConfiguration(config);
        }

        public bool DBStateInitialized()
        {
            return DBState != null && DBState != EDBState.eEmpty;
        }

        private EDBState? DBState
        {
            get
            {
                lock (_padlock)
                {
                    return field;
                }
            }
            set
            {
                lock (_padlock)
                {
                    field = value;
                }
            }
        } = null;
    }
}
