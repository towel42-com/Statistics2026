define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_AdminPages_AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_Helpers.js')
],
    function (mainTabsManager, AdminHelpers, Helpers) {
        'use strict';

        window.MyPluginHelpers = Helpers;

        async function loadInfoAsync(view, pluginConfig) {
            Dashboard.showLoadingMsg();

            var urlText = "/emby/Statistics2026/database_status";
            var url = ApiClient.getUrl(urlText);
            let dbStatus = await ApiClient.getJSON(url).catch(error => {
                console.error("database_status API call failed:", error);
                return;
            });

            var pendingPromises = [];

            Helpers.insertSystemStatus(view, "lastRunInfo");

            // if (Helpers.showDiagnosticInfo(pluginConfig)) {
            //     var playedUserMedia = Helpers.getSummaryInfo(view, "played_user_media", { promises: pendingPromises });
            //     view.querySelector(`#playedUserMedia`).style.display = '';
            //     view.querySelector("#playedUserMedia").innerHTML = (playedUserMedia);

            //     var userWatchMediaIssues = Helpers.getSummaryInfo(view, "user_watch_media_issues", { promises: pendingPromises });
            //     view.querySelector(`#userWatchMediaIssues`).style.display = '';
            //     view.querySelector("#userWatchMediaIssues").innerHTML = (userWatchMediaIssues);

            //     var multiCollectionMovies = "";
            //     multiCollectionMovies += Helpers.getSummaryInfo(view, "multi_collection_movies", { promises: pendingPromises });
            //     view.querySelector(`#multiCollectionMovies`).style.display = '';
            //     view.querySelector("#multiCollectionMovies").innerHTML = multiCollectionMovies;
            // }
            // else {
            //     view.querySelector(`#playedUserMedia`).style.display = 'none';
            //     view.querySelector(`#userWatchMediaIssues`).style.display = 'none';
            //     view.querySelector(`#multiCollectionMovies`).style.display = 'none';

            // }

            Promise.allSettled(pendingPromises).finally(() => {
                Dashboard.hideLoadingMsg();
            });
        }

        async function loadStats(view) {
            Dashboard.showLoadingMsg();

            let configValid = await Helpers.CheckForValidConfig();
            if (!configValid) {
                view.querySelector(`#lastRunInfo`).style.display = 'none';
                view.querySelector(`#buildInfo`).style.display = 'none';
                view.querySelector(`#playedUserMedia`).style.display = 'none';
                view.querySelector(`#userWatchMediaIssues`).style.display = 'none';
                view.querySelector(`#multiCollectionMovies`).style.display = 'none';
                Dashboard.hideLoadingMsg();
                return;
            }

            view.querySelector("#pageIntro").innerHTML = "<b>Status of the Statistics 2026 system</b>";

            ApiClient.getPluginConfiguration(Helpers.pluginId).then(
                function (config) {
                    loadInfoAsync(view, config);
                }
            );
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