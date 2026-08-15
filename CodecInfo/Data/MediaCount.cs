namespace CodecInfo.Data
{
    public class CMediaCount
    {
        public string Name { get; set; }
        public int Movies { get; set; }
        public int Episodes { get; set; }

        public void setCount( int episodeCount, int movieCount)
        {
            Episodes = episodeCount;
            Movies = movieCount;
        }

        public string ToString(int depth = 0)
        {
            var retVal = CValueGroupResponse._addToHtml(depth++, "<tr style=\"white-space: nowrap;\">");

            retVal += CValueGroupResponse._addToHtml(depth, $"<td style=\"text-align: left; white-space: nowrap;\">{Name}</td>");
            retVal += CValueGroupResponse._addToHtml(depth, $"<td>{Movies}</td>");
            retVal += CValueGroupResponse._addToHtml(depth, $"<td>{Episodes}</td>");
            retVal += CValueGroupResponse._addToHtml(--depth, "</tr>");

            return retVal;
        }
    
        public override string ToString()
        {
            return ToString(0);
        }
    }
}
