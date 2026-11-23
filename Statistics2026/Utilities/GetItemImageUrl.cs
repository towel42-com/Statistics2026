using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using ServiceStack;

namespace Statistics2026.Utilities
{
    public static class GetItemImageUrl
    {
        public static string ItemImageUrlFromItemId( string itemId, ILibraryManager? libManager, ILogger? logger = null, ImageType imageType = ImageType.Primary, int maxWidth = 400, int quality = 90, int imageIndex = 0 )
        {
            var guid = new Guid( itemId );
            return ItemImageUrlFromItemId( guid, libManager, logger, imageType, maxWidth, quality, imageIndex );
        }

        public static string ItemImageUrlFromItemId( long itemId, ILibraryManager? libManager, ILogger? logger = null, ImageType imageType = ImageType.Primary, int maxWidth = 400, int quality = 90, int imageIndex = 0 )
        {
            if( libManager == null )
                return string.Empty;

            BaseItem item = libManager.GetItemById( itemId );
            return item == null ? string.Empty : ItemImageUrlFromItem( item, logger, imageType, maxWidth, quality, imageIndex );
        }

        public static string ItemImageUrlFromItemId( Guid itemId, ILibraryManager? libManager, ILogger? logger = null, ImageType imageType = ImageType.Primary, int maxWidth = 400, int quality = 90, int imageIndex = 0 )
        {
            if( libManager == null )
                return string.Empty;

            BaseItem item = libManager.GetItemById( itemId );
            return item == null ? string.Empty : ItemImageUrlFromItem( item, logger, imageType, maxWidth, quality, imageIndex );
        }

        private static ConcurrentCache<(string itemId, ImageType imageType, int maxWidth, int quality, int index), string> sIdToUrlMap = new( 1000 );

        public static void ClearCache()
        {
            sIdToUrlMap.Clear();
        }

        public static string ItemImageUrlFromItem( BaseItem? item, ILogger? logger = null, ImageType imageType = ImageType.Primary, int maxWidth = 400, int quality = 90, int imageIndex = 0 )
        {
            if( item == null || item.Id.ToString().IsNullOrEmpty() )
                return string.Empty;

            (string, ImageType imageType, int maxWidth, int quality, int imageIndex) key = (item.Id.ToString(), imageType, maxWidth, quality, imageIndex);
            if( sIdToUrlMap.TryGetValue( key, out string? url ) )
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
            ItemImageInfo imageInfo = item.GetImageInfo( imageType, imageIndex );
            if( imageInfo == null )
            {
                logger?.Debug( $"Key {key} does not have an image info" );
                sIdToUrlMap.Add( key, string.Empty );
                return string.Empty;
            }

            string? imageTag = imageInfo?.DateModified.Ticks.ToString();
            string retVal = $"/emby/Items/{item.Id}/Images/{imageType}?maxWidth={maxWidth}&quality={quality}&tag={imageTag}";
            logger?.Debug( $"AddingKey {key} to url {retVal}" );
            sIdToUrlMap.Add( key, retVal );
            return retVal;
        }

        public static string ItemHtmlFromItem( BaseItem? item, string text, bool showImage, string height = "105px", string target = "" )
        {
            if( item == null )
                return text;

            string imageUrl = ItemImageUrlFromItem( item );
            if( imageUrl.IsNullOrEmpty() )
            {
                return text;
            }

            return ItemHtmlFromItemId( item.Id.ToString(), imageUrl, text, showImage, height, target );
        }


        public static string ItemHtmlFromItemId( string itemId, string itemUrl, string text, bool showImage, string height = "105px", string target = "" )
        {
            if( ( Plugin.Instance == null ) || ( Plugin.Instance.ServerId == null ) )
                throw new ArgumentNullException( "Plugin.Instance or Plugin.Instance.ServerId is null." );

            if( itemId == null || itemId.IsEmpty() )
                return "";

            string serverId = Plugin.Instance.ServerId;
            string imageUrl = $"/item?id={itemId}&serverId={serverId}";
            string retVal = ItemHtml( itemUrl, imageUrl, text, showImage, height, target );

            return retVal;
        }

        private static string ItemHtml( string imageUrl, string targetUrl, string text, bool showImage, string height, string target )
        {
            string retVal = $"<a is=\"emby-linkbutton\" ";
            if( !target.IsEmpty() )
            {
                retVal += "target=\"{target}\" ";
            }
            retVal += $"href=\"{targetUrl}\">";
            if( !imageUrl.IsEmpty() && showImage )
            {
                retVal += $"<img referrerpolicy=\"no-referrer\" loading=\"lazy\" src=\"{imageUrl}\" height=\"{height}\"/>";
            }
            retVal += $"{text}</a>";
            return retVal;
        }
    }
}
