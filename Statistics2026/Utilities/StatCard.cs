using Emby.Media.Common.Extensions;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using TextValueLine = (string data, string itemId, string url, bool asTitle);

namespace Statistics2026.Utilities
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

    public enum EStatCardStyle
    {
        eCompact,
        eDetailed
    };

    public abstract class StatCard
    {
        public string ToString( EStatCardStyle size )
        {
            switch( size )
            {
                case EStatCardStyle.eDetailed:
                    return "detailed";
                case EStatCardStyle.eCompact:
                    return "compact";
                default:
                    return string.Empty;
            }
        }

        public string Title { get; set; } = string.Empty;
        protected List<string>? Headers { get; set; } = null;

        public string SubTitle { get; set; } = string.Empty;

        public EStatCardStyle Style { get; set; } = EStatCardStyle.eCompact;
        public string HelpText { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string MediaItemId { get; set; } = string.Empty;

        public bool SortByKey { get; set; } = false;
        public bool UseSeparators { get; set; } = false;
        public bool HideHeaders { get; set; } = false;

        public enum EAlignment
        {
            eUnset,
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
            var retVal = string.Empty;
            if( alignment != EAlignment.eUnset )
            {
                retVal = $"style=\"text-align: {AlignmentText( alignment )}; white-space: nowrap;\"";
            }
            else
            {
                retVal = $"style=\"white-space: nowrap;\"";
            }
            return retVal;
        }

        public static EAlignment GetAlignmentForColumn( int column, Dictionary<int, StatCard.EAlignment>? columnAlignment )
        {
            var colAlign = StatCard.EAlignment.eUnset;
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
            Style = EStatCardStyle.eDetailed;
        }

        public StatCard( string title, string? helpText, EStatCardStyle style = EStatCardStyle.eCompact )
        {
            Title = title;
            HelpText = helpText ?? string.Empty;
            Style = style;
        }

        public abstract bool IsEmpty();
        public abstract bool DataIsTable();
        public abstract string GetDataString( int depth = 0 );
        public abstract bool IsClickableTable();

        public virtual StatCard.EAlignment alignmentForColumn( int column )
        {
            return StatCard.EAlignment.eLeft;
        }

        public virtual bool ShowHeaderColumn( int columnNum )
        {
            return true;
        }

        protected string AddHeader( int depth = 0, bool embedded = false )
        {
            var retVal = string.Empty;
            if( HideHeaders )
                return retVal;

            if( Headers != null )
            {
                retVal += StatCardResponse._addToHtml( depth++, "<thead>" );
                retVal += StatCardResponse._addToHtml( depth++, "<tr>" );
                if( ShowHeaderColumn( 0 ) )
                    retVal += StatCardResponse._addToHtml( depth, "<td>&nbsp;</td>" );
                for( var ii = 0; ii < Headers.Count(); ++ii )
                {
                    if( !ShowHeaderColumn( ii + 1 ) )
                        continue;
                    if( embedded && ( ii == 0 ) )
                        retVal += StatCardResponse._addToHtml( depth, $"<td></td>" );
                    else
                    {
                        var header = Headers[ ii ];
                        retVal += StatCardResponse._addToHtml( depth, $"<td {StatCard.GetStyleString( alignmentForColumn( ii ) )}>{header}</td>" );
                    }
                }

                retVal += StatCardResponse._addToHtml( --depth, "</tr>" );
                retVal += StatCardResponse._addToHtml( --depth, "</thead>" );
            }
            return retVal;
        }

        private void addData( ref string retVal, int depth = 0 )
        {
            if( DataIsTable() )
            {
                retVal = StatCardResponse._addToHtml( depth++, "<table>" );

                retVal += AddHeader( depth );
                retVal += StatCardResponse._addToHtml( depth++, "<tbody>" );
                retVal += GetDataString( depth );
                retVal += StatCardResponse._addToHtml( --depth, "</tbody>" );

                retVal += StatCardResponse._addToHtml( --depth, "</table>" );
            }
            else
                retVal = GetDataString( depth );
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

        public string DivId( string suffix = "" )
        {
            var divId = Regex.Replace( Title, @"\s", string.Empty );
            if ( !suffix.IsNullOrEmpty() )
                divId += "-" + suffix;
            return divId;
        }

        private void addHelp( ref StatCardResponse retVal, ref int depth )
        {
            if( !HelpText.IsNullOrEmpty() )
            {
                var id = DivId();

                retVal.addToHtml( depth, $"<div id=\"{id}\" class=\"infoBlock\"><i class=\"md-icon\">info</i></div>" );

                retVal.addDynamicButton( new DynamicButton { id = id, info = HelpText, title = Title } );
            }
        }

        private void addTitle( ref StatCardResponse retVal, ref int depth )
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

        protected string statCardClass( bool asTitle )
        {
            if( asTitle )
                return "statCard-stats-title";
            return UseSeparators ? "statCard-stats-numbersep" : "statCard-stats-number";
        }

        private int addData( int depth, ref StatCardResponse retVal )
        {
            var tableInfo = ToString( depth + 1 );

            if( !tableInfo.IsNullOrEmpty() )
            {
                var divId = DivId( "statTable" );
                var divData = $"id=\"{divId}\" class=\"{statCardClass( false )}\"";
                if( IsClickableTable() )
                    divData += " data-is-clickable=\"true\"";

                retVal.addToHtml( depth++, $"<div {divData}>" );
                retVal.addToHtml( 0, tableInfo );
                retVal.addToHtml( --depth, "</div>" );
            }

            return depth;
        }

        public object createStat()
        {
            var retVal = new StatCardResponse();

            var depth = 0;
            retVal.addToHtml( depth++, $"<div class=\"col {ToString( Style )}\">" );
            retVal.addToHtml( depth++, "<div class=\"statCard\">" );
            retVal.addToHtml( depth++, "<div class=\"statCard-content\">" );

            var preHelpDepth = depth;
            addHelp( ref retVal, ref depth );
            addTitle( ref retVal, ref depth );

            depth = addData( depth, ref retVal );

            while( depth != preHelpDepth )
            {
                retVal.addToHtml( --depth, "</div>" );
            }
            retVal.addToHtml( --depth, "</div>" ); // statCard-content
            retVal.addToHtml( --depth, "</div>" ); // statCard

            if( IsClickableTable() )
            {
                var divId = DivId( "statTable" );
                retVal.addToHtml( depth++, "<img src=\"data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///w==\"" );
                retVal.addToHtml( depth, "style=\"display:none\"" );
                retVal.addToHtml( depth, $"onerror=\"console.info('trigger on loading dummy gif hit'); if(window.MyPluginHelpers && window.MyPluginHelpers.initCollapsibleTable){{window.MyPluginHelpers.initCollapsibleTable('{divId}'); }} else {{ throw new Error('Emby Table Engine Failure: window.MyPluginHelpers.initCollapsibleTable is not defined or loaded yet.'); }} \"" );
                retVal.addToHtml( --depth, "/>" );
            }

            retVal.addToHtml( --depth, "</div>" ); // col
            return retVal;
        }
    }
}