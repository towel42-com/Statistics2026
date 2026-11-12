using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Logging;
using ServiceStack;
using SQLitePCL;
using SQLitePCL.pretty;
using Statistics2026;
using Statistics2026.Api;
using Statistics2026.Configuration;
using Statistics2026.Utilities;
using System.Data.Common;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Statistics2026.Utilities
{
    public class SortByText : IEquatable<SortByText>
    {
        public SortByText( string sortBy = "", string text = "" )
        {
            this.SortBy = sortBy;
            this.Text = text;
            if( Text.IsEmpty() )
                Text = SortBy;
            if( SortBy.IsEmpty() )
                SortBy = Text;
        }

        public string SortBy { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string JoinBy { get; set; } = ", ";

        public override bool Equals( object? obj )
        {
            return Equals( obj as SortByText );
        }

        public override int GetHashCode()
        {
            return HashCode.Combine( Text, SortBy );
        }

        public bool Equals( SortByText? other )
        {
            if( other is null )
                return false;
            if( ReferenceEquals( this, other ) )
                return true;

            return SortBy == other.SortBy && Text == other.Text;
        }

        public static bool operator ==( SortByText? left, SortByText? right )
        {
            if( left is null )
            {
                return right is null;
            }

            return left.Equals( right );
        }

        public static bool operator !=( SortByText? left, SortByText? right )
        {
            return !( left == right );
        }

        public static SortByText operator +( SortByText lhs, SortByText rhs )
        {
            if( lhs.JoinBy != rhs.JoinBy )
                throw new Exception( "Invalid join of two types of SortByText objects" );

            return new SortByText
            {
                Text = string.Join( lhs.JoinBy, new List<string> { lhs.Text, rhs.Text }.Where( s => !s.Trim().IsNullOrEmpty() ) ),
                SortBy = string.Join( lhs.JoinBy, new List<string> { lhs.SortBy, rhs.SortBy }.Where( s => !s.Trim().IsNullOrEmpty() ) )
            };
        }
    }
}
