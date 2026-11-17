define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Helpers.js')
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

            var seriesStats_totals2 = "";
            seriesStats_totals2 += Helpers.getSummaryInfo(view, "series_genre_list", "");
            seriesStats_totals2 += Helpers.getSummaryInfo(view, "series_actors_list", "");
            seriesStats_totals2 += Helpers.getSummaryInfo(view, "series_ratings_list", "");
            view.querySelector("#seriesStats_totals2").innerHTML = seriesStats_totals2;

            var seriesStats_size = "";
            seriesStats_size += Helpers.getSummaryInfo(view, "get_series/Largest", "", "", "largest_series");
            seriesStats_size += Helpers.getSummaryInfo(view, "get_series/Longest", "", "", "longest_series");
            seriesStats_size += Helpers.getSummaryInfo(view, "get_series/Smallest", "", "", "smallest_series");
            seriesStats_size += Helpers.getSummaryInfo(view, "get_series/Shortest", "", "", "shortest_series");
            view.querySelector("#seriesStats_size").innerHTML = seriesStats_size;

            var seriesStats_ratings = "";
            seriesStats_ratings += Helpers.getSummaryInfo(view, "get_series/HighestRated", "", "", "highest_rated_series");
            seriesStats_ratings += Helpers.getSummaryInfo(view, "get_series/LowestRated", "", "", "lowest_rated_series");
            view.querySelector("#seriesStats_ratings").innerHTML = seriesStats_ratings;

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
                mainTabsManager.setTabs(this, Helpers.getTabIndex("EpisodeSummary", AdminHelpers.getTabs), AdminHelpers.getTabs);
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