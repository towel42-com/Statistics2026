define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_AdminPages_AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_Helpers.js')
],
    function (mainTabsManager, AdminHelpers, Helpers) {
        'use strict';

        window.MyPluginHelpers = Helpers;
        const lastRunArray = ["MediaAnalysis", "MissingEpisodesAnalysis", "NotifyAvailableToday"];

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

            var seriesStats_totals = "";
            seriesStats_totals += Helpers.getSummaryInfo(view, "total_series_count", { promises: pendingPromises });
            seriesStats_totals += Helpers.getSummaryInfo(view, "total_series_studio_count", { promises: pendingPromises });
            seriesStats_totals += Helpers.getSummaryInfo(view, "total_missing_episodes", { promises: pendingPromises });
            view.querySelector("#seriesStats_totals").innerHTML = seriesStats_totals;

            var seriesStats_gar = "";
            seriesStats_gar += Helpers.getSummaryInfo(view, "series_genre_list", { promises: pendingPromises });
            seriesStats_gar += Helpers.getSummaryInfo(view, "series_actors_list", { promises: pendingPromises });
            seriesStats_gar += Helpers.getSummaryInfo(view, "series_ratings_list", { promises: pendingPromises });
            view.querySelector("#seriesStats_gar").innerHTML = seriesStats_gar;

            var seriesStats_biggest = "";
            seriesStats_biggest += Helpers.getSummaryInfo(view, "get_series/Largest", { div: "largest_series", promises: pendingPromises });
            seriesStats_biggest += Helpers.getSummaryInfo(view, "get_series/Longest",{ div: "longest_series", promises: pendingPromises });
            view.querySelector("#seriesStats_biggest").innerHTML = seriesStats_biggest;

            var seriesStats_smallest = "";
            seriesStats_smallest += Helpers.getSummaryInfo(view, "get_series/HighestRated", { div: "highest_rated_series", promises: pendingPromises });
            seriesStats_smallest += Helpers.getSummaryInfo(view, "get_series/LowestRated", { div: "lowest_rated_series", promises: pendingPromises });
            view.querySelector("#seriesStats_smallest").innerHTML = seriesStats_smallest;

            var seriesStats_video_quality = "";
            seriesStats_video_quality += Helpers.getSummaryInfo(view, "get_series/HighestBitrate", { div: "highest_bitrate_series", promises: pendingPromises });
            seriesStats_video_quality += Helpers.getSummaryInfo(view, "get_series/LowestBitrate", { div: "lowest_bitrate_series", promises: pendingPromises });
            view.querySelector("#seriesStats_video_quality").innerHTML = seriesStats_video_quality;

            var seriesStats_added_to_server = "";
            seriesStats_added_to_server += Helpers.getSummaryInfo(view, "get_series/FirstAdditionToServer", { div: "oldest_series_addition", promises: pendingPromises });
            seriesStats_added_to_server += Helpers.getSummaryInfo(view, "get_series/LatestAdditionToServer", { div: "latest_series_addition", promises: pendingPromises });
            view.querySelector("#seriesStats_added_to_server").innerHTML = seriesStats_added_to_server;

            var seriesStats_age_stats = "";
            seriesStats_age_stats += Helpers.getSummaryInfo(view, "get_series/OldestPremiereDate", { div: "oldest_series", promises: pendingPromises });
            seriesStats_age_stats += Helpers.getSummaryInfo(view, "get_series/LatestPremiereDate", { div: "latest_series", promises: pendingPromises });
            view.querySelector("#seriesStats_age_stats").innerHTML = seriesStats_age_stats;

            var seriesStats_mostLeastWatched = "";
            seriesStats_mostLeastWatched += Helpers.getSummaryInfo(view, "least_watched_shows", { promises: pendingPromises });
            seriesStats_mostLeastWatched += Helpers.getSummaryInfo(view, "most_watched_shows", { promises: pendingPromises });
            view.querySelector("#seriesStats_mostLeastWatched").innerHTML = seriesStats_mostLeastWatched;

            Promise.allSettled(pendingPromises).finally(() => {
                Dashboard.hideLoadingMsg();
            });
        }


        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                Helpers.initTabSystem(view, AdminHelpers.getTabHierarchy, AdminHelpers.getTabs);
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            loadStats(view);
        }
    }
);