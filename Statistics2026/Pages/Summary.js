define(['mainTabsManager', Dashboard.getConfigurationResourceUrl('Helpers.js')], function (mainTabsManager) {
    'use strict';

    ApiClient.getStatistics2026URL = function (url_to_get) {
        console.log("getStatistics2026URL Url = " + url_to_get);
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };

    const pluginId = "4BFE2894-AEA3-4D3C-A429-503B56D61711";

    function getSummaryInfo(view, whichSummary, parameters = "", div = "") {
        var url = ApiClient.getUrl("/emby/Statistics2026/" + whichSummary + parameters);

        if (div == "")
            div = whichSummary;

        ApiClient.getJSON(url).then(response => {
            view.querySelector("#" + div).innerHTML = response.html;

            response.dynamicButtons.forEach((v) => {
                view.querySelector("#" + v.id).addEventListener("click",
                    function () {
                        showInfo(v.info, v.title);
                    });
            });
        }).catch(error => {
            console.error("API call failed:", error);
        });

        return `<div name="${div}" id="${div}"></div>`;
    }

    function loadDebugInfo(view) {
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
            loadDebugInfo(view);

            if (config.LastUpdated === undefined) {
                Dashboard.alert({
                    message:
                        "No configuration found, please run the 'Statistics 2026' task on the Scheduled Tasks page and come back for the results."
                });

                view.querySelector(`#lastRunInfo`).style.display = 'none';

                Dashboard.hideLoadingMsg();
            } else {
                view.querySelector("#pageIntro").innerHTML = "This plugin will calculate media and user statistics "
                    + "from this Emby server instance.";


                var userInfo = "";
                userInfo += getSummaryInfo(view, "most_active_users", "?hasConnectUserID=" + config.hasConnectUserID + "&numUsers=" + config.numMostActiveUsers + "&excludeAdmin=" + config.excludeAdmin);
                userInfo += getSummaryInfo(view, "user_count", "?hasConnectUserID=" + config.hasConnectUserID + "&excludeAdmin=" + config.excludeAdmin);
                view.querySelector("#userInfo").innerHTML = (userInfo);

                var mediaInfo = "";
                mediaInfo += getSummaryInfo(view, "codec_summary", "?showAllCodecs=" + config.showAllCodecs);
                mediaInfo += getSummaryInfo(view, "resolution_summary", "?showAllResolutions=" + config.showAllResolutions);
                mediaInfo += getSummaryInfo(view, "dvprofile_summary", "?showUnknownDVProfiles=" + config.showUnknownDVProfiles + "&showAllDVProfiles=" + config.showAllDVProfiles);
                view.querySelector("#mediaInfo").innerHTML = mediaInfo;

                var movieStats = "";
                movieStats += getSummaryInfo(view, "total_movie_count");
                movieStats += getSummaryInfo(view, "total_collection_count");
                movieStats += getSummaryInfo(view, "total_studio_count");
                movieStats += getSummaryInfo(view, "get_movie/Largest", "?serverId=" + config.ServerId, "largest_movie");
                movieStats += getSummaryInfo(view, "get_movie/Smallest", "?serverId=" + config.ServerId, "smallest_movie");
                movieStats += getSummaryInfo(view, "get_movie/Longest", "?serverId=" + config.ServerId, "longest_movie");
                movieStats += getSummaryInfo(view, "get_movie/Shortest", "?serverId=" + config.ServerId, "shortest_movie");
                movieStats += getSummaryInfo(view, "get_movie/HighestRated", "?serverId=" + config.ServerId, "highest_rated_movie");
                movieStats += getSummaryInfo(view, "get_movie/LowestRated", "?serverId=" + config.ServerId, "lowest_rated_movie");
                // movieStats += getSummaryInfo(view, "get_movie/HighestBitrate", "?serverId=" + config.ServerId,"highest_bitrate_movie");
                // movieStats += getSummaryInfo(view, "get_movie/LowestBitrate", "?serverId=" + config.ServerId,"lowest_bitrate_movie");
                // movieStats += getSummaryInfo(view, "get_movie/Oldest", "?serverId=" + config.ServerId,"oldest_movie");
                // movieStats += getSummaryInfo(view, "get_movie/Newest", "?serverId=" + config.ServerId,"newest_movie");
                // movieStats += getSummaryInfo(view, "get_movie/MostRecent", "?serverId=" + config.ServerId,"most_recent_movie");
                // movieStats += getSummaryInfo(view, "get_movie/LeastRecent", "?serverId=" + config.ServerId,"least_recent_movie");
                view.querySelector("#movieStats").innerHTML = movieStats;

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