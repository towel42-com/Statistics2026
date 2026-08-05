using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using CodecInfoPlugin.Calculators;
using CodecInfoPlugin.Models;
using CodecInfoPlugin.Models.Configuration;
using System.Net.Mime;

namespace CodecInfoPlugin.Helpers
{
    public class Calculator : BaseCalculator
    {
        private readonly IFileSystem _fileSystem;
        private readonly List<Movie> _allMovies;
        private readonly List<Episode> _allEpisodes;

        public Calculator(IUserManager userManager, ILibraryManager libraryManager,
            IUserDataManager userDataManager, IFileSystem fileSystem, ILogger logger,
            IProviderManager providerManager, CancellationToken cancellationToken)
            : base(userManager, libraryManager, userDataManager, providerManager, logger, cancellationToken)
        {
            _fileSystem = fileSystem;

            _allMovies = GetAllMovies().ToList();
            _allEpisodes = GetAllEpisodes().ToList();
        }

        private (string primaryName, string secondaryName, string descName) GetDescName(Video video)
        {
            var primaryName = video.Name;
            var secondaryName = video.Name;
            var descName = video.Name;
            if (video is Episode episode )
            {
                primaryName = episode.SeriesName;
                descName = primaryName + " - " + secondaryName;
            }
            else
                secondaryName = "";
            return (primaryName, secondaryName, descName);
        }

        private List<MediaInfo> CalculateMediaInfo(bool episodes)
        {
            List<Video> videoList;
            if (episodes)
                videoList = _allEpisodes.Cast<Video>().ToList();
            else
                videoList = _allMovies.Cast<Video>().ToList();

            var mediaTypeName = episodes ? "Episode" : "Movie";

            _logger.Debug($"CalculateMediaInfo - Starting {mediaTypeName} Analysis");
            var retVal = new List<MediaInfo>();
            foreach (var video in videoList)
            {
                try
                {
                    var (primaryName, secondaryName, descName) = GetDescName(video);

                    var mediaStream = video.GetMediaStreams().FirstOrDefault(s => s != null && s.Type == MediaStreamType.Video);
                    if (!mediaStream?.Width.HasValue ?? true)
                    {
                        _logger.Warn($"CalculateMediaInfo - {mediaTypeName} - {descName} has no video stream or width information.");
                        continue;
                    }

                    var resolution = GetMediaResolution(mediaStream, true);
                    var codec = mediaStream?.Codec ?? "Unknown";
                    var dvProfile = GetDolbyVisionProfile(mediaStream);
                    retVal.Add(new MediaInfo
                    {
                        Id = video.Id.ToString(),
                        IsEpisode = episodes,
                        PrimaryName = primaryName,
                        SortName = video.SortName,
                        SecondaryName = secondaryName,
                        StartYear = video.ProductionYear?.ToString() ?? "Unknown",
                        Season = video.ParentIndexNumber ?? -1,
                        Episode = video.IndexNumber ?? -1,
                        Resolution = resolution,
                        CodecName = codec,
                        DolbyVisionProfile = dvProfile,
                        ServerLocation = video.Path ?? "Unknown"
                    });

                    _logger.Debug($"CalculateMediaInfo -     Processed {mediaTypeName} - {descName} items processed");
                }
                catch (Exception ex)
                {
                    _logger.Error($"CalculateMediaInfo {video.SortName}: {ex.Message}");
                }
            }
            _logger.Debug($"CalculateMediaInfo - Finished {mediaTypeName} Analysis - {retVal.Count} items processed");
            return retVal;
        }

        public List<MediaInfo> CalculateMediaInfo()
        {
            var retVal = CalculateMediaInfo(true);
            retVal.AddRange(CalculateMediaInfo(false));

            return retVal;
        }

