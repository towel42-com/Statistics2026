define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Helpers.js')
],
    function (mainTabsManager, AdminHelpers, Helpers) {
        'use strict';

        function loadPage(view, params) {
            ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                view.querySelector("#hasConnectUserID").checked = config.hasConnectUserID;
                view.querySelector("#showUnknownDVProfiles").checked = config.showUnknownDVProfiles;
                view.querySelector("#showAllResolutions").checked = config.showAllResolutions;
                view.querySelector("#numMostActive").value = config.numMostActiveUsers;
                view.querySelector("#searchLocation").value = config.searchLocation;
                view.querySelector("#numDaysFutureForMissing").value = config.numDaysFutureForMissing;
                view.querySelector("#numWatchedToReport").value = config.numWatchedToReport;
                view.querySelector("#numTiedToReport").value = config.numTiedToReport;
                view.querySelector("#excludeAdmin").checked = config.excludeAdmin;
                view.querySelector("#resetPlayCount").checked = config.resetPlayCount;
                view.querySelector("#resetDBState").checked = config.resetDBState;
                view.querySelector("#showDebugInfo").checked = config.showDebugInfo;
                view.querySelector("#reportOnMissingSpecials").checked = config.reportOnMissingSpecials;

                const collectionFilters = config.CollectionNamesToIgnoreFromMissingEpisodes || [];
                view.querySelector("#collectionFilterList").value = collectionFilters.join('\n');
            });
        }

        function getSeriesFilterHeader() {
            var cellBgStyle = "background: rgb(82, 181, 75) !important; color: #fff !important;";

            var retVal =
                `<thead id="seriesFilter_head">
                    <tr style="border-bottom: 2px solid rgba(255,255,255,0.1); height: 40px; user-select: none;">
                        <th class="info_cell_heading" data-sort="TrackSeries" data-type="checkbox" style="${cellBgStyle} width: 15%; padding: 8px; white-space: nowrap; cursor: pointer;">Track Series? <span class=\"sort-icon\"></span></th>
                        <th class="info_cell_heading" data-sort="TrackSpecials" data-type="checkbox" style="${cellBgStyle} width: 15%; padding: 8px; white-space: nowrap;">Track Specials? <span class=\"sort-icon\"></span></th>
                        <th class="info_cell_heading" data-sort="Name" data-type="string" style="${cellBgStyle} padding: 8px; white-space: nowrap; cursor: pointer;">Series Name (Year) <span class=\"sort-icon\"></span></th>
                        <th class="info_cell_heading" style="${cellBgStyle} width: 15%; padding: 8px; white-space: nowrap;">Min Season</th>
                        <th class="info_cell_heading" style="${cellBgStyle} width: 15%; padding: 8px; white-space: nowrap;">Max Season</th>
                    </tr>
                </thead>
                `;
            return retVal;
        }

        function getSeriesFilterRowData(item, index) {
            const chkTrackId = 'chkTrack_' + index;
            const chkSpecialsId = 'chkSpecials_' + index;

            const nameKey = `${item.PremiereYear || ''}_${item.SortName}`
            const trackSeriesLabel = item.TrackSeries ? 'Yes' : 'No';
            const trackSpecialsLabel = item.TrackSpecials ? 'Yes' : 'No';

            var retVal = `
                    <td style="padding: 8px; vertical-align: middle;">
                        <label class="emby-checkbox-label">
                            <input id="${chkTrackId}" is="emby-checkbox" type="checkbox" class="grid-enabled" ${item.TrackSeries ? 'checked' : ''} />
                            <span class="checkboxLabel textLabel_${index}_trackseries">${trackSeriesLabel}</span>
                        </label>

                    <td style="padding: 8px; vertical-align: middle;">
                        <label class="emby-checkbox-label">
                            <input id="${chkSpecialsId}" is="emby-checkbox" type="checkbox" class="grid-specials" ${item.TrackSpecials ? 'checked' : ''} />
                            <span class="checkboxLabel textLabel_${index}_trackspecials">${trackSpecialsLabel}</span>
                        </label>
                    </td>
                    <td style="padding: 8px; vertical-align: middle; font-weight: 500;" sort-value='${nameKey}'>
                        ${item.Name} <span style="opacity: 0.6; font-size: 0.9em; margin-left: 4px;">(${item.PremiereYear || ''})</span>
                    </td>
                    <td style="padding: 8px; vertical-align: middle;">
                        <input is="emby-input" type="number" class="grid-min" min="1" max="${item.NumSeasons}" value="${item.MinSeason !== -1 ? item.MinSeason : ''}" style="width: 100%; margin: 0;" placeholder="Min Season"/>
                    </td>
                    <td style="padding: 8px; vertical-align: middle;">
                        <input is="emby-input" type="number" class="grid-max" min="1" max="${item.NumSeasons}" value="${item.MaxSeason !== -1 ? item.MaxSeason : ''}" style="width: 100%; margin: 0;" placeholder="Max Season"/>
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
                const baseUrl = "Statistics2026/series_filter";

                Helpers.loadTableData(view, baseId, baseUrl, getSeriesFilterHeader, getSeriesFilterRowData, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg);
            }

            // init code here
            view.addEventListener('viewshow', function (e) {
                mainTabsManager.setTabs(this, Helpers.getTabIndex("Settings", AdminHelpers.getTabs), AdminHelpers.getTabs);
                Helpers.injectStyleSheet();

                loadPage(view, params);
                loadSeriesFilterTable();
            });

            function saveConfigData() {
                // Visual indicator: show subtle amber text during network request
                const collectionAutosaveStatus = view.querySelector('#collectionAutosaveStatus');
                collectionAutosaveStatus.textContent = "Saving...";
                collectionAutosaveStatus.style.color = "#ffb703";
                collectionAutosaveStatus.style.opacity = "1";

                const filterList = view.querySelector('#collectionFilterList');

                const linesToSave = filterList.value
                    .split('\n')
                    .map(line => line.trim())
                    .filter(line => line.length > 0);

                ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                    config.CollectionNamesToIgnoreFromMissingEpisodes = linesToSave;

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
                const collectionAutosaveStatus = view.querySelector('#collectionAutosaveStatus');
                collectionAutosaveStatus.textContent = "Unsaved Changes...";
                collectionAutosaveStatus.style.color = "#e63946";
                collectionAutosaveStatus.style.opacity = "1";

                debouncedSave();
            });

            view.querySelector('#collectionFilterList').addEventListener('blur', function () {
                saveConfigData();
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

            view.querySelector("#reportOnMissingSpecials").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.reportOnMissingSpecials = view.querySelector("#reportOnMissingSpecials").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );

            view.querySelector("#showDebugInfo").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.showDebugInfo = view.querySelector("#showDebugInfo").checked;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );

            view.querySelector("#resetDBState").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.resetDBState = view.querySelector("#resetDBState").checked;
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

            view.querySelector("#numDaysFutureForMissing").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.numDaysFutureForMissing = view.querySelector("#numDaysFutureForMissing").value;
                        ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                    });
                }
            );

            view.querySelector("#searchLocation").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.searchLocation = view.querySelector("#searchLocation").value;
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

            view.querySelector("#numTiedToReport").addEventListener("input",
                function () {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.numTiedToReport = parseInt(view.querySelector("#numTiedToReport").value) || 5;
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

            view.querySelector("#numDaysFutureForMissingHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("How many days in the future should Upcoming episodes and movies be searched for (limited by published schedules).", "Number of Days in the Future to Upcoming Videos");
                });

            view.querySelector("#searchLocationHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("URL to the search location for missing videos ie \"https://thepiratebay.org/search.php\"", "Search Location for Missing Videos");
                });

            view.querySelector("#numMostActiveHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("The default is 5, but you can limit how many most active users that are reported", "Number of Most Active Users");
                });

            view.querySelector("#numWatchedToReportHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("The default is 5, but you can limit how many Watched Videos to report on", "Number of Watched Videos");
                });

            view.querySelector("#numTiedToReportHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("The default is 5, but when reporting a list of media, and the value is tied how many do you want to show?", "Number of Tied Videos");
                });

            view.querySelector("#excludeAdminHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("For security reasons, Administrators are not typically viewers of media and likely should be excluded from analysis.", "Exclude Administrators");
                });

            view.querySelector("#resetPlayCountHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("When computing the user watch data, reset the playcount to the default value (typically 1), and show Administrators as watched for every video. This settings resets to false after the Reset has occured.", "Reset Play Count");
                });

            view.querySelector("#resetDBStateHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("Next time any analysis or reporting is done, recompute the database state. This settings resets to false after the Reset has occured", "Reset Database State");
                });

            view.querySelector("#reportOnMissingSpecialsHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("When analyzing missing episodes, check for episodes that are considered special (Season 0)", "Report on Missing Specials");
                });

            view.querySelector("#showDebugInfoHelp").addEventListener("click",
                function () {
                    Helpers.showInfo("More information is reported on the Summary screen, this functionality is also turned on if Debug Logging is enabled", "Show Debug Info");
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
    }
);

