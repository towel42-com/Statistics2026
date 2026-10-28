using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Logging;
using ServiceStack;
using SQLitePCL;
using SQLitePCL.pretty;
using Statistics2026;
using Statistics2026.Api;
using Statistics2026.Configuration;
using Statistics2026.Utilities;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Statistics2026.Api
{
    public class HtmlJsonResponse
    {
        public string html { get; set; } = string.Empty;
    }

    public class SeriesFilterResponse
    {
        public string SeriesId { get; set; } = string.Empty;
        public bool TrackSeries { get; set; } = false;
        public bool TrackSpecials { get; set; } = false;

        public string Name { get; set; } = string.Empty;
        public string SortName { get; set; } = string.Empty;
        public int PremiereYear { get; set; } = -1;
        public int MinSeason { get; set; } = -1;
        public int MaxSeason { get; set; } = -1;

        public bool HasSpecials { get; set; } = false;
        public int NumSeasons { get; set; } = 0;

        public string key()
        {
            var retVal = SortName;
            if( PremiereYear > 0 )
            {
                retVal += "_" + PremiereYear.ToString();
            }
            return retVal;
        }
    }


    public class GetDatabaseStatusReponse
    {
        public GetDatabaseStatusReponse() { }
        public GetDatabaseStatusReponse( PluginConfiguration config )
        {
            BuildDate = config.BuildDate;
            LastUpdated = config.LastUpdated;
            Version = config.Version;
            DBStateOK = Plugin.Instance?.IsDBStateSet( EDBState.eFullyInitialized ) ?? false;
            DBState = Plugin.Instance?.DBStateString() ?? EDBState.eEmpty.ToPrettyString();
        }
        public string BuildDate { get; set; } = string.Empty;
        public string LastUpdated { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string DBState { get; set; } = string.Empty;
        public bool DBStateOK { get; set; } = false;
    }

    public class GetTVSeriesProgressResponse
    {
        public string Name { get; set; } = string.Empty;
        public string ItemUrl { get; set; } = string.Empty;
        public string SeriesId { get; set; } = string.Empty;
        public int PremiereYear { get; set; } = -1;

        public PercentValue Episodes { get; set; } = new PercentValue();
        public PercentValue Specials { get; set; } = new PercentValue();

        public double Score
        {
            get;
            set
            {
                field = value;
                ScoreStr = field.ToString( "F1" );
            }
        } = 0.0;
        public string ScoreStr { get; set; } = string.Empty;
        public string SeriesStatus { get; set; } = string.Empty;
    }

    public class SortByText
    {
        public SortByText( string sortBy = "", string text = "" )
        {
            this.SortBy = sortBy;
            this.Text = text;
            if( Text.IsEmpty() )
                Text = SortBy;
            if( SortBy.IsEmpty() )
                SortBy = Text;
        }

        public string SortBy { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
