using System.Collections.Generic;

namespace CodecInfo.API
{
    public class CMediaItem
    {
        public string GroupName { get; set; }
        public string Title { get; set; }
        public string Id { get; set; }
        public int? Year { get; set; }
    }

    public class CMediaItemGroup
    {
        public List<CMediaItem> MediaItems { get; set; }
        public string Title { get; set; }
        public bool IsUnknownDolbyProfile { get; set; } = false;
    }

    public class CMediaItemCollection
    {
        public int Count { get; set; }
        public List<CMediaItemGroup> MediaItemGroups { get; set; }
    }
}
