using System.Runtime.InteropServices;

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage( AttributeTargets.Parameter, Inherited = false )]
    internal sealed class MaybeNullWhenAttribute : Attribute
    {
        public MaybeNullWhenAttribute( bool returnValue ) => ReturnValue = returnValue;

        public bool ReturnValue { get; }
    }
}

namespace System.Linq
{
    internal static class LinqExtensions
    {
        // Polyfill for the missing .ToHashSet() from the previous step
        public static HashSet<TSource> ToHashSet<TSource>( this IEnumerable<TSource> source, IEqualityComparer<TSource>? comparer = null )
        {
            if( source == null )
                throw new ArgumentNullException( nameof( source ) );
            return new HashSet<TSource>( source, comparer );
        }

        // Polyfill for the missing .Order() required by TZConvert.cs
        public static IOrderedEnumerable<TSource> Order<TSource>( this IEnumerable<TSource> source )
        {
            if( source == null )
                throw new ArgumentNullException( nameof( source ) );
            return source.OrderBy( x => x );
        }

        // Overload tracking with a custom comparer if the library needs it
        public static IOrderedEnumerable<TSource> Order<TSource>( this IEnumerable<TSource> source, IEqualityComparer<TSource> comparer )
        {
            if( source == null )
                throw new ArgumentNullException( nameof( source ) );
            return source.OrderBy( x => x );
        }
    }
}

namespace System
{
    // Keeping it internal keeps it completely invisible outside your specific plugin binary
    internal static class OperatingSystem
    {
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform( OSPlatform.Windows );

        public static bool IsMacOS() => RuntimeInformation.IsOSPlatform( OSPlatform.OSX );

        public static bool IsLinux() => RuntimeInformation.IsOSPlatform( OSPlatform.Linux );
    }

    internal readonly struct Index : IEquatable<Index>
    {
        private readonly int _value;
        public Index( int value, bool fromEnd = false ) => _value = fromEnd ? ( ~value ) : value;
        public int Value => _value < 0 ? ~_value : _value;
        public bool IsFromEnd => _value < 0;
        public int GetOffset( int length ) => IsFromEnd ? ( _value + length + 1 ) : _value;
        public bool Equals( Index other ) => _value == other._value;
        public override bool Equals( object obj ) => obj is Index other && Equals( other );
        public override int GetHashCode() => _value;
        public static implicit operator Index( int value ) => new Index( value );
    }

    internal readonly struct Range : IEquatable<Range>
    {
        public Index Start { get; }
        public Index End { get; }
        public Range( Index start, Index end ) { Start = start; End = end; }
        public static Range StartAt( Index start ) => new Range( start, new Index( 0, true ) );
        public static Range EndAt( Index end ) => new Range( new Index( 0 ), end );
        public static Range All => new Range( new Index( 0 ), new Index( 0, true ) );
        public bool Equals( Range other ) => Start.Equals( other.Start ) && End.Equals( other.End );
        public override bool Equals( object obj ) => obj is Range other && Equals( other );
        public override int GetHashCode() => HashCode.Combine( Start, End );
    }
}

#if !NET5_0_OR_GREATER // Only compiles if you are on an older framework
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false )]
    internal sealed class MemberNotNullWhenAttribute : Attribute
    {
        public bool ReturnValue { get; }
        public string[] Members { get; }

        public MemberNotNullWhenAttribute( bool returnValue, string member )
        {
            ReturnValue = returnValue;
            Members = new[] { member };
        }

        public MemberNotNullWhenAttribute( bool returnValue, params string[] members )
        {
            ReturnValue = returnValue;
            Members = members;
        }
    }
}
#endif

#if !NET5_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage( AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false )]
    // Toggle between public / internal if one causes an accessibility clash
    public sealed class MemberNotNullAttribute : Attribute
    {
        public string[] Members { get; }

        public MemberNotNullAttribute( string member )
        {
            Members = new[] { member };
        }

        public MemberNotNullAttribute( params string[] members )
        {
            Members = members;
        }
    }
}
#endif

#if !NET7_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false )]
    internal sealed class RequiredMemberAttribute : Attribute { }

    [AttributeUsage( AttributeTargets.All, AllowMultiple = true, Inherited = false )]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public string FeatureName { get; }
        public bool IsOptional { get; set; }

        public CompilerFeatureRequiredAttribute( string featureName )
        {
            FeatureName = featureName;
        }
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage( AttributeTargets.Constructor, AllowMultiple = false, Inherited = false )]
    internal sealed class SetsRequiredMembersAttribute : Attribute { }
}
#endif