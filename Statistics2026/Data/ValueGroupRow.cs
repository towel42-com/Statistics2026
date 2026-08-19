using MediaBrowser.Controller.Entities.Movies;
using System;
using System.Collections.Generic;

namespace Statistics2026.Data
{
    public class ValueGroupRow
    {
        public string Name { get; private set; }
        public List<int> Values { get; private set; }

        public ValueGroupRow(string name, List<int> values)
        {
            Name = name;
            Values = values;
        }

        public void setValues(List<int> values)
        {
            Values = values;
        }

        public string ToString(int depth = 0)
        {
            var retVal = ValueGroupResponse._addToHtml(depth++, "<tr style=\"white-space: nowrap;\">");

            retVal += ValueGroupResponse._addToHtml(depth, $"<td style=\"text-align: left; white-space: nowrap;\">{Name}</td>");
            foreach (var value in Values)
            {
                retVal += ValueGroupResponse._addToHtml(depth, $"<td>{value}</td>");
            }

            retVal += ValueGroupResponse._addToHtml(--depth, "</tr>");

            return retVal;
        }

        public override string ToString()
        {
            return ToString(0);
        }
    }
}
