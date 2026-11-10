using Emby.Media.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Statistics2026.Utilities
{
    public class MediaInfo
    {
        private static ConcurrentDictionary<(string, bool isUserVideo, bool minimal), MediaInfo> sMediaInfoCache = [];
        private static DateTimeOffset? DateCacheCreated = null;
        private static object _padlock = new object();

        private static string KeyForVideo( Video video )
        {
            return video.Id.ToString();
        }

        private static bool OutOfDate( Video video, MediaInfo mediaInfo )
        {
            if( video == null )
                return true;

            if( video.HasDateModifiedChanged( mediaInfo.DateItemModified ) )
                return true;

            if( ( video.Played != ( mediaInfo.IsPlayed ?? false ) ) || ( video.PlayCount != ( mediaInfo.PlayCount ?? 0 ) ) )
                return true;

            return false;
        }

        public MediaInfo() { }

        private static void SetCachedValue( MediaInfo mediaInfo )
        {
            if( mediaInfo.aOK )
            {
                var cacheKey = (mediaInfo.ItemId, mediaInfo.HasUserData, mediaInfo.IsMinimal);
                sMediaInfoCache[ cacheKey ] = mediaInfo;
            }
        }

        private static MediaInfo? GetCachedValue( Video video, bool minimalOnly )
        {
            lock( _padlock )
            {
                if( DateCacheCreated == null || DateCacheCreated.Value.AddHours( 24 * 7 ) < DateTimeOffset.Now )
                {
                    sMediaInfoCache.Clear();
                    DateCacheCreated = DateTimeOffset.Now;
                }
            }

            var cacheKey = (KeyForVideo( video ), video.UserDataSet, minimalOnly);
            MediaInfo? cachedValue = null;
            if( !sMediaInfoCache.TryGetValue( cacheKey, out cachedValue ) )
            {
                sMediaInfoCache.TryGetValue( (KeyForVideo( video ), video.UserDataSet, !minimalOnly), out cachedValue );
            }

            if( cachedValue == null )
                return null;

            if( OutOfDate( video, cachedValue ) )
            {
                if( cachedValue.IsMinimal )
                    sMediaInfoCache.Remove( cacheKey, out var currValue );
                else
                    sMediaInfoCache.Remove( (KeyForVideo( video ), video.UserDataSet, false), out var currValue );
                return null;
            }

            if( minimalOnly || ( !minimalOnly && !cachedValue.IsMinimal ) )
                return cachedValue;

            var json = JsonSerializer.Serialize( cachedValue );
            var copy = JsonSerializer.Deserialize<MediaInfo>( json );
            if( copy == null )
                throw new Exception( "Problem deserializing" );
            copy.IsMinimal = false;

            return copy;
        }


        public static MediaInfo CreateMinimal( ILogger logger, Video video )
        {
            return CreateMinimal( logger, video, true );
        }

        private static MediaInfo CreateMinimal( ILogger logger, Video video, bool minimalOnly )
        {
            var cachedValue = GetCachedValue( video, minimalOnly );
            if( cachedValue != null )
                return cachedValue;

            var retVal = new MediaInfo() { IsMinimal = true };
            retVal.aOK = false;
            if( video == null )
                return retVal;

            var (primaryName, secondaryName, descName) = GetDescName( video );
            retVal.ItemId = video.Id.ToString();
            retVal.DescriptiveName = descName;
            retVal.PrimaryName = primaryName;
            retVal.SecondaryName = secondaryName;
            retVal.SortName = video.SortName;
            retVal.IsEpisode = video is Episode;
            retVal.NumEpisodes = numEpisodes( video );
            retVal.IsTVSpecial = isTVSpecial( video );
            retVal.SeriesId = seriesId( video );
            retVal.RunTimeTicks = video.RunTimeTicks ?? 0;
            retVal.DateItemModified = video.DateModified;
            retVal.HasUserData = video.UserDataSet;
            retVal.PlayCount = null;
            retVal.IsPlayed = null;
            retVal.TotalTicksPlayed = null;
            if( video.UserDataSet )
            {
                retVal.PlayCount = video.PlayCount;
                retVal.IsPlayed = video.Played;
                retVal.TotalTicksPlayed = 0;

                var playCount = retVal.PlayCount ?? 0;
                var isPlayed = retVal.IsPlayed ?? false;
                if( isPlayed && ( playCount > 0 ) )
                    retVal.TotalTicksPlayed = ( video.PlayCount * video.RunTimeTicks );
            }

            if( video is Episode episode )
            {
                retVal.ListDisplayName = GetDisplayName( retVal.PrimaryName, secondaryName, retVal.Season, retVal.Episode );
                if( episode.Series != null )
                {
                    retVal.SortName = episode.Series.SortName + " - " + episode.SortName;
                    retVal.SeriesImageUrl = ItemImageUrl._ItemImageUrl( episode.Series, logger );
                    if( retVal.PremiereDate == null && episode.Series.PremiereDate.HasValue )
                    {
                        retVal.PremiereDate = episode.Series.PremiereDate.Value;
                    }
                }
            }
            else
            {
                retVal.ListDisplayName = retVal.PrimaryName;
            }
            retVal.aOK = !retVal.ItemId.IsNullOrEmpty() && ( retVal.RunTimeTicks != 0 );

            SetCachedValue( retVal );

            if( minimalOnly )
                return retVal;

            var json = JsonSerializer.Serialize( retVal );
            var copy = JsonSerializer.Deserialize<MediaInfo>( json );
            if( copy == null )
                throw new Exception( "Problem deserializing" );
            return copy;
        }

        public static MediaInfo Create( ILogger logger, Video video, TableDef? tableDef = null )
        {
            var cachedValue = GetCachedValue( video, false );
            if( cachedValue != null )
                return cachedValue;

            var retVal = CreateMinimal( logger, video, false );
            if( !retVal.aOK || ( retVal.aOK && !retVal.IsMinimal ) )
                return retVal;

            retVal.IsMinimal = false;

            var mediaStream = video.GetMediaStreams().FirstOrDefault( s => s != null && s.Type == MediaStreamType.Video );

            var resolutionBase = GetMediaResolution( mediaStream, false );
            var resolutionDetail = GetMediaResolution( mediaStream, true );

            var codec = mediaStream?.Codec ?? "Unknown";
            var dvProfile = GetDolbyVisionProfile( mediaStream );

            retVal.StartYear = video.ProductionYear?.ToString() ?? "Unknown";
            retVal.Season = video.ParentIndexNumber ?? -1;
            retVal.Episode = video.IndexNumber ?? -1;
            retVal.ResolutionDetail = resolutionDetail;
            retVal.ResolutionBase = resolutionBase;
            retVal.Codec = codec;
            retVal.DolbyVisionProfile = dvProfile;
            retVal.StudioNames = studioNames( video );
            retVal.Genres = genres( video );
            retVal.Rating = communityRating( video );

            retVal.ServerLocation = video.Path ?? "Unknown";
            retVal.FileSize = video.Size;
            if( video.PremiereDate.HasValue )
                retVal.PremiereDate = video.PremiereDate.Value;

            retVal.ImageUrl = ItemImageUrl._ItemImageUrl( video );

            if( retVal.PremiereDate == null )
            {
                BaseItem? curr = video;
                while( curr != null && curr.PremiereDate == null )
                {
                    curr = curr.Parent;
                }
                if( curr != null && curr.PremiereDate != null )
                {
                    retVal.PremiereDate = curr.PremiereDate.Value;
                }
            }
            retVal.TotalBitrate = video!.TotalBitrate;
            retVal.DateAdded = video!.DateCreated;

            retVal.aOK = !retVal.ItemId.IsNullOrEmpty() && ( retVal.RunTimeTicks != 0 );
            SetCachedValue( retVal );
            return retVal;
        }

        public static string[] studioNames( Video video )
        {
            var retVal = video.Studios;
            if( video is not Episode episode )
                return retVal;

            var series = episode.Series;
            if( series != null )
            {
                retVal = series.Studios;
            }

            return retVal;
        }

        public static string[] genres( Video video )
        {
            var retVal = video.Genres;
            if( video is not Episode episode )
                return retVal;

            var series = episode.Series;
            if( series != null )
            {
                retVal = series.Genres;
            }

            return retVal;
        }

        public static double communityRating( Video video )
        {
            var retVal = video.CommunityRating ?? 0.0;
            if( video is not Episode episode )
                return retVal;

            var series = episode.Series;
            if( series != null )
            {
                retVal = series.CommunityRating ?? 0.0;
            }

            return retVal;
        }

        public static string seriesId( Video video )
        {
            var isEpisode = video is Episode;
            if( !isEpisode )
                return string.Empty;

            if( video is not Episode episode )
                return string.Empty;

            var series = episode.Series;
            return series != null ? series.Id.ToString() : string.Empty;
        }

        public static bool isTVSpecial( RemoteSearchResult result )
        {
            return ( result.SortParentIndexNumber ?? result.ParentIndexNumber ) == 0;
        }

        public static bool isTVSpecial( Video? video )
        {
            if( video == null )
                return false;

            var isEpisode = video is Episode;
            if( !isEpisode )
                return false;

            if( video is not Episode episode )
                return false;

            return ( episode.SortParentIndexNumber != null && episode.SortParentIndexNumber == 0 ) ||
              ( episode.ParentIndexNumber != null && episode.ParentIndexNumber == 0 ); // season 0 is the specials season
        }

        public static int numEpisodes( Video video )
        {
            var isEpisode = video is Episode;
            if( !isEpisode )
                return 0;

            if( video is not Episode episode )
                return 0;

            var retVal = 1;
            if( episode.IndexNumber != null && episode.IndexNumberEnd != null )
            {
                var start = episode.IndexNumber ?? -1;
                var end = episode.IndexNumberEnd ?? start;
                retVal = end - start + 1;
            }

            return retVal;
        }

        public static string GetDisplayName( Video? video )
        {
            if( video == null )
                return string.Empty;

            var retVal = video.Name;
            var episode = video as Episode;
            if( episode == null )
                return video.Name;

            var primName = episode.SeriesName;
            var secName = episode.Name;
            var season = video.ParentIndexNumber ?? -1;
            var episodeNum = video.IndexNumber ?? -1;

            return GetDisplayName( primName, secName, season, episodeNum );
        }

        public static string GetDisplayName( string primaryName, string secondaryName, int season, int episode )
        {
            if( ( season >= 0 ) && ( episode >= 0 ) && !secondaryName.IsNullOrEmpty() )
                return $"{primaryName} - S{season:D2}E{episode:D2} - {secondaryName}";
            return primaryName;
        }

        public static (string primaryName, string secondaryName, string descName) GetDescName( Video video )
        {
            var primaryName = video.Name;
            var secondaryName = video.Name;
            var descName = video.Name;
            if( video is Episode episode )
            {
                primaryName = episode.SeriesName;
                descName = primaryName + " - " + secondaryName;
            }
            else
            {
                secondaryName = string.Empty;
            }

            return (primaryName, secondaryName, descName);
        }

        private static string GetMediaResolution( MediaStream? typeInfo, bool includeDetails )
        {
            if( typeInfo == null || typeInfo.Width == null )
                return Constants.NoResolution;

            var width = typeInfo.Width.Value;

            var details = string.Empty;
            if( includeDetails )
            {
                details = $" ({typeInfo.Width}x{typeInfo.Height})";
            }

            if( width >= 1281 && width <= 1920 )
                return Constants.HD + details;
            if( width >= 3841 && width <= 7680 )
                return Constants._8k + details;
            if( width >= 1921 && width <= 3840 )
                return Constants._4k + details;
            if( width >= 1200 && width <= 1280 )
                return Constants._720p + details;
            //if (width < 1200)
            return Constants.SD + details;
        }

        private static string GetDolbyVisionProfile( MediaStream? mediaStream )
        {
            if( mediaStream == null )
                return Constants.MissingVideoStream;

            if( mediaStream.Profile == null )
                return Constants.UnknownDolbyProfile;

            var codec = mediaStream.Codec.ToUpper();
            if( codec != Constants.HEVC && codec != Constants.AV1 )
                return Constants.NonDolbyVisionCompatibleCodec;

            var dvProfile = mediaStream.ExtendedVideoSubTypeDescription;
            return dvProfile.ToLower() == "none" ? Constants.NoDolbyProfile : dvProfile;
        }

        public int? PlayCount { get; set; }
        public bool? IsPlayed { get; set; }
        public long? TotalTicksPlayed { get; set; }

        public string ItemId { get; set; } = string.Empty;
        public string DescriptiveName { get; set; } = string.Empty;
        public string PrimaryName { get; set; } = string.Empty;// movie title or series name
        public string ListDisplayName { get; set; } = string.Empty;// name suitible for displaying

        public string SortName { get; set; } = string.Empty;
        public string SecondaryName { get; set; } = string.Empty; // episode name
        public string StartYear { get; set; } = string.Empty; // release year for movies, year of the of the first season of the TV show

        public bool IsEpisode { get; set; } = false;
        public bool IsTVSpecial { get; set; } = false;
        public string SeriesId { get; set; } = string.Empty;
        public string? SeriesImageUrl { get; set; } = null;
        public int Season { get; set; } = 0;
        public int Episode { get; set; } = 0;
        public int NumEpisodes { get; set; } = 1;

        public string ResolutionBase { get; set; } = string.Empty;// just SD/HD/4k/8k etc
        public string ResolutionDetail { get; set; } = string.Empty;// includes details of resolution
        public string Codec { get; set; } = string.Empty;
        public string DolbyVisionProfile { get; set; } = string.Empty;
        public string[] StudioNames { get; set; } = { };
        public string[] Genres { get; set; } = { };
        public string ServerLocation { get; set; } = string.Empty;
        public long FileSize { get; set; } = 0;
        public string? ImageUrl { get; set; } = null;
        public long RunTimeTicks { get; set; } = 0;
        public double Rating { get; set; } = 0.0;
        public long TotalBitrate { get; set; } = 0;
        public DateTimeOffset? PremiereDate { get; set; } = null;
        public DateTimeOffset? DateAdded { get; set; } = null;
        public bool aOK { get; set; } = false;
        public DateTimeOffset DateItemModified { get; set; } = DateTimeOffset.MinValue;

        public bool IsMinimal { get; set; } = false;
        public bool HasUserData { get; set; } = false;
    }
}
