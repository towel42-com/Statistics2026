namespace CodecInfo.Data
{
    public class CValueGroup
    {
        public string Title { get; set; }

        public string TableInfo { get; set; }
        public string ValueLineTwo { get; set; }
        public string ValueLineThree { get; set; }
        public string Size { get; set; }
        public object Raw { get; set; }
        public string ExtraInformation { get; set; }
        public string Id { get; set; }

        public CValueGroup()
        {
            Size = "small";
        }

        public CValueGroup(string title, string extraInformation, string size = "half")
        {
            Title = title;
            ExtraInformation = extraInformation;
            Size = size;

            TableInfo = $"<table><tr><td></td><td>Movies</td><td>Episodes</td></tr>";
            ValueLineTwo = null;
            ValueLineThree = null;
        }

        public void addRow( string category, int episodeCount, int movieCount)
        {
            TableInfo += $"<tr><td>{category}</td><td>{movieCount}</td><td>{episodeCount}</td></tr>";
        }

        public void endTable()
        {
            TableInfo += "</table>";
        }

    }
}