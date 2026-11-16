define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Helpers.js')
],
    function (mainTabsManager, AdminHelpers, Helpers) {
        'use strict';

        window.MyPluginHelpers = Helpers;

        async function loadDebugInfoAsync(view, pluginConfig) {
            Dashboard.showLoadingMsg();

            var urlText = "/emby/Statistics2026/database_status";
            var url = ApiClient.getUrl(urlText);
            let dbStatus = await ApiClient.getJSON(url).catch(error => {
                console.error("database_status API call failed:", error);
                return;
            });

            var debugInfo = "Version <b>" + pluginConfig.Version
                + "</b> - Build Date - <b>" + pluginConfig.BuildDate + "</b>"
                + " - Database Status <b>" + dbStatus.DBState + " (";
            if (!dbStatus.DBStateOK)
                debugInfo += "not ";
            debugInfo += "OK)";
            if (dbStatus.DBStateMessage != "" )
                debugInfo += ` - ${dbStatus.DBStateMessage}`;
            debugInfo += `</b>`;
            DBStateMessage

            view.querySelector(`#debugInfo`).style.display = '';
            view.querySelector("#debugInfo").innerHTML = debugInfo;

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
            Dashboard.hideLoadingMsg();
        }

        async function loadStats(view) {
            Dashboard.showLoadingMsg();

            let configValid = await Helpers.CheckForValidConfig();
            if (!configValid) {
                view.querySelector(`#lastRunInfo`).style.display = 'none';
                Dashboard.hideLoadingMsg();
                return;
            }

            ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                view.querySelector(`#debugInfo`).style.display = 'none';
                loadDebugInfoAsync(view, config);
            });

            Helpers.insertSystemStatus(view, "lastRunInfo");

            view.querySelector("#pageIntro").innerHTML = "Status of the Statistics 2026 system";

            Dashboard.hideLoadingMsg();
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