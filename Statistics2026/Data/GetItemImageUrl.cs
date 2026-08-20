using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Users;
using SQLitePCL.pretty;
using Statistics2026;
using Statistics2026.Api;
using Statistics2026.Configuration;
using Statistics2026.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;


namespace Statistics2026.Data
{
    public sealed class ItemImageUrl
    {
        public static string _ItemImageUrl(string itemId, ILibraryManager libManager, ImageType imageType, int maxWidth, int quality, int imageIndex = 0)
        {
            var guid = new Guid(itemId);
            return _ItemImageUrl(guid, libManager, imageType, maxWidth, quality, imageIndex);
        }

        public static string _ItemImageUrl(long itemId, ILibraryManager libManager, ImageType imageType, int maxWidth, int quality, int imageIndex = 0)
        {
            var item = libManager.GetItemById(itemId);
            if (item == null)
                return null;
            return _ItemImageUrl(item, imageType, maxWidth, quality, imageIndex);
        }

        public static string _ItemImageUrl(Guid itemId, ILibraryManager libManager, ImageType imageType, int maxWidth, int quality, int imageIndex = 0)
        {
            var item = libManager.GetItemById(itemId);
            if (item == null)
                return null;
            return _ItemImageUrl(item, imageType, maxWidth, quality, imageIndex);
        }

        public static string _ItemImageUrl(BaseItem item, ImageType imageType, int maxWidth, int quality, int imageIndex = 0)
        {
            if (item == null)
                return null;

            if (!item.HasImage(imageType, imageIndex))
                return null;

            var imageInfo = item.GetImageInfo(imageType, imageIndex);
            if (imageInfo == null)
                return null;

            var imageTag = imageInfo?.DateModified.Ticks.ToString();
            var retVal = $"/emby/Items/{item.Id}/Images/{imageType}?maxWidth={maxWidth}&quality={quality}&tag={imageTag}";
            return retVal;
        }
    }
}
