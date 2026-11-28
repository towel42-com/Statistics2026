define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_AdminPages_AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_Helpers.js')
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

            var pendingPromises = [];

            var movieStats_totals = "";
            movieStats_totals += Helpers.getSummaryInfo(view, "total_movie_count", { promises: pendingPromises });
            movieStats_totals += Helpers.getSummaryInfo(view, "total_collection_count", { promises: pendingPromises });
            movieStats_totals += Helpers.getSummaryInfo(view, "total_movie_studio_count", { promises: pendingPromises });
            movieStats_totals += Helpers.getSummaryInfo(view, "total_missing_movies", { promises: pendingPromises });
            view.querySelector("#movieStats_totals").innerHTML = movieStats_totals;

            var movieStats_gar = "";
            movieStats_gar += Helpers.getSummaryInfo(view, "movie_genre_list", { promises: pendingPromises });
            movieStats_gar += Helpers.getSummaryInfo(view, "movie_actors_list", { promises: pendingPromises });
            movieStats_gar += Helpers.getSummaryInfo(view, "movie_ratings_list", { promises: pendingPromises });
            view.querySelector("#movieStats_gar").innerHTML = movieStats_gar;

            var movieStats_biggest = "";
            movieStats_biggest += Helpers.getSummaryInfo(view, "get_movie/Largest", { div: "largest_movie", promises: pendingPromises });
            movieStats_biggest += Helpers.getSummaryInfo(view, "get_movie/Longest", { div: "longest_movie", promises: pendingPromises });
            view.querySelector("#movieStats_biggest").innerHTML = movieStats_biggest;

            var movieStats_smallest = "";
            movieStats_smallest += Helpers.getSummaryInfo(view, "get_movie/Smallest", { div: "smallest_movie", promises: pendingPromises });
            movieStats_smallest += Helpers.getSummaryInfo(view, "get_movie/Shortest", { div: "shortest_movie", promises: pendingPromises });
            view.querySelector("#movieStats_smallest").innerHTML = movieStats_smallest;

            var movieStats_ratings = "";
            movieStats_ratings += Helpers.getSummaryInfo(view, "get_movie/HighestRated", { div: "highest_rated_movie", promises: pendingPromises });
            movieStats_ratings += Helpers.getSummaryInfo(view, "get_movie/LowestRated", { div: "lowest_rated_movie", promises: pendingPromises });
            view.querySelector("#movieStats_ratings").innerHTML = movieStats_ratings;

            var movieStats_video_quality = "";
            movieStats_video_quality += Helpers.getSummaryInfo(view, "get_movie/HighestBitrate", { div: "highest_bitrate_movie", promises: pendingPromises });
            movieStats_video_quality += Helpers.getSummaryInfo(view, "get_movie/LowestBitrate", { div: "lowest_bitrate_movie", promises: pendingPromises });
            view.querySelector("#movieStats_video_quality").innerHTML = movieStats_video_quality;

            var movieStats_added_to_server = "";
            movieStats_added_to_server += Helpers.getSummaryInfo(view, "get_movie/FirstAdditionToServer", { div: "oldest_movie_addition", promises: pendingPromises });
            movieStats_added_to_server += Helpers.getSummaryInfo(view, "get_movie/LatestAdditionToServer", { div: "latest_movie_addition", promises: pendingPromises });
            view.querySelector("#movieStats_added_to_server").innerHTML = movieStats_added_to_server;

            var movieStats_age_stats = "";
            movieStats_age_stats += Helpers.getSummaryInfo(view, "get_movie/OldestPremiereDate", { div: "oldest_movie", promises: pendingPromises });
            movieStats_age_stats += Helpers.getSummaryInfo(view, "get_movie/LatestPremiereDate", { div: "latest_movie", promises: pendingPromises });
            view.querySelector("#movieStats_age_stats").innerHTML = movieStats_age_stats;

            var movieStats_mostLeastWatched = "";
            movieStats_mostLeastWatched += Helpers.getSummaryInfo(view, "least_watched_movies", { promises: pendingPromises });
            movieStats_mostLeastWatched += Helpers.getSummaryInfo(view, "most_watched_movies", { promises: pendingPromises });
            view.querySelector("#movieStats_mostLeastWatched").innerHTML = movieStats_mostLeastWatched;

            Promise.allSettled(pendingPromises).finally(() => {
                Dashboard.hideLoadingMsg();
            });
        }


        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                Helpers.initTabSystem(view, AdminHelpers.getTabHierarchy, Dashboard.getConfigurationPageUrl);

            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            loadStats(view);
       }
    }
);