define(['mainTabsManager', Dashboard.getConfigurationResourceUrl('Helpers.js')], function (mainTabsManager) {
    'use strict';

    ApiClient.getCodecInfoURL = function (url_to_get) {
        console.log("getCodecInfoURL Url = " + url_to_get);
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };

    const pluginId = "4BFE2894-AEA3-4D3C-A429-503B56D61711";

    function getSummaryInfo(view, whichSummary, parameters="") {
        var url = ApiClient.getUrl( "/emby/codec_info/" + whichSummary+parameters);

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

    function loadStats(view) {
        Dashboard.showLoadingMsg();

        ApiClient.getPluginConfiguration(pluginId).then(function (config) {

            view.querySelector("#lastRunInfo").innerHTML = "Last Codec Analysis finished at <b> " + config.LastUpdated + "</b> - Version <b>" + config.Version + "</b> - Build Date - <b>" + config.BuildDate + "</b>";
            if (config.LastUpdated === undefined) {
                Dashboard.alert({
                    message:
                        "No configuration found, please run the 'Media Codec Infromation' task on the Scheduled Tasks page and come back for the results."
                });

                view.querySelector(`#lastRunInfo`).style.display = 'none';

                Dashboard.hideLoadingMsg();
            } else {
                view.querySelector("#pageIntro").innerHTML = "This plugin will calculate codec and dolby profile information "
                    + "from this Emby server instance.";

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