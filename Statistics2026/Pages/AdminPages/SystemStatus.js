define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Helpers.js')
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

            Helpers.insertSystemStatus(view, "lastRunInfo");

            if (Helpers.showDiagnosticInfo(pluginConfig)) {
                var playedUserMedia = Helpers.getSummaryInfo(view, "played_user_media", "", "");
                view.querySelector(`#playedUserMedia`).style.display = '';
                view.querySelector("#playedUserMedia").innerHTML = (playedUserMedia);

                var userWatchMediaIssues = Helpers.getSummaryInfo(view, "user_watch_media_issues", "", "");
                view.querySelector(`#userWatchMediaIssues`).style.display = '';
                view.querySelector("#userWatchMediaIssues").innerHTML = (userWatchMediaIssues);

                var multiCollectionMovies = "";
                multiCollectionMovies += Helpers.getSummaryInfo(view, "multi_collection_movies", "", "");
                view.querySelector(`#multiCollectionMovies`).style.display = '';
                view.querySelector("#multiCollectionMovies").innerHTML = multiCollectionMovies;
            }
            else {
                view.querySelector(`#playedUserMedia`).style.display = 'none';
                view.querySelector(`#userWatchMediaIssues`).style.display = 'none';
                view.querySelector(`#multiCollectionMovies`).style.display = 'none';

            }
            Dashboard.hideLoadingMsg();
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
                    Dashboard.hideLoadingMsg();
                }
            );
        }


        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                mainTabsManager.setTabs(this, Helpers.getTabIndex("SystemStatus", AdminHelpers.getTabs), AdminHelpers.getTabs);
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