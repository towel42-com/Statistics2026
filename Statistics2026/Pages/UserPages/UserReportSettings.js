define([
    'baseView',
    'loading',
    'mainTabsManager',
    ApiClient.getUrl('web/configurationpage?name=Statistics2026_Pages_UserPages_UserHelpers.js'),
    ApiClient.getUrl('web/configurationpage?name=Statistics2026_Pages_Helpers.js'),
    'emby-input',
    'emby-button',
    'emby-checkbox',
    'emby-scroller',
    'emby-select'
],
    function (BaseView, loading, mainTabsManager, UserPageHelpers, Helpers) {
        'use strict';

        function loadPage(view, params) {
            Helpers.getUserPluginConfig().then(
                function (config) {

                    ApiClient.getUser(params.userId).then(function (user) {
                        view.querySelector("#UserTitle").innerHTML = "Report Settings for: " + user.name;
                    });
                    view.querySelector("#showImages").checked = config.userReportSettings.showImages;

                    view.querySelector("#numFavoriteYears").value = config.userReportSettings.numFavoriteYears;
                    view.querySelector("#numGenresToReport").value = config.userReportSettings.numGenresToReport;
                    view.querySelector("#numLastSeenToReport").value = config.userReportSettings.numLastSeenToReport;
                    view.querySelector("#numWatchedToReport").value = config.userReportSettings.numWatchedToReport;
                    view.querySelector("#numTiedToReport").value = config.userReportSettings.numTiedToReport;
                }
            );
        }

        return function (view, params) {
            view.addEventListener('viewshow', async function (e) {
                Helpers.initTabSystem(view, UserPageHelpers.getTabHierarchy, UserPageHelpers.getRelativeTabPageUrl);

                let configValid = await Helpers.CheckForValidConfig();
                if (!configValid) {
                    view.querySelector(`#lastRunInfo`).style.display = 'none';
                    loading.hide();
                    return;
                }

                loadPage(view, params);
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            view.querySelector("#showImages").addEventListener("click",
                function () {
                    Helpers.getUserPluginConfig().then(function (config) {
                        config.userReportSettings.showImages = view.querySelector("#showImages").checked;
                        Helpers.updateUserPluginConfiguration(config);
                    });
                }
            );
            view.querySelector("#showImagesHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Show Images", "In reports and summaries, show the images along with the titles");
                });

            view.querySelector("#numFavoriteYears").addEventListener("input",
                function () {
                    Helpers.getUserPluginConfig().then(function (config) {
                        const parsed = parseInt(view.querySelector("#numFavoriteYears").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.userReportSettings.numFavoriteYears = value;
                        Helpers.updateUserPluginConfiguration(config);
                    });
                }
            );
            view.querySelector("#numFavoriteYearsHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Favorite Years", "The default is 5, but when reporting a list of favorite years, how many should be shown?");
                });

            view.querySelector("#numGenresToReport").addEventListener("input",
                function () {
                    Helpers.getUserPluginConfig().then(function (config) {
                        const parsed = parseInt(view.querySelector("#numGenresToReport").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.userReportSettings.numGenresToReport = value;
                        Helpers.updateUserPluginConfiguration(config);
                    });
                }
            );
            view.querySelector("#numGenresToReportHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Genres", "The default is 5, but when reporting a list of genres, how many should be shown?");
                });

            view.querySelector("#numLastSeenToReport").addEventListener("input",
                function () {
                    Helpers.getUserPluginConfig().then(function (config) {
                        const parsed = parseInt(view.querySelector("#numLastSeenToReport").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.userReportSettings.numLastSeenToReport = value;
                        Helpers.updateUserPluginConfiguration(config);
                    });
                }
            );
            view.querySelector("#numLastSeenToReportHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Last Seen", "The default is 5, but when reporting a list of last seen videos, how many should be shown?");
                });

            view.querySelector("#numWatchedToReport").addEventListener("input",
                function () {
                    Helpers.getUserPluginConfig().then(function (config) {
                        const parsed = parseInt(view.querySelector("#numWatchedToReport").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.userReportSettings.numWatchedToReport = value;
                        Helpers.updateUserPluginConfiguration(config);
                    });
                }
            );
            view.querySelector("#numWatchedToReportHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Most Watched", "The default is 5, but when reporting a list of most watched videos, how many should be shown?");
                });

            view.querySelector("#numTiedToReport").addEventListener("input",
                function () {
                    Helpers.getUserPluginConfig().then(function (config) {
                        const parsed = parseInt(view.querySelector("#numTiedToReport").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.userReportSettings.numTiedToReport = value;
                        Helpers.updateUserPluginConfiguration(config);
                    });
                }
            );
            view.querySelector("#numTiedToReportHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Tied Movies", "The default is 5, but when reporting a list of movies, and the value is tied how many do you want to show?");
                });
        };
    }
);

