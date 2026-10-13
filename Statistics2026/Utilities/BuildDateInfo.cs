using System;
using System.Linq;
using System.Reflection;

namespace Statistics2026.Utilities
{
    public static class BuildDateInfo
    {
        public static DateTimeOffset GetBuildDate()
        {
            var attribute = Assembly.GetExecutingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault( a => a.Key == "BuildDate" );

            if( attribute != null && DateTimeOffset.TryParse( attribute.Value, out DateTimeOffset buildDate ) )
            {
                return buildDate;
            }

            return DateTimeOffset.MinValue;
        }
    }
}