define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('Stats2026_AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Stats2026_Helpers.js')
],
    function (mainTabsManager, AdminHelpers, Helpers) {
        'use strict';

        window.MyPluginHelpers = Helpers;

        const lastRunArray = ["All"];

        async function loadStats(view) {
            Dashboard.showLoadingMsg();

            let configValid = await Helpers.CheckForValidConfig();
            if (!configValid) {
                view.querySelector(`#lastRunInfo`).style.display = 'none';
                Dashboard.hideLoadingMsg();
                return;
            }

            Helpers.insertGetLastRunInfo(view, lastRunArray, "lastRunInfo");

            var userInfo = "";
            userInfo += Helpers.getSummaryInfo(view, "most_active_users", "", "");
            userInfo += Helpers.getSummaryInfo(view, "user_count", "", "");
            view.querySelector("#userInfo").innerHTML = (userInfo);

            var mediaInfo = "";
            mediaInfo += Helpers.getSummaryInfo(view, "codec_summary");
            mediaInfo += Helpers.getSummaryInfo(view, "resolution_summary", "", "");
            mediaInfo += Helpers.getSummaryInfo(view, "dvprofile_summary", "", "");
            view.querySelector("#mediaInfo").innerHTML = mediaInfo;

            Dashboard.hideLoadingMsg();
        }


        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                Helpers.initMultiRowTabs(this, 'Stats2026_Summary', AdminHelpers);
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