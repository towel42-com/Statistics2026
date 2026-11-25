define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_AdminPages_AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_Helpers.js')
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
                    view.querySelector("#showDebugInfo").checked = config.systemSettings.showDebugInfo;
                    view.querySelector("#showDiagnosticInfo").checked = config.systemSettings.showDiagnosticInfo;
                    view.querySelector("#showLastRunInfo").checked = config.systemSettings.showLastRunInfo;
                    view.querySelector("#resetPlayCount").checked = config.systemSettings.resetPlayCount;
                    view.querySelector("#resetDBState").checked = config.systemSettings.resetDBState;
                    view.querySelector("#numMostActive").value = config.userReportSettings.numMostActiveUsers;
                    view.querySelector("#showUnknownDVProfiles").checked = config.systemSettings.showUnknownDVProfiles;
                    view.querySelector("#showAllResolutions").checked = config.systemSettings.showAllResolutions;
                }
            );
        }

        return function (view, params) {
            // init code here
            view.addEventListener('viewshow', async function (e) {
                Helpers.initTabSystem(view, AdminHelpers.getTabHierarchy, AdminHelpers.getTabs);
                Helpers.injectStyleSheet();

                loadPage(view, params);

                var isDebugLoggingEnabled = await Helpers.isDebugLoggingEnabled();
                view.querySelector("#showDebugInfo").disabled = !isDebugLoggingEnabled;
                view.querySelector("#showDiagnosticInfo").disabled = !isDebugLoggingEnabled;
                
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            view.querySelector("#showDebugInfo").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(
                        async function (config) {
                            config.systemSettings.showDebugInfo = view.querySelector("#showDebugInfo").checked;
                            ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                        }
                    );
                    view.querySelector("#showDiagnosticInfo").disabled = !view.querySelector("#showDebugInfo").checked;
                }
            );
            view.querySelector("#showDebugInfoHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Show Debug Info", "More information is reported on the pages. Only available when debug logging is enabled.");
                });

            view.querySelector("#showDiagnosticInfo").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.systemSettings.showDiagnosticInfo = view.querySelector("#showDiagnosticInfo").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#showDiagnosticInfoHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Show Diagnostic Info", "Diagnostic information is reported when debug info is enabled.. Only available when debug logging is enabled");
                });

            view.querySelector("#resetPlayCount").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.systemSettings.resetPlayCount = view.querySelector("#resetPlayCount").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#resetPlayCountHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Reset Play Count", "When computing the user watch data, reset the playcount to the default value (typically 1), and show Administrators as watched for every video. This settings resets to false after the Reset has occured.");
                });

            view.querySelector("#showLastRunInfo").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.systemSettings.showLastRunInfo = view.querySelector("#showLastRunInfo").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#showLastRunInfoHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Show Last Run Info", "Each page will show a summary of each analysis' last run");
                });

            view.querySelector("#resetDBState").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.systemSettings.resetDBState = view.querySelector("#resetDBState").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#resetDBStateHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Reset Database State", "Next time any analysis or reporting is done, recompute the database state. This settings resets to false after the Reset has occured");
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

            view.querySelector("#showUnknownDVProfiles").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.systemSettings.showUnknownDVProfiles = view.querySelector("#showUnknownDVProfiles").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#showUnknownDVProfilesHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Show Unknown Dolby Vision Profile Count", "Normally unknown Dolby Vision Profiles are hidden, checking this option will display the count of unknown Dolby Vision profiles.");
                });

            view.querySelector("#showAllResolutions").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.systemSettings.showAllResolutions = view.querySelector("#showAllResolutions").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#showAllResolutionsHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Show All Resolutions", "Normally only resolutions in use are show, checking this option will display all resolutions.");
                });
         };
    }
);

