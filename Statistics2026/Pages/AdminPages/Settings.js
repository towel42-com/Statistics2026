define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Helpers.js')
],
    function (mainTabsManager, AdminHelpers, Helpers) {
        'use strict';

        function getEmbyColorValue(variableName) {
            const computedStyles = window.getComputedStyle(document.body);
            let value = computedStyles.getPropertyValue(variableName).trim();
            if (value) {
                return value;
            }

            // Extract baseline components if values rely on Emby's modern HSL split structure
            let h = computedStyles.getPropertyValue(`${variableName}-hue`).trim();
            let s = computedStyles.getPropertyValue(`${variableName}-saturation`).trim();
            let l = computedStyles.getPropertyValue(`${variableName}-lightness`).trim();
            let a = computedStyles.getPropertyValue(`${variableName}-alpha`).trim();
            if (variableName == "--button-background-blur") {
                h = computedStyles.getPropertyValue(`--button-background-hue`).trim();
                s = computedStyles.getPropertyValue(`--button-background-saturation`).trim();
            }
            else if (variableName == "--focus-text-color") {
                //    color: hsla(var(--theme-text-color-hue),var(--theme-text-color-saturation),var(--focus-text-color-lightness),var(--theme-text-color-alpha))
                h = computedStyles.getPropertyValue(`--theme-text-color-hue`).trim();
                s = computedStyles.getPropertyValue(`--theme-text-color-saturation`).trim();
                l = computedStyles.getPropertyValue(`--focus-text-color-lightness`).trim();
                a = computedStyles.getPropertyValue(`--theme-text-color-alpha`).trim();
            }
            else if (variableName == "--theme-secondary-text-color") {
                h = computedStyles.getPropertyValue(`--theme-text-color-hue`).trim();
                s = computedStyles.getPropertyValue(`--theme-text-color-saturation`).trim();
                l = computedStyles.getPropertyValue(`--theme-text-color-lightness`).trim();
                a = computedStyles.getPropertyValue(`--theme-secondary-text-color-alpha`).trim();
            }

            // Format percentage suffixes cleanly for standard CSS consumption
            if (s && !s.includes('%')) {
                s = `${s}%`;
            }

            if (l && !l.includes('%')) {
                l = `${l}%`;
            }

            if (!a && variableName.endsWith("-lightended")) {
                a = "60%";
            }

            if (h && s && l && a) {
                return `hsla(${h} ${s} ${l} ${a})`;
            }

            if (h && s && l && !a) {
                return `hsl(${h} ${s} ${l})`;
            }

            return "undefined";
        }

        function getSwatchCard(variableName) {
            const swatchCard = document.createElement("div");
            var swatchCardCss = "display: flex; align-items: center; justify-content: space-between; flex-wrap: nowrap; gap: 16px; padding: 10px; border: 1px solid var(--line-background); border-radius: 4px; background-color: var(--theme-background-accent); min-width: max-content; box-sizing: border-box;";

            swatchCard.style.cssText = swatchCardCss;

            const leftLayoutBlock = document.createElement("div");
            leftLayoutBlock.style.cssText = "display: flex; align-items: center; gap: 12px; width: max-content; min-width: max-content; flex-shrink: 0; ";

            const embyColor = getEmbyColorValue(variableName);

            const colorBlock = document.createElement("div");
            colorBlock.style.cssText = `width: 32px; height: 32px; border-radius: 4px; border: 1px solid var(--line-background); background-color: ${embyColor}; flex-shrink: 0;`;

            const labelText = document.createElement("code");
            labelText.style.cssText = "display: flex; align-items: center; justify-content: space-between; width: max-content; gap: 8px; flex-shrink: 0;";

            const varNameSpan = document.createElement("span");
            const varNameTextCss = "color: var(--text-normal); font-size: 0.85rem; font-family: monospace; white-space: nowrap; min-width: max-content; flex-shrink: 0;";
            varNameSpan.style.cssText = varNameTextCss;
            varNameSpan.textContent = variableName;

            const embyColorCss = "color: var(--theme-primary-color); font-size: 0.85rem; font-family: monospace; white-space: nowrap; min-width: max-content; font-weight: bold; text-align: right; margin-left: auto; flex-shrink: 0;";

            const embyColorSpan = document.createElement("span");
            embyColorSpan.style.cssText = embyColorCss;
            embyColorSpan.textContent = embyColor;

            labelText.appendChild(varNameSpan);
            labelText.appendChild(embyColorSpan);

            leftLayoutBlock.appendChild(colorBlock);
            leftLayoutBlock.appendChild(labelText);

            swatchCard.appendChild(leftLayoutBlock);

            return swatchCard;
        }

        function stripSuffixFromList(str, suffixes) {
            for (const suffix of suffixes) {
                if (str.endsWith(suffix)) {
                    return str.slice(0, -suffix.length);
                }
            }
            return str;
        }

        function getAllVariablesFromCSSProperties(properties) {
            let variables = [];

            for (let i = 0; i < properties.length; i++) {
                let propName = properties[i];
                if (propName.toLowerCase().includes("error")) {
                    let xyz = 0;
                }

                if (propName.startsWith('--')) {
                    if (propName.endsWith('-width')
                        || propName.endsWith('-top')
                        || propName.endsWith('-left')
                        || propName.endsWith('-right')
                        || propName.endsWith('-bottom')
                        || propName.endsWith('-cards')
                        || propName.endsWith('-filter')
                        || propName.endsWith('-position')
                        || propName.endsWith('-cards')
                        || propName.endsWith('-size-adjust')
                        || propName.endsWith('-cards')
                        || propName.endsWith('-size')
                        || propName.endsWith('-opacity')
                        || propName.endsWith('--focus-blur-background-lightness')
                        || propName.startsWith("--local")
                    ) {
                        continue;
                    }

                    propName = stripSuffixFromList(propName, ["-hue", "-saturation", "-lightness", "-alpha"]);
                    variables.push(propName);
                }
            }

            variables = [...new Set(variables)];
            return variables;
        }

        function getAllStyleVariables(view) {
            const windowStyles = window.getComputedStyle(document.documentElement);

            let variables = getAllVariablesFromCSSProperties(windowStyles);

            variables.sort((lhs, rhs) => {
                const lhsValue = getEmbyColorValue(lhs);
                const rhsValue = getEmbyColorValue(rhs);

                const isLHSUndefined = (lhsValue == "undefined");
                const isRHSUndefined = (rhsValue == "undefined");
                if (isLHSUndefined == isRHSUndefined)
                    return lhs.localeCompare(rhs);

                if (isLHSUndefined) {
                    return 1;
                }
                if (isRHSUndefined) {
                    return -1;
                }
                return 0;
            });

            return variables;
        }


        function loadColorPanel(view, params) {
            // Query specifically within the active view context fragment
            const gridContainer = view.querySelector("#embyColorGrid");

            // Protect against empty queries or duplicate execution rendering loops
            if (gridContainer && gridContainer.children.length === 0) {
                const embyVariables = getAllStyleVariables(view);
                embyVariables.forEach(variableName => {
                    const swatchCard = getSwatchCard(variableName);
                    gridContainer.appendChild(swatchCard);
                });
            }
        }


        function loadPage(view, params) {
            view.querySelector(`#embyColorGridPanel`).style.display = 'none';
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

                loadColorPanel(view, params);
            });
        }

        function getSeriesFilterHeader() {
            var retVal =
                `
                <tr style="text-align: left;">
                    <th data-column="TrackSeries"   data-type="checkbox" class="info_cell_heading" style="width: 15%; padding: 8px; white-space: nowrap; cursor: pointer;">Track Series? <span class=\"sort-icon\"></span></th>
                    <th data-column="TrackSpecials" data-type="checkbox" class="info_cell_heading" style="width: 15%; padding: 8px; white-space: nowrap;">Track Specials? <span class=\"sort-icon\"></span></th>
                    <th data-column="SeriesName"    data-type="string"   class="info_cell_heading" style="padding: 8px; white-space: nowrap; cursor: pointer;">Series Name (Year) <span class=\"sort-icon\"></span></th>
                    <th data-column="MinSeason"                          class="info_cell_heading" style="width: 15%; padding: 8px; white-space: nowrap;">Min Season</th>
                    <th data-column="MaxSeason"                          class="info_cell_heading" style="width: 15%; padding: 8px; white-space: nowrap;">Max Season</th>
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
                    <td style="padding: 8px; vertical-align: middle; font-weight: 500;" sort-value='${nameKey}'>
                        ${item.Name} <span style="opacity: 0.6; font-size: 0.9em; margin-left: 4px;">${yearString}</span>
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
            view.addEventListener('viewshow', function (e) {
                mainTabsManager.setTabs(this, Helpers.getTabIndex("Settings", AdminHelpers.getTabs), AdminHelpers.getTabs);
                Helpers.injectStyleSheet();

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