        public Dictionary< string, MediaCountModel > CalculateMediaResolutions( bool episodes )
        {
            List<Video> videoList;
            if (episodes)
                videoList = _allEpisodes.Cast<Video>().ToList();
            else
                videoList = _allMovies.Cast<Video>().ToList();

            var mediaTypeName = episodes ? "Episode" : "Movie";

            var qualityCounts = new Dictionary<string, MediaCountModel>();

            _logger.Debug($"CalculateMediaResolutions - Starting {mediaTypeName} Analysis");
            foreach (var video in videoList.Where(w => w.Name != null).OrderBy(x => x.SortName))
            {
                var (primaryName, secondaryName, descName) = GetDescName(video);

                try
                {
                    var quality = GetMediaResolution(video.GetMediaStreams().FirstOrDefault(s => s != null && s.Type == MediaStreamType.Video), false).Trim();
                    if (!qualityCounts.TryGetValue(quality, out var qualityModel))
                    {
                        qualityModel = new MediaCountModel { Name = quality, Movies = 0, Episodes = 0 };
                        qualityCounts[quality] = qualityModel;
                    }
                    if ( episodes)
                        qualityCounts[quality].Episodes++;
                    else
                        qualityCounts[quality].Movies++;
                    _logger.Debug($"CalculateMediaResolutions -    Processed - {mediaTypeName} - {descName} {quality}");
                }
                catch (Exception ex)
                {
                    _logger.Debug($"CalculateMediaResolutions - Error {descName}: {ex.Message}");
                }
            }
            _logger.Debug($"CalculateMediaResolutions - Finished {mediaTypeName} Analysis");
            return qualityCounts;
        }

        public ValueGroup CalculateMediaResolutions()
        {
            var qualityCounts = CalculateMediaResolutions(true); // Get episode resolutions
            var movieQualityCounts = CalculateMediaResolutions(false); // Get movie resolutions

            _logger.Debug($"CalculateMediaResolutions - Finished all video Resolution Analysis");

            _logger.Debug($"CalculateMediaResolutions - Merging results");
            // Merge the two dictionaries
            foreach (var kvp in movieQualityCounts)
            {
                if (!qualityCounts.TryGetValue(kvp.Key, out var qualityModel))
                {
                    qualityModel = new MediaCountModel { Name = kvp.Key, Movies = 0, Episodes = 0 };
                    qualityCounts[kvp.Key] = qualityModel;
                }
                qualityModel.Movies += kvp.Value.Movies;
                qualityModel.Episodes += kvp.Value.Episodes;
            }
            _logger.Debug($"CalculateMediaResolutions - Finished Merging results");

            return new ValueGroup
            {
                Title = Constants.MediaResolutions,
                ValueLineOne = $"<table><tr><td></td><td>Movies</td><td>Episodes</td></tr>{string.Join("", qualityCounts.Values)}</table>",
                ValueLineTwo = "",
                ValueLineThree = null,
                ExtraInformation = Constants.HelpMediaResolutions,
                Size = "half"
            };
        }

        string GetMediaResolution(MediaStream typeInfo, bool includeDetails)
        {
            if (typeInfo == null || typeInfo.Width == null)
                return "Resolution Not Available";

            int width = typeInfo.Width.Value;

            var details = string.Empty;
            if (includeDetails)
            {
                details = $" ({typeInfo.Width}x{typeInfo.Height})";
            }

            if (width >= 1281 && width <= 1920) return "1080p" + details;
            if (width >= 3841 && width <= 7680) return "8K" + details;
            if (width >= 1921 && width <= 3840) return "4K" + details;
            if (width >= 1200 && width <= 1280) return "720p" + details;
            //if (width < 1200) 
            return "SD" + details;

            //return "Resolution Not Available";
        }

        private string GetDolbyVisionProfile(MediaStream mediaStream)
        {
            if (mediaStream == null)
                return "Unknown Media";

            if (mediaStream.Profile == null)
                return "Unknown Dolby Profile";

            var codec = mediaStream.Codec.ToLower();
            if (codec != "hevc" && codec != "av1")
                return "Non Dolby Vision Compatible Codec";

            var dvProfile = mediaStream.ExtendedVideoSubTypeDescription;
            if (dvProfile.ToLower() == "none")
                return "No Dolby Profile";

            return dvProfile;
        }

        private bool AddDolbyVisionProfile(ref MediaStream mediaStream, ref Dictionary<string, MediaCountModel> dvProfiles, string mediaName, bool isMovie)
        {
            var dvProfile = GetDolbyVisionProfile(mediaStream);
            if (dvProfile == null || dvProfile == "")
                return false;

            if (!dvProfiles.TryGetValue(dvProfile, out var model))
            {
                model = new MediaCountModel { Name = dvProfile, Movies = 0, Episodes = 0 };
                dvProfiles[dvProfile] = model;
            }
            if (isMovie)
                dvProfiles[dvProfile].Movies++;
            else
                dvProfiles[dvProfile].Episodes++;

            _logger.Debug($"AddDolbyVisionProfile - {mediaName}: {dvProfile}");

            return true;
        }

