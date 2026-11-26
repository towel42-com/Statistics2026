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
                    view.querySelector("#numDaysForMovieStreaming").value = config.movieSettings.numDaysForMovieStreaming;
                    view.querySelector("#showSearchTheMovieDB").checked = config.movieSettings.showSearchTheMovieDB;

                    const searchLocations = config.movieSettings.searchLocations || [];
                    view.querySelector("#searchLocations").value = searchLocations.join('\n');;

                    view.querySelector("#showFilteredMissingSummaries").checked = config.movieSettings.showFilteredMissingSummaries;
                    view.querySelector("#alwaysShowJustWatch").checked = config.movieSettings.alwaysShowJustWatch;
                    view.querySelector("#maxNumStreamingProviders").value = config.movieSettings.maxNumStreamingProviders;

                    const collectionFilters = config.movieSettings.CollectionFiltersForMissingMovies || [];
                    view.querySelector("#collectionFilterList").value = collectionFilters.join('\n');

                    view.querySelector("#showImages").checked = config.movieSettings.showImages;
                    view.querySelector("#numWatchedToReport").value = config.movieSettings.numWatchedToReport;
                    view.querySelector("#numTiedToReport").value = config.movieSettings.numTiedToReport;
                    view.querySelector("#numGenresToReport").value = config.movieSettings.numGenresToReport;
                    view.querySelector("#numActorsToReport").value = config.movieSettings.numActorsToReport;
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

                loadPage(view, params);
            });

            function saveConfigData() {
                // Visual indicator: show subtle amber text during network request
                const collectionAutosaveStatus = view.querySelector('#collectionAutosaveStatus');
                collectionAutosaveStatus.textContent = "Saving...";
                collectionAutosaveStatus.style.color = "#ffb703";
                collectionAutosaveStatus.style.opacity = "1";

                const searchLocationsAutosaveStatus = view.querySelector('#searchLocationsAutosaveStatus');
                searchLocationsAutosaveStatus.textContent = "Saving...";
                searchLocationsAutosaveStatus.style.color = "#ffb703";
                searchLocationsAutosaveStatus.style.opacity = "1";

                const searchLocations = view.querySelector('#searchLocations');
                const searchUrls = searchLocations.value
                    .split('\n')
                    .map(line => line.trim())
                    .filter(line => line.length > 0);

                const filterList = view.querySelector('#collectionFilterList');

                const missingMovieFilters = filterList.value
                    .split('\n')
                    .map(line => line.trim())
                    .filter(line => line.length > 0);

                ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                    config.movieSettings.searchLocations = searchUrls;
                    config.movieSettings.CollectionFiltersForMissingMovies = missingMovieFilters;

                    ApiClient.updatePluginConfiguration(Helpers.pluginId, config).then(function (result) {
                        // Visual indicator: show green confirmation text
                        collectionAutosaveStatus.textContent = "Saved";
                        collectionAutosaveStatus.style.color = "#52b788";

                        searchLocationsAutosaveStatus.textContent = "Saved";
                        searchLocationsAutosaveStatus.style.color = "#52b788";

                        // Fade the "Saved" text away after 2 seconds
                        setTimeout(() => {
                            collectionAutosaveStatus.style.opacity = "0";
                            searchLocationsAutosaveStatus.style.opacity = "0";
                        }, 2000);

                        Dashboard.processPluginConfigurationUpdateResult(result);
                    });
                });
            }

            const debouncedSave = debounce(saveConfigData, 1000);

            view.querySelector('#searchLocations').addEventListener('input', function () {
                const autosaveStatus = view.querySelector('#searchLocationsAutosaveStatus');
                autosaveStatus.textContent = "Unsaved Changes...";
                autosaveStatus.style.color = "#e63946";
                autosaveStatus.style.opacity = "1";

                debouncedSave();
            });

            view.querySelector('#searchLocations').addEventListener('blur', function () {
                saveConfigData();
            });
            view.querySelector("#searchLocationsHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Search Locations for Missing Videos", "URLs to the search location for missing videos ie \"https://www.google.com/search?q=\"");
                });

            view.querySelector('#collectionFilterList').addEventListener('blur', function () {
                saveConfigData();
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            view.querySelector("#numDaysForMovieStreaming").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.movieSettings.numDaysForMovieStreaming = view.querySelector("#numDaysForMovieStreaming").value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );

            view.querySelector("#numDaysForMovieStreamingHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Days after the Premiere", "Since digital availability is usually some time after the known release date, how many days to wait before showing a movie as missing? This number also has an effect on reporting if a movie is missing from a collection. If the movie is available for streaming, this number is ignored for showing search or not.");
                });
            view.querySelector("#showSearchTheMovieDB").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.movieSettings.showSearchTheMovieDB = view.querySelector("#showSearchTheMovieDB").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#showSearchTheMovieDBHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Search The Movie DB", "Enable searching for the media on The MovieDB");
                });

            view.querySelector("#showFilteredMissingSummaries").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.movieSettings.showFilteredMissingSummaries = view.querySelector("#showFilteredMissingSummaries").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#showFilteredMissingSummariesHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Filtered Missing Summaries", "For summaries of movies missing, show the filtered results.");
                });

            view.querySelector("#alwaysShowJustWatch").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.movieSettings.alwaysShowJustWatch = view.querySelector("#alwaysShowJustWatch").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#alwaysShowJustWatchHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Always Show Just Watch", "When listing media streaming services always show a link for the Just Watch website");
                });

            view.querySelector("#maxNumStreamingProviders").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        const parsed = parseInt(view.querySelector("#maxNumStreamingProviders").value);
                        const value = Number.isNaN(parsed) ? 3 : parsed;

                        config.movieSettings.maxNumStreamingProviders = value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#maxNumStreamingProvidersHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Streaming Providers", "How many Streaming Providers to show when listing media");
                });

            view.querySelector('#collectionFilterList').addEventListener('input', function () {
                const autosaveStatus = view.querySelector('#collectionAutosaveStatus');
                autosaveStatus.textContent = "Unsaved Changes...";
                autosaveStatus.style.color = "#e63946";
                autosaveStatus.style.opacity = "1";

                debouncedSave();
            });
            view.querySelector("#collectionFilterHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Collection Filter", "Regular expressions or strings, that when matched remove the matches from the missing report.");
                });


            view.querySelector("#showImages").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.movieSettings.showImages = view.querySelector("#showImages").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#showImagesHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Show Images", "In reports and summaries, show the images along with the titles");
                });


            view.querySelector("#numWatchedToReport").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        const parsed = parseInt(view.querySelector("#numWatchedToReport").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.movieSettings.numWatchedToReport = value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#numWatchedToReportHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Watched Movies", "The default is 5, but you can limit how many Watched Movies to report on");
                });

            view.querySelector("#numTiedToReport").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        const parsed = parseInt(view.querySelector("#numTiedToReport").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.movieSettings.numTiedToReport = value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#numTiedToReportHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Tied Videos", "The default is 5, but when reporting a list of media, and the value is tied how many do you want to show?");
                });

            view.querySelector("#numGenresToReport").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        const parsed = parseInt(view.querySelector("#numGenresToReport").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.movieSettings.numGenresToReport = value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#numGenresToReportHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Genres", "The default is 5, but when reporting a list of genres, how many should be shown?");
                });

            view.querySelector("#numActorsToReport").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        const parsed = parseInt(view.querySelector("#numActorsToReport").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.movieSettings.numActorsToReport = value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#numActorsToReportHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Actors", "The default is 5, but when reporting a list of actors, how many should be shown?");
                });
        };
    }
);

