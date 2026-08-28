using System;
using System.Collections.Generic;

namespace Statistics2026
{
    internal static class Constants
    {
        internal const string Name = "Codec And User Analysis";
        internal const string MediaResolutions = "Media Resolutions";
        internal const string MediaCodecs = "Media Codecs";
        internal const string DolbyVisionProfiles = "Dolby Vision Profiles";
        internal const string HelpMediaResolutions = "Entries with Resolution Not Available can be located in the log file after debug logging has been enabled by searching CalculateMediaResolutions.";
        internal const string HelpMediaCodecs = "Entries with Unknown can be located in the log file after debug logging has been enabled by searching CalculateMediaCodecs";
        internal const string HelpDolbyVisionProfile = "Videos with hevc or av1 codecs will also have their Dolby Vision Profile tracked.";

        // user summary constants
        internal const string TotalUsers = "Total Users";
        internal const string MostActiveUsers = "Most Active Users";
        internal const string HelpMostActiveUsers = "Top <numUsers> users that are the most active on the Emby server. This includes viewing movies and episodes.";

        // movie summary constants
        internal const string TotalMovies = "Total Movies";
        internal const string HelpTotalMovies = "Total movies in the Emby library.";

        internal const string TotalUserMovies = "Total Movies for User";
        internal const string HelpTotalUserMovies = "Total movies in the Emby library available to the User.";

        internal const string TotalUserMoviesWatched = "Total Movies Watched by User";
        internal const string HelpTotalUserMoviesWatched = "Total Movies Watched by the User in the Emby library.";

        internal const string TotalCollections = "Total Collections";
        internal const string HelpTotalCollections = "Total collections in the Emby library.";

        internal const string TotalStudios = "Total Studios";
        internal const string HelpTotalStudios = "Total studios in the Emby library.";
        internal const string BiggestMovie = "Largest Movie";
        internal const string SmallestMovie = "Smallest Movie";
        internal const string LongestMovie = "Longest Movie";
        internal const string ShortestMovie = "Shortest Movie";

        internal const string HighestRatedMovie = "Highest Rated Movie";
        internal const string LowestRatedMovie = "Lowest Rated Movie";
        internal const string HighestBitrateMovie = "Highest Bitrate Movie";
        internal const string LowestBitrateMovie = "Lowest Bitrate Movie";
        internal const string OldestMoviePremiere = "Oldest Movie Premiere";
        internal const string LatestMoviePremiere = "Latest Movie Premier";
        internal const string FirstMovieAddition = "First Movie Added to Server";
        internal const string LatestMovieAddition = "Latest Movie Added to Server";

        internal const string TotalTVShowsWatched = "Total TV Series Watched";
        internal const string HelpTotalTVShowsWatched = "Total TV Series Watched by the user in the Emby library.";

        internal const string TotalTVShows = "Total TV Series";
        internal const string HelpTotalTVShows = "Total TV Series the Emby library.";
        internal const string TotalUserTVShows = "Total TV Series for User";
        internal const string HelpTotalUserTVShows = "Total TV Series the Emby library available to the User.";

        internal const string TotalTVEpisodes = "Total TV Episodes";
        internal const string HelpTotalTVEpisodes = "Total TV Series the Emby library (Excludes Specials).";

        internal const string TotalUserTVEpisodes = "Total TV Episodes for User";
        internal const string HelpTotalUserTVEpisodes = "Total TV Series the Emby library available to the User (Excludes Specials).";

        internal const string TotalUserTVEpisodesWatched = "Total TV Episodes watched by User";
        internal const string HelpTotalUserTVEpisodesWatched = "Total TV Episodes watched by the user in the Emby library available to the User (Excludes Specials).";

        internal const string HelpTotalTVNetworks = "Total TV Networks And Studios in the Emby library.";
        internal const string TotalTVNetworks = "Total TV Networks and Studios";
        internal const string BiggestSeries = "Largest TV Series Total Size";
        internal const string SmallestSeries = "Smallest TV Series Total Size";
        internal const string LongestSeries = "Longest TV Series Total Runtime";
        internal const string ShortestSeries = "Shortest TV Series Total Runtime";

        internal const string HighestRatedSeries = "Highest Average Rating TV Series";
        internal const string LowestRatedSeries = "Lowest Average Rating TV Series";

        internal const string HighestBitrateSeries = "Highest Average Bitrate TV Series";
        internal const string LowestBitrateSeries = "Lowest Average Bitrate TV Series";
        internal const string OldestSeriesPremiere = "Oldest TV Series Premiere";
        internal const string LatestSeriesPremiere = "Latest TV Series Premier";
        internal const string OldestEpisodePremiere = "Oldest TV Episode Premier";
        internal const string LatestEpisodePremiere = "Latest TV Episode Premier";
        internal const string FirstSeriesAddition = "First TV Series Added to Server";
        internal const string LatestSeriesAddition = "Latest TV Series Added to Server";
        internal const string FirstEpisodeAddition = "First TV Episode Added to Server";
        internal const string LatestEpisodeAddition = "Latest TV Episode Added to Server";
        internal const string MostWatchedShows = "Most Watched Shows";
        internal const string LeastWatchedShows = "Least Watched Shows";
        internal const string HelpMostWatchedShows = "Most watched shows based on the average percent of episodes finished per user.";
        internal const string HelpLeastWatchedShows = "Least watched shows based on the average percent of episodes finished per user.";

        internal const string MissingVideoStream = "Missing Video Stream";
        internal const string UnknownDolbyProfile = "Unknown Dolby Profile";
        internal const string NonDolbyVisionCompatibleCodec = "Non Dolby Vision Compatible Codec";
        internal const string NoDolbyProfile = "No Dolby Profile";

        internal const string UserTotalTimeWatched = "Total Time Watched";
        internal const string UserTotalWatchableTime = "Total Watchable Time";

        internal const string UserTotalMovieTimeWatched = "Total Time Watching Movies";
        internal const string UserTotalMovieWatchableTime = "Total Watchable Movie Time";

        internal const string UserTotalEpisodeTimeWatched = "Total Time Watching Episodes";
        internal const string UserTotalEpisodeWatchableTime = "Total Watchable Episode Time";

        internal const string FavoriteMovieYears = "Favorite Movie Years";
        internal const string FavoriteMovieGenres = "Favorite Movie Genres";

        internal const string LastSeenTVSeries = "Last Seen TV Series";
        internal const string LastSeenMovies = "Last Seen Movies";
        internal const string HelpLastSeenTVSeries = "The last TV episodes seen by user.";
        internal const string HelpLastSeenMovies = "The last movies seen by user.";
        public static readonly string[] UnknownDolbyProfiles =
        {
            MissingVideoStream,
            UnknownDolbyProfile,
            NonDolbyVisionCompatibleCodec,
            NoDolbyProfile
        };

        public static bool IsUnknownDolbyProfile(string profile)
        {
            if (string.IsNullOrEmpty(profile))
                return true;
            return Array.Exists(UnknownDolbyProfiles, f => f.Equals(profile, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsDolbyVision50(string profile)
        {
            return profile == "Profile 5.0";
        }

        internal const string NoResolution = "Resolution Not Available";
        internal const string HD = "1080p";
        internal const string _4k = "4K";
        internal const string _8k = "8K";
        internal const string _720p = "720p";
        internal const string SD = "SD";
        internal const string HEVC = "HEVC";
        internal const string AV1 = "AV1";
    }
}