using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Services;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Statistics20.Data
{
    public class DynamicButton
    {
        public string id { get; set; }
        public string info { get; set; }
        public string title { get; set; }
    };

    public class ValueGroupResponse
    {
        public string html { get; set; }
        public DynamicButton[] dynamicButtons { get; set; } = new DynamicButton[] { };

        public void addDynamicButton(DynamicButton button)
        {
            var local = dynamicButtons;
            Array.Resize(ref local, local.Length + 1);
            local[local.Length - 1] = button;
            dynamicButtons = local;
        }

        public static string _addToHtml(int depth, string _html)
        {
            var retVal = "";
            if (depth > 0)
                retVal += new string(' ', 4 * depth);
            retVal += _html;
            if (!retVal.EndsWith("\n"))
                retVal += "\n";
            return retVal;
        }

        public void addToHtml(int depth, string _html)
        {
            html += _addToHtml(depth, _html);
        }
    };

    public class ValueGroup
    {
        public string Title { get; set; }

        //public string TableInfo { get; set; }
        public List<MediaCount> MediaCounts;

        public string ValueLineTwo { get; set; }
        public string ValueLineThree { get; set; }
        public string Size { get; set; }
        public object Raw { get; set; }
        public string ExtraInformation { get; set; }
        public string Id { get; set; }

        public ValueGroup()
        {
            Size = "small";
            MediaCounts = new List<MediaCount>();
        }

        public ValueGroup(string title, string extraInformation, string size = "half")
        {
            MediaCounts = new List<MediaCount>();

            Title = title;
            ExtraInformation = extraInformation;
            Size = size;

            ValueLineTwo = null;
            ValueLineThree = null;
        }

        private int findRow( string category)
        {
            for (int i = 0; i < MediaCounts.Count; i++)
            {
                if (MediaCounts[i].Name == category)
                    return i;
            }
            return -1;
        }

        public void addRow(string category, int episodeCount, int movieCount)
        {
            int currRow = findRow( category);
            if ( currRow == -1 )
                MediaCounts.Add(new MediaCount() { Name = category, Movies = movieCount, Episodes = episodeCount });
            else
                MediaCounts[currRow].setCount(episodeCount, movieCount);
        }
        public override string ToString()
        {
            return ToString(0);
        }

        public string ToString(int depth = 0)
        {
            var retVal = ValueGroupResponse._addToHtml(depth++, "<table>");

            retVal += ValueGroupResponse._addToHtml(depth++, "<tr>");
            retVal += ValueGroupResponse._addToHtml(depth, "<td></td>");
            retVal += ValueGroupResponse._addToHtml(depth, "<td>Movies</td>");
            retVal += ValueGroupResponse._addToHtml(depth, "<td>Episodes</td>");
            retVal += ValueGroupResponse._addToHtml(--depth, "</tr>");

            foreach (var mediaCount in MediaCounts)
            {
                retVal += mediaCount.ToString(depth);
            }
            retVal += ValueGroupResponse._addToHtml(--depth, "</table>");
            return retVal;
        }

        public object createStat(string serverId = "", string rootDivName = "")
        {
            var retVal = new ValueGroupResponse();

            if (rootDivName != "")
            {
                rootDivName = $" id=\"{rootDivName}\"";
            }

            int depth = 0;
            retVal.addToHtml(depth++, $"<div class=\"col {Size}\" {rootDivName}>");
            retVal.addToHtml(depth++, "<div class=\"statCard\">");
            retVal.addToHtml(depth++, "<div class=\"statCard-content\">");

            if (ExtraInformation != null && ExtraInformation != "")
            {
                string id = Regex.Replace(Title, @"\s", string.Empty);

                retVal.addToHtml(depth, $"<div id=\"{id}\" class=\"infoBlock\"><i class=\"md-icon\">info</i></div>");

                retVal.addDynamicButton(new DynamicButton { id = id, info = ExtraInformation, title = Title });
            }

            var showImage = (serverId != "") && (Id != "");
            if (showImage)
            {
                //var imageUrl = ApiClient.getImageUrl(Id, { type: "Primary", quality: 90 });
                string imageUrl = "";
                retVal.addToHtml(depth, $"<a is=\"emby-linkbutton\" href=\"/item?id={Id}&serverId={serverId}\"><img src=\"{imageUrl}\" height=\"105px\"/></a>");
                retVal.addToHtml(depth++, "<div>");

                if (Title != "")
                {
                    retVal.addToHtml(depth, $"<div class=\"statCard-stats-title-left\">{Title}</div>");
                }
            }
            else if (Title != "")
            {
                retVal.addToHtml(depth++, "<div style=\"width: 100%;\">");
                retVal.addToHtml(depth, $"<div class=\"statCard-stats-title\">{Title}</div>");
            }

            var tableInfo = ToString(depth + 1);

            if (tableInfo != "" && tableInfo != null)
            {
                retVal.addToHtml(depth++, $"<div class=\"statCard-stats-number\">");
                retVal.addToHtml(0, tableInfo);
                retVal.addToHtml(--depth, "</div>");
            }

            if (ValueLineTwo != "" && ValueLineTwo != null)
            {
                retVal.addToHtml(depth, $"<div class=\"statCard-stats-number\">{ValueLineTwo}</div>");
            }

            if (ValueLineThree != "" && ValueLineThree != null)
            {
                retVal.addToHtml(depth, $"<div class=\"statCard-stats-number\">{ValueLineThree}</div>");
            }

            retVal.addToHtml(--depth, "</div>");
            retVal.addToHtml(--depth, "</div>");
            retVal.addToHtml(--depth, "</div>");
            retVal.addToHtml(--depth, "</div>");
            retVal.addToHtml(--depth, "</div>"); // not sure why this is here, but it was in the original code.

            return retVal;
        }

    }
}