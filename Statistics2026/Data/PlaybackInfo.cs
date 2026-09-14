using Emby.ApiClient.Model;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using ServiceStack;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace Statistics2026.Data
{
    public class PlaybackInfo
    {
        public static PlaybackInfo Create(SessionInfo session, EmbyInterfaces embyInterfaces)
        {
            if (_PlaybackTracker == null)
                _PlaybackTracker = new Dictionary<string, PlaybackInfo>();

            var serverItem = embyInterfaces._libraryManager.GetItemById(session.NowPlayingItem.Id);

            var key = CreateKey(session, serverItem);
            PlaybackInfo? retVal = null;
            if (_PlaybackTracker.ContainsKey(key))
            {
                //_logger.Info("Existing tracker found! : " + key);
                retVal = _PlaybackTracker[key];
            }
            else
            {
                retVal = new PlaybackInfo();

                retVal.Key = key;
                retVal.UserId = session.UserId;
                retVal.ItemId = GetNowPlayingItemId(serverItem);
                retVal.ItemName = GetItemName(session.NowPlayingItem);

                _PlaybackTracker[key] = retVal;

                retVal.LoadPlaybackState(embyInterfaces);
            }

            if (session.PlayState.PositionTicks != null)
            {
                retVal.setPlaybackPosition(session.PlayState.PositionTicks.Value);
            }

            return retVal;
        }

        public static void RemoveInactivePlayinfo(List<PlaybackInfo> activeSessions, EmbyInterfaces embyInterfaces)
        {
            if (_PlaybackTracker == null)
                return;

            List<string> key_list = new List<string>();
            foreach (string key in _PlaybackTracker.Keys)
            {
                key_list.Add(key);
            }

            foreach (string key in key_list)
            {
                PlaybackInfo playbackInfo = _PlaybackTracker[key];
                if (activeSessions.Contains(playbackInfo) == false)
                {
                    embyInterfaces._logger.Info("Saving final duration for Item : " + key);

                    playbackInfo.UpdatePlaybackState(embyInterfaces);

                    embyInterfaces._logger.Info("Removing Old Key from playback_trackers : " + key);
                    _PlaybackTracker.Remove(key);
                }
            }
        }

        private static string GetNowPlayingItemId(BaseItem baseItem)
        {
            if (baseItem == null)
                return string.Empty;

            return baseItem.Id.ToString();
        }

        private static string CreateKey(SessionInfo session, BaseItem baseItem)
        {
            List<string> keyComponents = new List<string>()
            {
                session.DeviceId,
                session.UserId,
                baseItem.Id.ToString(),
                GetPlaybackMethod(session)
            };

            return keyComponents.Join("|");
        }

        static private string GetItemName(MediaBrowser.Model.Dto.BaseItemDto item)
        {
            string retVal = "Not Known";

            if (item == null)
            {
                return retVal;
            }

            if (item.Type == "Episode")
            {
                retVal = $"{item.SeriesName} - S{item.ParentIndexNumber:D2}E{item.IndexNumber:D2} - {item.Name}";
            }
            else if (item.Type == "Audio")
            {
                string artist = "Not Known";
                if (item.ArtistItems != null && item.AlbumArtists.Length > 0)
                {
                    List<string> artists_list = new List<string>();
                    foreach (var artist_pair in item.AlbumArtists)
                    {
                        artists_list.Add(artist_pair.Name);
                    }
                    artist = string.Join(", ", artists_list);
                }
                string album = "Not Known";
                if (string.IsNullOrEmpty(item.Album) == false)
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

        private static string GetPlaybackMethod(SessionInfo session)
        {
            string retVal = "na";
            if (session.PlayState != null && session.PlayState.PlayMethod != null)
            {
                retVal = session.PlayState.PlayMethod.Value.ToString();
            }
            if (session.PlayState != null && session.PlayState.PlayMethod == MediaBrowser.Model.Session.PlayMethod.Transcode)
            {
                if (session.TranscodingInfo != null)
                {
                    string video_codec = "direct";
                    if (session.TranscodingInfo.IsVideoDirect == false)
                    {
                        video_codec = session.TranscodingInfo.VideoCodec;
                    }
                    string audio_codec = "direct";
                    if (session.TranscodingInfo.IsAudioDirect == false)
                    {
                        audio_codec = session.TranscodingInfo.AudioCodec;
                    }
                    retVal += " (v:" + video_codec + " a:" + audio_codec + ")";
                }
            }

            return retVal;
        }

        public void LoadPlaybackState(EmbyInterfaces embyInterfaces)
        {
            var db = StatisticsDB.GetInstance(embyInterfaces);

            long? totalTicks = db.GetTotalTicksPlayed(UserId, ItemId);

            TotalTicks = totalTicks ?? 0;
        }

        public void setPlaybackPosition(long? ticks)
        {
            if (ticks == null)
                return;

            if (!hasInitialPlaybackPosition() || SessionStartTickPos!.Value > ticks.Value)
                SessionStartTickPos = ticks;
            else
                SessionCurrTickPos = ticks;
        }

        public void UpdatePlaybackState(EmbyInterfaces embyInterfaces)
        {
            if (SessionStartTickPos == null || SessionCurrTickPos == null)
                return;

            TotalTicks += SessionCurrTickPos.Value - SessionStartTickPos.Value;
            var db = StatisticsDB.GetInstance(embyInterfaces);
            db.UpdateTotalTicksPlayed(UserId, ItemId, TotalTicks);


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
        public long TotalTicks { get; private set; } = 0;

        private static Dictionary<string, PlaybackInfo>? _PlaybackTracker = null;

        public long? SessionStartTickPos { get; private set; } = null;
        public long? SessionCurrTickPos { get; private set; } = null;
    }
}
