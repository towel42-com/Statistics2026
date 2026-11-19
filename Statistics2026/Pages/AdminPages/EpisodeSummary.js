define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('Stats2026_AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Stats2026_Helpers.js')
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

            var seriesStats_totals = "";
            seriesStats_totals += Helpers.getSummaryInfo(view, "total_tv_count", "");
            seriesStats_totals += Helpers.getSummaryInfo(view, "total_tv_studio_count", "");
            seriesStats_totals += Helpers.getSummaryInfo(view, "total_missing_episodes", "");
            view.querySelector("#seriesStats_totals").innerHTML = seriesStats_totals;

            var seriesStats_gar = "";
            seriesStats_gar += Helpers.getSummaryInfo(view, "series_genre_list", "");
            seriesStats_gar += Helpers.getSummaryInfo(view, "series_actors_list", "");
            seriesStats_gar += Helpers.getSummaryInfo(view, "series_ratings_list", "");
            view.querySelector("#seriesStats_gar").innerHTML = seriesStats_gar;

            var seriesStats_biggest = "";
            seriesStats_biggest += Helpers.getSummaryInfo(view, "get_series/Largest", "", "", "largest_series");
            seriesStats_biggest += Helpers.getSummaryInfo(view, "get_series/Longest", "", "", "longest_series");
            view.querySelector("#seriesStats_biggest").innerHTML = seriesStats_biggest;

            var seriesStats_smallest = "";
            seriesStats_smallest += Helpers.getSummaryInfo(view, "get_series/HighestRated", "", "", "highest_rated_series");
            seriesStats_smallest += Helpers.getSummaryInfo(view, "get_series/LowestRated", "", "", "lowest_rated_series");
            view.querySelector("#seriesStats_smallest").innerHTML = seriesStats_smallest;

            var seriesStats_video_quality = "";
            seriesStats_video_quality += Helpers.getSummaryInfo(view, "get_series/HighestBitrate", "", "", "highest_bitrate_series");
            seriesStats_video_quality += Helpers.getSummaryInfo(view, "get_series/LowestBitrate", "", "", "lowest_bitrate_series");
            view.querySelector("#seriesStats_video_quality").innerHTML = seriesStats_video_quality;

            var seriesStats_added_to_server = "";
            seriesStats_added_to_server += Helpers.getSummaryInfo(view, "get_series/FirstAdditionToServer", "", "", "oldest_series_addition");
            seriesStats_added_to_server += Helpers.getSummaryInfo(view, "get_series/LatestAdditionToServer", "", "", "latest_series_addition");
            view.querySelector("#seriesStats_added_to_server").innerHTML = seriesStats_added_to_server;

            var seriesStats_age_stats = "";
            seriesStats_age_stats += Helpers.getSummaryInfo(view, "get_series/OldestPremiereDate", "", "", "oldest_series");
            seriesStats_age_stats += Helpers.getSummaryInfo(view, "get_series/LatestPremiereDate", "", "", "latest_series");
            view.querySelector("#seriesStats_age_stats").innerHTML = seriesStats_age_stats;

            var seriesStats_mostLeastWatched = "";
            seriesStats_mostLeastWatched += Helpers.getSummaryInfo(view, "least_watched_shows", "", "");
            seriesStats_mostLeastWatched += Helpers.getSummaryInfo(view, "most_watched_shows", "", "");
            view.querySelector("#seriesStats_mostLeastWatched").innerHTML = seriesStats_mostLeastWatched;

            var episodeStats_age_stats = "";
            episodeStats_age_stats += Helpers.getSummaryInfo(view, "get_episode/OldestPremiereDate", "", "", "oldest_episode");
            episodeStats_age_stats += Helpers.getSummaryInfo(view, "get_episode/LatestPremiereDate", "", "", "latest_episode");
            view.querySelector("#episodeStats_age_stats").innerHTML = episodeStats_age_stats;

            var episodeStats_added_to_server = "";
            episodeStats_added_to_server += Helpers.getSummaryInfo(view, "get_episode/FirstAdditionToServer", "", "", "oldest_episode_addition");
            episodeStats_added_to_server += Helpers.getSummaryInfo(view, "get_episode/LatestAdditionToServer", "", "", "latest_episode_addition");
            view.querySelector("#episodeStats_added_to_server").innerHTML = episodeStats_added_to_server;

            Dashboard.hideLoadingMsg();
        }


        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                Helpers.initMultiRowTabs(this, 'Stats2026_EpisodeSummary', AdminHelpers);
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