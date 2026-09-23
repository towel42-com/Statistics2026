using Emby.Media.Common.Extensions;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using TextValueLine = (string data, string itemId, string url, bool asTitle);

namespace Statistics2026.Utilities
{
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

        public TextBasedStatCard( string title, string? helpText, EStatCardStyle size = EStatCardStyle.eDetailed )
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
                var (data, itemId, url, asTitle) = ValueLines[ ii ];

                if( data.IsNullOrEmpty() )
                    continue;
                var value = data;
                if( ValueLines.Count() > 1 )
                    value = CheckMaxLength( value );


                var dataHtml = $"<div class=\"{statCardClass( asTitle )}\" {style}>{value}</div>";

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
}