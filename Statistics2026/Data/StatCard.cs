using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Dto;     // Namespace containing BaseItemDto
using MediaBrowser.Model.Entities;// Namespace containing ImageType
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Linq;
using Emby.Media.Common.Extensions;
using Statistics2026.Api;

namespace Statistics2026.Data
{
    public class DynamicButton
    {
        public string id { get; set; } = String.Empty;
        public string info { get; set; } = String.Empty;
        public string title { get; set; } = String.Empty;
    };

    public class StatCardResponse
    {
        public string html { get; set; } = String.Empty;
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
            if (_html.IsNullOrEmpty())
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


    public class StatCardRow
    {
        public string Name { get; private set; } = String.Empty;
        public List<long>? Values { get; private set; } = null;

        public StatCardRow(string name, List<long>? values)
        {
            Name = name;
            Values = values;
        }

        public void setValues(List<long> values)
        {
            Values = values;
        }

        public string ToString(int depth = 0)
        {
            var retVal = StatCardResponse._addToHtml(depth++, "<tr style=\"white-space: nowrap;\">");

            retVal += StatCardResponse._addToHtml(depth, $"<td style=\"text-align: left; white-space: nowrap;\">{Name}</td>");
            if (Values != null)
            {
                foreach (var value in Values)
                {
                    retVal += StatCardResponse._addToHtml(depth, $"<td>{value}</td>");
                }
            }

            retVal += StatCardResponse._addToHtml(--depth, "</tr>");

            return retVal;
        }

        public override string ToString()
        {
            return ToString(0);
        }
    }

    public abstract class StatCard
    {
        public string Title { get; set; } = String.Empty;
        protected List<string>? Headers { get; set; } = null;

        public string SubTitle { get; set; } = String.Empty;

        public string Size { get; set; } = String.Empty;
        public string HelpText { get; set; } = String.Empty;
        public string ImageUrl { get; set; } = String.Empty;
        public string MediaItemId { get; set; } = String.Empty;

        public string ServerId { get; set; } = String.Empty;
        public string HtmlDivId { get; set; } = String.Empty;
        public bool SortByKey { get; set; } = false;

        public StatCard()
        {
            Size = "small";
        }

        public StatCard(string title, string? helpText, string size = "half")
        {
            Title = title;
            if ( helpText != null )
                HelpText = helpText;
            else
                HelpText = String.Empty;
            Size = size;
        }

        public override string ToString()
        {
            return ToString(0);
        }

        public abstract bool IsEmpty();
        public abstract string GetDataString(int depth = 0);

        private void addData(ref string retVal, int depth = 0)
        {
            retVal = StatCardResponse._addToHtml(depth++, "<table>");

            if (Headers != null)
            {
                retVal += StatCardResponse._addToHtml(depth++, "<tr>");
                retVal += StatCardResponse._addToHtml(depth, "<td>&nbsp;</td>");
                foreach (var header in Headers)
                {
                    retVal += StatCardResponse._addToHtml(depth, $"<td>{header}</td>");
                }
                retVal += StatCardResponse._addToHtml(--depth, "</tr>");
            }

            retVal += GetDataString(depth);

            retVal += StatCardResponse._addToHtml(--depth, "</table>");
        }

        public string ToString(int depth = 0)
        {
            var retVal = StatCardResponse._addToHtml(depth, SubTitle);
            if (IsEmpty())
                return retVal;

            addData(ref retVal, depth);

            return retVal;
        }

        private void addHelp(ref StatCardResponse retVal, int depth)
        {
            if (!HelpText.IsNullOrEmpty())
            {
                string id = Regex.Replace(Title, @"\s", string.Empty);

                retVal.addToHtml(depth, $"<div id=\"{id}\" class=\"infoBlock\"><i class=\"md-icon\">info</i></div>");

                retVal.addDynamicButton(new DynamicButton { id = id, info = HelpText, title = Title });
            }
        }

        private void addTitle(ref StatCardResponse retVal, int depth)
        {
            string titleClass = "statCard-stats-title";
            var showImage = !ServerId.IsNullOrEmpty() && !ImageUrl.IsNullOrEmpty() && !MediaItemId.IsNullOrEmpty();
            if (showImage)
            {
                var itemUrl = ItemImageUrl.ItemUrl(MediaItemId, ServerId, ImageUrl);
                retVal.addToHtml(depth, itemUrl);
                retVal.addToHtml(depth++, "<div>");
                titleClass = "statCard-stats-title-left";
            }
            else
            {
                retVal.addToHtml(depth++, "<div style=\"width: 100%;\">");
            }

            if (!Title.IsNullOrEmpty())
            {
                retVal.addToHtml(depth, $"<div class=\"{titleClass}\">{Title}</div>");

            }
        }

