using Emby.Media.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static Statistics2026.Data.StatCard;
using TextValueLine = (string data, string itemId, string url);

namespace Statistics2026.Data
{
    public class DynamicButton
    {
        public string id { get; set; } = string.Empty;
        public string info { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
    };

    public class StatCardResponse
    {
        public string html { get; set; } = string.Empty;
        public DynamicButton[] dynamicButtons { get; set; } = new DynamicButton[] { };

        public void addDynamicButton( DynamicButton button )
        {
            var local = dynamicButtons;
            Array.Resize( ref local, local.Length + 1 );
            local[ local.Length - 1 ] = button;
            dynamicButtons = local;
        }

        public static string _addToHtml( int depth, string _html )
        {
            if( _html.IsNullOrEmpty() )
                return string.Empty;

            var retVal = string.Empty;
            if( depth > 0 )
                retVal += new string( ' ', 4 * depth );
            retVal += _html;
            if( !retVal.EndsWith( "\n" ) )
                retVal += "\n";
            return retVal;
        }

        public void addToHtml( int depth, string _html )
        {
            html += _addToHtml( depth, _html );
        }

        public int cnt( string text, string substring )
        {
            var count = 0;
            var minIndex = text.IndexOf( substring, 0 );
            while( minIndex != -1 )
            {
                count++;
                // Advance index past the matched substring to find non-overlapping matches
                minIndex = text.IndexOf( substring, minIndex + substring.Length );
            }

            return count;
        }

        public void closeDivs( ref int depth )
        {
            while( depth > 0 )
            {
                addToHtml( --depth, "</div>" );
            }
        }
    };

    public enum EStatCardSize
    {
        eSmall,  // 33%
        eHalf,   // 50%
        eMedium, // 66%
        eLarge   // 100%
    };

    public abstract class StatCard
    {
        public string ToString( EStatCardSize size )
        {
            switch( size )
            {
                case EStatCardSize.eSmall:
                    return "small";
                case EStatCardSize.eHalf:
                    return "half";
                case EStatCardSize.eMedium:
                    return "medium";
                case EStatCardSize.eLarge:
                    return "large";
                default:
                    return string.Empty;
            }
        }

        public string Title { get; set; } = string.Empty;
        protected List<string>? Headers { get; set; } = null;

        public string SubTitle { get; set; } = string.Empty;

