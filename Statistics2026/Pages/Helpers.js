define([
    'mainTabsManager',
    'dialogHelper',
    'toast',
    'prompt',
    ApiClient.getUrl('web/configurationpage?name=Statistics2026_tabulator_tabulator.js'),
    'appRouter',
    'emby-button',
    'emby-tabs'
],
    function (mainTabsManager, dialogHelper, toast, prompt, Tabulator, appRouter) {
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

        async function showDiagnosticInfo(pluginConfig) {
            if (!pluginConfig.showDiagnosticInfo || !pluginConfig.showDebugInfo) {
                return false;
            }

            return await isDebugLoggingEnabled();
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

        function getTVProgressTabulatorColumns() {
            return [
                {
                    title: "TV Series (Premiere Year)",
                    field: "Name",
                    sorter: "string",
                    hozAlign: "left",
                    headerFilter: "input",
                    headerFilterPlaceholder: "Filter series...",
                    tooltip: function (e, cell) {
                        const info = cell.getRow().getData();
                        return `${info.Name} (${info.PremiereYear})`;
                    },
                    formatter: function (cell) {
                        const item = cell.getRow().getData();
                        const yearString = (item.PremiereYear && item.PremiereYear > 0)
                            ? `(${item.PremiereYear})`
                            : "";

                        const wrapper = document.createElement("span");
                        wrapper.style.fontWeight = "500";
                        wrapper.innerHTML = `${item.Name} <span style="opacity: 0.6; font-size: 0.9em; margin-left: 4px;">${yearString}</span>`;
                        return wrapper;
                    }
                },
                {
                    title: 'Progress <span class="infoBlock" id="episodesInfo" style="margin-left: 6px; cursor: pointer;"><i class="md-icon">info</i></span>',
                    field: "EpisodeProgess",
                    sorter: "number",
                    hozAlign: "left",
                    tooltip: function (e, cell) {
                        const info = cell.getRow().getData();
                        return info.Episodes ? info.Episodes.String : "";
                    },
                    formatter: function (cell) {
                        const info = cell.getRow().getData();
                        const el = cell.getElement();

                        if (info.Episodes && typeof info.Episodes.Percent === "number") {
                            const progressClass = calculateProgressClass(info.Episodes.Percent);
                            if (progressClass) {
                                el.classList.add(progressClass);
                            }
                        }

                        return info.Episodes ? info.Episodes.String : "";
                    }
                },
                {
                    title: "Score",
                    field: "Score",
                    sorter: "number",
                    hozAlign: "left",
                    tooltip: function (e, cell) {
                        const info = cell.getRow().getData();
                        return `${info.ScoreStr}/10`;
                    },
                    formatter: function (cell) {
                        const info = cell.getRow().getData();
                        return `${info.ScoreStr}/10`;
                    }
                },
                {
                    title: "Series Status",
                    field: "Status",
                    sorter: "string",
                    hozAlign: "left",
                    headerFilter: "list",
                    headerFilterParams: {
                        values: {
                            "": "All",
                            "Continuing": "Continuing",
                            "Ended": "Ended"
                        }
                    },
                    tooltip: function (e, cell) {
                        return cell.getRow().getData().SeriesStatus || "";
                    },
                    formatter: function (cell) {
                        return cell.getRow().getData().SeriesStatus || "";
                    }
                }
            ];
        }

        let tvProgressTable = null;
        function LoadTVProgress(view, userName) {
            view.querySelector("#UserTitle").innerHTML = "TV Series Progress for: " + userName;

            const baseId = "TVSeriesProgress";
            const endpoint = "Statistics2026/tv_series_progress/" + encodeURIComponent(userName);

            const customOptions = {
                initialSort: [
                    { column: "EpisodeProgess", dir: "desc" }
                ]
            };

            if (tvProgressTable) {
                tvProgressTable.destroy();
                tvProgressTable = null;
            }

            tvProgressTable = loadTabulatorTable(
                view,
                baseId,
                endpoint,
                getTVProgressTabulatorColumns(),
                null,
                customOptions
            );
        }

        function LoadUserSummaryStats(view, userName, showLoadingFunc, hideLoadingFunc) {
            showLoadingFunc();

            view.querySelector("#UserTitle").innerHTML = "User Statistics for: " + userName;
            view.querySelector("#generalStats").innerHTML = "";

            var pendingPromises = [];

            var generalStats = "";
            generalStats += getSummaryInfo(view, "total_time_watched", { user: userName, parameters: "?episodes=all", promises: pendingPromises });
            generalStats += getSummaryInfo(view, "total_watchable_time", { user: userName, parameters: "?episodes=all", promises: pendingPromises });
            view.querySelector("#generalStats").innerHTML = generalStats;

            Promise.allSettled(pendingPromises).finally(() => {
                hideLoadingFunc();
            });
        }

        function LoadUserMovieSummaryStats(view, userName, showLoadingFunc, hideLoadingFunc) {
            showLoadingFunc();

            view.querySelector("#UserTitle").innerHTML = "Movie Summary Statistics for: " + userName;
            view.querySelector("#movieStats").innerHTML = "";

            var pendingPromises = [];

            var movieStats = "";
            movieStats += getSummaryInfo(view, "total_movie_count", { user: userName, promises: pendingPromises });
            movieStats += getSummaryInfo(view, "total_movies_watched", { user: userName, promises: pendingPromises });
            movieStats += getSummaryInfo(view, "movie_favorite_years", { user: userName, promises: pendingPromises });
            movieStats += getSummaryInfo(view, "movie_favorite_genres", { user: userName, promises: pendingPromises });
            movieStats += getSummaryInfo(view, "total_time_watched", { user: userName, parameters: "?episodes=false", promises: pendingPromises }, "total_movie_time_watched");
            movieStats += getSummaryInfo(view, "total_watchable_time", { user: userName, parameters: "?episodes=false", promises: pendingPromises }, "total_movie_watchable_time");
            view.querySelector("#movieStats").innerHTML = movieStats;

            var movieMostWatchedStats = "";
            movieMostWatchedStats += getSummaryInfo(view, "last_seen", { user: userName, parameters: "?episodes=false", promises: pendingPromises }, "last_seen_movies");
            movieMostWatchedStats += getSummaryInfo(view, "most_watched_movies", { user: userName, promises: pendingPromises });
            view.querySelector("#movieMostWatchedStats").innerHTML = movieMostWatchedStats;

            Promise.allSettled(pendingPromises).finally(() => {
                hideLoadingFunc();
            });
        }

        function LoadUserTVSummaryStats(view, userName, showLoadingFunc, hideLoadingFunc) {
            showLoadingFunc();

            view.querySelector("#UserTitle").innerHTML = "TV Summary Statistics for: " + userName;
            view.querySelector("#showStats").innerHTML = "";

            var pendingPromises = [];

            var showStats = "";
            showStats += getSummaryInfo(view, "total_tv_count", { user: userName, promises: pendingPromises });
            showStats += getSummaryInfo(view, "total_tv_watched", { user: userName, promises: pendingPromises });
            showStats += getSummaryInfo(view, "total_series_finished", { user: userName, promises: pendingPromises });
            showStats += getSummaryInfo(view, "tv_favorite_genres", { user: userName, promises: pendingPromises });
            showStats += getSummaryInfo(view, "total_time_watched", { user: userName, parameters: "?episodes=true", promises: pendingPromises }, "total_episode_time_watched");
            showStats += getSummaryInfo(view, "total_watchable_time", { user: userName, parameters: "?episodes=true", promises: pendingPromises }, "total_episode_watchable_time");
            view.querySelector("#showStats").innerHTML = showStats;

            var seriesMostWatchedStats = "";
            seriesMostWatchedStats += getSummaryInfo(view, "last_seen", { user: userName, parameters: "?episodes=true", promises: pendingPromises }, "last_seen_tv");
            seriesMostWatchedStats += getSummaryInfo(view, "most_watched_shows", { user: userName, promises: pendingPromises });
            view.querySelector("#seriesMostWatchedStats").innerHTML = seriesMostWatchedStats;

            Promise.allSettled(pendingPromises).finally(() => {
                hideLoadingFunc();
            });
        }

        const lazyImageObserver = new IntersectionObserver(function (entries, observer) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    let dataSrc = img.getAttribute("data-src");
                    if (dataSrc) {
                        if (!/^(https?:|data:)/i.test(dataSrc)) {
                            dataSrc = dataSrc.startsWith("/") ? dataSrc : ApiClient.getUrl(dataSrc);
                            const token = ApiClient.accessToken();
                            if (token && !/[?&]api_key=/i.test(dataSrc)) {
                                dataSrc += (dataSrc.includes("?") ? "&" : "?") + "api_key=" + encodeURIComponent(token);
                            }
                        }

                        img.src = dataSrc;
                        img.removeAttribute("data-src");
                        img.classList.remove("lazy");
                    }
                    observer.unobserve(img);
                }
            });
        }, {
            rootMargin: "100px 0px"
        });

        function formatHtmlWithImages(cell, html) {
            if (!html) {
                return "";
            }

            const wrapper = document.createElement("div");
            wrapper.innerHTML = html;

            const images = wrapper.querySelectorAll("img");
            images.forEach(function (img) {
                img.addEventListener("load", function () {
                    const row = cell.getRow();
                    if (row) {
                        row.normalizeHeight();
                    }

                    const col = cell.getColumn();
                    if (col) {
                        col.setWidth(true);
                    }
                }, { once: true });

                if (img.hasAttribute("data-src")) {
                    lazyImageObserver.observe(img);
                }
            });

            return wrapper;
        }

        function createTextTooltip(dataProperty) {
            return function (e, cell) {
                const data = cell.getRow().getData();
                const prop = data[dataProperty];
                if (prop && prop.SortBy) {
                    return prop.SortBy;
                }
                return cell.getElement().innerText.trim();
            };
        }

        function getMediaTabulatorColumns(parentName, showOnServer, showMissing, showStreaming) {
            const columns = [
                {
                    title: "Name",
                    field: "Name",
                    sorter: "string",
                    headerFilter: "input",
                    headerFilterPlaceholder: "Filter name...",
                    hozAlign: "left",
                    tooltip: createTextTooltip("DisplayName"),
                    formatter: function (cell) {
                        const data = cell.getRow().getData();
                        return formatHtmlWithImages(cell, data.DisplayName ? data.DisplayName.Text : "");
                    }
                },
                {
                    title: parentName,
                    field: "ParentName",
                    sorter: "string",
                    headerFilter: "input",
                    headerFilterPlaceholder: `Filter ${parentName}...`,
                    hozAlign: "left",
                    tooltip: createTextTooltip("ParentName"),
                    formatter: function (cell) {
                        const data = cell.getRow().getData();
                        return formatHtmlWithImages(cell, data.ParentName ? data.ParentName.Text : "");
                    }
                },
                {
                    title: "Premiere Date",
                    field: "PremiereDate",
                    sorter: "date",
                    hozAlign: "left",
                    tooltip: createTextTooltip("PremiereDate"),
                    formatter: function (cell) {
                        const data = cell.getRow().getData();
                        const el = cell.getElement();

                        if (showMissing && data.SearchLocation && data.SearchLocation.Text !== "" && data.PremiereDate) {
                            const statusClass = calculateMediaStatusClass(new Date(data.PremiereDate));
                            if (statusClass) {
                                el.classList.add(statusClass);
                            }
                        }

                        return data.PremiereDate || "";
                    }
                }
            ];

            if (showOnServer) {
                columns.push(
                    {
                        title: "Resolution",
                        field: "Resolution",
                        sorter: "string",
                        headerFilter: "input",
                        headerFilterPlaceholder: `Filter resolution...`,
                        hozAlign: "left",
                        tooltip: createTextTooltip("ResolutionDetail"),
                        formatter: function (cell) {
                            return cell.getRow().getData().ResolutionDetail || "";
                        }
                    },
                    {
                        title: "Codec",
                        field: "Codec",
                        sorter: "string",
                        headerFilter: "input",
                        headerFilterPlaceholder: `Filter codec...`,
                        hozAlign: "left",
                        tooltip: createTextTooltip("Codec"),
                        formatter: function (cell) {
                            return cell.getRow().getData().Codec || "";
                        }
                    },
                    {
                        title: "Dolby Vision Profile",
                        field: "DolbyVisionProfile",
                        sorter: "string",
                        headerFilter: "input",
                        headerFilterPlaceholder: `Filter Dolby Vision profile...`,
                        hozAlign: "left",
                        tooltip: createTextTooltip("DolbyVisionProfile"),
                        formatter: function (cell) {
                            return cell.getRow().getData().DolbyVisionProfile || "";
                        }
                    }
                );
            }

            if (showStreaming) {
                columns.push({
                    title: "Streaming Services(s)",
                    field: "StreamingServies",
                    sorter: "string",
                    hozAlign: "left",
                    variableHeight: true,
                    tooltip: createTextTooltip("StreamingProviders"),
                    formatter: function (cell) {
                        const data = cell.getRow().getData();
                        return formatHtmlWithImages(cell, data.StreamingProviders ? data.StreamingProviders.Text : "");
                    }
                });
            }

            if (showMissing) {
                columns.push({
                    title: "Search Locations",
                    field: "Search",
                    sorter: "string",
                    hozAlign: "left",
                    tooltip: createTextTooltip("SearchLocation"),
                    formatter: function (cell) {
                        const data = cell.getRow().getData();
                        return data.SearchLocation ? data.SearchLocation.Text : "";
                    }
                });
            }

            if (showOnServer) {
                columns.push({
                    title: "Location on Server",
                    field: "ServerLocation",
                    sorter: "string",
                    headerFilter: "input",
                    headerFilterPlaceholder: `Filter location...`,
                    hozAlign: "left",
                    tooltip: createTextTooltip("ServerLocation"),
                    formatter: function (cell) {
                        const data = cell.getRow().getData();
                        return data.ServerLocation ? data.ServerLocation.Text : "";
                    }
                });
            }

            return columns;
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

        function getSummaryInfo(view, whichSummary, { user = "", parameters = "", div = "", promises = null } = {}) {
            var urlText = "/emby/Statistics2026/" + whichSummary;
            if (user != "" && user !== undefined) {
                urlText += "/" + user;
            }
            urlText += parameters;
            console.info("getSummaryInfo - '" + urlText + "'");
            var url = ApiClient.getUrl(urlText);

            if (div == "") {
                div = whichSummary;
            }

            var requestPromise = ApiClient.getJSON(url).then(response => {
                view.querySelector("#" + div).innerHTML = response.html;

                response.dynamicButtons.forEach((btn) => {
                    view.querySelector("#" + btn.id).addEventListener("click",
                        function () {
                            showInfo(btn.title, btn.info, true);
                        });
                });

                injectImageProxy(div, view);
            }).catch(error => {
                var errorMessage = "'" + error + "' - '" + div + "' - '" + urlText + "'";
                console.error("getSummaryInfo failed:", errorMessage);
            });

            if (Array.isArray(promises)) {
                promises.push(requestPromise);
            }

            return `<div name="${div}" id="${div}"></div>`;
        }

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

        let adminPagesSelectedUserId = sessionStorage.getItem("Statistics2026_AdminPages_SelectedUserId");
        function setAdminPagesSelectedUserId(userId) {
            adminPagesSelectedUserId = userId;
            if (userId) {
                sessionStorage.setItem('Statistics2026_AdminPages_SelectedUserId', userId);
            }
        }

        function getAdminPagesSelectedUserId() {
            return adminPagesSelectedUserId;
        }

        function loadUsers(view, comboBoxId, loadDataFunc) {
            ApiClient.getUsers().then(function (users) {
                console.log(`users: ${users}`);
                var select = view.querySelector(comboBoxId);
                if (!select)
                    return;

                users.forEach((user) => {
                    var option = document.createElement(`option`);
                    option.value = user.Id;
                    option.innerHTML = user.Name;
                    select.appendChild(option);
                });

                if (adminPagesSelectedUserId && select.querySelector(`option[value="${adminPagesSelectedUserId}"]`)) {
                    select.value = adminPagesSelectedUserId;
                } else if (users.length > 0) {
                    setAdminPagesSelectedUserId(users[0].Id);
                }

                if (typeof loadDataFunc === 'function' && select.value) {
                    loadDataFunc(view, select.value);
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

        function getRequestedConfigurationPageName() {
            // 1. Check hash query string first (standard Emby SPA route: #!/configurationpage?name=Statistics2026_Pages_Movies)
            const hash = window.location.hash || '';
            const hashQueryIndex = hash.indexOf('?');
            if (hashQueryIndex !== -1) {
                const hashParams = new URLSearchParams(hash.substring(hashQueryIndex));
                const hashName = hashParams.get('name');
                if (hashName) {
                    return hashName;
                }
            }

            // 2. Fallback to standard window.location.search (?name=Statistics2026_Pages_Movies)
            const searchParams = new URLSearchParams(window.location.search || '');
            return searchParams.get('name') || '';
        }

        function alignSubTabsToHeader(view) {
            const root = (view && typeof view.querySelector === 'function') ? view : document;
            const subTabsWrapper = root.querySelector('#dynamicSubTabs');
            const contentPrimary = root.querySelector('.content-primary');

            // Find the currently visible active tab inside .skinHeader
            const allActiveTopTabs = document.querySelectorAll('.skinHeader .emby-tab-button-active');
            let activeTopTab = null;
            for (let ii = 0; ii < allActiveTopTabs.length; ii++) {
                if (allActiveTopTabs[ii].offsetParent !== null) {
                    activeTopTab = allActiveTopTabs[ii];
                    break;
                }
            }

            const headerSlider = activeTopTab ? activeTopTab.closest('.emby-tabs-slider') : null;

            if (!subTabsWrapper || !contentPrimary || !activeTopTab || !subTabsWrapper.isConnected) {
                return;
            }

            // Reset margin first to measure natural width cleanly
            subTabsWrapper.style.setProperty('margin-left', '0px', 'important');

            const topTabRect = activeTopTab.getBoundingClientRect();
            const containerRect = contentPrimary.getBoundingClientRect();
            const subBarWidth = subTabsWrapper.getBoundingClientRect().width;

            // Ignore measurements if the view is hidden (display: none)
            if (containerRect.width === 0 || topTabRect.width === 0) {
                return;
            }

            const headerRight = headerSlider
                ? Math.min(headerSlider.getBoundingClientRect().right, containerRect.right)
                : containerRect.right;

            // Default: align left edge of sub-tabs with left edge of active main tab
            let offsetLeft = topTabRect.left - containerRect.left;

            // If left-aligning overflows past the right edge, align right edge with active main tab's right edge
            if (topTabRect.left + subBarWidth > headerRight) {
                offsetLeft = topTabRect.right - subBarWidth - containerRect.left;
            }

            // Clamp within .content-primary bounds so it never clips off either edge
            const maxOffset = Math.max(0, containerRect.width - subBarWidth);
            offsetLeft = Math.max(0, Math.min(offsetLeft, maxOffset));

            subTabsWrapper.style.setProperty('margin-left', `${Math.round(offsetLeft)}px`, 'important');
            subTabsWrapper.style.setProperty('margin-right', 'auto', 'important');
        }

        function getTabsForHierarchy(hierarchy) {
            return hierarchy.map(function (group) {
                return {
                    href: group.href,
                    name: group.name
                };
            });
        }

        function initTabSystem(view, hierarchyFn, getTabUrlFn) {
            const hierarchy = hierarchyFn();
            let activeGroupIndex = 0;
            let activeGroup = hierarchy[0];

            const currentPageName = getRequestedConfigurationPageName();

            for (let ii = 0; ii < hierarchy.length; ii++) {
                const subTabs = hierarchy[ii].subTabs || [];
                for (let jj = 0; jj < subTabs.length; jj++) {
                    if (subTabs[jj].page === currentPageName) {
                        activeGroupIndex = ii;
                        activeGroup = hierarchy[ii];
                        break;
                    }
                }
            }

            function getTabs() {
                return getTabsForHierarchy(hierarchy);
            }

            // 1. Set Row 1 (Emby Header Tabs)
            mainTabsManager.setTabs(view, activeGroupIndex, getTabs);

            // 2. Build Row 2 inside .content-primary
            const contentPrimary = view.querySelector('.content-primary');
            if (!contentPrimary) {
                return;
            }

            let subTabsWrapper = view.querySelector('#dynamicSubTabs');
            if (subTabsWrapper) {
                subTabsWrapper.remove();
            }

            const subTabs = (activeGroup && activeGroup.subTabs) ? activeGroup.subTabs : [];
            if (subTabs.length <= 1) {
                return;
            }

            // Read live colors from the currently visible top header tab bar
            const allHeaderSliders = document.querySelectorAll('.skinHeader .emby-tabs-slider');
            let headerSlider = null;
            for (let ii = 0; ii < allHeaderSliders.length; ii++) {
                if (allHeaderSliders[ii].offsetParent !== null) {
                    headerSlider = allHeaderSliders[ii];
                    break;
                }
            }

            const headerActiveBtn = headerSlider
                ? headerSlider.querySelector('.emby-tab-button-active')
                : document.querySelector('.skinHeader .emby-tab-button-active');
            const headerInactiveBtn = headerSlider
                ? headerSlider.querySelector('.emby-tab-button:not(.emby-tab-button-active)')
                : document.querySelector('.skinHeader .emby-tab-button:not(.emby-tab-button-active)');

            const trackBg = headerSlider ? window.getComputedStyle(headerSlider).backgroundColor : '';
            const activeBg = headerActiveBtn ? window.getComputedStyle(headerActiveBtn).backgroundColor : '';
            const activeColor = headerActiveBtn ? window.getComputedStyle(headerActiveBtn).color : '';
            const inactiveColor = headerInactiveBtn ? window.getComputedStyle(headerInactiveBtn).color : '';

            subTabsWrapper = document.createElement('div');
            subTabsWrapper.id = 'dynamicSubTabs';
            subTabsWrapper.className = 'subTabsNativeBar';

            if (trackBg && trackBg !== 'rgba(0, 0, 0, 0)' && trackBg !== 'transparent') {
                subTabsWrapper.style.setProperty('background-color', trackBg, 'important');
            }

            const slider = document.createElement('div');
            slider.className = 'emby-tabs-slider';

            for (let ii = 0; ii < subTabs.length; ii++) {
                const subTab = subTabs[ii];
                const isCurrent = subTab.page === currentPageName;

                const btn = document.createElement('button');
                btn.type = 'button';
                btn.setAttribute('is', 'emby-button');
                btn.className = 'emby-tab-button';

                if (isCurrent) {
                    btn.classList.add('emby-tab-button-active');
                    if (activeBg && activeBg !== 'rgba(0, 0, 0, 0)') {
                        btn.style.setProperty('background-color', activeBg, 'important');
                    }
                    if (activeColor) {
                        btn.style.setProperty('color', activeColor, 'important');
                    }
                } else if (inactiveColor) {
                    btn.style.setProperty('color', inactiveColor, 'important');
                }

                const foreground = document.createElement('div');
                foreground.className = 'emby-button-foreground';
                foreground.textContent = subTab.name;
                btn.appendChild(foreground);

                btn.addEventListener('click', function () {
                    if (!isCurrent) {
                        console.log(`Page requested: ${ii} - '${subTab.page}' - '${subTab.name}'`);
                        appRouter.show(getTabUrlFn(subTab.page));
                    }
                });

                slider.appendChild(btn);
            }

            subTabsWrapper.appendChild(slider);
            contentPrimary.insertBefore(subTabsWrapper, contentPrimary.firstChild);

            // Clean up any previous observers/listeners on this view
            if (view._subTabResizeObserver) {
                view._subTabResizeObserver.disconnect();
            }
            if (view._subTabResizeHandler) {
                window.removeEventListener('resize', view._subTabResizeHandler);
            }

            const scheduleAlign = () => {
                requestAnimationFrame(() => {
                    requestAnimationFrame(() => alignSubTabsToHeader(view));
                });
            };

            if (typeof ResizeObserver !== 'undefined') {
                const observer = new ResizeObserver(scheduleAlign);
                if (headerSlider) {
                    observer.observe(headerSlider);
                }
                observer.observe(contentPrimary);
                view._subTabResizeObserver = observer;
            }

            view._subTabResizeHandler = scheduleAlign;
            window.addEventListener('resize', scheduleAlign);

            scheduleAlign();
        }

        (function injectStyleSheets() {
            const cssId = 'statistics2026-tabulator-stylesheet';
            if (!document.getElementById(cssId)) {
                const link = document.createElement('link');
                link.id = cssId;
                link.rel = 'stylesheet';
                link.type = 'text/css';
                link.href = ApiClient.getUrl('web/configurationpage', {
                    name: 'Statistics2026_tabulator_tabulator.css',
                    v: '2'
                });
                document.head.appendChild(link);
            }

            const STYLE_ID = 'statistics2026-stylesheet';
            if (!document.getElementById(STYLE_ID)) {
                const link = document.createElement('link');

                link.id = STYLE_ID;
                link.rel = 'stylesheet';
                link.type = 'text/css';
                link.href = ApiClient.getUrl('web/configurationpage', {
                    name: 'Statistics2026_Resources_style.css',
                    v: '2'
                });

                document.head.appendChild(link);
            }
        })();

        function attachTopScrollbar(container, table) {
            const tableHolder = container.querySelector('.tabulator-tableholder');
            const header = container.querySelector('.tabulator-header');
            if (!tableHolder || !header) {
                return;
            }

            const topScroll = document.createElement('div');
            topScroll.className = 'tabulator-top-scrollbar';

            const topScrollInner = document.createElement('div');
            topScrollInner.className = 'tabulator-top-scrollbar-inner';
            topScroll.appendChild(topScrollInner);

            // Insert directly above the column header bar
            header.parentNode.insertBefore(topScroll, header);

            let isSyncing = false;

            topScroll.addEventListener('scroll', function () {
                if (isSyncing) return;
                isSyncing = true;
                tableHolder.scrollLeft = topScroll.scrollLeft;
                isSyncing = false;
            });

            tableHolder.addEventListener('scroll', function () {
                if (isSyncing) return;
                isSyncing = true;
                topScroll.scrollLeft = tableHolder.scrollLeft;
                isSyncing = false;
            });

            function syncWidth() {
                const tableEl = tableHolder.querySelector('.tabulator-table');
                const contentWidth = tableEl ? tableEl.scrollWidth : tableHolder.scrollWidth;
                topScrollInner.style.width = contentWidth + 'px';
                topScroll.style.display = contentWidth > tableHolder.clientWidth ? 'block' : 'none';
            }

            table.on('renderComplete', syncWidth);
            table.on('columnResized', syncWidth);
            window.addEventListener('resize', syncWidth);
        }

        function loadTabulatorTable(view, baseId, endpoint, columns, getExtraParamsFn, customOptions) {
            const selector = `#${baseId}_table`;
            const container = view.querySelector(selector);
            if (!container) {
                console.error(`loadTabulatorTable: Could not find container '${selector}' in view`, view);
                return null;
            }

            const defaultOptions = {
                ajaxURL: endpoint,
                pagination: true,
                paginationMode: 'remote',
                paginationSize: 25,
                paginationSizeSelector: [25, 50, 100, 250, 500],
                sortMode: 'remote',
                filterMode: 'remote',
                headerFilterLiveFilterDelay: 350,
                layout: 'fitDataFill',
                placeholder: 'No records found',
                columns: columns,
                ajaxRequestFunc: function (url, config, reqParams) {
                    const queryParams = {
                        StartIndex: (reqParams.page - 1) * reqParams.size,
                        Limit: reqParams.size
                    };

                    if (reqParams.sort && reqParams.sort.length > 0) {
                        queryParams.SortBy = reqParams.sort[0].field;
                        queryParams.SortOrder = reqParams.sort[0].dir === 'desc' ? 'Descending' : 'Ascending';
                    }

                    if (reqParams.filter && reqParams.filter.length > 0) {
                        const filterMap = {};
                        reqParams.filter.forEach(function (f) {
                            if (f.value !== undefined && f.value !== null && String(f.value).trim() !== '') {
                                filterMap[f.field] = String(f.value).trim();
                            }
                        });

                        if (Object.keys(filterMap).length > 0) {
                            queryParams.Filters = JSON.stringify(filterMap);
                        }
                    }
                    if (getExtraParamsFn !== undefined && typeof getExtraParamsFn === 'function') {
                        Object.assign(queryParams, getExtraParamsFn(reqParams));
                    }

                    const apiUrl = ApiClient.getUrl(endpoint, queryParams);

                    return ApiClient.getJSON(apiUrl).then(function (response) {
                        // Supports both Emby QueryResult { Items, TotalRecordCount } or raw array fallback
                        const items = Array.isArray(response) ? response : (response.Items || []);
                        const totalCount = Array.isArray(response) ? response.length : (response.TotalRecordCount || 0);

                        return {
                            last_page: Math.max(1, Math.ceil(totalCount / reqParams.size)),
                            data: items
                        };
                    });
                }
            };

            const table = new Tabulator(container, Object.assign({}, defaultOptions, customOptions || {}));
            table.on('tableBuilt', function () {
                attachTopScrollbar(container, table);
            });
            return table;
        }

        return {
            pluginId,
            CheckForValidConfig,
            LoadTVProgress,
            LoadUserMovieSummaryStats,
            LoadUserTVSummaryStats,
            LoadUserSummaryStats,
            getSummaryInfo,
            insertGetLastRunInfo,
            insertSystemStatus,
            initCollapsibleTable,
            loadUsers,
            setAdminPagesSelectedUserId,
            getAdminPagesSelectedUserId,
            showInfo,
            showInfoEx,
            getUserPluginConfig,
            updateUserPluginConfiguration,
            isDebugLoggingEnabled,
            showDiagnosticInfo,
            initTabSystem,
            loadTabulatorTable,
            getMediaTabulatorColumns,
            createTextTooltip,
            formatHtmlWithImages
        };
    }
)
