define([
    'dialogHelper',
    'toast',
    'prompt',
    'emby-button'
],
    function (dialogHelper, toast, prompt) {
        'use strict';

        const pluginId = "23ADB024-F759-438F-B9A7-D5912A75596C";

        async function getStatistics2026Data(url_to_get, options = {}) {
            console.log("getStatistics2026Data Url = " + url_to_get);
            return ApiClient.ajax({
                type: "GET",
                url: url_to_get,
                dataType: "json",
                signal: options.signal
            });
        }

        async function getUserPluginConfig() {
            const url = ApiClient.getUrl('/emby/Statistics2026/get_user_config');
            console.log(`getUserPluginConfig: ${url}`);

            return await ApiClient.getJSON(url);
        }

        async function updateUserPluginConfiguration(config) {
            const url = ApiClient.getUrl('/emby/Statistics2026/set_user_config');
            console.log(`updateUserPluginConfiguration: ${url}`);

            const payload = JSON.stringify(config);
            return await ApiClient.ajax({
                type: 'POST',
                url: url,
                data: payload,
                contentType: 'application/json'
            });
        }

        async function CheckForValidConfig() {
            const urlText = "/emby/Statistics2026/database_status";
            const url = ApiClient.getUrl(urlText);

            let response = await ApiClient.getJSON(url).catch(error => {
                const errorMessage = `'${error}' - '${urlText}'`;
                console.error("database_status failed:", errorMessage);
                showInfo("Problem connecting to back end", errorMessage, false);
                return null;
            });

            if (!response) {
                return false;
            }

            if (!response.DBStateOK) {
                let errorMessage = "";
                let systemStatusObj = null;

                systemStatusObj = await getSystemStatusAsync().catch(error => {
                    errorMessage = `'${error}'`;
                    console.error("getSystemStatus failed:", errorMessage);
                });

                let message = `<div>${response.DBStateMessage}</div>`;

                if (systemStatusObj != null) {
                    if (systemStatusObj.html != "" && systemStatusObj.html !== undefined) {
                        message += systemStatusObj.html;
                    }

                    if (systemStatusObj.error != "" && systemStatusObj.error !== undefined) {
                        errorMessage = systemStatusObj.error;
                    }
                }

                if (errorMessage != "" && errorMessage !== undefined) {
                    message += errorMessage;
                }

                const title = "Incomplete or Missing Database";
                showInfo(title, message, true);
            }
            return Boolean(response.DBStateOK);
        }

        async function isDebugLoggingEnabled() {
            const url = ApiClient.getUrl("/emby/System/Configuration");
            let sysConfig = await ApiClient.getJSON(url).catch(error => {
                return false;
            });
            return sysConfig ? sysConfig.EnableDebugLevelLogging : false;
        }

        function calculateProgressClass(value) {
            value = Math.round(value);
            if (value == 0)
                return ``;
            else if (value < 20)
                return `progress-red`;
            else if (value < 40)
                return `progress-darkRed`;
            else if (value < 60)
                return `progress-orange`;
            else if (value < 80)
                return `progress-darkOrange`;
            else if (value < 100)
                return `progress-darkGreen`;
            else
                return `progress-green`;
        }

        function numDaysTo(date) {
            const todayDT = new Date();
            const today = new Date(todayDT.getFullYear(), todayDT.getMonth(), todayDT.getDate());
            const numMS = today - date;
            const msPerDay = 24 * 60 * 60 * 1000;

            const numDays = Math.round(numMS / msPerDay);
            return numDays;
        }

        function calculateMediaStatusClass(date) {
            const numDays = numDaysTo(date);

            if (numDays < 0)
                return ``;
            if (numDays <= 3)
                return `progress-green`;
            else if (numDays < 7)
                return `progress-darkGreen`;
            else if (numDays < 14)
                return `progress-yellow`;
            else if (numDays < 28)
                return `progress-orange`;
            else if (numDays < 42)
                return `progress-darkOrange`;
            else if (numDays < 56)
                return `progress-red`;
            else
                return `progress-darkRed`;
        }

        function getTVProgressRowData(info) {
            let progressClass = calculateProgressClass(info.Episodes.Percent);
            var retVal = `
            <td style="vertical-align: middle; white-space: nowrap; text-align:left;">${info.Name} (${info.PremiereYear})</td>
            <td class="center ${progressClass}" style="vertical-align: middle; white-space: nowrap; text-align:left;">${info.Episodes.String}</td>
            <td style="vertical-align: middle; white-space: nowrap; text-align:left;">${info.ScoreStr}/10</td>
            <td style="vertical-align: middle; white-space: nowrap; text-align:left;">${info.SeriesStatus}</td>
            `;
            return retVal;
        }

        function getTVProgressHeader() {
            var retVal = `
                <tr style="text-align: left; white-space: nowrap;">
                    <th data-column="Name" data-type="string" class="info_cell_heading">TV Series (Premiere Year)<span class="sort-icon"></span></th>
                    <th data-column="EpisodeProgess" data-type="progress" class="info_cell_heading" style="white-space: nowrap;">Progress<span class="sort-icon"></span><span class="infoBlock" id="episodesInfo"><i class="md-icon">info</i></span></th>
                    <th data-column="Score" data-type="number" class="info_cell_heading">Score<span class="sort-icon"></span></th>
                    <th data-column="Status" data-type="string" class="info_cell_heading">Series Status<span class="sort-icon"></span></th>
                </tr>`;
            return retVal;
        }

        function LoadTVProgress(view, userName, showLoadingFunc, hideLoadingFunc) {
            view.querySelector("#UserTitle").innerHTML = "TV Series Progress for " + userName;

            var url = "Statistics2026/tv_series_progress/" + userName;

            const sortit = () => {
                sortTable(1, "progress", 'TVSeriesProgress', showLoadingFunc, hideLoadingFunc, 'desc');
            };

            loadTableDataAsync(view, 'TVSeriesProgress', url, getTVProgressHeader, getTVProgressRowData, showLoadingFunc, hideLoadingFunc, sortit);
        }

        function LoadUserStats(view, userName, showLoadingFunc, hideLoadingFunc) {
            showLoadingFunc();

            try {
                view.querySelector("#UserTitle").innerHTML = "User statistics for " + userName;
                view.querySelector("#generalStats").innerHTML = "";
                view.querySelector("#movieStats").innerHTML = "";
                view.querySelector("#showStats").innerHTML = "";

                var generalStats = "";
                generalStats += getSummaryInfo(view, "total_time_watched", userName, "?episodes=all");
                generalStats += getSummaryInfo(view, "total_watchable_time", userName, "?episodes=all");
                view.querySelector("#generalStats").innerHTML = generalStats;

                var movieStats = "";
                movieStats += getSummaryInfo(view, "total_movie_count", userName);
                movieStats += getSummaryInfo(view, "total_movies_watched", userName);
                movieStats += getSummaryInfo(view, "movie_favorite_years", userName);
                movieStats += getSummaryInfo(view, "movie_favorite_genres", userName);
                movieStats += getSummaryInfo(view, "total_time_watched", userName, "?episodes=false", "total_movie_time_watched");
                movieStats += getSummaryInfo(view, "total_watchable_time", userName, "?episodes=false", "total_movie_watchable_time");
                view.querySelector("#movieStats").innerHTML = movieStats;

                var movieMostWatchedStats = "";
                movieMostWatchedStats += getSummaryInfo(view, "last_seen", userName, "?episodes=false", "last_seen_movies");
                movieMostWatchedStats += getSummaryInfo(view, "most_watched_movies", userName);
                view.querySelector("#movieMostWatchedStats").innerHTML = movieMostWatchedStats;

                var showStats = "";
                showStats += getSummaryInfo(view, "total_tv_count", userName);
                showStats += getSummaryInfo(view, "total_tv_watched", userName);
                showStats += getSummaryInfo(view, "total_series_finished", userName);
                showStats += getSummaryInfo(view, "tv_favorite_genres", userName);
                showStats += getSummaryInfo(view, "total_time_watched", userName, "?episodes=true", "total_episode_time_watched");
                showStats += getSummaryInfo(view, "total_watchable_time", userName, "?episodes=true", "total_episode_watchable_time");
                view.querySelector("#showStats").innerHTML = showStats;

                var seriesMostWatchedStats = "";
                seriesMostWatchedStats += getSummaryInfo(view, "last_seen", userName, "?episodes=true", "last_seen_tv");
                seriesMostWatchedStats += getSummaryInfo(view, "most_watched_shows", userName, "");
                view.querySelector("#seriesMostWatchedStats").innerHTML = seriesMostWatchedStats;

            } finally {
                hideLoadingFunc();
            }
        }

        function getMediaRowData(info, showOnServer, showMissing, showStreaming) {
            var retVal = `
            <td style="text-align:left;" sort-value="${info.DisplayName.SortBy}">${info.DisplayName.Text}</td>
            <td style="text-align:left;" sort-value="${info.ParentName.SortBy}">${info.ParentName.Text}</td>
            `;
            let premiereDateClass = "";
            if (showMissing && info.SearchLocation.Text != "") {
                premiereDateClass = `class='${calculateMediaStatusClass(new Date(info.PremiereDate))}'`;
            }

            retVal += `<td style="text-align:left;" sort-value="${info.PremiereDate}" ${premiereDateClass}> ${info.PremiereDate}</td>`;

            if (showOnServer) {
                retVal += `
                <td style="text-align:left;">${info.ResolutionDetail}</td>
                <td style="text-align:left;">${info.Codec}</td>
                <td style="text-align:left;">${info.DolbyVisionProfile}</td>
                <td style="text-align:left;" sort-value="${info.ServerLocation.SortBy}">${info.ServerLocation.Text}</td>
                `;
            }

            if (showStreaming) {
                retVal += `<td style="text-align:left;"  sort-value="${info.StreamingProviders.SortBy}">${info.StreamingProviders.Text}</td>`;
            }

            if (showMissing) {
                retVal += `<td style="text-align:left;" sort-value="${info.SearchLocation.SortBy}">${info.SearchLocation.Text}</td>`;
            }

            return retVal;
        }

        function getMediaHeader(parentName, showOnServer, showMissing, showStreaming) {
            var retVal =
                `<tr style="text-align: left;">
                <th data-column="Name" data-type="string" class="info_cell_heading">Name<span class="sort-icon"></span></th>
                <th data-column="ParentName" data-type="string" class="info_cell_heading">${parentName}<span class="sort-icon"></span></th>
                <th data-column="PremiereDate" data-type="date" class="info_cell_heading">Premiere Date<span class="sort-icon"></span></th>
                `;

            if (showOnServer) {
                retVal += `
                <th data-column="Resolution" data-type="string" class="info_cell_heading">Resolution<span class="sort-icon"></span></th>
                <th data-column="Codec" data-type="string" class="info_cell_heading">Codec<span class="sort-icon"></span></th>
                <th data-column="DolbyVisionProfile" data-type="string" class="info_cell_heading">Dolby Vision Profile<span class="sort-icon"></span></th>
                <th data-column="ServerLocation" data-type="string" class="info_cell_heading">Location on Server<span class="sort-icon"></span></th>
                `;
            }

            if (showStreaming) {
                retVal += `<th data-column="StreamingServies" data-type="string" class="info_cell_heading">Streaming Services(s)<span class="sort-icon"></span></th>`;
            }

            if (showMissing) {
                retVal += `<th data-column="Search" data-type="string" class="info_cell_heading">Click to Search<span class="sort-icon"></span></th>`;
            }

            retVal += "</tr>";
            return retVal;
        }

        async function getSystemStatusAsync() {
            let urlText = `/Statistics2026/get_system_status`;
            console.info("get_system_status - '" + urlText + "'");
            var url = ApiClient.getUrl(urlText);

            let response = await ApiClient.getJSON(url);
            return response;
        }

        async function insertSystemStatus(view, div) {
            let errorMessage = "";
            let systemStatusObj = null;

            systemStatusObj = await getSystemStatusAsync().catch(error => {
                errorMessage = `'${error}'`;
                console.error("getSystemStatus failed:", errorMessage);
            });

            let message = '';

            if (systemStatusObj != null) {
                if (systemStatusObj.html != "" && systemStatusObj.html !== undefined) {
                    message += systemStatusObj.html;
                }

                if (systemStatusObj.error != "" && systemStatusObj.error !== undefined) {
                    errorMessage = systemStatusObj.error;
                }
            }

            if (errorMessage != "" && errorMessage !== undefined) {
                message += errorMessage;
            }

            view.querySelector("#" + div).innerHTML = message;
        }

        async function getLastRunInfo(whichRuns) {
            var param = whichRuns.join(',');
            var urlText = `/Statistics2026/last_run_info?whichRuns=${param}`;
            console.info("last_run_info - '" + urlText + "'");
            var url = ApiClient.getUrl(urlText);

            let response = await ApiClient.getJSON(url);
            return response;
        }

        async function insertGetLastRunInfo(view, whichRuns, div) {
            getUserPluginConfig().then(function (config) {
                var item = view.querySelector("#" + div);
                if (config.showLastRunInfo) {
                    item.style.display = '';
                    getLastRunInfo(whichRuns).then(response => {
                        item.innerHTML = response.html;
                    }).catch(error => {
                        var errorMessage = "'" + error + "' - '" + div + "' - '" + whichRuns.join(',') + "'";
                        console.error("insertGetLastRunInfo failed:", errorMessage);
                        item.innerHTML = `<tr><td colspan="2">Error loading ${whichRuns} status</td></tr>`;
                    });
                }
                else {
                    item.innerHTML = '';
                    item.style.display = 'none';
                }
            });
        }

        function getSummaryInfo(view, whichSummary, user, parameters = "", div = "") {
            var urlText = "/emby/Statistics2026/" + whichSummary;
            if (user != "" && user !== undefined)
                urlText += "/" + user;
            urlText += parameters;
            console.info("getSummaryInfo - '" + urlText + "'");
            var url = ApiClient.getUrl(urlText);

            if (div == "")
                div = whichSummary;

            ApiClient.getJSON(url).then(response => {
                view.querySelector("#" + div).innerHTML = response.html;

                response.dynamicButtons.forEach((v) => {
                    view.querySelector("#" + v.id).addEventListener("click",
                        function () {
                            showInfo(v.title, v.info, true);
                        });
                });

                injectImageProxy(div, view);
            }).catch(error => {
                var errorMessage = "'" + error + "' - '" + div + "' - '" + urlText + "'";
                console.error("getSummaryInfo failed:", errorMessage);
            });

            return `<div name="${div}" id="${div}"></div>`;
        }

        function getTabIndex(tab_name, getTabsFn) {
            var index = 0;

            var tabs = getTabsFn();
            for (index = 0; index < tabs.length; ++index) {
                var path = tabs[index].href;
                if (path.endsWith("=" + tab_name)) {
                    return index;
                }
            }
            return -1;
        }

        const STYLE_ID = 'statistics2026-stylesheet';
        function injectImageProxy(target, view) {
            let root = view || document;

            if (typeof target === 'string') {
                // Supports passing either "myDivId", "#myDivId", or ".myClass"
                const selector = (target.startsWith('#') || target.startsWith('.')) ? target : '#' + target;
                root = root.querySelector(selector);
                if (!root) {
                    return;
                }
            } else if (target instanceof Node) {
                root = target;
            }

            const token = ApiClient.accessToken();

            root.querySelectorAll('img.image-proxy:not([data-proxy-injected="true"])').forEach(function (img) {
                const baseUrl = img.dataset.src;
                if (!baseUrl || img.getAttribute('src')) {
                    return;
                }

                const separator = baseUrl.includes('?') ? '&' : '?';
                img.src = baseUrl + separator + 'api_key=' + encodeURIComponent(token);
                img.dataset.proxyInjected = 'true';
            });
        }

        function injectStyleSheet() {
            if (document.getElementById(STYLE_ID))  // already added
                return;

            const cssUrl = 'configurationpage?name=style.css';
            const link = document.createElement('link');
            link.id = STYLE_ID;
            link.rel = 'stylesheet';
            link.type = 'text/css';
            link.href = cssUrl;

            document.head.appendChild(link);
        }

        const activeLoaders = new Map();

        async function loadTableDataAsync(view, baseId, apiEndpoint, getHeaderFunc, getRowDataFunc, showLoadingFunc, hideLoadingFunc, onFinished) {
            if (activeLoaders.has(baseId)) {
                activeLoaders.get(baseId).abort();
                activeLoaders.delete(baseId);
            }

            const abortController = new AbortController();
            const { signal } = abortController;
            activeLoaders.set(baseId, abortController);

            var url = ApiClient.getUrl(apiEndpoint);

            var load_status = view.querySelector('#' + baseId + '_status');
            if (load_status !== undefined && load_status != null) {
                load_status.style.display = '';
                load_status.innerHTML = "Loading Data...";
            }

            showLoadingFunc();
            try {
                console.log("url: " + url);
                let resultData = await getStatistics2026Data(url, { signal }).catch(error => {
                    if (error.name === 'AbortError') {
                        console.log(`Load operation for ${baseId} was interrupted during network fetch.`);
                        return;
                    }

                    var errorMessage = "'" + error + "' - '" + url + "'";
                    console.error("loadTableDataAsync failed:", errorMessage, "url:", url);
                    return;
                });

                if (resultData === undefined || signal.aborted) {
                    if (!signal.aborted)
                        console.error("loadTableDataAsync failed: result data was undefined", "url:", url);
                    return;
                }

                var tbody = view.querySelector('#' + baseId + '_body');
                tbody.innerHTML = "";

                var thead = view.querySelector('#' + baseId + '_head');
                thead.innerHTML = getHeaderFunc();
                setupSortability(view, baseId, showLoadingFunc, hideLoadingFunc);

                let minChunkSize = 50;
                let chunkSize = Math.min(minChunkSize, Math.trunc(resultData.length / 20));
                if (chunkSize == 0)
                    chunkSize = minChunkSize;

                let currentIndex = 0;
                function renderNextChunk() {
                    if (signal.aborted) {
                        console.log(`Rendering loop for ${baseId} was interrupted.`);
                        return;
                    }

                    showLoadingFunc();
                    const endIndex = Math.min(currentIndex + chunkSize, resultData.length);
                    const fragment = document.createDocumentFragment();

                    if (load_status !== undefined && load_status != null) {
                        load_status.innerHTML = `Loading Data... ${currentIndex} of ${resultData.length}`;
                    }
                    for (var index = currentIndex; index < endIndex; ++index) {
                        var info = resultData[index];

                        var row_bg_col = "#BBBBBB00";
                        if (index % 2 == 0) {
                            row_bg_col = "#BBBBBB1C";
                        }

                        const tr = document.createElement('tr');
                        tr.style.backgroundColor = row_bg_col;
                        tr.innerHTML = getRowDataFunc(info, index, tr);
                        fragment.appendChild(tr);
                    }

                    injectImageProxy(fragment);
                    tbody.appendChild(fragment);
                    currentIndex = endIndex;

                    if (currentIndex < resultData.length) {
                        requestAnimationFrame(renderNextChunk);
                    }
                    else {
                        if (activeLoaders.get(baseId) === abortController) {
                            activeLoaders.delete(baseId);
                        }

                        getUserPluginConfig().then(function (config) {
                            var found = false;
                            config.TableSortEntries.forEach(entry => {
                                if (entry.TableId == baseId) {
                                    var headers = thead.querySelectorAll('th');
                                    headers.forEach((header, index) => {
                                        if (header.getAttribute('data-column') == entry.ColumnName) {
                                            var dataType = header.getAttribute('data-type');
                                            sortTable(index, dataType, entry.TableId, showLoadingFunc, hideLoadingFunc, entry.AscDesc);
                                            found = true;
                                        }
                                    });
                                }
                            });

                            if (found == false && onFinished !== undefined) {
                                onFinished();
                            }
                        });

                        if (load_status !== undefined && load_status != null) {
                            load_status.style.display = 'none';
                        }
                        hideLoadingFunc();
                    }
                }
                requestAnimationFrame(renderNextChunk);
            }
            catch (error) {
                if (error.name !== 'AbortError') {
                    console.error("Unexpected error in loadTableDataAsync workflow:", error);
                }
            }
            finally {
                hideLoadingFunc();
            }
        }

        function loadUsers(view, comboBoxId, loadDataFunc) {
            ApiClient.getUsers().then(function (users) {
                console.log(`users: ${users}`);
                var select = view.querySelector(comboBoxId);
                users.forEach((user) => {
                    var option = document.createElement(`option`);
                    option.value = user.Id;
                    option.innerHTML = user.Name;
                    select.appendChild(option);
                });
                if (users.length > 0) {
                    loadDataFunc(view, users[0].Id);
                }
            });
        }

        function setupSortability(view, baseId, showLoadingFunc, hideLoadingFunc) {
            const root = view || document;
            root.querySelectorAll(`#${baseId}_table thead th`).forEach((header) => {
                header.addEventListener('click', () => {
                    const columnType = header.getAttribute('data-type');
                    let idx = header.cellIndex;
                    sortTable(idx, columnType, baseId, showLoadingFunc, hideLoadingFunc);
                });
            });
        }

        /**
         * Simplified wrapper for showing a quick informational alert.
         * @param {string} title The dialog header text.
         * @param {string} message The alert body content.
         * @param {boolean} [isHtml=false] True to parse string as HTML, false to treat as plain text.
         * @returns {Promise<boolean>}
         */
        function showInfo(title, message, isHtml = false) {
            let htmlContent = message;

            // If not explicit HTML, safely escape text to prevent accidental rendering bugs or vulnerabilities
            if (!isHtml) {
                const div = document.createElement('div');
                div.textContent = message;
                htmlContent = div.innerHTML;
            }

            return showInfoEx({
                title: title,
                html: htmlContent,
                confirmText: 'Got It'
            });
        }

        /**
         * Advanced Emby dialog framework engine supporting auto-sizing HTML content.
         * @param {Object} options Configuration parameters.
         * @param {string} options.title The dialog header text.
         * @param {string} options.html The raw HTML string markup to inject.
         * @param {string} [options.confirmText='OK'] Label text for the submit button.
         * @param {string} [options.cancelText] Optional label text to enable a secondary cancel button.
         * @returns {Promise<boolean>} Resolves to true if confirmed, false if cancelled/closed.
         */
        function showInfoEx(options = {}) {
            return new Promise(function (resolve) {
                let isResolved = false;

                function finish(result) {
                    if (isResolved) {
                        return;
                    }
                    isResolved = true;
                    resolve(result);
                }

                let footerButtonsHtml = '';
                if (options.cancelText) {
                    footerButtonsHtml += `<button is="emby-button" type="button" class="button-cancel raised btnCancel" style="margin-right: 10px; min-width: 80px;">${options.cancelText}</button>`;
                }

                const okButtonText = options.confirmText || (options.cancelText ? 'OK' : 'Got It');
                footerButtonsHtml += `<button is="emby-button" type="button" class="button-submit raised btnConfirm" style="min-width: 80px;">${okButtonText}</button>`;

                const footerStyle = (options.cancelText && options.confirmText)
                    ? 'display: flex; justify-content: flex-end; margin-top: 2em;'
                    : 'display: flex; justify-content: center; margin-top: 2em; width: 100%;';

                const dialogTemplate = `
            <div class="dialog dialog-fixedSize dialog-alert" style="padding: 2.2em; width: max-content; min-width: 280px; max-width: 90vw; border-radius: 6px; box-sizing: border-box;">
                <h2 class="dialogTitle" style="margin-top: 0; font-weight: 500; text-align: center; width: 100%;"></h2>
                <div class="dialogContent" style="margin: 1.5em 0; line-height: 1.5; font-size: 1.05em; height: auto;"></div>
                <div class="dialogFooter" style="${footerStyle}">
                    ${footerButtonsHtml}
                </div>
            </div>
        `;

                const containerDiv = document.createElement('div');
                containerDiv.innerHTML = dialogTemplate.trim();
                const targetDialog = containerDiv.querySelector('.dialog');

                // Inject title and body separately so unclosed tags in options.html cannot break the footer
                targetDialog.querySelector('.dialogTitle').textContent = options.title || 'Notification';
                targetDialog.querySelector('.dialogContent').innerHTML = options.html || '';

                injectImageProxy(targetDialog);

                const confirmBtn = targetDialog.querySelector('.btnConfirm');
                if (confirmBtn) {
                    confirmBtn.addEventListener('click', function () {
                        finish(true);
                        dialogHelper.close(targetDialog);
                    });
                }

                if (options.cancelText) {
                    const cancelBtn = targetDialog.querySelector('.btnCancel');
                    if (cancelBtn) {
                        cancelBtn.addEventListener('click', function () {
                            finish(false);
                            dialogHelper.close(targetDialog);
                        });
                    }
               }
                targetDialog.addEventListener('close', function () {
                    finish(false);
                    targetDialog.remove();
                });

                document.body.appendChild(targetDialog);
                dialogHelper.open(targetDialog);
            });
        }

        const sortDirections = new Map();
        async function sortTable(columnIndex, dataType, baseId, showLoadingFunc, hideLoadingFunc, forcedDir) {
            showLoadingFunc();

            var load_status = document.querySelector('#' + baseId + '_status');
            if (load_status !== undefined && load_status != null) {
                load_status.innerHTML = "Sorting Data...";
                load_status.style.display = '';
            }

            await new Promise(r => requestAnimationFrame(() => setTimeout(r, 0)));

            var tableId = baseId + '_table';
            const table = document.getElementById(tableId);
            const tbody = table.querySelector("tbody");
            const rows = Array.from(tbody.querySelectorAll("tr"));

            let currentMap = sortDirections.get(tableId);
            if (currentMap === undefined) {
                sortDirections.set(tableId, new Map());
                currentMap = sortDirections.get(tableId);
            }

            let currentDirection = currentMap.get(columnIndex);
            if (forcedDir === undefined) {
                currentDirection = currentMap.get(columnIndex);
                if (currentDirection === undefined) {
                    currentDirection = 'asc';
                } else {
                    currentDirection = currentDirection === 'asc' ? 'desc' : 'asc';
                }
            } else {
                currentDirection = forcedDir;
            }

            sortDirections.get(tableId).set(columnIndex, currentDirection);

            // Reset indicator classes on all headers
            table.querySelectorAll("th").forEach(th => th.classList.remove("asc", "desc"));
            // Add current sorting indicator class to the active header
            table.querySelectorAll("th")[columnIndex].classList.add(currentDirection);

            getUserPluginConfig().then(function (config) {
                let columnName = table.querySelectorAll("th")[columnIndex].getAttribute('data-column');

                var found = false;
                config.TableSortEntries.forEach(entry => {
                    if (entry.TableId == baseId) {
                        entry.ColumnName = columnName;
                        entry.AscDesc = currentDirection;
                        found = true;
                    }
                });

                if (!found) {
                    config.TableSortEntries.push({
                        TableId: baseId,
                        ColumnName: columnName,
                        AscDesc: currentDirection
                    });
                }
                updateUserPluginConfiguration(config);
            });

            setTimeout(() => {
                rows.sort((rowA, rowB) => {
                    let cellA = rowA.children[columnIndex].getAttribute('sort-value');
                    if (cellA == null || cellA == '')
                        cellA = rowA.children[columnIndex].textContent;
                    cellA = cellA.trim();

                    let cellB = rowB.children[columnIndex].getAttribute('sort-value');
                    if (cellB == null || cellB == '')
                        cellB = rowB.children[columnIndex].textContent;
                    cellB = cellB.trim();

                    if ((dataType != 'checkbox') && cellA == '' && cellB != '') {
                        return currentDirection === 'asc' ? -1 : 1;
                    }
                    if ((dataType != 'checkbox') && cellB == '') {
                        return currentDirection === 'asc' ? 1 : -1;
                    }

                    let diff = 0;
                    if (dataType === 'number') {
                        // Strip out currency symbols or non-numeric formatting characters if present
                        const numA = parseFloat(cellA.replace(/[^0-9.-]+/g, ""));
                        const numB = parseFloat(cellB.replace(/[^0-9.-]+/g, ""));
                        diff = currentDirection === 'asc' ? numA - numB : numB - numA;
                    } else if (dataType == 'date') {
                        const dateA = new Date(cellA);
                        const dateB = new Date(cellB);
                        diff = currentDirection === 'asc' ? dateA - dateB : dateB - dateA;
                    } else if (dataType == 'progress') {
                        const matchA = cellA.match(/(?<percent>\d+)\%/);
                        const matchB = cellB.match(/(?<percent>\d+)\%/);

                        const numA = matchA ? +matchA.groups.percent : 0;
                        const numB = matchB ? +matchB.groups.percent : 0;

                        diff = currentDirection === 'asc' ? numA - numB : numB - numA;
                    } else if (dataType == 'checkbox') {
                        const inputA = rowA.children[columnIndex].querySelector('input');
                        const inputB = rowB.children[columnIndex].querySelector('input');

                        const valA = inputA ? (inputA.checked ? 1 : 0) : 0;
                        const valB = inputB ? (inputB.checked ? 1 : 0) : 0;

                        diff = currentDirection === 'asc' ? valA - valB : valB - valA;
                    } else { //if (dataType == 'string') {
                        diff = currentDirection === 'asc'
                            ? cellA.localeCompare(cellB)
                            : cellB.localeCompare(cellA);
                    }
                    return diff;
                });

                tbody.innerHTML = "";
                rows.forEach(row => tbody.appendChild(row));
                if (load_status !== undefined && load_status != null) {
                    load_status.style.display = 'none';
                }
                hideLoadingFunc();
            }, 10);
        }

        function initCollapsibleTable(containerId) {
            const container = document.getElementById(containerId);
            if (!container)
                return;
            if (container.getAttribute("data-is-clickable") !== "true")
                return;

            if (container.dataset.listenerAttached === "true")
                return;

            container.dataset.listenerAttached = true;

            const thead = container.querySelector(`table thead`);
            if (thead) {
                const width = thead.getBoundingClientRect().width;
                container.style.minWidth = `${width}px`;
                thead.style.display = 'none';
            }

            container.addEventListener("click", (event) => {
                const target = event.target;

                // 1. Prevent expanding if clicking an optional metric box
                const clickableMetric = target.closest(".is-clickable");
                if (clickableMetric) {
                    event.stopPropagation();
                    console.log("Emby Metric Clicked:", clickableMetric.textContent.trim());
                    return;
                }

                // 2. Handle structural collapsible summary row click
                const mainRow = target.closest(".clickable-row");
                if (!mainRow)
                    return;

                // Secure Navigation: Search down the dynamic virtual DOM node
                let detailRow = mainRow.nextElementSibling;
                while (detailRow && !detailRow.classList.contains("detail-row")) {
                    detailRow = detailRow.nextElementSibling;
                }

                // Toggle state classes to match Section 5 CSS styles
                if (detailRow) {
                    mainRow.classList.toggle("open");
                    detailRow.classList.toggle("show");
                }
            });
        }

        return {
            pluginId,
            CheckForValidConfig,
            LoadTVProgress,
            LoadUserStats,
            getMediaHeader,
            getMediaRowData,
            getSummaryInfo,
            insertGetLastRunInfo,
            insertSystemStatus,
            getTabIndex,
            initCollapsibleTable,
            injectStyleSheet,
            loadTableDataAsync,
            loadUsers,
            showInfo,
            showInfoEx,
            getUserPluginConfig,
            updateUserPluginConfiguration,
            isDebugLoggingEnabled
        };
    }
)
