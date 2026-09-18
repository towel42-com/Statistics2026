using Statistics2026.Configuration;
using Statistics2026.Data;

namespace Statistics2026.Api
{
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

    public class MediaItemResponse
    {
        public MediaItemResponse() { }
        public MediaItemResponse( MediaInfo? media )
        {
            if( media == null )
                return;

            ListDisplayName = media!.ListDisplayName;
            StartYear = media!.StartYear;
            ResolutionDetail = media!.ResolutionDetail;
            Codec = media!.Codec;
            if( ( media.Codec == "hevc" ) || ( media!.Codec == "av1" ) )
            {
                DolbyVisionProfile = media!.DolbyVisionProfile;
            }

            ServerLocation = media!.ServerLocation;
        }

        public string ListDisplayName { get; set; } = string.Empty;
        public string StartYear { get; set; } = string.Empty;
        public string ResolutionDetail { get; set; } = string.Empty;
        public string Codec { get; set; } = string.Empty;
        public string DolbyVisionProfile { get; set; } = string.Empty;
        public string ServerLocation { get; set; } = string.Empty;
        public string ItemUrl { get; set; } = string.Empty;
    }
}