        public ValueGroup CalculateDVProfileInfo(bool showUnknownDVProfileCount)
        {
            var dvProfiles = new Dictionary<string, MediaCountModel>();

            _logger.Debug($"CalculateDVProfileInfo - Starting Movie Analysis");
            foreach (var movie in _allMovies.Where(w => w.SortName != null).OrderBy(x => x.SortName))
            {
                try
                {
                    var mediaStream = movie.GetMediaStreams().FirstOrDefault(s => s != null && s.Type == MediaStreamType.Video);
                    AddDolbyVisionProfile(ref mediaStream, ref dvProfiles, movie.SortName, true);
                }
                catch (Exception ex)
                {
                    _logger.Debug($"CalculateDVProfileInfo-Error {movie.SortName}: {ex.Message}");
                }
            }

            _logger.Debug($"CalculateDVProfileInfo - Finished Movie Analysis");
            _logger.Debug($"CalculateDVProfileInfo - Starting Episode Analysis");
            foreach (var episode in _allEpisodes.Where(w => w.SortName != null).OrderBy(x => x.SortName))
            {
                try
                {
                    var mediaStream = episode.GetMediaStreams().FirstOrDefault(s => s != null && s.Type == MediaStreamType.Video);
                    AddDolbyVisionProfile(ref mediaStream, ref dvProfiles, episode.SortName, false);
                }
                catch (Exception ex)
                {
                    _logger.Debug($"CalculateDVProfileInfo-episode-Error {episode.SortName}: {ex.Message}");
                }
            }
            _logger.Debug($"CalculateDVProfileInfo - Finished Episode Analysis");

            var tableValueString = $"<table><tr><td></td><td>Movies</td><td>Episodes</td></tr>";

            if (showUnknownDVProfileCount)
            {
                bool foundUnknown = false;
                foreach (var entry in dvProfiles)
                {
                    if (CodecInfoPlugin.Configuration.PluginConfiguration.IsUnknownDolbyProfile(entry.Value.Name))
                    {
                        foundUnknown = true;
                        tableValueString += entry.Value.ToString();
                    }
                }
                if (!foundUnknown)
                {
                    tableValueString += "<tr><td>Unknown Dolby Profile</td><td>0</td><td>0</td></tr>";
                }
            }

            bool found50 = false;
            foreach (var entry in dvProfiles)
            {
                if (entry.Value.Name == "Profile 5.0")
                {
                    found50 = true;
                    tableValueString += entry.Value.ToString();
                }
            }

            if (!found50)
            {
                tableValueString += "<tr><td>Profile 5.0</td><td>0</td><td>0</td></tr>";
            }

            foreach (var entry in dvProfiles)
            {
                if (CodecInfoPlugin.Configuration.PluginConfiguration.IsUnknownDolbyProfile(entry.Value.Name))
                    continue;

                if (entry.Value.Name != "Profile 5.0")
                    tableValueString += entry.Value.ToString();
            }
            tableValueString += "</table>";

            return new ValueGroup
            {
                Title = Constants.DolbyVisionProfiles,
                ValueLineOne = tableValueString,
                ValueLineTwo = "",
                ValueLineThree = null,
                ExtraInformation = Constants.HelpDolbyVisionProfile,
                Size = "half"
            };
        }


