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
                    view.querySelector("#numDaysFutureForMissing").value = config.tvSettings.numDaysFutureForMissing;
                    view.querySelector("#showSearchTheMovieDB").checked = config.tvSettings.showSearchTheMovieDB;

                    const searchLocations = config.tvSettings.searchLocations || [];
                    view.querySelector("#searchLocations").value = searchLocations.join('\n');;

                    view.querySelector("#showFilteredMissingSummaries").checked = config.tvSettings.showFilteredMissingSummaries;
                    view.querySelector("#reportOnMissingSpecials").checked = config.tvSettings.reportOnMissingSpecials;
                
                    view.querySelector("#alwaysShowJustWatch").checked = config.tvSettings.alwaysShowJustWatch;
                    view.querySelector("#maxNumStreamingProviders").value = config.tvSettings.maxNumStreamingProviders;

                    view.querySelector("#showImages").checked = config.tvSettings.showImages;
                    view.querySelector("#numWatchedToReport").value = config.tvSettings.numWatchedToReport;
                    view.querySelector("#numTiedToReport").value = config.tvSettings.numTiedToReport;
                    view.querySelector("#numGenresToReport").value = config.tvSettings.numGenresToReport;
                    view.querySelector("#numActorsToReport").value = config.tvSettings.numActorsToReport;
                }
            );
        }

        function getSeriesFilterHeader() {
            var retVal =
                `
                <tr style="text-align: left;">
                    <th data-column="SeriesName"    data-type="string"   class="info_cell_heading" style="padding: 8px; white-space: nowrap; cursor: pointer;">Series Name (Year) <span class=\"sort-icon\"></span></th>
                    <th data-column="TrackSeries"   data-type="checkbox" class="info_cell_heading" style="width: 15%; padding: 8px; white-space: nowrap; cursor: pointer;">Track Series? <span class=\"sort-icon\"></span></th>
                    <th data-column="TrackSpecials" data-type="checkbox" class="info_cell_heading" style="width: 15%; padding: 8px; white-space: nowrap;">Track Specials? <span class=\"sort-icon\"></span></th>
                    <th data-column="MinSeason"                          class="info_cell_heading" style="width: 15%; padding: 8px; white-space: nowrap;">Min Season Number</th>
                    <th data-column="MaxSeason"                          class="info_cell_heading" style="width: 15%; padding: 8px; white-space: nowrap;">Max Season Number</th>
                </tr>
                `;
            return retVal;
        }

        function getSeriesFilterRowData(item, index, tr) {
            const chkTrackId = 'chkTrack_' + index;
            const chkSpecialsId = 'chkSpecials_' + index;

            const nameKey = `${item.PremiereYear || ''}_${item.SortName}`
            const trackSeriesLabel = item.TrackSeries ? 'Yes' : 'No';
            const trackSpecialsLabel = item.TrackSpecials ? 'Yes' : 'No';

            tr._seriesFilterItem = item;
            tr.setAttribute('data-index', index);

            let yearString = ``;
            if (item.PremiereYear && (item.PremiereYear > 0))
                yearString = `(${item.PremiereYear})`
            var retVal = `
                    <td style="padding: 8px; vertical-align: middle; font-weight: 500;" sort-value='${nameKey}'>
                        ${item.Name} <span style="opacity: 0.6; font-size: 0.9em; margin-left: 4px;">${yearString}</span>
                    </td>
                    <td style="padding: 8px; vertical-align: middle;">
                        <label class="emby-checkbox-label">
                            <input id="${chkTrackId}" is="emby-checkbox" type="checkbox" class="seriesfilter-trackseries" ${item.TrackSeries ? 'checked' : ''} />
                            <span class="checkboxLabel textLabel_${index}_trackseries">${trackSeriesLabel}</span>
                        </label>

                    <td style="padding: 8px; vertical-align: middle;">
                        <label class="emby-checkbox-label">
                            <input id="${chkSpecialsId}" is="emby-checkbox" type="checkbox" class="seriesfilter-trackspecials" ${item.TrackSpecials ? 'checked' : ''} />
                            <span class="checkboxLabel textLabel_${index}_trackspecials">${trackSpecialsLabel}</span>
                        </label>
                    </td>
                    <td style="padding: 8px; vertical-align: middle;">
                        <input is="emby-input" type="number" class="seriesfilter-minseason" min="1" max="${item.NumSeasons}" value="${item.MinSeason !== -1 ? item.MinSeason : ''}" style="width: 100%; margin: 0;" placeholder="Min Season"/>
                    </td>
                    <td style="padding: 8px; vertical-align: middle;">
                        <input is="emby-input" type="number" class="seriesfilter-maxseason" min="1" max="${item.NumSeasons}" value="${item.MaxSeason !== -1 ? item.MaxSeason : ''}" style="width: 100%; margin: 0;" placeholder="Max Season"/>
                    </td>
                `;

            return retVal;
        }

        function debounce(func, delay) {
            let timeout;
            return function (...args) {
                clearTimeout(timeout);
                timeout = setTimeout(() => func.apply(this, args), delay);
            };
        }

        return function (view, params) {
            function loadSeriesFilterTable() {
                const baseId = 'seriesFilter';
                const baseUrl = "Statistics2026/series_filters";

                Helpers.loadTableData(view, baseId, baseUrl, getSeriesFilterHeader, getSeriesFilterRowData, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg);
            }

            // init code here
            view.addEventListener('viewshow', async function (e) {
                Helpers.initTabSystem(view, AdminHelpers.getTabHierarchy, AdminHelpers.getTabs);

                loadPage(view, params);
                loadSeriesFilterTable();
            });

            function saveSeriesFilterItem(filterItem) {
                const baseUrl = ApiClient.getUrl(`Statistics2026/set_series_filter`);

                const params = {
                    series_id: filterItem.SeriesId || '',
                    track_series: !!filterItem.TrackSeries,
                    track_specials: !!filterItem.TrackSpecials,
                    min_season: filterItem.MinSeason !== null && filterItem.MinSeason !== undefined ? filterItem.MinSeason : -1,
                    max_season: filterItem.MaxSeason !== null && filterItem.MaxSeason !== undefined ? filterItem.MaxSeason : -1,
                };

                const queryString = new URLSearchParams(params).toString();
                const finalUrl = `${baseUrl}?${queryString}`

                ApiClient.ajax({
                    type: 'POST', // or 'GET'
                    url: finalUrl,
                    dataType: 'text'
                }).then(function () {
                    // SUCCESS: Request parsed completely and processed by your entry point!
                    console.log(`Successfully updated configuration for Series ID: ${filterItem.SeriesId}`);
                }).catch(function (error) {
                    // Triggers instantly if the response fails or hits an error code (404, 500, etc.)
                    console.error("Plugin endpoint failed:", error);
                });
            }

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

                ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                    config.tvSettings.searchLocations = searchUrls;

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

            // Handle saving instantly when a checkbox column is clicked or checked
            view.querySelector('#seriesFilter_body').addEventListener('change', function (e) {
                const row = e.target.closest('tr');
                if (!row || !row._seriesFilterItem)
                    return;

                const index = parseInt(row.getAttribute('data-index'), 10);

                if (e.target.classList.contains('seriesfilter-trackseries')) {
                    row._seriesFilterItem.TrackSeries = e.target.checked;

                    const lbl = row.querySelector(`.textLabel_${index}_trackseries`);
                    if (lbl)
                        lbl.textContent = e.target.checked ? 'Yes' : 'No';
                }

                if (e.target.classList.contains('seriesfilter-trackspecials')) {
                    row._seriesFilterItem.TrackSpecials = e.target.checked;
                    const lbl = row.querySelector(`.textLabel_${index}_trackspecials`);
                    if (lbl)
                        lbl.textContent = e.target.checked ? 'Yes' : 'No';
                }

                if (e.target.classList.contains('seriesfilter-minseason')) {
                    const val = e.target.value;
                    row._seriesFilterItem.MinSeason = val === '' ? -1 : parseInt(val, 10);
                }

                if (e.target.classList.contains('seriesfilter-maxseason')) {
                    const val = e.target.value;
                    row._seriesFilterItem.MaxSeason = val === '' ? -1 : parseInt(val, 10);
                }

                saveSeriesFilterItem(row._seriesFilterItem);
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });


            view.querySelector("#numDaysFutureForMissing").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.tvSettings.numDaysFutureForMissing = view.querySelector("#numDaysFutureForMissing").value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#numDaysFutureForMissingHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Days in the Future to list Upcoming Videos", "How many days in the future should upcoming episodes and movies be searched for (limited by published schedules).");
                });

            view.querySelector("#showSearchTheMovieDB").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.tvSettings.showSearchTheMovieDB = view.querySelector("#showSearchTheMovieDB").checked;
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
                        config.tvSettings.showFilteredMissingSummaries = view.querySelector("#showFilteredMissingSummaries").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#showFilteredMissingSummariesHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Filtered Missing Summaries", "For summaries of episodes missing, show the filtered results.");
                });

            view.querySelector("#reportOnMissingSpecials").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.tvSettings.reportOnMissingSpecials = view.querySelector("#reportOnMissingSpecials").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#reportOnMissingSpecialsHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Report on Missing Specials", "When analyzing missing episodes, check for episodes that are considered special (Season 0)");
                });

            view.querySelector("#alwaysShowJustWatch").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.tvSettings.alwaysShowJustWatch = view.querySelector("#alwaysShowJustWatch").checked;
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

                        config.tvSettings.maxNumStreamingProviders = value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#maxNumStreamingProvidersHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Streaming Providers", "How many Streaming Providers to show when listing media");
                });

            view.querySelector("#numWatchedToReport").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        const parsed = parseInt(view.querySelector("#numWatchedToReport").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.tvSettings.numWatchedToReport = value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#numWatchedToReportHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Number of Watched Videos", "The default is 5, but you can limit how many Watched Videos to report on");
                });

            view.querySelector("#showImages").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.tvSettings.showImages = view.querySelector("#showImages").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );
            view.querySelector("#showImagesHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Show Images", "In reports and summaries, show the images along with the titles");
                });

            view.querySelector("#numTiedToReport").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        const parsed = parseInt(view.querySelector("#numTiedToReport").value);
                        const value = Number.isNaN(parsed) ? 5 : parsed;

                        config.tvSettings.numTiedToReport = value;
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

                        config.tvSettings.numGenresToReport = value;
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

                        config.tvSettings.numActorsToReport = value;
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

