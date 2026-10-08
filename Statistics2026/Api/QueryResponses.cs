using ServiceStack;
using Statistics2026.Configuration;
using Statistics2026.Utilities;

namespace Statistics2026.Api
{
    public class HtmlJsonResponse
    {
        public string html { get; set; } = string.Empty;
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

    public class MediaItemResponse
    {
        public SortByText DisplayName { get; set; } = new();
        public string PremiereDate { get; set; } = string.Empty;
        public string ResolutionDetail { get; set; } = string.Empty;
        public string Codec { get; set; } = string.Empty;
        public string DolbyVisionProfile { get; set; } = string.Empty;

        public SortByText ServerLocation { get; set; } = new();
        public SortByText SearchLocation { get; set; } = new();
        public SortByText ParentName { get; set; } = new();

        public string ItemUrl { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
    }
}
