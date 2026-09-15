define(['mainTabsManager', Dashboard.getConfigurationResourceUrl('Helpers.js'), Dashboard.getConfigurationResourceUrl('SharedHelpers.js')], function (mainTabsManager, Helpers, SharedHelpers) {
    'use strict';

    function loadPage(view, params) {
        ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {

            view.querySelector("#hasConnectUserID").checked = config.hasConnectUserID;
            view.querySelector("#showUnknownDVProfiles").checked = config.showUnknownDVProfiles;
            view.querySelector("#showAllResolutions").checked = config.showAllResolutions;
            view.querySelector("#numMostActive").value = config.numMostActiveUsers;
            view.querySelector("#numWatchedToReport").value = config.numWatchedToReport;
            view.querySelector("#excludeAdmin").checked = config.excludeAdmin;
            view.querySelector("#resetPlayCount").checked = config.resetPlayCount;
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
                ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                    config.hasConnectUserID = view.querySelector("#hasConnectUserID").checked;
                    ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                });
            }
        );

        view.querySelector("#excludeAdmin").addEventListener("click",
            function () {
                ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                    config.excludeAdmin = view.querySelector("#excludeAdmin").checked;
                    ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                });
            }
        );

        view.querySelector("#resetPlayCount").addEventListener("click",
            function () {
                ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                    config.resetPlayCount = view.querySelector("#resetPlayCount").checked;
                    ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                });
            }
        );

        view.querySelector("#numMostActive").addEventListener("input",
            function () {
                ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                    config.numMostActiveUsers = parseInt(view.querySelector("#numMostActive").value) || 5;
                    ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                });
            }
        );

        view.querySelector("#numWatchedToReport").addEventListener("input",
            function () {
                ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                    config.numWatchedToReport = parseInt(view.querySelector("#numWatchedToReport").value) || 5;
                    ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                });
            }
        );

        view.querySelector("#showUnknownDVProfiles").addEventListener("click",
            function () {
                ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                    config.showUnknownDVProfiles = view.querySelector("#showUnknownDVProfiles").checked;
                    ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                });
            }
        );
        view.querySelector("#showAllResolutions").addEventListener("click",
            function () {
                ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                    config.showAllResolutions = view.querySelector("#showAllResolutions").checked;
                    ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                });
            }
        );


        view.querySelector("#numMostActiveHelp").addEventListener("click",
            function () {
                Helpers.showInfo("The default is 5, but you can limit how many most active users that are reported", "Number of Most Active Users");
            });

        view.querySelector("#numWatchedToReportHelp").addEventListener("click",
            function () {
                Helpers.showInfo("The default is 5, but you can limit how many Watched Videos to report on", "Number of Watched Videos");
            });

        view.querySelector("#excludeAdminHelp").addEventListener("click",
            function () {
                Helpers.showInfo("For security reasons, Administrators are not typically viewers of media and likely should be excluded from analysis.", "Exclude Administrators");
            });

        view.querySelector("#resetPlayCountHelp").addEventListener("click",
            function () {
                Helpers.showInfo("When computing the user watch data, reset the playcount to the default value (typically 1), and show Administrators as watched for every video. This settings resets to false after the Reset has occured.", "Reset Play Count");
            });

        
        view.querySelector("#hasConnectUserIDHelp").addEventListener("click",
            function () {
                Helpers.showInfo("Normally all users are shown, checking this option will display only users with a Connect User ID.", "Show Users with Connect User ID");
            });
        view.querySelector("#showUnknownDVProfilesHelp").addEventListener("click",
            function () {
                Helpers.showInfo("Normally unknown Dolby Vision Profiles are hidden, checking this option will display the count of unknown Dolby Vision profiles.", "Show Unknown Dolby Vision Profile Count");
            });
        view.querySelector("#showAllResolutionsHelp").addEventListener("click",
            function () {
                Helpers.showInfo("Normally only resolutions in use are show, checking this option will display all resolutions.", "Show All Resolutions");
            });

    };
});

