using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using ServiceStack;
using Statistics2026.Data;
using System.Collections.Generic;

namespace Statistics2026.Utilities
{
    public class PlaybackInfo
    {
        public static async Task<PlaybackInfo> Create( SessionInfo session, EmbyInterfaces embyInterfaces )
        {
            _PlaybackTracker ??= [];

            BaseItem serverItem = embyInterfaces._libraryManager.GetItemById( session.NowPlayingItem.Id );

            string key = CreateKey( session, serverItem );
            PlaybackInfo? retVal;
            if( _PlaybackTracker.ContainsKey( key ) )
            {
                retVal = _PlaybackTracker[ key ];
            }
            else
            {
                retVal = new PlaybackInfo
                {
                    Key = key,
                    UserId = session.UserId,
                    ItemId = GetNowPlayingItemId( serverItem ),
                    ItemName = GetItemName( session.NowPlayingItem )
                };

                _PlaybackTracker[ key ] = retVal;

                await retVal.LoadPlaybackState( embyInterfaces );
            }

            if( session.PlayState.PositionTicks != null )
            {
                retVal.setPlaybackPosition( session.PlayState.PositionTicks.Value );
            }

            return retVal;
        }

        public static void RemoveInactivePlayinfo( List<PlaybackInfo> activeSessions, EmbyInterfaces embyInterfaces )
        {
            if( _PlaybackTracker == null )
                return;

            List<string> key_list = [ .. _PlaybackTracker.Keys ];

            foreach( string key in key_list )
            {
                PlaybackInfo playbackInfo = _PlaybackTracker[ key ];
                if( activeSessions.Contains( playbackInfo ) == false )
                {
                    embyInterfaces.Logger().Info( "Saving final duration for Item : " + key );

                    playbackInfo.UpdatePlaybackState( embyInterfaces ).GetAwaiter().GetResult();

                    embyInterfaces.Logger().Info( "Removing Old Key from playback_trackers : " + key );
                    _ = _PlaybackTracker.Remove( key );
                }
            }
        }

        private static string GetNowPlayingItemId( BaseItem baseItem )
        {
            return baseItem == null ? string.Empty : baseItem.Id.ToString();
        }

        private static string CreateKey( SessionInfo session, BaseItem baseItem )
        {
            List<string> keyComponents =
            [
                session.DeviceId,
                session.UserId,
                baseItem.Id.ToString(),
                GetPlaybackMethod(session)
            ];

            return keyComponents.Join( "|" );
        }

        private static string GetItemName( MediaBrowser.Model.Dto.BaseItemDto item )
        {
            string retVal = "Not Known";

            if( item == null )
            {
                return retVal;
            }

            if( item.Type == "Episode" )
            {
                retVal = $"{item.SeriesName} - S{item.ParentIndexNumber:D2}E{item.IndexNumber:D2} - {item.Name}";
            }
            else if( item.Type == "Audio" )
            {
                string artist = "Not Known";
                if( item.ArtistItems != null && item.AlbumArtists.Length > 0 )
                {
                    List<string> artists_list = [];
                    foreach( NameIdPair? artist_pair in item.AlbumArtists )
                    {
                        artists_list.Add( artist_pair.Name );
                    }

                    artist = string.Join( ", ", artists_list );
                }

                string album = "Not Known";
                if( string.IsNullOrEmpty( item.Album ) == false )
                {
                    album = item.Album;
                }

                retVal = artist + " - " + item.Name + " (" + album + ")";

            }
            else
            {
                retVal = item.Name;
            }

            return retVal;
        }

        private static string GetPlaybackMethod( SessionInfo session )
        {
            string retVal = "na";
            if( session.PlayState != null && session.PlayState.PlayMethod != null )
            {
                retVal = session.PlayState.PlayMethod.Value.ToString();
            }

            if( session.PlayState != null && session.PlayState.PlayMethod == MediaBrowser.Model.Session.PlayMethod.Transcode )
            {
                if( session.TranscodingInfo != null )
                {
                    string video_codec = "direct";
                    if( session.TranscodingInfo.IsVideoDirect == false )
                    {
                        video_codec = session.TranscodingInfo.VideoCodec;
                    }

                    string audio_codec = "direct";
                    if( session.TranscodingInfo.IsAudioDirect == false )
                    {
                        audio_codec = session.TranscodingInfo.AudioCodec;
                    }

                    retVal += " (v:" + video_codec + " a:" + audio_codec + ")";
                }
            }

            return retVal;
        }

        public async Task LoadPlaybackState( EmbyInterfaces embyInterfaces )
        {
            await using Statistics2026DB db = await Statistics2026DB.GetInstance( embyInterfaces, GetType().Name );

            long? totalTicksPlayed = await db.GetTotalTicksPlayed( UserId, ItemId );

            TotalTicksPlayed = totalTicksPlayed ?? 0;
        }

        public void setPlaybackPosition( long? ticks )
        {
            if( ticks == null )
                return;

            if( !hasInitialPlaybackPosition() || SessionStartTickPos!.Value > ticks.Value )
                SessionStartTickPos = ticks;
            else
                SessionCurrTickPos = ticks;
        }

        public async Task UpdatePlaybackState( EmbyInterfaces embyInterfaces )
        {
            if( SessionStartTickPos == null || SessionCurrTickPos == null )
                return;

            TotalTicksPlayed += SessionCurrTickPos.Value - SessionStartTickPos.Value;
            await using Statistics2026DB db = await Statistics2026DB.GetInstance( embyInterfaces, GetType().Name );
            await db.UpdateTotalTicksPlayed( UserId, ItemId, TotalTicksPlayed );

            SessionStartTickPos = SessionCurrTickPos;
            SessionCurrTickPos = null;
        }

        private bool hasInitialPlaybackPosition()
        {
            return SessionStartTickPos != null;
        }

        public string Key { set; get; } = string.Empty;

        public string UserId { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public long TotalTicksPlayed { get; private set; } = 0;

        private static Dictionary<string, PlaybackInfo>? _PlaybackTracker = null;

        public long? SessionStartTickPos { get; private set; } = null;
        public long? SessionCurrTickPos { get; private set; } = null;
    }
}
