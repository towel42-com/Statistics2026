define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('Stats2026_AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Stats2026_Helpers.js')
],
    function (mainTabsManager, AdminHelpers, Helpers) {
        'use strict';

        window.MyPluginHelpers = Helpers;

        const lastRunArray = ["MediaAnalysis", "MissingMoviesAnalysis"];

        async function loadStats(view) {
            Dashboard.showLoadingMsg();

            let configValid = await Helpers.CheckForValidConfig();
            if (!configValid) {
                view.querySelector(`#lastRunInfo`).style.display = 'none';
                Dashboard.hideLoadingMsg();
                return;
            }

            Helpers.insertGetLastRunInfo(view, lastRunArray, "lastRunInfo");

            var movieStats_totals = "";
            movieStats_totals += Helpers.getSummaryInfo(view, "total_movie_count", "");
            movieStats_totals += Helpers.getSummaryInfo(view, "total_collection_count", "");
            movieStats_totals += Helpers.getSummaryInfo(view, "total_movie_studio_count", "");
            movieStats_totals += Helpers.getSummaryInfo(view, "total_missing_movies", "");
            view.querySelector("#movieStats_totals").innerHTML = movieStats_totals;

            var movieStats_gar = "";
            movieStats_gar += Helpers.getSummaryInfo(view, "movie_genre_list", "");
            movieStats_gar += Helpers.getSummaryInfo(view, "movie_actors_list", "");
            movieStats_gar += Helpers.getSummaryInfo(view, "movie_ratings_list", "");
            view.querySelector("#movieStats_gar").innerHTML = movieStats_gar;

            var movieStats_biggest = "";
            movieStats_biggest += Helpers.getSummaryInfo(view, "get_movie/Largest", "", "", "largest_movie");
            movieStats_biggest += Helpers.getSummaryInfo(view, "get_movie/Longest", "", "", "longest_movie");
            view.querySelector("#movieStats_biggest").innerHTML = movieStats_biggest;

            var movieStats_smallest = "";
            movieStats_smallest += Helpers.getSummaryInfo(view, "get_movie/Smallest", "", "", "smallest_movie");
            movieStats_smallest += Helpers.getSummaryInfo(view, "get_movie/Shortest", "", "", "shortest_movie");
            view.querySelector("#movieStats_smallest").innerHTML = movieStats_smallest;

            var movieStats_ratings = "";
            movieStats_ratings += Helpers.getSummaryInfo(view, "get_movie/HighestRated", "", "", "highest_rated_movie");
            movieStats_ratings += Helpers.getSummaryInfo(view, "get_movie/LowestRated", "", "", "lowest_rated_movie");
            view.querySelector("#movieStats_ratings").innerHTML = movieStats_ratings;

            var movieStats_video_quality = "";
            movieStats_video_quality += Helpers.getSummaryInfo(view, "get_movie/HighestBitrate", "", "", "highest_bitrate_movie");
            movieStats_video_quality += Helpers.getSummaryInfo(view, "get_movie/LowestBitrate", "", "", "lowest_bitrate_movie");
            view.querySelector("#movieStats_video_quality").innerHTML = movieStats_video_quality;

            var movieStats_added_to_server = "";
            movieStats_added_to_server += Helpers.getSummaryInfo(view, "get_movie/FirstAdditionToServer", "", "", "oldest_movie_addition");
            movieStats_added_to_server += Helpers.getSummaryInfo(view, "get_movie/LatestAdditionToServer", "", "", "latest_movie_addition");
            view.querySelector("#movieStats_added_to_server").innerHTML = movieStats_added_to_server;

            var movieStats_age_stats = "";
            movieStats_age_stats += Helpers.getSummaryInfo(view, "get_movie/OldestPremiereDate", "", "", "oldest_movie");
            movieStats_age_stats += Helpers.getSummaryInfo(view, "get_movie/LatestPremiereDate", "", "", "latest_movie");
            view.querySelector("#movieStats_age_stats").innerHTML = movieStats_age_stats;

            var movieStats_mostLeastWatched = "";
            movieStats_mostLeastWatched += Helpers.getSummaryInfo(view, "least_watched_movies", "", "");
            movieStats_mostLeastWatched += Helpers.getSummaryInfo(view, "most_watched_movies", "", "");
            view.querySelector("#movieStats_mostLeastWatched").innerHTML = movieStats_mostLeastWatched;

            Dashboard.hideLoadingMsg();
        }


        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                Helpers.initMultiRowTabs(this, 'Stats2026_MovieSummary', AdminHelpers);
                Helpers.injectStyleSheet();
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            loadStats(view);
       }
    }
);