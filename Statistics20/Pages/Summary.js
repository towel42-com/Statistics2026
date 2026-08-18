define(['mainTabsManager', Dashboard.getConfigurationResourceUrl('Helpers.js')], function (mainTabsManager) {
    'use strict';

    ApiClient.getStatistics20URL = function (url_to_get) {
        console.log("getStatistics20URL Url = " + url_to_get);
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };

    const pluginId = "4BFE2894-AEA3-4D3C-A429-503B56D61711";

    function getSummaryInfo(view, whichSummary, parameters = "") {
        var url = ApiClient.getUrl("/emby/codec_info/" + whichSummary + parameters);

        ApiClient.getJSON(url).then(response => {
            view.querySelector("#" + whichSummary).innerHTML = response.html;

            response.dynamicButtons.forEach((v) => {
                view.querySelector("#" + v.id).addEventListener("click",
                    function () {
                        showInfo(v.info, v.title);
                    });
            });
        }).catch(error => {
            console.error("API call failed:", error);
        });

        return `<div name="${whichSummary}" id="${whichSummary}"></div>`;
    }

    function loadDebugInfo( view ) {
        var url = ApiClient.getUrl("/emby/System/Configuration");
        ApiClient.getJSON(url).then(response => {

            if (response.EnableDebugLevelLogging) {
                ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                    var debugInfo = "Version <b>" + config.Version + "</b> - Build Date - <b>" + config.BuildDate + "</b>";
                    view.querySelector(`#debugInfo`).style.display = '';
                    view.querySelector("#debugInfo").innerHTML = debugInfo;
                });
            }
            else
                view.querySelector(`#debugInfo`).style.display = 'none';
        }).catch(error => {
            console.error("API call failed:", error);
        });
    }

    function loadStats(view) {
        Dashboard.showLoadingMsg();

        ApiClient.getPluginConfiguration(pluginId).then(function (config) {

            var lastRunInfo = "Last Codec Analysis finished at <b> " + config.LastUpdated + "</b>";

            view.querySelector("#lastRunInfo").innerHTML = lastRunInfo;
            view.querySelector(`#debugInfo`).style.display = 'none';
            loadDebugInfo( view );

            if (config.LastUpdated === undefined) {
                Dashboard.alert({
                    message:
                        "No configuration found, please run the 'Statistics 2.0' task on the Scheduled Tasks page and come back for the results."
                });

                view.querySelector(`#lastRunInfo`).style.display = 'none';

                Dashboard.hideLoadingMsg();
            } else {
                view.querySelector("#pageIntro").innerHTML = "This plugin will calculate media and user statistics "
                    + "from this Emby server instance.";


                var userStats = "";
                userStats += getSummaryInfo(view, "most_active_users", "?hasConnectUserID=" + config.hasConnectUserID);
                userStats += getSummaryInfo(view, "user_count", "?hasConnectUserID=" + config.hasConnectUserID);
                view.querySelector("#userStats").innerHTML = (userStats);

                var mediaStats = "";
                mediaStats += getSummaryInfo(view, "codec_summary", "?showAllCodecs=" + config.showAllCodecs);
                mediaStats += getSummaryInfo(view, "resolution_summary", "?showAllResolutions=" + config.showAllResolutions);
                mediaStats += getSummaryInfo(view, "dvprofile_summary", "?showUnknownDVProfiles=" + config.showUnknownDVProfiles + "&showAllDVProfiles=" + config.showAllDVProfiles);

                view.querySelector("#mediaStats").innerHTML = (mediaStats);

                Dashboard.hideLoadingMsg();
            }
        });
    }

    return function (view, params) {
        view.addEventListener('viewshow', function (e) {

            mainTabsManager.setTabs(this, getTabIndex("Summary"), getTabs);
        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });

        loadStats(view);
    }
});