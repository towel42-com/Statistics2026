define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Helpers.js')
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

            var movieStats_size = "";
            movieStats_size += Helpers.getSummaryInfo(view, "get_movie/Largest", "", "", "largest_movie");
            movieStats_size += Helpers.getSummaryInfo(view, "get_movie/Longest", "", "", "longest_movie");
            movieStats_size += Helpers.getSummaryInfo(view, "get_movie/Smallest", "", "", "smallest_movie");
            movieStats_size += Helpers.getSummaryInfo(view, "get_movie/Shortest", "", "", "shortest_movie");
            view.querySelector("#movieStats_size").innerHTML = movieStats_size;

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
                mainTabsManager.setTabs(this, Helpers.getTabIndex("MovieSummary", AdminHelpers.getTabs), AdminHelpers.getTabs);
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