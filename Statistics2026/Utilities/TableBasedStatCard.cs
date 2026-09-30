using System;
using System.Collections.Generic;
using System.Linq;

namespace Statistics2026.Utilities
{
    public class TableBasedStatCardRow
    {
        public string Name { get; private set; } = string.Empty;
        public List<object>? Values { get; set; } = null;
        public bool isClickable { get; set; } = false;

        public TableBasedStatCardRow( string name, List<object>? values, bool isClickable )
        {
            Name = name;
            Values = values;
        }

        public string ToString( int depth = 0, int numColumns = 0, StatCard.EAlignment keyColAlignment = StatCard.EAlignment.eLeft, Dictionary<int, StatCard.EAlignment>? columnAlignment = null, bool showCategory = true, Func<int, string, List<string>>? classesForValueFunc = null )
        {
            var rowText = "<tr ";
            var classes = string.Empty;
            if( isClickable )
            {
                List<string> classArray = [];
                classArray.Add( "clickable-row" );
                classes = string.Join( " ", classArray );
            }

            rowText += $"class=\"{string.Join( " ", classes )}\" ";
            rowText += $"{StatCard.GetStyleString()}>";

            var retVal = StatCardResponse._addToHtml( depth++, rowText );
            if( showCategory )
            {
                var td = Name;
                if( isClickable )
                {
                    td = $"<span class=\"row-toggle\">▶</span> {Name}";
                }

                retVal += StatCardResponse._addToHtml( depth++, $"<td {StatCard.GetStyleString( keyColAlignment )}>" );
                retVal += StatCardResponse._addToHtml( depth, td );
                retVal += StatCardResponse._addToHtml( --depth, $"</td>" );
            }

            if( Values != null )
            {
                for( var ii = 0; ii < Values.Count(); ++ii )
                {
                    var td = Values[ ii ].ToString();
                    if( !showCategory && ( Values.Count == 1 ) && ( ii == 0 ) && isClickable )
                    {
                        td = $"<span class=\"row-toggle\">▶</span>{td}";
                    }

                    var colSpan = "colSpan=\"1\"";
                    if( Values.Count() != numColumns )
                    {
                        colSpan = $"colSpan=\"{numColumns}\"";
                    }

                    classes = string.Empty;
                    if( classesForValueFunc != null )
                    {
                        var classArray = classesForValueFunc( ii, Values[ ii ].ToString() );
                        if( classArray.Count != 0 )
                        {
                            classes = $"class=\"{string.Join( " ", classArray )}\"";
                        }
                    }

                    retVal += StatCardResponse._addToHtml( depth, $"<td {classes} {colSpan} {GetStyleString( ii, columnAlignment )}>{td}</td>" );
                }
            }

            retVal += StatCardResponse._addToHtml( --depth, "</tr>" );

            return retVal;
        }

        public string GetStyleString( int columnNumber, Dictionary<int, StatCard.EAlignment>? columnAlignment )
        {
            StatCard.EAlignment colAlign = StatCard.EAlignment.eUnset;
            if( columnAlignment != null )
            {
                columnAlignment.TryGetValue( columnNumber, out colAlign );
            }
            return StatCard.GetStyleString( colAlign );
        }

        public override string ToString()
        {
            return ToString( 0 );
        }
    }

    public class TableBasedStatCard : StatCard
    {
        private readonly List<TableBasedStatCardRow> Rows;
        private readonly Dictionary<int, StatCard.EAlignment> _columnAlignment = [];
        private Func<int, string, List<string>>? _classesForColumnFunc = null;
        private StatCard.EAlignment _keyColumnAlignment = EAlignment.eLeft;
        public override bool IsEmpty() { return Rows == null || Rows.Count == 0; }
        public bool ShowCategory { get; set; } = true;
        public int? ColumnCount { get; private set; } = null;
        public TableBasedStatCard()
            : base()
        {
            Rows = [];
        }

        public TableBasedStatCard( string title, string helpText, List<HeaderDef> headers, EStatCardStyle size = EStatCardStyle.eDetailed )
            : base( title, helpText, size )
        {
            Rows = [];
            Headers = [ headers ];
        }

        public TableBasedStatCard( string title, string helpText, List<List<HeaderDef>> headers, EStatCardStyle size = EStatCardStyle.eDetailed )
            : base( title, helpText, size )
        {
            Rows = [];
            Headers = headers;
        }

        public TableBasedStatCard( string title, string helpText, List<string> headers, EStatCardStyle size = EStatCardStyle.eDetailed )
            : base( title, helpText, size )
        {
            Rows = [];
            List<HeaderDef> tmp = [];
            foreach( var curr in headers )
                tmp.Add( new HeaderDef( curr ) );
            Headers = [ tmp ];
        }

