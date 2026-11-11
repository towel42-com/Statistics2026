using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using ServiceStack;
using System;

namespace Statistics2026.Utilities
{
    public static class ItemImageUrl
    {
        public static string _ItemImageUrl( string itemId, ILibraryManager? libManager, ILogger? logger = null, ImageType imageType = ImageType.Primary, int maxWidth = 400, int quality = 90, int imageIndex = 0 )
        {
            var guid = new Guid( itemId );
            return _ItemImageUrl( guid, libManager, logger, imageType, maxWidth, quality, imageIndex );
        }

        public static string _ItemImageUrl( long itemId, ILibraryManager? libManager, ILogger? logger = null, ImageType imageType = ImageType.Primary, int maxWidth = 400, int quality = 90, int imageIndex = 0 )
        {
            if( libManager == null )
                return string.Empty;

            var item = libManager.GetItemById( itemId );
            return item == null ? string.Empty : _ItemImageUrl( item, logger, imageType, maxWidth, quality, imageIndex );
        }

        public static string _ItemImageUrl( Guid itemId, ILibraryManager? libManager, ILogger? logger = null, ImageType imageType = ImageType.Primary, int maxWidth = 400, int quality = 90, int imageIndex = 0 )
        {
            if( libManager == null )
                return string.Empty;

            var item = libManager.GetItemById( itemId );
            return item == null ? string.Empty : _ItemImageUrl( item, logger, imageType, maxWidth, quality, imageIndex );
        }

        private static ConcurrentCache<(string itemId, ImageType imageType, int maxWidth, int quality, int index), string> sIdToUrlMap = new(1000);
        public static string _ItemImageUrl( BaseItem item, ILogger? logger = null, ImageType imageType = ImageType.Primary, int maxWidth = 400, int quality = 90, int imageIndex = 0 )
        {
            if( item == null || item.Id.ToString().IsNullOrEmpty() )
                return string.Empty;

            var key = (item.Id.ToString(), imageType, maxWidth, quality, imageIndex);
            if( sIdToUrlMap.TryGetValue( key, out var url ) )
            {
                logger?.Debug( $"Key {key} exists url {url}" );
                return url;
            }

            if( !item.HasImage( imageType, imageIndex ) )
            {
                logger?.Debug( $"Key {key} does not have an image" );
                sIdToUrlMap.Add( key, string.Empty );
                return string.Empty;
            }
            var imageInfo = item.GetImageInfo( imageType, imageIndex );
            if( imageInfo == null )
            {
                logger?.Debug( $"Key {key} does not have an image info" );
                sIdToUrlMap.Add( key, string.Empty );
                return string.Empty;
            }

            var imageTag = imageInfo?.DateModified.Ticks.ToString();
            var retVal = $"/emby/Items/{item.Id}/Images/{imageType}?maxWidth={maxWidth}&quality={quality}&tag={imageTag}";
            logger?.Debug( $"AddingKey {key} to url {retVal}" );
            sIdToUrlMap.Add( key, retVal );
            return retVal;
        }

        public static string ItemUrlFromItemId( string itemId, string itemUrl, string text = "", string height = "105px", string target = "" )
        {
            if( ( Plugin.Instance == null ) || ( Plugin.Instance.ServerId == null ) )
                throw new ArgumentNullException( "Plugin.Instance or Plugin.Instance.ServerId is null." );

            if( itemId == null || itemId.IsEmpty() )
                return "";

            var serverId = Plugin.Instance.ServerId;
            var imageUrl = $"/item?id={itemId}&serverId={serverId}";
            return ItemUrlFromImageUrl( imageUrl, itemUrl, text, height);
        }

        public static string ItemUrlFromImageUrl( string imageUrl, string targetUrl, string text = "", string height = "105px", string target = "" )
        {
            string retVal = $"<a is=\"emby-linkbutton\" ";
            if ( !target.IsEmpty() )
            {
                retVal += "target=\"{target}\" ";
            }
            retVal += $"href=\"{targetUrl}\">";
            if( !imageUrl.IsEmpty() )
            {
                retVal += $"<img loading=\"lazy\" src=\"{imageUrl}\" height=\"{height}\"/>";
            }
            retVal += $"{text}</a>";
            return retVal;
        }
    }
}
