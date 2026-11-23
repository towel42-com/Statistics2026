define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('Stats2026_AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Stats2026_Helpers.js')
],
    function (mainTabsManager, AdminHelpers, Helpers) {
        'use strict';

        function stripSuffixFromList(str, suffixes) {
            for (const suffix of suffixes) {
                if (str.endsWith(suffix)) {
                    return str.slice(0, -suffix.length);
                }
            }
            return str;
        }

        function loadPage(view, params) {
            ApiClient.getPluginConfiguration(Helpers.pluginId).then(
                function (config) {
                    view.querySelector("#excludeAdmin").checked = config.userReportSettings.excludeAdmin;
                    view.querySelector("#hasConnectUserID").checked = config.userReportSettings.hasConnectUserID;
                    view.querySelector("#numMostActive").value = config.userReportSettings.numMostActiveUsers;
                }
            );
        }

        return function (view, params) {
            // init code here
            view.addEventListener('viewshow', async function (e) {
                Helpers.initTabSystem(view, AdminHelpers.getTabHierarchy, AdminHelpers.getTabs);
                Helpers.injectStyleSheet();

                loadPage(view, params);
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            view.querySelector("#excludeAdmin").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.userReportSettings.excludeAdmin = view.querySelector("#excludeAdmin").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#excludeAdminHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Exclude Administrators", "For security reasons, Administrators are not typically viewers of media and likely should be excluded from analysis.");
                });

            view.querySelector("#hasConnectUserID").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.userReportSettings.hasConnectUserID = view.querySelector("#hasConnectUserID").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#hasConnectUserIDHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Show Users with Connect User ID", "Normally all users are shown, checking this option will display only users with a Connect User ID.");
                });

            view.querySelector("#numMostActive").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        const parsed = parseInt(view.querySelector("#numMostActive").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.userReportSettings.numMostActiveUsers = value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#numMostActiveHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Most Active Users", "The default is 5, but you can limit how many most active users that are reported");
                });
        };
    }
);