        public EStatCardSize Size { get; set; } = EStatCardSize.eSmall;
        public string HelpText { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string MediaItemId { get; set; } = string.Empty;

        public string HtmlDivId { get; set; } = string.Empty;
        public bool SortByKey { get; set; } = false;

        public enum EAlignment
        {
            eLeft,
            eRight,
            eCenter
        };

        public static string AlignmentText( EAlignment alignment )
        {
            switch( alignment )
            {
                case EAlignment.eLeft:
                    return "left";
                case EAlignment.eRight:
                    return "right";
                case EAlignment.eCenter:
                    return "center";
                default:
                    return string.Empty;
            }
        }

        public static string GetStyleString( EAlignment alignment )
        {
            var style = $"style=\"text-align: {AlignmentText( alignment )}; white-space: nowrap;\"";
            return style;
        }

        public static EAlignment GetAlignmentForColumn( int column, Dictionary<int, StatCard.EAlignment>? columnAlignment )
        {
            var colAlign = StatCard.EAlignment.eLeft;
            if( columnAlignment != null && columnAlignment.TryGetValue( column, out var columnAlign ) )
                colAlign = columnAlign;
            return colAlign;
        }
        public static string GetStyleString( int column, Dictionary<int, StatCard.EAlignment>? columnAlignment )
        {
            return GetStyleString( GetAlignmentForColumn( column, columnAlignment ) );
        }

        public static string GetStyleString()
        {
            return GetStyleString( EAlignment.eLeft );
        }

        public StatCard()
        {
            Size = EStatCardSize.eHalf;
        }

        public StatCard( string title, string? helpText, EStatCardSize size = EStatCardSize.eHalf )
        {
            Title = title;
            HelpText = helpText ?? string.Empty;
            Size = size;
        }

        public abstract bool IsEmpty();
        public abstract string GetDataString( int depth = 0 );

        public virtual StatCard.EAlignment alignmentForColumn( int column )
        {
            return StatCard.EAlignment.eLeft;
        }

        private void addData( ref string retVal, int depth = 0 )
        {
            retVal = StatCardResponse._addToHtml( depth++, "<table>" );

            if( Headers != null )
            {
                retVal += StatCardResponse._addToHtml( depth++, "<tr>" );
                retVal += StatCardResponse._addToHtml( depth, "<td>&nbsp;</td>" );
                for( var ii = 0; ii < Headers.Count(); ++ii )
                {
                    var header = Headers[ ii ];
                    retVal += StatCardResponse._addToHtml( depth, $"<td {StatCard.GetStyleString( alignmentForColumn( ii ) )}>{header}</td>" );
                }

                retVal += StatCardResponse._addToHtml( --depth, "</tr>" );
            }

            retVal += GetDataString( depth );

            retVal += StatCardResponse._addToHtml( --depth, "</table>" );
        }

        public override string ToString()
        {
            return ToString( 0 );
        }

        public string ToString( int depth = 0 )
        {
            var retVal = string.Empty;
            if( IsEmpty() )
                return retVal;

            addData( ref retVal, depth );

            return retVal;
        }

        private void addHelp( ref StatCardResponse retVal, int depth )
        {
            if( !HelpText.IsNullOrEmpty() )
            {
                var id = Regex.Replace( Title, @"\s", string.Empty );

                retVal.addToHtml( depth, $"<div id=\"{id}\" class=\"infoBlock\"><i class=\"md-icon\">info</i></div>" );

                retVal.addDynamicButton( new DynamicButton { id = id, info = HelpText, title = Title } );
            }
        }

        private void addTitle( ref StatCardResponse retVal, int depth )
        {
            var showImage = !ImageUrl.IsNullOrEmpty() && !MediaItemId.IsNullOrEmpty();
            string? titleClass;
            if( showImage )
            {
                var itemUrl = ItemImageUrl.ItemUrl( MediaItemId, ImageUrl );
                retVal.addToHtml( depth, itemUrl );
                retVal.addToHtml( depth++, "<div>" );
                titleClass = "statCard-stats-title-left";
            }
            else
            {
                titleClass = "statCard-stats-title";
                retVal.addToHtml( depth++, "<div style=\"width: 100%;\">" );
            }

            if( !Title.IsNullOrEmpty() )
            {
                retVal.addToHtml( depth, $"<div class=\"{titleClass}\">{Title}</div>" );
            }
            if( !SubTitle.IsNullOrEmpty() )
            {
                retVal.addToHtml( depth, $"<div class=\"{titleClass}\">{SubTitle}</div>" );
            }

        }

        private int addData( int depth, ref StatCardResponse retVal )
        {
            var tableInfo = ToString( depth + 1 );

            if( !tableInfo.IsNullOrEmpty() )
            {
                retVal.addToHtml( depth++, $"<div class=\"statCard-stats-number\">" );
                retVal.addToHtml( 0, tableInfo );
                retVal.addToHtml( --depth, "</div>" );
            }

            return depth;
        }

        public object createStat( string rootDivName = "" )
        {
            var retVal = new StatCardResponse();

            if( !rootDivName.IsNullOrEmpty() )
            {
                rootDivName = $" id=\"{rootDivName}\"";
            }

            var depth = 0;
            retVal.addToHtml( depth++, $"<div class=\"col {ToString( Size )}\" {rootDivName}>" );
            retVal.addToHtml( depth++, "<div class=\"statCard\">" );
            retVal.addToHtml( depth++, "<div class=\"statCard-content\">" );

            addHelp( ref retVal, depth );
            addTitle( ref retVal, depth );

            depth = addData( depth, ref retVal );

            retVal.closeDivs( ref depth );
            return retVal;
        }
    }

