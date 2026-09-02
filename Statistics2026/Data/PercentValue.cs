using MediaBrowser.Controller.Entities;
using ServiceStack;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;


namespace Statistics2026.Data
{
    public class PercentValue
    {
        private int? _count = null;
        public int? Total { get; set; } = null;

        public string _string { get; set; } = string.Empty;
        public string String
        {
            get
            {
                if (_string.IsEmpty())
                {
                    if (_count == null && Total == null)
                        _string = "0 of 0 (0%)";
                    else if (_count == null && Total != null)
                        _string = $"0 of {Total} (0%)";
                    else if (_count != null && Total == null)
                        _string = $"{_count} of 0 (100%)";
                    else
                    {
                        Percent = (100.0 * _count) / (1.0 * Total) ?? 0.0;
                        _string = $"{_count} of {Total} ({Percent:F0}%)";
                    }
                }
                return _string;
            }
            set { _string = value; }
        }
        public double Percent { get; set; } = 0.0;

        public int Count
        {
            get
            {
                return _count ?? 0;
            }
            set
            {
                _count = value;
                if (_count == null || Total == null)
                    return;

                if (Total == 0)
                {
                    String = string.Empty;
                    Percent = 0.0;
                    return;
                }

                Percent = (100.0 * _count) / (1.0 * Total) ?? 0.0;
                _string = $"{_count}/{Total} ({Percent:F0}%)";
            }
        }
    }
}