        private int addData(int depth, ref StatCardResponse retVal)
        {
            var tableInfo = ToString(depth + 1);

            if (!tableInfo.IsNullOrEmpty())
            {
                retVal.addToHtml(depth++, $"<div class=\"statCard-stats-number\">");
                retVal.addToHtml(0, tableInfo);
                retVal.addToHtml(--depth, "</div>");
            }
            return depth;
        }

        public object createStat(string rootDivName = "")
        {
            var retVal = new StatCardResponse();

            if (!rootDivName.IsNullOrEmpty())
            {
                rootDivName = $" id=\"{rootDivName}\"";
            }

            int depth = 0;
            retVal.addToHtml(depth++, $"<div class=\"col {Size}\" {rootDivName}>");
            retVal.addToHtml(depth++, "<div class=\"statCard\">");
            retVal.addToHtml(depth++, "<div class=\"statCard-content\">");

            addHelp(ref retVal, depth);
            addTitle(ref retVal, depth);

            depth = addData(depth, ref retVal);

            retVal.addToHtml(--depth, "</div>");
            retVal.addToHtml(--depth, "</div>");
            retVal.addToHtml(--depth, "</div>");
            retVal.addToHtml(--depth, "</div>");
            retVal.addToHtml(--depth, "</div>"); // not sure why this is here, but it was in the original code.

            return retVal;
        }

    }

    public class TextBasedStatCard : StatCard
    {
        public bool AsNumberedList { get; set; } = false;
        private List<(string data, string itemId, string url)> ValueLines { get; set; }
        public override bool IsEmpty() { return ValueLines == null || ValueLines.Count == 0; }
        public TextBasedStatCard()
            : base()
        {
            ValueLines = new List<(string, string, string)>();
        }

        public TextBasedStatCard(string title, string? helpText, string size = "half")
            : base(title, helpText, size)
        {
            ValueLines = new List<(string, string, string)>();
        }

        private string CheckMaxLength(string value)
        {
            return value.Length > 30 ? value.Substring(0, 27) + "..." : value;
        }

        public void AddLine(string value)
        {
            AddLine(value, "", "");
        }

        public void AddLine(string value, string itemId, string url)
        {
            ValueLines.Add((CheckMaxLength(value), itemId, url));
        }

        public override string GetDataString(int depth = 0)
        {
            string retVal = "";
            string style = "";
            if (AsNumberedList)
            {
                style = "style=\"text-align: left; white-space: nowrap;\"";
                retVal += StatCardResponse._addToHtml(depth++, $"<ol>");
            }
            foreach (var valueLine in ValueLines)
            {
                if (valueLine.data.IsNullOrEmpty())
                    continue;
                var dataHtml = $"<div class=\"statCard-stats-number\" {style}>{valueLine.data}</div>";

                var showImage = !ServerId.IsNullOrEmpty() && !valueLine.url.IsNullOrEmpty() && !valueLine.itemId.IsNullOrEmpty();
                if (showImage)
                {
                    dataHtml = ItemImageUrl.ItemUrl(valueLine.itemId, ServerId, valueLine.url, dataHtml, "50px");
                }
                var html = dataHtml;

                if (AsNumberedList)
                {
                    html = $"<li {style}>" + dataHtml + "</li>";
                }
                retVal += StatCardResponse._addToHtml(depth, html);
            }
            if (AsNumberedList)
            {
                retVal += StatCardResponse._addToHtml(--depth, "<ol>");
            }
            return retVal;
        }
    };

    public class TableBasedStatCard : StatCard
    {
        private List<StatCardRow> Rows;
        public override bool IsEmpty() { return Rows == null || Rows.Count == 0; }
        public TableBasedStatCard()
            : base()
        {
            Rows = new List<StatCardRow>();
        }
        public TableBasedStatCard(string title, string helpText, List<string> headers, string size = "half")
            : base(title, helpText, size)
        {
            Rows = new List<StatCardRow>();

            Headers = headers;
        }

        private int findRow(string name)
        {
            for (int i = 0; i < Rows.Count; i++)
            {
                if (Rows[i].Name == name)
                    return i;
            }
            return -1;
        }

        public void addRow(string category, List<int> values)
        {
            var longValues = new List<long>();
            foreach( var value in values )
                longValues.Add(value);
            addRow( category, longValues);
        }

        public void addRow(string category, List<long> values)
        {
            int currRow = findRow(category);
            StatCardRow row;
            if (currRow == -1)
            {
                row = new StatCardRow(category, null);
                Rows.Add(row);
            }
            else
                row = Rows[currRow];
            row.setValues(values);
        }

        public override string GetDataString(int depth = 0)
        {
            List<StatCardRow> valuesToUse = Rows;
            if (SortByKey)
            {
                valuesToUse = valuesToUse.OrderBy(row => row.Name).ToList();
            }

            string retVal = "";
            foreach (var row in valuesToUse)
            {
                retVal += row.ToString(depth);
            }
            return retVal;
        }
    }
}