        public ValueGroup CalculateMediaCodecs()
        {
            var codecCounts = new Dictionary<string, MediaCountModel>();

            _logger.Debug($"CalculateMediaCodecs - Starting Movie Analysis");
            foreach (var movie in _allMovies.Where(w => w.SortName != null).OrderBy(x => x.SortName))
            {
                try
                {
                    var codec = movie.GetMediaStreams().FirstOrDefault(s => s != null && s.Type == MediaStreamType.Video)?.Codec ?? "Unknown";
                    if (!codecCounts.TryGetValue(codec, out var codecModel))
                    {
                        codecModel = new MediaCountModel { Name = codec, Movies = 0, Episodes = 0 };
                        codecCounts[codec] = codecModel;
                    }
                    codecCounts[codec].Movies++;

                    _logger.Debug($"CalculateMediaCodecs {movie.SortName} {codec}");
                }
                catch (Exception ex)
                {
                    _logger.Debug($"CalculateMediaCodecs-Error {movie.SortName}: {ex.Message}");
                }
            }

            _logger.Debug($"CalculateMediaCodecs - Finished Movie Analysis");
            _logger.Debug($"CalculateMediaCodecs - Starting Episode Analysis");
            foreach (var episode in _allEpisodes.Where(w => w.SortName != null).OrderBy(x => x.SortName))
            {
                try
                {
                    var codec = episode.GetMediaStreams().FirstOrDefault(s => s != null && s.Type == MediaStreamType.Video)?.Codec ?? "Unknown";
                    if (!codecCounts.TryGetValue(codec, out var codecModel))
                    {
                        codecModel = new MediaCountModel { Name = codec, Movies = 0, Episodes = 0 };
                        codecCounts[codec] = codecModel;
                    }
                    codecCounts[codec].Episodes++;
                    _logger.Debug($"CalculateMediaCodecs-episode {(episode.Series?.SortName ?? "invalid name")}: {episode.SortName} {codec}");
                }
                catch (Exception ex)
                {
                    _logger.Debug($"CalculateMediaCodecs-episode-Error {episode.SortName}: {ex.Message}");
                }
            }

            _logger.Debug($"CalculateMediaCodecs - Finished Episode Analysis");
            return new ValueGroup
            {
                Title = Constants.MediaCodecs,
                ValueLineOne = $"<table><tr><td></td><td>Movies</td><td>Episodes</td></tr>{string.Join("", codecCounts.Values)}</table>",
                ValueLineTwo = "",
                ValueLineThree = null,
                ExtraInformation = Constants.HelpMediaCodecs,
                Size = "half"
            };
        }

        public MediaItemCollection CalculateEpisodeCodecItems()
        {
            var codecEpisodeMap = new Dictionary<string, List<CodecInfoPlugin.Models.MediaItem>>();

            foreach (var episode in _allEpisodes.Where(w => w.SortName != null).OrderBy(x => x.SortName))
            {
                _logger.Debug($"CalculateEpisodeCodecItems {episode.Name}");
                var codec = episode.GetMediaStreams().FirstOrDefault(s => s != null && s.Type == MediaStreamType.Video)?.Codec ?? "Unknown";

                if (!codecEpisodeMap.TryGetValue(codec, out var episodeList))
                {
                    episodeList = new List<CodecInfoPlugin.Models.MediaItem>();
                    codecEpisodeMap[codec] = episodeList;
                }

                var episodeName = "S" + episode.ParentIndexNumber.ToString().PadLeft(2, '0') + "E" + episode.IndexNumber.ToString().PadLeft(2, '0') + ": " + episode.Name;
                var mediaItem = new CodecInfoPlugin.Models.MediaItem { Id = episode.Id.ToString(), GroupName = episode.SeriesName, Title = episodeName, Year = episode.ProductionYear };
                episodeList.Add(mediaItem);

                if (codec == "Unknown")
                    _logger.Debug($"CalculateEpisodeCodecItems-Unknown {episode.Name}");
            }

            var list = codecEpisodeMap.Select(pair => new MediaItemGroup
            {
                Title = pair.Key,
                MediaItems = pair.Value,
                IsUnknownDolbyProfile = false
            }).ToList();

            return new MediaItemCollection()
            {
                Count = list.Count(),
                MediaItemGroups = list
            };
        }

        public MediaItemCollection CalculateMovieCodecItems()
        {
            var codecMovieMap = new Dictionary<string, List<CodecInfoPlugin.Models.MediaItem>>();

            foreach (var movie in _allMovies.Where(w => w.SortName != null).OrderBy(x => x.SortName))
            {
                _logger.Debug($"CalculateMovieCodecItems {movie.Name}");
                var codec = movie.GetMediaStreams().FirstOrDefault(s => s != null && s.Type == MediaStreamType.Video)?.Codec ?? "Unknown";

                if (!codecMovieMap.TryGetValue(codec, out var movieList))
                {
                    movieList = new List<CodecInfoPlugin.Models.MediaItem>();
                    codecMovieMap[codec] = movieList;
                }
                movieList.Add(new CodecInfoPlugin.Models.MediaItem { Id = movie.Id.ToString(), Title = movie.Name, Year = movie.ProductionYear });
                _logger.Debug($"{codec} {codecMovieMap.Count}");

                if (codec == "Unknown")
                    _logger.Debug($"CalculateMovieCodecItems-Unknown {movie.Name}");
            }

            var list = codecMovieMap.Select(pair => new MediaItemGroup
            {
                Title = pair.Key,
                MediaItems = pair.Value,
                IsUnknownDolbyProfile = false
            }).ToList();

            return new MediaItemCollection()
            {
                Count = list.Count(),
                MediaItemGroups = list
            };
        }

