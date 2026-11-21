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

            var pendingPromises = [];

            var episodeStats_totals = "";
            episodeStats_totals += Helpers.getSummaryInfo(view, "total_episode_count", { promises: pendingPromises });
            episodeStats_totals += Helpers.getSummaryInfo(view, "total_episode_studio_count", { promises: pendingPromises });
            view.querySelector("#episodeStats_totals").innerHTML = episodeStats_totals;

            var episodeStats_gar = "";
            episodeStats_gar += Helpers.getSummaryInfo(view, "episode_genre_list", { promises: pendingPromises });
            episodeStats_gar += Helpers.getSummaryInfo(view, "episode_actors_list", { promises: pendingPromises });
            view.querySelector("#episodeStats_gar").innerHTML = episodeStats_gar;

            var episodeStats_age_stats = "";
            episodeStats_age_stats += Helpers.getSummaryInfo(view, "get_episode/OldestPremiereDate", { div: "oldest_episode", promises: pendingPromises });
            episodeStats_age_stats += Helpers.getSummaryInfo(view, "get_episode/LatestPremiereDate", { div: "latest_episode", promises: pendingPromises });
            view.querySelector("#episodeStats_age_stats").innerHTML = episodeStats_age_stats;

            var episodeStats_added_to_server = "";
            episodeStats_added_to_server += Helpers.getSummaryInfo(view, "get_episode/FirstAdditionToServer", { div: "oldest_episode_addition", promises: pendingPromises });
            episodeStats_added_to_server += Helpers.getSummaryInfo(view, "get_episode/LatestAdditionToServer", { div: "latest_episode_addition", promises: pendingPromises });
            view.querySelector("#episodeStats_added_to_server").innerHTML = episodeStats_added_to_server;

            Promise.allSettled(pendingPromises).finally(() => {
                Dashboard.hideLoadingMsg();
            });
        }


        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                Helpers.initTabSystem(view, AdminHelpers.getTabHierarchy, AdminHelpers.getTabs);
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