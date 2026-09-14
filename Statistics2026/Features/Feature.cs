using Emby.Features;
using System;
using System.Collections.Generic;
using System.Text;

namespace Statistics2026
{
    public class Feature : IFeatureFactory
    {
        public const string StaticId = "Statistics2026";

        public List<FeatureInfo> GetFeatureInfos(string language)
        {
            return new List<FeatureInfo>
            {
                new FeatureInfo
                {
                    Id = StaticId,
                    Name = Plugin.StaticName,
                    FeatureType = FeatureType.User
                }
            };
        }
    }
}
