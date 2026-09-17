using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;
using Statistics2026.Configuration;
using Statistics2026.Data;
using System;
using System.Collections.Generic;

namespace Statistics2026.Api
{
    public class GetDatabaseStatusReponse
    {
        public GetDatabaseStatusReponse() { }
        public GetDatabaseStatusReponse(PluginConfiguration config)
        {
            BuildDate = config.BuildDate;
            LastUpdated = config.LastUpdated;
            Version = config.Version;
            DBStateOK = Plugin.Instance?.IsDBStateSet(EDBState.eFullyInitialized) ?? false;
            DBState = Plugin.Instance?.DBStateAsString() ?? EDBState.eEmpty.ToPrettyString();
        }
        public string BuildDate { get; set; } = String.Empty;
        public string LastUpdated { get; set; } = String.Empty;
        public string Version { get; set; } = String.Empty;
        public string DBState { get; set; } = String.Empty;
        public bool DBStateOK { get; set; } = false;
    }

    public class GetTVSeriesProgressResponse
    {
        public string Name { get; set; } = String.Empty;
        public string ItemUrl { get; set; } = String.Empty;
        public string SeriesId { get; set; } = String.Empty;
        public int PremiereYear { get; set; } = -1;

        public PercentValue Episodes { get; set; } = new PercentValue();
        public PercentValue Specials { get; set; } = new PercentValue();

        private double _score = 0.0;
        public double Score
        {
            get { return _score; }
            set
            {
                _score = value;
                ScoreStr = _score.ToString("F1");
            }
        }
        public string ScoreStr { get; set; } = String.Empty;
        public string SeriesStatus { get; set; } = String.Empty;
    }

    public class GetItemImageUrlResponse
    {
        public string Name { get; set; } = String.Empty;
        public string PrimaryImageUrl { get; set; } = String.Empty;
    }

    public class MediaItemResponse
    {
        public MediaItemResponse() { }
        public MediaItemResponse(MediaInfo? media)
        {
            if (media == null)
                return;

            ListDisplayName = media!.ListDisplayName;
            StartYear = media!.StartYear;
            ResolutionDetail = media!.ResolutionDetail;
            Codec = media!.Codec;
            if ((media.Codec == "hevc") || (media!.Codec == "av1"))
            {
                DolbyVisionProfile = media!.DolbyVisionProfile;
            }
            ServerLocation = media!.ServerLocation;
        }

        public string ListDisplayName { get; set; } = String.Empty;
        public string StartYear { get; set; } = String.Empty;
        public string ResolutionDetail { get; set; } = String.Empty;
        public string Codec { get; set; } = String.Empty;
        public string DolbyVisionProfile { get; set; } = String.Empty;
        public string ServerLocation { get; set; } = String.Empty;
        public string ItemUrl { get; set; } = String.Empty;
    }
}
