using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaBrowser.Model.IO;
using Statistics2026.Data;


using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;

using MediaBrowser.Model.Entities;

using MediaBrowser.Model.Logging;
using System.Net.Mime;
using Statistics2026.Api;

namespace Statistics2026.Data
{
    public class MediaInfo
    {
        public MediaInfo() { }
        public MediaInfo(Video video, IFileSystem fileSystem)
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
            ServerLocation = video.Path ?? "Unknown";
            FileSize = (fileSystem != null) ? fileSystem.GetFileSystemInfo(video.Path).Length : 0;
            RunTimeTicks = video.RunTimeTicks ?? 0;
            ImageUrl = ItemImageUrl._ItemImageUrl(video, ImageType.Primary, 400, 90);
            Rating = video.CommunityRating ?? 0.0;
        }

        public (string primaryName, string secondaryName, string descName) GetDescName(Video video)
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

        string GetMediaResolution(MediaStream typeInfo, bool includeDetails)
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


        private string GetDolbyVisionProfile(MediaStream mediaStream)
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

        public string ItemId { get; set; }
        public string DescriptiveName { get; set; }
        public string PrimaryName { get; set; } // movie title or series name
        public string SortName { get; set; }
        public string SecondaryName { get; set; } // episode name
        public string StartYear { get; set; } // release year for movies, year of the of the first season of the TV show

        public bool IsEpisode { get; set; }
        public int Season { get; set; }
        public int Episode { get; set; }

        public string ResolutionBase { get; set; } // just SD/HD/4k/8k etc
        public string ResolutionDetail { get; set; } // includes details of resolution
        public string Codec { get; set; }
        public string DolbyVisionProfile { get; set; }
        public string[] StudioNames { get; set; }
        public string ServerLocation { get; set; }
        public long FileSize { get; set; }
        public string ImageUrl { get; set; }
        public long RunTimeTicks { get; set; }
        public double Rating { get; set; }
    }
}
