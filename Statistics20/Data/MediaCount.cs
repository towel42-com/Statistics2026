namespace Statistics20.Data
{
    public class MediaCount
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
            var retVal = ValueGroupResponse._addToHtml(depth++, "<tr style=\"white-space: nowrap;\">");

            retVal += ValueGroupResponse._addToHtml(depth, $"<td style=\"text-align: left; white-space: nowrap;\">{Name}</td>");
            retVal += ValueGroupResponse._addToHtml(depth, $"<td>{Movies}</td>");
            retVal += ValueGroupResponse._addToHtml(depth, $"<td>{Episodes}</td>");
            retVal += ValueGroupResponse._addToHtml(--depth, "</tr>");

            return retVal;
        }
    
        public override string ToString()
        {
            return ToString(0);
        }
    }
}