        public TableBasedStatCard( string title, string helpText, List<List<string>> headers, EStatCardStyle size = EStatCardStyle.eDetailed )
            : base( title, helpText, size )
        {
            Rows = [];

            Headers = [];
            foreach( var currRow in headers )
            {
                List<HeaderDef> tmp = [];
                foreach( var curr in currRow )
                    tmp.Add( new HeaderDef( curr ) );
                Headers.Add( tmp );
            }
        }

        public override bool ShowHeaderColumn( int columnNum )
        {
            return ShowCategory || ( columnNum != 0 );
        }

        public void SetDataColumnAlignment( int columnNum, StatCard.EAlignment alignment )
        {
            _columnAlignment[ columnNum ] = alignment;
        }

        public void SetClassForColumnFunc( Func<int, string, List<string>> classesForColumnFunc )
        {
            _classesForColumnFunc = classesForColumnFunc;
        }

        public void SetKeyColumnAlignment( StatCard.EAlignment alignment )
        {
            _keyColumnAlignment = alignment;
        }

        private int findRow( string name )
        {
            for( var i = 0; i < Rows.Count; i++ )
            {
                if( Rows[ i ].Name == name )
                    return i;
            }

            return -1;
        }

        public void addRow( string category, List<object> values, bool clickable = false )
        {
            var currRow = findRow( category );
            TableBasedStatCardRow row;
            if( currRow == -1 )
            {
                row = new TableBasedStatCardRow( category, null, clickable );
                Rows.Add( row );
                currRow = Rows.Count - 1;
            }
            else
            {
                row = Rows[ currRow ];
            }

            row.Values = values;
            row.isClickable = clickable;
            Rows[ currRow ] = row;

            UpdateColumnCount();
        }

        private void UpdateColumnCount()
        {
            for( var ii = 0; ii < Rows.Count; ++ii )
            {
                if( Rows[ ii ].isClickable )
                    continue;
                if( Rows[ ii ].Values == null )
                    continue;

                if( ColumnCount == null )
                {
                    ColumnCount = Rows[ ii ].Values!.Count;
                }
                else
                {
                    if( ColumnCount != Rows[ ii ].Values!.Count )
                        throw new Exception( "Invalid number of columns" );
                }
            }
        }

        public override StatCard.EAlignment alignmentForColumn( int column )
        {
            return StatCard.GetAlignmentForColumn( column, _columnAlignment );
        }

        public override bool IsClickableTable()
        {
            foreach( var row in Rows )
            {
                if( row.isClickable )
                    return true;
            }

            return false;
        }

        public override bool DataIsTable() { return true; }
        public override string GetDataString( int depth = 0 )
        {
            var valuesToUse = Rows;
            if( SortByKey )
            {
                valuesToUse = valuesToUse.OrderBy( row => row.Name ).ToList();
            }

            var retVal = string.Empty;
            var inClickableGroup = false;

            foreach( var row in valuesToUse )
            {
                if( row.isClickable && inClickableGroup )
                {
                    retVal += StatCardResponse._addToHtml( --depth, "</table>" );
                    retVal += StatCardResponse._addToHtml( --depth, "</div>" );
                    retVal += StatCardResponse._addToHtml( --depth, "</td>" );
                    retVal += StatCardResponse._addToHtml( --depth, "</tr>" );
                }

                retVal += row.ToString( depth, ColumnCount ?? 0, _keyColumnAlignment, _columnAlignment, ShowCategory, _classesForColumnFunc );

                if( row.isClickable )
                {
                    retVal += StatCardResponse._addToHtml( depth++, "<tr class=\"detail-row\">" );
                    retVal += StatCardResponse._addToHtml( depth++, $"<td colspan=\"{ColumnCount}\">" );
                    retVal += StatCardResponse._addToHtml( depth++, "<div class=\"detail-content\">" );
                    retVal += StatCardResponse._addToHtml( depth++, "<table style=\"width: 100%; border-collapse: collapse;\">" );
                    retVal += AddHeader( depth, true );

                    inClickableGroup = true;
                }
            }

            if( inClickableGroup )
            {
                retVal += StatCardResponse._addToHtml( --depth, "</table>" );
                retVal += StatCardResponse._addToHtml( --depth, "</div>" );
                retVal += StatCardResponse._addToHtml( --depth, "</td>" );
                retVal += StatCardResponse._addToHtml( --depth, "</tr>" );
            }

            return retVal;
        }
    }
}