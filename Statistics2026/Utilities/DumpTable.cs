using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Statistics2026.Utilities
{
    public static class DumpTable
    {
        public static void dumpTable( List<string> header, List<List<object>> rows, Action<string> dumpFunc )
        {
            List<int> columnWidths = [];
            foreach( var item in header )
            {
                columnWidths.Add( item.Length + 2 );
            }

            foreach( var currRow in rows )
            {
                if( currRow.Count() != header.Count() )
                {
                    throw new InvalidDataContractException( "Header column count is different than row column count" );
                }

                for( var ii = 0; ii < currRow.Count(); ++ii )
                {
                    var currItemString = currRow[ ii ]?.ToString() ?? string.Empty;

                    columnWidths[ ii ] = Math.Max( currItemString.Length + 2, columnWidths[ ii ] );
                }
            }

            dumpRow( columnWidths, null, dumpFunc );
            dumpRow( columnWidths, header.Cast<object>().ToList(), dumpFunc );
            dumpRow( columnWidths, null, dumpFunc );
            foreach( var currRow in rows )
            {
                dumpRow( columnWidths, currRow.Cast<object>().ToList(), dumpFunc );
            }

            dumpRow( columnWidths, null, dumpFunc );
        }

        private static void dumpRow( List<int> columnWidths, List<object>? items, Action<string> dumpFunc )
        {
            var rowString = string.Empty;
            if( items == null )
            {
                for( var ii = 0; ii < columnWidths.Count(); ++ii )
                {
                    if( ii == 0 )
                    {
                        rowString += "|";
                    }

                    rowString += new string( '-', columnWidths[ ii ] );
                    rowString += "|";
                }
            }
            else
            {
                if( columnWidths.Count() != items.Count() )
                {
                    throw new InvalidDataContractException( "Column width column count is different than row column count" );
                }

                for( var ii = 0; ii < items.Count(); ++ii )
                {
                    var currItem = items[ ii ]?.ToString() ?? string.Empty;
                    var totalPadding = columnWidths[ ii ] - currItem.Length;
                    var leftPadding = totalPadding / 2;

                    currItem = new string( ' ', leftPadding ) + currItem + new string( ' ', totalPadding - leftPadding );
                    if( ii == 0 )
                    {
                        rowString += "|";
                    }

                    rowString += currItem;
                    rowString += "|";
                }
            }

            dumpFunc( rowString );
        }
    }
}