        public MediaItemCollection CalculateEpisodeDVProfileList()
        {
            var dvProfileMap = new Dictionary<string, List<CodecInfoPlugin.Models.MediaItem>>();

            foreach (var episode in _allEpisodes.Where(w => w.SortName != null).OrderBy(x => x.Series.SortName))
            {
                _logger.Debug($"CalculateEpisodeDVProfileList - '{episode.SeriesName} - {episode.Name}'");
                var mediaStream = episode.GetMediaStreams().FirstOrDefault(s => s != null && s.Type == MediaStreamType.Video);

                var dvProfile = GetDolbyVisionProfile(mediaStream);
                if (dvProfile == null || dvProfile == "")
                {
                    continue;
                }

                if (!dvProfileMap.TryGetValue(dvProfile, out var episodeList))
                {
                    episodeList = new List<CodecInfoPlugin.Models.MediaItem>();
                    dvProfileMap[dvProfile] = episodeList;
                }

                var episodeName = "S" + episode.ParentIndexNumber.ToString().PadLeft(2, '0') + "E" + episode.IndexNumber.ToString().PadLeft(2, '0') + ": " + episode.Name;
                var mediaItem = new CodecInfoPlugin.Models.MediaItem { Id = episode.Id.ToString(), GroupName = episode.SeriesName, Title = episodeName, Year = episode.ProductionYear };

                episodeList.Add(mediaItem);
                _logger.Debug($"CalculateEpisodeDVProfileList - {dvProfile} - '{episode.SeriesName}' - '{episode.Name}'");
            }

            _logger.Debug($"CalculateEpisodeDVProfileList - Converting to Episode Collection");
            var list = dvProfileMap.Select(pair => new MediaItemGroup
            {
                Title = pair.Key,
                MediaItems = pair.Value,
                IsUnknownDolbyProfile = CodecInfoPlugin.Configuration.PluginConfiguration.IsUnknownDolbyProfile(pair.Key)
            }).ToList();

            return new MediaItemCollection()
            {
                Count = list.Count(),
                MediaItemGroups = list
            };
        }

        public MediaItemCollection CalculateMovieDVProfileList()
        {
            var dvProfileMap = new Dictionary<string, List<CodecInfoPlugin.Models.MediaItem>>();

            foreach (var movie in _allMovies.Where(w => w.SortName != null).OrderBy(x => x.SortName))
            {
                _logger.Debug($"CalculateMovieDVProfileList {movie.Name}");
                var mediaStream = movie.GetMediaStreams().FirstOrDefault(s => s != null && s.Type == MediaStreamType.Video);

                var dvProfile = GetDolbyVisionProfile(mediaStream);
                if (dvProfile == null || dvProfile == "")
                {
                    continue;
                }

                if (!dvProfileMap.TryGetValue(dvProfile, out var movieList))
                {
                    movieList = new List<CodecInfoPlugin.Models.MediaItem>();
                    dvProfileMap[dvProfile] = movieList;
                }
                movieList.Add(new CodecInfoPlugin.Models.MediaItem { Id = movie.Id.ToString(), Title = movie.Name, Year = movie.ProductionYear });
                _logger.Debug($"CalculateMovieDVProfileList - {dvProfile} #{dvProfileMap[dvProfile].Count}");
            }

            var list = dvProfileMap.Select(pair => new MediaItemGroup
            {
                Title = pair.Key,
                MediaItems = pair.Value,
                IsUnknownDolbyProfile = CodecInfoPlugin.Configuration.PluginConfiguration.IsUnknownDolbyProfile(pair.Key)
            }).ToList();

            foreach (var obj in list)
            {
                _logger.Debug($"CalculateMovieDVProfileList - Post List created - {obj.Title} - {obj.IsUnknownDolbyProfile}");
            }

            return new MediaItemCollection()
            {
                Count = list.Count(),
                MediaItemGroups = list
            };
        }
    }
}