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
                retVal.Date = DateTime.Now;
                retVal.UserId = session.UserId;
                retVal.DeviceName = session.DeviceName;
                retVal.ClientName = session.Client;
                retVal.ItemId = GetNowPlayingItemId(serverItem);
                retVal.ItemName = GetItemName(session.NowPlayingItem);
                retVal.PlaybackMethod = GetPlaybackMethod(session);
                retVal.TranscodeReasons = GetTranscodingReasons(session);
                retVal.ItemType = session.NowPlayingItem.Type;
                retVal.RemoteAddress = session.RemoteEndPoint.ToString();
                retVal._serverItem = serverItem;
                _PlaybackTracker[key] = retVal;

                var db = StatisticsDB.GetInstance(embyInterfaces);
                db.LoadPlaybackState(ref retVal);
            }

            if (session.PlayState.PositionTicks != null)
            {
                retVal.setPlaybackPosition(session.PlayState.PositionTicks);
            }

            return retVal;
        }

        public void setPlaybackPosition(long? ticks)
        {
            if (ticks == null)
                return;

            if ((StartPlaybackPositionTicks == long.MaxValue) || (ticks < StartPlaybackPositionTicks))
                StartPlaybackPositionTicks = ticks ?? 0;

            if ((EndPlaybackPositionTicks == long.MinValue) || (ticks > EndPlaybackPositionTicks!))
                EndPlaybackPositionTicks = ticks ?? 0;
        }

        public static void RemoveOldPlayinfo(List<PlaybackInfo> activeSessions, EmbyInterfaces embyInterfaces)
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

                    var db = StatisticsDB.GetInstance(embyInterfaces);
                    db.UpdatePlaybackState(playbackInfo);

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

        private static string GetTranscodingReasons(SessionInfo session)
        {
            string retVal = string.Empty;

            if (session.TranscodingInfo != null &&
                session.TranscodingInfo.TranscodeReasons != null &&
                session.TranscodingInfo.TranscodeReasons.Length > 0)
            {
                List<string> reasons = new List<string>();
                foreach (var reason in session.TranscodingInfo.TranscodeReasons)
                {
                    reasons.Add(reason.ToString());
                }
                reasons.Sort();
                retVal = string.Join(" ", reasons);
            }
            return retVal;
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


        public string Key { set; get; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.MinValue;

        public string UserId { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string PlaybackMethod { get; set; } = string.Empty;
        public string TranscodeReasons { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string RemoteAddress { get; set; } = string.Empty;
        public long StartPlaybackPositionTicks { get; set; } = long.MaxValue;
        public long EndPlaybackPositionTicks { get; set; } = long.MinValue;

        private BaseItem? _serverItem { get; set; } = null;
        private static Dictionary<string, PlaybackInfo>? _PlaybackTracker = null;
    }
}
