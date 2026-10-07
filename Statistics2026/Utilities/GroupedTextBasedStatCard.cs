using Emby.Media.Common.Extensions;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

using TextValueLine = (string data, string itemId, string url, bool asTitle);

namespace Statistics2026.Utilities
{
    public class GroupedTextBasedStatCard : StatCard
    {
        public EListType ListType { get; set; } = EListType.eUnordered;
        public bool IgnoreLength { get; set; } = false;
        private List<(TextValueLine key, List<TextValueLine> values)> Values { get; set; }
        private Dictionary<TextValueLine, int> ValueKeyPosMap { get; set; }
        public override bool IsEmpty() { return Values == null || Values.Count == 0; }
        public GroupedTextBasedStatCard()
            : base()
        {
            Values = [];
            ValueKeyPosMap = [];
        }

        public GroupedTextBasedStatCard( string title, string? helpText, EStatCardStyle size = EStatCardStyle.eDetailed )
            : base( title, helpText, size )
        {
            Values = [];
            ValueKeyPosMap = [];
        }

        private string CheckMaxLength( string value )
        {
            return value;
        }

        public void AddLine( string key, bool asTitle )
        {
            AddLine( (key, string.Empty, string.Empty, asTitle), new TextValueLine() );
        }

        public void AddLine( string key, string value, bool asTitle )
        {
            AddLine( (key, string.Empty, string.Empty, false), (value, string.Empty, string.Empty, asTitle) );
        }

        public void AddLine( TextValueLine key, string value, bool asTitle )
        {
            AddLine( key, (value, string.Empty, string.Empty, asTitle) );
        }

        public void AddLine( string key, TextValueLine value )
        {
            AddLine( (key, string.Empty, string.Empty, false), value );
        }

        public void AddLine( TextValueLine key, TextValueLine value )
        {
            if( ValueKeyPosMap.TryGetValue( key, out var pos ) )
            {
                Values[ pos ].Item2.Add( value );
            }
            else
            {
                Values.Add( (key, new List<TextValueLine>() { value }) );
                ValueKeyPosMap[ key ] = Values.Count - 1;
            }
        }

        public override bool IsClickableTable() { return false; }
        public override bool DataIsTable() { return false; }

        private string getDataHtmlForItem( TextValueLine currValue, string style )
        {
            if( currValue.data.IsNullOrEmpty() )
                return string.Empty;
            var value = currValue.data;
            if( Values.Count() > 1 )
                value = CheckMaxLength( currValue.data );

            var dataHtml = $"<div class=\"{statCardClass( currValue.asTitle )}\" {style}>{value}</div>";

            var showImage = !currValue.url.IsNullOrEmpty() && !currValue.itemId.IsNullOrEmpty();
            if( showImage )
            {
                dataHtml = ItemImageUrl.ItemUrl( currValue.itemId, currValue.url, dataHtml, "50px" );
            }

            return dataHtml;
        }

        override public string GetDataString( int depth )
        {
            if( ListType != EListType.eNumbered && ListType != EListType.eUnordered )
            {
                throw new Exception( "GetDataStringUnordered called for a non-grouped or non-Unordered and non-Numbered list type" );
            }

            if( IsEmpty() )
                return string.Empty;

            var style = GetStyleString( EAlignment.eLeft );
            var rootListType = ( ListType == EListType.eUnordered ) ? "ul" : "ol";

            int getKeyCount( TextValueLine key )
            {
                if( ValueKeyPosMap.TryGetValue( key, out var values ) )
                {
                    return Values[ values ].values.Count;
                }
                return 0;
            }

            var retVal = string.Empty;
            retVal += StatCardResponse._addToHtml( depth++, $"<{rootListType}>" );

            for( var ii = 0; ii < Values.Count; ++ii )
            {
                if( ii != 0 )
                {
                    if( ( ListType == EListType.eUnordered ) || getKeyCount( Values[ ii - 1 ].key ) > 1 )
                    {
                        retVal += StatCardResponse._addToHtml( --depth, "</ul>" );
                        retVal += StatCardResponse._addToHtml( --depth, "</li>" );
                    }
                }

                if( ListType == EListType.eUnordered )
                {
                    //if( ii != 0 )
                    //    retVal += StatCardResponse._addToHtml( --depth, "</ul>" );

                    retVal += StatCardResponse._addToHtml( depth++, $"<li {style}>" + getDataHtmlForItem( Values[ ii ].key, style ) );
                }

                if( ( ListType == EListType.eUnordered ) || ( Values[ ii ].values.Count > 1 ) )
                {
                    retVal += StatCardResponse._addToHtml( depth++, $"<li {style}>" );
                    retVal += StatCardResponse._addToHtml( depth++, "<ul>" );
                }

                for( var jj = 0; jj < Values[ ii ].values.Count; ++jj )
                {
                    var dataHtml = getDataHtmlForItem( Values[ ii ].values[ jj ], style );
                    var html = $"<li {style}>" + dataHtml + "</li>";
                    retVal += StatCardResponse._addToHtml( depth, html );
                }
            }

            if( ( ListType == EListType.eUnordered ) || getKeyCount( Values[ Values.Count - 1 ].key ) > 1 )
            {
                retVal += StatCardResponse._addToHtml( --depth, "</ul>" );
                retVal += StatCardResponse._addToHtml( --depth, "</li>" );
            }

            retVal += StatCardResponse._addToHtml( --depth, $"</{rootListType}>" );

            return retVal;
        }
    }
}
