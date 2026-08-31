define(['mainTabsManager', Dashboard.getConfigurationResourceUrl('Helpers.js')], function (mainTabsManager, Helpers) {
    'use strict';

    function loadPage(view, params) {
        ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {

            view.querySelector("#hasConnectUserID").checked = config.hasConnectUserID;
            view.querySelector("#showAllCodecs").checked = config.showAllCodecs;
            view.querySelector("#showUnknownDVProfiles").checked = config.showUnknownDVProfiles;
            view.querySelector("#showAllDVProfiles").checked = config.showAllDVProfiles;
            view.querySelector("#showAllResolutions").checked = config.showAllResolutions;
            view.querySelector("#numMostActive").value = config.numMostActiveUsers;
            view.querySelector("#excludeAdmin").checked = config.excludeAdmin;
            
        });
    }

    return function (view, params) {

        loadPage(view, params);


        // init code here
        view.addEventListener('viewshow', function (e) {
            mainTabsManager.setTabs(this, Helpers.getTabIndex("Settings"), Helpers.getTabs);
            Helpers.injectStyleSheet(e);
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

        view.querySelector("#excludeAdmin").addEventListener("click",
            function () {
                ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                    config.excludeAdmin = view.querySelector("#excludeAdmin").checked;
                    ApiClient.updatePluginConfiguration(pluginId, config);
                });
            }
        );

        view.querySelector("#numMostActive").addEventListener("input",
            function () {
                ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                    config.numMostActiveUsers = view.querySelector("#hasConnectUserID").value.trim();
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


        view.querySelector("#numMostActiveHelp").addEventListener("click",
            function () {
                Helpers.showInfo("The default is 5, but you can limit how many most active users that are reported", "Number of Most Active Users");
            });
        view.querySelector("#excludeAdminHelp").addEventListener("click",
            function () {
                Helpers.showInfo("For security reasons, Administrators are not typically viewers of media and likely should be excluded from analysis.", "Exclude Administrators");
            });
        view.querySelector("#hasConnectUserIDHelp").addEventListener("click",
            function () {
                Helpers.showInfo("Normally all users are shown, checking this option will display only users with a Connect User ID.", "Show Users with Connect User ID");
            });
        view.querySelector("#showAllCodecsHelp").addEventListener("click",
            function () {
                Helpers.showInfo("Normally only codecs found in use are shown, checking this option will display all codecs.", "Show All Codecs");
            });
        view.querySelector("#showUnknownDVProfilesHelp").addEventListener("click",
            function () {
                Helpers.showInfo("Normally unknown Dolby Vision Profiles are hidden, checking this option will display the count of unknown Dolby Vision profiles.", "Show Unknown Dolby Vision Profile Count");
            });
        view.querySelector("#showAllDVProfilesHelp").addEventListener("click",
            function () {
                Helpers.showInfo("Normally only Dolby Vision Profiles in use are shown, checking this option will display all Dolby Vision profiles.", "Show All Dolby Vision Profiles");
            });
        view.querySelector("#showAllResolutionsHelp").addEventListener("click",
            function () {
                Helpers.showInfo("Normally only resolutions in use are show, checking this option will display all resolutions.", "Show All Resolutions");
            });

    };
});

