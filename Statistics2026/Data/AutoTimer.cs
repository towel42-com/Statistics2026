using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using Statistics2026.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;


namespace Statistics2026.Data
{
    public class AutoTimer : IDisposable
    {
        public string? Text = null;
        private Stopwatch _stopWatch = Stopwatch.StartNew();
        private ILogger? _logger = null;
        private bool _debug = true;

        public AutoTimer(string text, ILogger? logger, bool debug = true)
        {
            Text = text;
            _debug = debug;
            _logger = logger;
        
            SendMessage($"Starting {Text}");
        }

        public long ElapsedMilliseconds()
        {
            return _stopWatch?.ElapsedMilliseconds ?? 0;
        }

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void SendMessage(string message)
        {
            if (_debug)
                _logger?.Debug(message);
            else
                _logger?.Info(message);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            SendMessage($"Finished {Text} - {_stopWatch?.ElapsedMilliseconds ?? 0}ms");

            _disposed = true;
        }
        ~AutoTimer()
        {
            Dispose(false);
        }
    }
}
