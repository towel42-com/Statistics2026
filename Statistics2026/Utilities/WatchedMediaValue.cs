using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack;
using Statistics2026.Api;
using Statistics2026.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using WatchedMediaValueItemData = (string id, string name, long playCount, long denominator, double playCountPerUser);

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
