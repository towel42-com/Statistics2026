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
        eTextOnly = 0x01,
        eUnordered = 0x02,
        eNumbered = 0x04,
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
                dataHtml = ItemImageUrl.ItemUrl( currValue.itemId, currValue.url, dataHtml, "50px" );
            }

            return dataHtml;
        }

        public override string GetDataString( int depth )
        {
            if( ListType != EListType.eTextOnly )
            {
                throw new Exception( "GetDataString called for non text only list type" );
            }

            var retVal = string.Empty;

            for( var ii = 0; ii < ValueLines.Count; ++ii )
            {
                var html = getDataHtmlForItem( ValueLines[ ii ], string.Empty );
                retVal += StatCardResponse._addToHtml( depth, html );
            }

            return retVal;
        }
    };
}
