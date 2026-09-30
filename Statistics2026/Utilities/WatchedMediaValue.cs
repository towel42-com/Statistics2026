using Statistics2026.Data;

namespace Statistics2026.Utilities
{
    public class WatchedMediaValue
    {
        public string ItemId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public long PlayCount { get; set; } = 0;
        public long Denominator { get; set; } = 0;
        public double PlayCountPerUser { get; set; } = 0.0;
        public EMediaType MediaType { get; set; } = EMediaType.eEpisode;

        public string Title()
        {
            var title = Name;
            if( MediaType == EMediaType.eMovie )
            {
                title += $" - Watched {PlayCount} time";

                if( PlayCount != 1 )
                    title += "s";
            }
            else
            {
                if( Denominator == PlayCount )
                {
                    title += $" - {Denominator} Episodes played 1 time each";
                }
                else
                {
                    title += $" - For {Denominator} Episodes, a total of {PlayCount} play";
                    if( PlayCount != 1 )
                        title += "s";
                }
            }

            return title;
        }
    }
}
