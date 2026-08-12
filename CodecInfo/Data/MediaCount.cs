namespace CodecInfo.Data
{
    public class CMediaCount
    {
        public string Name { get; set; }
        public int Movies { get; set; }
        public int Episodes { get; set; }

        public override string ToString()
        {
            return $"<tr><td>{Name}</td><td>{Movies}</td><td>{Episodes}</td></tr>";
        }
    }
}
