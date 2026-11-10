using Emby.Media.Common.Extensions;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

using TextValueLine = (string data, string itemId, string url, bool asTitle);

namespace Statistics2026.Utilities
{
    public enum EListType
    {
        eTextOnly,
        eUnordered,
        eNumbered
    }

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

        public void AddLine( string value, bool asTitle )
        {
            AddLine( value, string.Empty, string.Empty, asTitle );
        }

        public void AddLine( string value, string itemId, string url, bool asTitle )
        {
            ValueLines.Add( (value, itemId, url, asTitle) );
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

            var dataHtml = $"<div class=\"{statCardClass( currValue.asTitle )}\" {style}>{value}</div>";

            var showImage = !currValue.url.IsNullOrEmpty() && !currValue.itemId.IsNullOrEmpty();
            if( showImage )
            {
                dataHtml = ItemImageUrl.ItemUrlFromItemId( currValue.itemId, currValue.url, dataHtml, "50px" );
            }

            return dataHtml;
        }

        public override string GetDataString( int depth )
        {
            if( ListType != EListType.eTextOnly && ListType != EListType.eNumbered )
            {
                throw new Exception( $"GetDataString called for non text only list type - ${ListType}" );
            }

            var retVal = string.Empty;
            if( ListType == EListType.eNumbered )
            {
                retVal += HelperFuncs.addToHtml( depth++, "<ol>" );
            }

            for( var ii = 0; ii < ValueLines.Count; ++ii )
            {
                var html = getDataHtmlForItem( ValueLines[ ii ], string.Empty );
                if ( ListType == EListType.eNumbered )
                {
                    html = $"<li>{html}</li>";
                }
                retVal += HelperFuncs.addToHtml( depth, html );
            }

            if( ListType == EListType.eNumbered )
            {
                retVal += HelperFuncs.addToHtml( --depth, "</ol>" );
            }

            return retVal;
        }
    };
}
