using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System;


namespace Statistics2026.Data
{
    public sealed class ItemImageUrl
    {
        public static string _ItemImageUrl(string itemId, ILibraryManager libManager, ImageType imageType=ImageType.Primary, int maxWidth=400, int quality=90, int imageIndex = 0)
        {
            var guid = new Guid(itemId);
            return _ItemImageUrl(guid, libManager, imageType, maxWidth, quality, imageIndex);
        }

        public static string _ItemImageUrl(long itemId, ILibraryManager libManager, ImageType imageType=ImageType.Primary, int maxWidth=400, int quality=90, int imageIndex = 0)
        {
            var item = libManager.GetItemById(itemId);
            if (item == null)
                return String.Empty;
            return _ItemImageUrl(item, imageType, maxWidth, quality, imageIndex);
        }

        public static string _ItemImageUrl(Guid itemId, ILibraryManager libManager, ImageType imageType=ImageType.Primary, int maxWidth=400, int quality=90, int imageIndex = 0)
        {
            var item = libManager.GetItemById(itemId);
            if (item == null)
                return String.Empty;
            return _ItemImageUrl(item, imageType, maxWidth, quality, imageIndex);
        }

        public static string _ItemImageUrl(BaseItem item, ImageType imageType=ImageType.Primary, int maxWidth=400, int quality=90, int imageIndex = 0)
        {
            if (item == null)
                return String.Empty;

            if (!item.HasImage(imageType, imageIndex))
                return String.Empty;

            var imageInfo = item.GetImageInfo(imageType, imageIndex);
            if (imageInfo == null)
                return String.Empty;

            var imageTag = imageInfo?.DateModified.Ticks.ToString();
            var retVal = $"/emby/Items/{item.Id}/Images/{imageType}?maxWidth={maxWidth}&quality={quality}&tag={imageTag}";
            return retVal;
        }

        public static string ItemUrl( string itemId, string serverId, string itemUrl, string text="", string height= "105px")
        {
            return $"<a is=\"emby-linkbutton\" href=\"/item?id={itemId}&serverId={serverId}\"><img src=\"{itemUrl}\" height=\"{height}\"/>{text}</a>";
        }
    }
}
