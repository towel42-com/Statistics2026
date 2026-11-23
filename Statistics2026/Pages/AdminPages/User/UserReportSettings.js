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
                    view.querySelector("#showImages").checked = config.userReportSettings.showImages;

                    view.querySelector("#numFavoriteYears").value = config.userReportSettings.numFavoriteYears;
                    view.querySelector("#numGenresToReport").value = config.userReportSettings.numGenresToReport;
                    view.querySelector("#numLastSeenToReport").value = config.userReportSettings.numLastSeenToReport;
                    view.querySelector("#numMostWatchedToReport").value = config.userReportSettings.numMostWatchedToReport;
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

            view.querySelector("#showImages").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.userReportSettings.showImages = view.querySelector("#showImages").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#showImagesHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Show Images", "In reports and summaries, show the images along with the titles");
                });

            view.querySelector("#numFavoriteYears").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        const parsed = parseInt(view.querySelector("#numFavoriteYears").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.userReportSettings.numFavoriteYears = value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#numFavoriteYearsHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Favorite Years", "The default is 5, but when reporting a list of favorite years, how many should be shown?");
                });

            view.querySelector("#numGenresToReport").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        const parsed = parseInt(view.querySelector("#numGenresToReport").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.userReportSettings.numGenresToReport = value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#numGenresToReportHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Genres", "The default is 5, but when reporting a list of genres, how many should be shown?");
                });

            view.querySelector("#numLastSeenToReport").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        const parsed = parseInt(view.querySelector("#numLastSeenToReport").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.userReportSettings.numLastSeenToReport = value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#numLastSeenToReportHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Last Seen", "The default is 5, but when reporting a list of last seen videos, how many should be shown?");
                });

            view.querySelector("#numMostWatchedToReport").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        const parsed = parseInt(view.querySelector("#numMostWatchedToReport").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.userReportSettings.numMostWatchedToReport = value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#numMostWatchedToReportHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Most Watched", "The default is 5, but when reporting a list of most watched videos, how many should be shown?");
                });
        };
    }
);

