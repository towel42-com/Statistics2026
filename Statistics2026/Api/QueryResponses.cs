using ServiceStack;
using Statistics2026.Configuration;
using Statistics2026.Utilities;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;

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

    public class MediaItemResponse
    {
        public SortByText DisplayName { get; set; } = new();
        public string PremiereDate { get; set; } = string.Empty;
        public string ResolutionDetail { get; set; } = string.Empty;
        public string Codec { get; set; } = string.Empty;
        public string DolbyVisionProfile { get; set; } = string.Empty;

        public SortByText StudioNames { get; set; } = new();
        public SortByText ServerLocation { get; set; } = new();
        public SortByText SearchLocation { get; set; } = new();
        public SortByText ParentName { get; set; } = new();
        public string ParentUrl { get; set; } = string.Empty;

        public string ItemUrl
        {
            get;
            set
            {
                field = value ?? string.Empty;
                if( value != null && value != string.Empty )
                    DisplayName.Text = value;
                else
                    DisplayName.Text = DisplayName.SortBy;
            }
        } = string.Empty;
        public string ItemId { get; set; } = string.Empty;

        public void setParentInfo( string parentJson )
        {
            ParentUrl = string.Empty;
            ParentName = new();
            try
            {
                var jsonNode = JsonNode.Parse( parentJson );

                var parentArray = jsonNode?.AsArray();
                if( parentArray == null )
                    return;

                List<SortByText> sortByTexts = [];
                List<(string id, string url)> urls = [];
                foreach( var parentNode in parentArray )
                {
                    if( parentNode == null )
                        continue;

                    var name = parentNode[ "name" ]?.GetValue<string>() ?? string.Empty;
                    var sortName = parentNode[ "sort_name" ]?.GetValue<string>() ?? string.Empty;
                    var id = parentNode[ "id" ]?.GetValue<string>() ?? string.Empty;
                    var url = parentNode[ "url" ]?.GetValue<string>() ?? string.Empty;
                    if( name.IsNullOrEmpty() || sortName.IsNullOrEmpty() ||  id.IsNullOrEmpty() || url.IsNullOrEmpty() )
                        continue;

                    if( ParentName.SortBy != string.Empty )
                        ParentName.SortBy += ", ";
                    ParentName.SortBy += sortName;

                    var currParentUrl = ItemImageUrl.ItemUrl( id, url, name );
                    if( currParentUrl != null && currParentUrl != string.Empty )
                    {
                        if( ParentUrl != string.Empty )
                            ParentUrl += "\n";
                        ParentUrl += currParentUrl;
                        if( !ParentName.Text.IsEmpty() )
                            ParentName.Text += "\n";
                        ParentName.Text += currParentUrl;
                    }
                    else
                    {

                        if( ParentName.Text != string.Empty )
                            ParentName.Text += ", ";
                        ParentName.Text += name;
                    }
                }

            }
            catch( Exception )
            {
            }
        }
    }
}
