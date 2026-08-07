using System.Collections.Generic;

namespace CodecInfo.API
{
    public class MediaItem
    {
        public string GroupName { get; set; }
        public string Title { get; set; }
        public string Id { get; set; }
        public int? Year { get; set; }
    }

    public class MediaItemGroup
    {
        public List<MediaItem> MediaItems { get; set; }
        public string Title { get; set; }
        public bool IsUnknownDolbyProfile { get; set; } = false;
    }

    public class MediaItemCollection
    {
        public int Count { get; set; }
        public List<MediaItemGroup> MediaItemGroups { get; set; }
    }
}
