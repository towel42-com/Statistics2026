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
using SQLitePCL.pretty;
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
        private string _text = null;
        private Stopwatch _stopWatch = null;
        private ILogger _logger = null;
        public AutoTimer(string text, ILogger logger)
        {
            _text = text;
            _logger = logger;
            _stopWatch = Stopwatch.StartNew();
        }
        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _logger.Debug($"{_text} - {_stopWatch.ElapsedMilliseconds}ms");

            _disposed = true;
        }

        ~AutoTimer()
        {
            Dispose(false);
        }
    }
}
