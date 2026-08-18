define(['mainTabsManager', Dashboard.getConfigurationResourceUrl('Helpers.js')], function (mainTabsManager) {
    'use strict';

    const pluginId = "4BFE2894-AEA3-4D3C-A429-503B56D61711";

    function loadPage(view, params) {
        ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            
            view.querySelector("#hasConnectUserID").checked = config.hasConnectUserID;
            view.querySelector("#showAllCodecs").checked = config.showAllCodecs;
            view.querySelector("#showUnknownDVProfiles").checked = config.showUnknownDVProfiles;
            view.querySelector("#showAllDVProfiles").checked = config.showAllDVProfiles;
            view.querySelector("#showAllResolutions").checked = config.showAllResolutions;
        });
    }

    return function (view, params) {

        loadPage(view, params);


        // init code here
        view.addEventListener('viewshow', function (e) {

            mainTabsManager.setTabs(this, getTabIndex("Settings"), getTabs);
        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });

        view.querySelector("#hasConnectUserID").addEventListener("click",
            function () {
                ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                    config.hasConnectUserID = view.querySelector("#hasConnectUserID").checked;
                    ApiClient.updatePluginConfiguration(pluginId, config);
                });
            }
        );

        view.querySelector("#showAllCodecs").addEventListener("click",
            function () {
                ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                    config.showAllCodecs = view.querySelector("#showAllCodecs").checked;
                    ApiClient.updatePluginConfiguration(pluginId, config);
                });
            }
        );

        view.querySelector("#showUnknownDVProfiles").addEventListener("click",
            function () {
                ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                    config.showUnknownDVProfiles = view.querySelector("#showUnknownDVProfiles").checked;
                    ApiClient.updatePluginConfiguration(pluginId, config);
                });
            }
        );
        view.querySelector("#showAllDVProfiles").addEventListener("click",
            function () {
                ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                    config.showAllDVProfiles = view.querySelector("#showAllDVProfiles").checked;
                    ApiClient.updatePluginConfiguration(pluginId, config);
                });
            }
        );
        view.querySelector("#showAllResolutions").addEventListener("click",
            function () {
                ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                    config.showAllResolutions = view.querySelector("#showAllResolutions").checked;
                    ApiClient.updatePluginConfiguration(pluginId, config);
                });
            }
        );

        view.querySelector("#hasConnectUserIDDiv").addEventListener("click",
            function () {
                showInfo("Normally all users are shown, checking this option will display only users with a Connect User ID.", "Show Users with Connect User ID");
            });
        view.querySelector("#showAllCodecsDiv").addEventListener("click",
            function () {
                showInfo("Normally only codecs found in use are shown, checking this option will display all codecs.", "Show All Codecs");
            });
        view.querySelector("#showUnknownDVProfilesDiv").addEventListener("click",
            function () {
                showInfo("Normally unknown Dolby Vision Profiles are hidden, checking this option will display the count of unknown Dolby Vision profiles.", "Show Unknown Dolby Vision Profile Count");
            });
        view.querySelector("#showAllDVProfilesDiv").addEventListener("click",
            function () {
                showInfo("Normally only Dolby Vision Profiles in use are shown, checking this option will display all Dolby Vision profiles.", "Show All Dolby Vision Profiles");
            });
        view.querySelector("#showAllResolutionsDiv").addEventListener("click",
            function () {
                showInfo("Normally only resolutions in use are show, checking this option will display all resolutions.", "Show All Resolutions");
            });

    };
});