    public class TextBasedStatCard : StatCard
    {
        public enum EListType
        {
            eUnordered,
            eNumbered,
            eNumberedGroupByKey
        }

        public EListType ListType
        {
            get
            {
                if( ( ValueLines.Count == 1 ) || ( KeyValueLines.Count == 0 ) )
                {
                    return EListType.eUnordered;
                }
                return field;
            }

            set;
        } = EListType.eUnordered;
        public bool IgnoreLength { get; set; } = false;
        private List<TextValueLine> ValueLines { get; set; }
        private List<string> KeyValueLines { get; set; }
        public override bool IsEmpty() { return ValueLines == null || ValueLines.Count == 0; }
        public TextBasedStatCard()
            : base()
        {
            ValueLines = [];
            KeyValueLines = [];
        }

        public TextBasedStatCard( string title, string? helpText, EStatCardSize size = EStatCardSize.eHalf )
            : base( title, helpText, size )
        {
            ValueLines = [];
            KeyValueLines = [];
        }

        private string CheckMaxLength( string value )
        {
            return value;
        }

        public void AddKey( string key )
        {
            KeyValueLines.Add( key );
        }

        public void AddLine( string value )
        {
            AddLine( value, string.Empty, string.Empty );
        }

        public void AddLine( string value, string itemId, string url )
        {
            ValueLines.Add( (value, itemId, url) );
        }

        public override string GetDataString( int depth = 0 )
        {
            if( ListType == EListType.eNumberedGroupByKey && ( KeyValueLines.Count != ValueLines.Count ) )
            {
                throw new Exception( "For grouped numbered lists, keys list must be of equal size to the values list" );
            }

            Dictionary<string, int>? keyCount = null;
            if( ListType == EListType.eNumberedGroupByKey )
            {
                keyCount = [];
                foreach( var key in KeyValueLines )
                {
                    if( keyCount.TryGetValue( key, out var value ) )
                    {
                        keyCount[ key ] = value + 1;
                    }
                    else
                    {
                        keyCount.Add( key, 1 );
                    }
                }
            }

            var retVal = string.Empty;
            var style = string.Empty;
            if( ListType != EListType.eUnordered )
            {
                style = GetStyleString( EAlignment.eLeft );
                retVal += StatCardResponse._addToHtml( depth++, $"<ol>" );
            }

            var prevKey = string.Empty;
            //int currKeyCount = 0;

            for( var ii = 0; ii < ValueLines.Count; ++ii )
            {
                var (data, itemId, url) = ValueLines[ ii ];

                if( data.IsNullOrEmpty() )
                    continue;
                var value = data;
                if( ValueLines.Count() > 1 )
                    value = CheckMaxLength( value );
                var dataHtml = $"<div class=\"statCard-stats-number\" {style}>{value}</div>";

                var showImage = !url.IsNullOrEmpty() && !itemId.IsNullOrEmpty();
                if( showImage )
                {
                    dataHtml = ItemImageUrl.ItemUrl( itemId, url, dataHtml, "50px" );
                }

                var html = dataHtml;

                if( ListType == EListType.eNumberedGroupByKey )
                {
                    if( prevKey != KeyValueLines[ ii ] )
                    {
                        if( !prevKey.IsNullOrEmpty() )
                        {
                            if( keyCount!.TryGetValue( prevKey, out var prevCnt ) )
                            {
                                if( prevCnt > 1 )
                                {
                                    retVal += StatCardResponse._addToHtml( --depth, "</ul>" );
                                    retVal += StatCardResponse._addToHtml( --depth, "</li>" );
                                }
                            }
                        }

                        if( keyCount!.TryGetValue( KeyValueLines[ ii ], out var cnt ) )
                        {
                            if( cnt > 1 )
                            {
                                retVal += StatCardResponse._addToHtml( depth++, $"<li {style}>" );
                                retVal += StatCardResponse._addToHtml( depth++, "<ul>" );
                            }
                        }

                        prevKey = KeyValueLines[ ii ];
                    }
                }

                if( ListType != EListType.eUnordered )
                {
                    html = $"<li {style}>" + dataHtml + "</li>";
                }

                retVal += StatCardResponse._addToHtml( depth, html );
            }

            if( ListType == EListType.eNumberedGroupByKey )
            {
                if( keyCount!.TryGetValue( KeyValueLines[ KeyValueLines.Count - 1 ], out var cnt ) )
                {
                    if( cnt > 1 )
                    {
                        retVal += StatCardResponse._addToHtml( --depth, "</ul>" );
                        retVal += StatCardResponse._addToHtml( --depth, "</li>" );
                    }
                }
            }

            if( ListType != EListType.eUnordered )
            {
                retVal += StatCardResponse._addToHtml( --depth, "<ol>" );
            }

            return retVal;
        }
    };

