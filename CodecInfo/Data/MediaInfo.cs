using System.Collections.Generic;

namespace CodecInfo.Data
{
    public class CMediaInfo
    {
        public bool IsEpisode { get; set; }

        public string Id { get; set; }
        public string PrimaryName { get; set; } // movie title or series name
        public string SortName { get; set; } 
        public string SecondaryName { get; set; } // episode name
        public string StartYear { get; set; } // release year for movies, year of the of the first season of the TV show

        public int Season { get; set; }
        public int Episode { get; set; }

        public string Resolution { get; set; }
        public string CodecName { get; set; }
        public string DolbyVisionProfile { get; set; }
        public string ServerLocation { get; set; }
    }
}
