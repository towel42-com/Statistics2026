using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using System;
using System.Linq;

namespace Statistics2026.Data
{
    public class MediaInfo : IDisposable
    {
        public MediaInfo() { }
        public MediaInfo(Video video)
        {
            var (primaryName, secondaryName, descName) = GetDescName(video);

            var mediaStream = video.GetMediaStreams().FirstOrDefault(s => s != null && s.Type == MediaStreamType.Video);
            if (!mediaStream?.Width.HasValue ?? true)
            {
                return;
            }

            var resolutionBase = GetMediaResolution(mediaStream, false);
            var resolutionDetail = GetMediaResolution(mediaStream, true);
            var codec = mediaStream?.Codec ?? "Unknown";
            var dvProfile = GetDolbyVisionProfile(mediaStream);

            ItemId = video.Id.ToString();
            IsEpisode = video is Episode;
            DescriptiveName = descName;
            PrimaryName = primaryName;
            SortName = video.SortName;
            SecondaryName = secondaryName;
            StartYear = video.ProductionYear?.ToString() ?? "Unknown";
            Season = video.ParentIndexNumber ?? -1;
            Episode = video.IndexNumber ?? -1;
            ResolutionDetail = resolutionDetail;
            ResolutionBase = resolutionBase;
            Codec = codec;
            DolbyVisionProfile = dvProfile;
            StudioNames = video.Studios;
            Genres = video.Genres;
            Rating = video.CommunityRating ?? 0.0;
            if (IsEpisode)
            {
                var episode = video as Episode;
                if (episode == null)
                {
                    throw new ArgumentNullException("episode was not an episode");
                }
                var series = episode!.Series;
                if (series != null)
                {
                    StudioNames = series.Studios;
                    Genres = series.Genres;
                    Rating = series.CommunityRating ?? 0.0;
                    SeriesId = series.Id.ToString();
                }
                IsTVSpecial = (episode.SortParentIndexNumber != null && episode.SortParentIndexNumber == 0) ||
                              (episode.ParentIndexNumber != null && episode.ParentIndexNumber == 0); // season 0 is the specials season
                if (episode.IndexNumber != null && episode.IndexNumberEnd != null)
                {
                    var start = episode.IndexNumber ?? -1;
                    var end = episode.IndexNumberEnd ?? start;
                    NumEpisodes = end - start + 1;
                }
            }

            ServerLocation = video.Path ?? "Unknown";
            FileSize = video.Size;
            RunTimeTicks = video.RunTimeTicks ?? 0;
            ImageUrl = ItemImageUrl._ItemImageUrl(video);

            TotalBitrate = video.TotalBitrate;
            if (video.PremiereDate.HasValue)
                PremiereDate = video.PremiereDate.Value.DateTime;
            DateAdded = video.DateCreated.DateTime;
        }

        public static (string primaryName, string secondaryName, string descName) GetDescName(Video video)
        {
            var primaryName = video.Name;
            var secondaryName = video.Name;
            var descName = video.Name;
            if (video is Episode episode)
            {
                primaryName = episode.SeriesName;
                descName = primaryName + " - " + secondaryName;
            }
            else
                secondaryName = "";
            return (primaryName, secondaryName, descName);
        }

        string GetMediaResolution(MediaStream? typeInfo, bool includeDetails)
        {
            if (typeInfo == null || typeInfo.Width == null)
                return Constants.NoResolution;

            int width = typeInfo.Width.Value;

            var details = string.Empty;
            if (includeDetails)
            {
                details = $" ({typeInfo.Width}x{typeInfo.Height})";
            }

            if (width >= 1281 && width <= 1920) return Constants.HD + details;
            if (width >= 3841 && width <= 7680) return Constants._8k + details;
            if (width >= 1921 && width <= 3840) return Constants._4k + details;
            if (width >= 1200 && width <= 1280) return Constants._720p + details;
            //if (width < 1200)
            return Constants.SD + details;
        }


        private string GetDolbyVisionProfile(MediaStream? mediaStream)
        {
            if (mediaStream == null)
                return Constants.MissingVideoStream;

            if (mediaStream.Profile == null)
                return Constants.UnknownDolbyProfile;

            var codec = mediaStream.Codec.ToUpper();
            if (codec != Constants.HEVC && codec != Constants.AV1)
                return Constants.NonDolbyVisionCompatibleCodec;

            var dvProfile = mediaStream.ExtendedVideoSubTypeDescription;
            if (dvProfile.ToLower() == "none")
                return Constants.NoDolbyProfile;

            return dvProfile;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            _disposed = true;
        }

        ~MediaInfo()
        {
            Dispose(false);
        }

        public string ItemId { get; set; } = String.Empty;
        public string DescriptiveName { get; set; } = String.Empty;
        public string PrimaryName { get; set; } = String.Empty;// movie title or series name
        public string SortName { get; set; } = String.Empty;
        public string SecondaryName { get; set; } = String.Empty; // episode name
        public string StartYear { get; set; } = String.Empty; // release year for movies, year of the of the first season of the TV show

        public bool IsEpisode { get; set; } = false;
        public bool IsTVSpecial { get; set; } = false;
        public string SeriesId { get; set; } = String.Empty;
        public int Season { get; set; } = 0;
        public int Episode { get; set; } = 0;
        public int NumEpisodes { get; set; } = 1;

        public string ResolutionBase { get; set; } = String.Empty;// just SD/HD/4k/8k etc
        public string ResolutionDetail { get; set; } = String.Empty;// includes details of resolution
        public string Codec { get; set; } = String.Empty;
        public string DolbyVisionProfile { get; set; } = String.Empty;
        public string[] StudioNames { get; set; } = { };
        public string[] Genres { get; set; } = { };
        public string ServerLocation { get; set; } = String.Empty;
        public long FileSize { get; set; } = 0;
        public string? ImageUrl { get; set; } = null;
        public long RunTimeTicks { get; set; } = 0;
        public double Rating { get; set; } = 0.0;
        public long TotalBitrate { get; set; } = 0;
        public DateTime? PremiereDate { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.MinValue;
        private bool _disposed = false;

    }
}