    public class TableBasedStatCardRow
    {
        public string Name { get; private set; } = string.Empty;
        public List<long>? Values { get; private set; } = null;

        public TableBasedStatCardRow( string name, List<long>? values )
        {
            Name = name;
            Values = values;
        }

        public void setValues( List<long> values )
        {
            Values = values;
        }

        public string ToString( int depth = 0, StatCard.EAlignment keyColAlignment = EAlignment.eLeft, Dictionary<int, StatCard.EAlignment>? columnAlignment = null )
        {
            var retVal = StatCardResponse._addToHtml( depth++, $"<tr {StatCard.GetStyleString()}>" );

            retVal += StatCardResponse._addToHtml( depth, $"<td {StatCard.GetStyleString( keyColAlignment )}>{Name}</td>" );
            if( Values != null )
            {
                for( var ii = 0; ii < Values.Count(); ++ii )
                {
                    retVal += StatCardResponse._addToHtml( depth, $"<td {StatCard.GetStyleString( ii, columnAlignment )}>{Values[ ii ]}</td>" );
                }
            }

            retVal += StatCardResponse._addToHtml( --depth, "</tr>" );

            return retVal;
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
        private StatCard.EAlignment _keyColumnAlignment = EAlignment.eLeft;
        public override bool IsEmpty() { return Rows == null || Rows.Count == 0; }
        public TableBasedStatCard()
            : base()
        {
            Rows = [];
        }
        public TableBasedStatCard( string title, string helpText, List<string> headers, EStatCardSize size = EStatCardSize.eHalf )
            : base( title, helpText, size )
        {
            Rows = [];

            Headers = headers;
        }

        public void SetDataColumnAlignment( int columnNum, StatCard.EAlignment alignment )
        {
            _columnAlignment[ columnNum ] = alignment;
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

        public void addRow( string category, List<int> values )
        {
            var longValues = new List<long>();
            foreach( var value in values )
                longValues.Add( value );
            addRow( category, longValues );
        }

        public void addRow( string category, List<long> values )
        {
            var currRow = findRow( category );
            TableBasedStatCardRow row;
            if( currRow == -1 )
            {
                row = new TableBasedStatCardRow( category, null );
                Rows.Add( row );
            }
            else
            {
                row = Rows[ currRow ];
            }

            row.setValues( values );
        }

        public override StatCard.EAlignment alignmentForColumn( int column )
        {
            return StatCard.GetAlignmentForColumn( column, _columnAlignment );
        }

        public override string GetDataString( int depth = 0 )
        {
            var valuesToUse = Rows;
            if( SortByKey )
            {
                valuesToUse = valuesToUse.OrderBy( row => row.Name ).ToList();
            }

            var retVal = string.Empty;
            foreach( var row in valuesToUse )
            {
                retVal += row.ToString( depth, _keyColumnAlignment, _columnAlignment );
            }

            return retVal;
        }
    }
}