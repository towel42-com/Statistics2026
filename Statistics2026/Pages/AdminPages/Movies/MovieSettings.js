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
                    view.querySelector("#numDaysForMovieStreaming").value = config.numDaysForMovieStreaming;
                
                    view.querySelector("#numWatchedToReport").value = config.numWatchedToReport;
                    view.querySelector("#numGenresToReport").value = config.numGenresToReport;
                    view.querySelector("#numActorsToReport").value = config.numActorsToReport;
                
                    view.querySelector("#showFilteredMissingSummaries").checked = config.showFilteredMissingSummaries;

                    const collectionFilters = config.CollectionFiltersForMissingMovies || [];
                    view.querySelector("#collectionFilterList").value = collectionFilters.join('\n');
                }
            );
        }

        function debounce(func, delay) {
            let timeout;
            return function (...args) {
                clearTimeout(timeout);
                timeout = setTimeout(() => func.apply(this, args), delay);
            };
        }

        return function (view, params) {
            // init code here
            view.addEventListener('viewshow', async function (e) {
                Helpers.initTabSystem(view, AdminHelpers.getTabHierarchy, AdminHelpers.getTabs);
                Helpers.injectStyleSheet();

                loadPage(view, params);

                var isDebugLoggingEnabled = await Helpers.isDebugLoggingEnabled();
                
            });

            function saveConfigData() {
                // Visual indicator: show subtle amber text during network request
                const collectionAutosaveStatus = view.querySelector('#collectionAutosaveStatus');
                collectionAutosaveStatus.textContent = "Saving...";
                collectionAutosaveStatus.style.color = "#ffb703";
                collectionAutosaveStatus.style.opacity = "1";

                const filterList = view.querySelector('#collectionFilterList');

                const missingMovieFilters = filterList.value
                    .split('\n')
                    .map(line => line.trim())
                    .filter(line => line.length > 0);

                ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                    config.CollectionFiltersForMissingMovies = missingMovieFilters;

                    ApiClient.updatePluginConfiguration(Helpers.pluginId, config).then(function (result) {
                        // Visual indicator: show green confirmation text
                        collectionAutosaveStatus.textContent = "Saved";
                        collectionAutosaveStatus.style.color = "#52b788";

                        // Fade the "Saved" text away after 2 seconds
                        setTimeout(() => {
                            collectionAutosaveStatus.style.opacity = "0";
                        }, 2000);

                        Dashboard.processPluginConfigurationUpdateResult(result);
                    });
                });
            }

            const debouncedSave = debounce(saveConfigData, 1000);

            view.querySelector('#collectionFilterList').addEventListener('input', function () {
                const autosaveStatus = view.querySelector('#collectionAutosaveStatus');
                autosaveStatus.textContent = "Unsaved Changes...";
                autosaveStatus.style.color = "#e63946";
                autosaveStatus.style.opacity = "1";

                debouncedSave();
            });

            view.querySelector('#collectionFilterList').addEventListener('blur', function () {
                saveConfigData();
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            view.querySelector("#showFilteredMissingSummaries").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.showFilteredMissingSummaries = view.querySelector("#showFilteredMissingSummaries").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );

            view.querySelector("#numDaysForMovieStreaming").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.numDaysForMovieStreaming = view.querySelector("#numDaysForMovieStreaming").value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );

            view.querySelector("#numWatchedToReport").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        const parsed = parseInt(view.querySelector("#numWatchedToReport").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.numWatchedToReport = value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );

            view.querySelector("#numGenresToReport").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        const parsed = parseInt(view.querySelector("#numGenresToReport").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.numGenresToReport = value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );

            view.querySelector("#numActorsToReport").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        const parsed = parseInt(view.querySelector("#numActorsToReport").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.numActorsToReport = value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );

            view.querySelector("#numDaysForMovieStreamingHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Days after the Premiere", "Since digital availability is usually some time after the known release date, how many days to wait before showing a movie as missing? If the movie is available for streaming, this number is ignored.");
                });
            
            view.querySelector("#numWatchedToReportHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Watched Videos", "The default is 5, but you can limit how many Watched Videos to report on");
                });

            view.querySelector("#numGenresToReportHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Genres", "The default is 5, but when reporting a list of genres, how many should be shown?");
                });

            view.querySelector("#numActorsToReportHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Actors", "The default is 5, but when reporting a list of actors, how many should be shown?");
                });

            view.querySelector("#showFilteredMissingSummariesHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Filtered Missing Summaries", "For summaries of episodes or movies missing, show the filtered results.");
                });

        };
    }
);

