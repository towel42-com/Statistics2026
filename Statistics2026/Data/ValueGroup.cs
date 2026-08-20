using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Services;
using Emby.ApiClient; // Namespace containing the ApiClient
using MediaBrowser.Model.Dto;     // Namespace containing BaseItemDto
using MediaBrowser.Model.Entities;// Namespace containing ImageType
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Linq;
using Emby.Media.Common.Extensions;

namespace Statistics2026.Data
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
            if (_html == null || _html == "")
                return "";

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
        public List<string> Headers { get; private set; }

        public string SubTitle { get; set; }

        //public string TableInfo { get; set; }
        private List<ValueGroupRow> Values;

        public string ValueLineTwo { get; set; }
        public string ValueLineThree { get; set; }
        public string Size { get; set; }
        public string HelpText { get; set; }
        public string ImageUrl { get; set; }
        public string MediaItemId { get; set; }

        public string ServerId { get; set; }
        public string HtmlDivId { get; set; }
        public bool SortByKey { get; set; }

        public ValueGroup()
        {
            Size = "small";
            Values = new List<ValueGroupRow>();
        }

        public ValueGroup(string title, string helpText, List<string> headers, string size = "half")
        {
            Values = new List<ValueGroupRow>();
            Headers = headers;

            Title = title;
            HelpText = helpText;
            Size = size;

            ValueLineTwo = null;
            ValueLineThree = null;
        }

        private int findRow(string name)
        {
            for (int i = 0; i < Values.Count; i++)
            {
                if (Values[i].Name == name)
                    return i;
            }
            return -1;
        }

        public void addRow(string category, List<int> values)
        {
            int currRow = findRow(category);
            ValueGroupRow row = null;
            if (currRow == -1)
            {
                row = new ValueGroupRow(category, null);
                Values.Add(row);
            }
            else
                row = Values[currRow];
            row.setValues(values);
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public string ToString(int depth = 0)
        {
            var retVal = ValueGroupResponse._addToHtml(depth, SubTitle);
            if (Values.Count == 0)
                return retVal;

            retVal = ValueGroupResponse._addToHtml(depth++, "<table>");

            retVal += ValueGroupResponse._addToHtml(depth++, "<tr>");
            retVal += ValueGroupResponse._addToHtml(depth, "<td>&nbsp;</td>");
            foreach (var header in Headers)
            {
                retVal += ValueGroupResponse._addToHtml(depth, $"<td>{header}</td>");
            }
            retVal += ValueGroupResponse._addToHtml(--depth, "</tr>");

            List<ValueGroupRow> valuesToUse = Values;
            if (SortByKey)
            {
                valuesToUse = valuesToUse.OrderBy(row => row.Name).ToList();
            }

            foreach (var row in valuesToUse)
            {
                retVal += row.ToString(depth);
            }
            retVal += ValueGroupResponse._addToHtml(--depth, "</table>");

            return retVal;
        }

        public object createStat( string rootDivName = "")
        {
            var retVal = new ValueGroupResponse();

            if (rootDivName != "" && rootDivName != null)
            {
                rootDivName = $" id=\"{rootDivName}\"";
            }

            int depth = 0;
            retVal.addToHtml(depth++, $"<div class=\"col {Size}\" {rootDivName}>");
            retVal.addToHtml(depth++, "<div class=\"statCard\">");
            retVal.addToHtml(depth++, "<div class=\"statCard-content\">");

            if (HelpText != null && HelpText != "")
            {
                string id = Regex.Replace(Title, @"\s", string.Empty);

                retVal.addToHtml(depth, $"<div id=\"{id}\" class=\"infoBlock\"><i class=\"md-icon\">info</i></div>");

                retVal.addDynamicButton(new DynamicButton { id = id, info = HelpText, title = Title });
            }

            var showImage = !ServerId.IsNullOrEmpty() && !ImageUrl.IsNullOrEmpty() && !MediaItemId.IsNullOrEmpty();
            if (showImage)
            {
                retVal.addToHtml(depth, $"<a is=\"emby-linkbutton\" href=\"/item?id={MediaItemId}&serverId={ServerId}\"><img src=\"{ImageUrl}\" height=\"105px\"/></a>");
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