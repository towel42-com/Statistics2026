using Emby.Media.Common.Extensions;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

using TextValueLine = ((string start, string end)? range, string data, string itemId, string url, bool asTitle);

namespace Statistics2026.Utilities
{
    public class TextBasedStatCard : StatCard
    {
        public EListType ListType
        {
            get
            {
                if( ValueLines.Count == 1 )
                {
                    return EListType.eTextOnly;
                }
                return field;
            }

            set;
        } = EListType.eTextOnly;
        public bool IgnoreLength { get; set; } = false;
        private List<TextValueLine> ValueLines { get; set; }
        public override bool IsEmpty() { return ValueLines == null || ValueLines.Count == 0; }
        public TextBasedStatCard()
            : base()
        {
            ValueLines = [];
        }

        public TextBasedStatCard( string title, string? helpText, EStatCardStyle size = EStatCardStyle.eDetailed )
            : base( title, helpText, size )
        {
            ValueLines = [];
        }

        private string CheckMaxLength( string value )
        {
            return value;
        }

        public void AddLine( (string,string) range, string value, bool asTitle )
        {
            AddLine( value, string.Empty, string.Empty, asTitle, range );
        }

        public void AddLine( string value, bool asTitle )
        {
            AddLine( value, string.Empty, string.Empty, asTitle );
        }

        public void AddLine( string value, string itemId, string url, bool asTitle, (string, string)? range=null )
        {
            ValueLines.Add( (range, value, itemId, url, asTitle) );
        }

        public override bool IsClickableTable() { return false; }
        public override bool DataIsTable() { return false; }

        private string getDataHtmlForItem( TextValueLine currValue, string style )
        {
            if( currValue.data.IsNullOrEmpty() )
                return string.Empty;
            var value = currValue.data;
            if( ValueLines.Count() > 1 )
                value = CheckMaxLength( currValue.data );

            var dataHtml = string.Empty;
            if( ListType == EListType.eRangeOrdered )
            {
                if( currValue.range == null )
                    throw new NullReferenceException( "Range unordered data missing the range" );

                dataHtml =
                    $"<li>" +
                    $"    <span class=\"range-range\">{currValue.range.Value.start} to {currValue.range.Value.end}</span>\n" +
                    $"    <span class=\"range-value\">{value}</span>\n" +
                    $"</li>";
            }
            else 
            {
                dataHtml = $"<div class=\"{statCardClass( currValue.asTitle )}\" {style}>{value}</div>";
            }

            var showImage = !currValue.url.IsNullOrEmpty() && !currValue.itemId.IsNullOrEmpty();
            if( showImage )
            {
                dataHtml = ItemImageUrl.ItemUrlFromItemId( currValue.itemId, currValue.url, dataHtml, "50px" );
            }

            return dataHtml;
        }

        public override string GetDataString( int depth )
        {
            if( ListType != EListType.eTextOnly && ListType != EListType.eNumbered && ListType != EListType.eRangeOrdered )
            {
                throw new Exception( $"GetDataString called for non text only list type - ${ListType}" );
            }

            var retVal = string.Empty;
            if( ListType == EListType.eNumbered )
            {
                retVal += HelperFuncs.addToHtml( depth++, "<ol>" );
            }
            else if( ListType == EListType.eRangeOrdered )
            {
                retVal += HelperFuncs.addToHtml( depth++, "<ol class=\"range-list\">" );
            }

            for( var ii = 0; ii < ValueLines.Count; ++ii )
            {
                var html = getDataHtmlForItem( ValueLines[ ii ], string.Empty );
                if( ListType == EListType.eNumbered )
                {
                    html = $"<li>{html}</li>";
                }
                retVal += HelperFuncs.addToHtml( depth, html );
            }

            if( ListType == EListType.eNumbered )
            {
                retVal += HelperFuncs.addToHtml( --depth, "</ol>" );
            }
            else if( ListType == EListType.eUnordered )
            {
                retVal += HelperFuncs.addToHtml( --depth, "</ul>" );
            }

            return retVal;
        }
    };
}
