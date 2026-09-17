using Emby.Features;
using System.Collections.Generic;

namespace Statistics2026.Features
{
    public class Feature : IFeatureFactory
    {
        public const string StaticId = "Statistics2026";

        public List<FeatureInfo> GetFeatureInfos( string language )
        {
            return
            [
                new FeatureInfo
                {
                    Id = StaticId,
                    Name = Plugin.StaticName,
                    FeatureType = FeatureType.User
                }
            ];
        }
    }
}
