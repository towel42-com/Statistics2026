define(function () {
    const pluginId = "23ADB024-F759-438F-B9A7-D5912A75596C";

    async function CheckForValidConfig() {
        var urlText = "/emby/Statistics2026/database_status";
        var url = ApiClient.getUrl(urlText);

        let response = await ApiClient.getJSON(url).catch(error => {
            var errorMessage = "'" + error + "' - '" + urlText + "'";
            console.error("database_status failed:", errorMessage);
        });

        if (response.LastUpdated === undefined) {
            showInfo("No configuration found, please run the 'Statistics 2026' task on the Scheduled Tasks page and come back for the results.", "No Configuration Found");
            return false;
        }
        if (response.DBStateOK == false) {
            showInfo("The database has not been initialized, please run the 'Statistics 2026' task on the Scheduled Tasks page and come back for the results.", "No Configuration Found");
            return false;
        }
        return true;
    }

    if (!String.prototype.endsWith2) {
        String.prototype.endsWith2 = function (searchString, position) {
            var subjectString = this.toString();
            if (typeof position !== 'number' || !isFinite(position) || Math.floor(position) !== position || position > subjectString.length) {
                position = subjectString.length;
            }
            position -= searchString.length;
            var lastIndex = subjectString.indexOf(searchString, position);
            return lastIndex !== -1 && lastIndex === position;
        };
    }


    function calculateProgressClass(value) {
        if (value == 0)
            return ``;
        else if (value < 40)
            return `progress-20`;
        else if (value < 60)
            return `progress-40`;
        else if (value < 80)
            return `progress-60`;
        else if (value < 100)
            return `progress-80`;
        else
            return `progress-100`;
    };

    function getTVProgressRowData(info) {
        var retVal = "";
        retVal += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.Name + " (" + info.PremiereYear + ")</td>";
        retVal += "<td class='center " + calculateProgressClass(info.Episodes.Percent) + "' style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.Episodes.String + "</td>";
        retVal += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.ScoreStr + "/10</td>";
        retVal += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.SeriesStatus + "</td>";
        return retVal;
    }

    function getTVProgressHeader() {
        var retVal = "";
        retVal += "<tr style=\"text-align: left; white-space: nowrap;\">";
        retVal += "    <th data-column=\"Name\" data-type=\"string\" class=\"info_cell_heading\">TV Series (Premiere Year)<span class=\"sort-icon\"></span></th>";
        retVal += "    <th data-column=\"EpisodeProgess\" data-type=\"progress\" class=\"info_cell_heading\" style=\"white-space: nowrap;\">Progress<span class=\"sort-icon\"></span><span class=\"infoBlock\" id=\"episodesInfo\"><i class=\"md-icon\">info</i></span></th>";
        retVal += "    <th data-column=\"Score\" data-type=\"number\" class=\"info_cell_heading\">Score<span class=\"sort-icon\"></span></th>";
        retVal += "    <th data-column=\"Status\" data-type=\"string\" class=\"info_cell_heading\">Series Status<span class=\"sort-icon\"></span></th>";
        retVal += "</tr>";
        return retVal;
    }

    function LoadTVProgress(view, userName, showLoadingFunc, hideLoadingFunc) {
        view.querySelector("#UserTitle").innerHTML = "TV Series Progress for " + userName;

        var url = "Statistics2026/tv_series_progress/" + userName;

        const sortit = () => {
            sortTable(1, "progress", 'TVSeriesProgress', showLoadingFunc, hideLoadingFunc, 'desc');
        };

        loadTableData(view, 'TVSeriesProgress', url, getTVProgressHeader, getTVProgressRowData, showLoadingFunc, hideLoadingFunc, sortit);
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

            hideLoadingFunc();
        } catch {
            hideLoadingFunc();
        }
    }

    function getMediaRowData(info, showOnServer, showMissing) {
        var retVal = "";
        retVal += "<td style='align='left' sort-value='" + info.SortName + "'>" + info.ListDisplayName + "</td>";
        retVal += "<td style='align='right'>" + info.PremiereDate + "</td>";

        if (showOnServer) {
            retVal += "<td style='align='left'>" + info.ResolutionDetail + "</td>";
            retVal += "<td style='align='left'>" + info.Codec + "</td>";
            retVal += "<td style='align='left'>" + info.DolbyVisionProfile + "</td>";
            retVal += "<td style='align='left' sort-value='" + info.LocationSortName + "'>" + info.ServerLocation + "</td>";
        }

        if (showMissing) {
            retVal += "<td style='align='left' sort-value='" + info.SearchSortName + "'>" + info.SearchLocation + "</td>";
        }

        return retVal;
    }

    function getMediaHeader(showOnServer, showMissing) {
        var retVal = "";
        retVal += "<tr style=\"text-align: left;\">";
        retVal += "    <th data-column=\"Name\" data-type=\"string\" class=\"info_cell_heading\">Name<span class=\"sort-icon\"></span></th>";
        retVal += "    <th data-column=\"PremiereDate\" data-type=\"date\" class=\"info_cell_heading\">Premiere Date<span class=\"sort-icon\"></span></th>";

        if (showOnServer) {
            retVal += "    <th data-column=\"Resolution\" data-type=\"string\" class=\"info_cell_heading\">Resolution<span class=\"sort-icon\"></span></th>";
            retVal += "    <th data-column=\"Codec\" data-type=\"string\" class=\"info_cell_heading\">Codec<span class=\"sort-icon\"></span></th>";
            retVal += "    <th data-column=\"DolbyVisionProfile\" data-type=\"string\" class=\"info_cell_heading\">Dolby Vision Profile<span class=\"sort-icon\"></span></th>";
            retVal += "    <th data-column=\"ServerLocation\" data-type=\"string\" class=\"info_cell_heading\">Location on Server<span class=\"sort-icon\"></span></th>";
        }

        if (showMissing) {
            retVal += "    <th data-column=\"Search\" data-type=\"string\" class=\"info_cell_heading\">Click to Search<span class=\"sort-icon\"></span></th>";
        }

        retVal += "</tr>";
        return retVal;
    }

    function getMissingMediaRowData(info) {
        var retVal = "";
        retVal += "<td style='align='left' sort-value='" + info.SortName + "'>" + info.ListDisplayName + "</td>";
        retVal += "<td style='align='right'>" + info.PremiereDate + "</td>";
        retVal += "<td style='align='left' sort-value='" + info.LocationSortName + "'>" + info.ServerLocation + "</td>";
        return retVal;
    }

    getStatistics2026Data = function (url_to_get) {
        console.log("getStatistics2026Data Url = " + url_to_get);
        return ApiClient.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };

    async function getLastRunInfo(view, whichRuns, div) {
        const promises = whichRuns.map(whichRun => {

            var urlText = "/emby/Statistics2026/last_run/" + whichRun;
            console.info("last_run- '" + urlText + "'");
            var url = ApiClient.getUrl(urlText);

            return ApiClient.getJSON(url).then(response => {
                return response.html;
            }).catch(error => {
                var errorMessage = "'" + error + "' - '" + div + "' - '" + urlText + "'";
                console.error("getLastRunInfo failed:", errorMessage);
                return `<tr><td colspan="2">Error loading ${whichRun}</td></tr>`;
            });
        });

        try {
            const htmlChunks = await Promise.all(promises);
            let tableHtml = "<table><tbody>";
            for (const chunk of htmlChunks) {
                tableHtml += `<tr><td>${chunk}</td></tr>`;
            }
            tableHtml += "</tbody></table>";
            view.querySelector("#" + div).innerHTML = tableHtml;
        } catch (criticalError) {
            console.error("Critical error in batch execution:", criticalError);
        }
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
                        showInfo(v.info, v.title);
                    });
            });
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
            if (path.endsWith2("=" + tab_name)) {
                return index;
            }
        }
        return -1;
    }

    function sortableTableStyle() {
        var retVal = '.tooltip {position: relative;display: inline-block;border-bottom: 1px dotted black;} ' +
            '.tooltip .tooltiptext {visibility: hidden; background-color: black; color: #fff; border-radius: 6px; padding: 5px 0; position: absolute;z-index: 1;} ' +
            '.tooltip:hover .tooltiptext {visibility: visible;} ' +
            '.info_cell {white-space: nowrap; padding-left:45px; padding-right:20px; font-size:smaller;}' +
            '.info_cell_heading {white-space: nowrap; padding-left:20px; padding-right:20px;font-size:smaller;}' +
            '.sortable-table-styled - styled {' +
            '   width: 100 %;' +
            '   border-collapse: collapse;' +
            '} ' +
            '.sortable-table-styled th {' +
            '    cursor: pointer;' +
            '    background - color: #f2f2f2;' +
            '    padding: 10px;' +
            '    user - select: none;' +
            '}' +
            '.sortable-table-styled td {' +
            '   padding: 10px;' +
            '   border - bottom: 1px solid #ddd;' +
            '}' +
            '.sortable-table-styled th .sort-icon::after {' +
            '    content: " ↕";' +
            '    opacity: 0.4;' +
            '}' +
            '.sortable-table-styled th.asc .sort-icon::after {' +
            '    content: " ↑";' +
            '    opacity: 1;' +
            '}' +
            '.sortable-table-styled th.desc .sort-icon::after {' +
            '    content: " ↓";' +
            '    opacity: 1;' +
            '}';
        return retVal;
    }

    function injectSortableTableStyle(document) {
        var style = document.createElement('style');
        style.innerHTML = sortableTableStyle();
        var ref = document.querySelector('script');
        ref.parentNode.insertBefore(style, ref);
    }

    const STYLE_ID = 'statistics2026-stylesheet';
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

    // 'missing_episode_results_status', 'missing_episode_results_body', 'missing_episode_results_head',

    async function loadTableData(view, baseId, apiEndpoint, getHeaderFunc, getRowDataFunc, showLoadingFunc, hideLoadingFunc, onFinished) {
        var url = ApiClient.getUrl(apiEndpoint);

        var load_status = view.querySelector('#' + baseId + '_status');
        load_status.style.display = '';
        load_status.innerHTML = "Loading Data...";

        showLoadingFunc();
        try {
            console.log("url: " + url);
            let resultData = await getStatistics2026Data(url).catch(error => {
                var errorMessage = "'" + error + "' - '" + url + "'";
                console.error("loadTableData failed:", errorMessage, "url:", url);
                hideLoadingFunc();
                return;
            });

            if (resultData === undefined) {
                console.error("loadTableData failed: result data was undefined", "url:", url);
                hideLoadingFunc();
                return;
            }

            // console.log("resultData: " + JSON.stringify(resultData));

            var tbody = view.querySelector('#' + baseId + '_body');
            tbody.innerHTML = "";

            var thead = view.querySelector('#' + baseId + '_head');
            thead.innerHTML = getHeaderFunc();
            setupSortability(baseId, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg);

            let currentIndex = 0;
            let chunkSize = Math.min(50, Math.trunc(resultData.length / 20));
            function renderNextChunk() {
                showLoadingFunc();
                const endIndex = Math.min(currentIndex + chunkSize, resultData.length);
                const fragment = document.createDocumentFragment();

                load_status.innerHTML = `Loading Data... ${currentIndex} of ${resultData.length}`;
                for (var index = currentIndex; index < endIndex; ++index) {
                    var info = resultData[index];

                    var row_bg_col = "#BBBBBB00";
                    if (index % 2 == 0) {
                        row_bg_col = "#BBBBBB1C";
                    }

                    const tr = document.createElement('tr');
                    tr.style.backgroundColor = row_bg_col;
                    tr.innerHTML = getRowDataFunc(info);
                    fragment.appendChild(tr);
                }

                tbody.appendChild(fragment);
                currentIndex = endIndex;

                if (currentIndex < resultData.length) {
                    requestAnimationFrame(renderNextChunk);
                }
                else {
                    if (onFinished !== undefined) {
                        onFinished();
                    }
                    load_status.style.display = 'none';
                    hideLoadingFunc();
                }
            }
            requestAnimationFrame(renderNextChunk);
        }
        finally {
            hideLoadingFunc();
        }
    }

    function loadUsers(view, comboBoxId, loadDataFunc) {
        ApiClient.getUsers().then(function (users) {
            console.log(`users: {users}`);
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

    function setupSortability(baseId, showLoadingFunc, hideLoadingFunc) {
        document.querySelectorAll(`#${baseId}_table thead th`).forEach((header, index) => {
            header.addEventListener('click', () => {
                const columnType = header.getAttribute('data-type');
                let idx = header.cellIndex;
                sortTable(idx, columnType, baseId, showLoadingFunc, hideLoadingFunc);
            });
        });
    }

    function showInfo(_text, _title) {
        ApiClient.getJSON(ApiClient.getUrl('Sessions'))
            .then((sessions) => {
                // 2. Find the session that matches this browser's unique Device ID
                const myCurrentDevice = ApiClient.deviceId();
                const mySession = sessions.find(s => s.DeviceId === myCurrentDevice);

                if (mySession) {
                    const urlString = ApiClient.getUrl(`Sessions/${mySession.Id}/Message`);
                    const payload = {
                        Header: _title,
                        Text: _text
                    };
                    ApiClient.ajax(
                        {
                            type: 'POST',
                            url: urlString,
                            data: JSON.stringify(payload),
                            contentType: 'application/json'
                        })
                        .then(function (response) {
                            console.log("Message sent successfully!");
                        })
                        .catch(function (error) {
                            console.error("Failed to send message:", error);
                        });
                } else {
                }
            })
            .catch((err) => console.error("Error fetching sessions:", err));
    }


    const sortDirections = new Map();
    async function sortTable(columnIndex, dataType, baseId, showLoadingFunc, hideLoadingFunc, forcedDir) {
        showLoadingFunc();

        var load_status = document.querySelector('#' + baseId + '_status');
        load_status.innerHTML = "Sorting Data...";
        load_status.style.display = '';

        await new Promise(r => requestAnimationFrame(() => setTimeout(r, 0)));

        var tableId = baseId + '_table';
        const table = document.getElementById(tableId);
        const tbody = table.querySelector("tbody");
        // Convert HTMLCollection of rows into a real Array
        const rows = Array.from(tbody.querySelectorAll("tr"));

        // Toggle between Ascending ('asc') and Descending ('desc')

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

                if (dataType === 'number') {
                    // Strip out currency symbols or non-numeric formatting characters if present
                    const numA = parseFloat(cellA.replace(/[^0-9.-]+/g, ""));
                    const numB = parseFloat(cellB.replace(/[^0-9.-]+/g, ""));
                    return currentDirection === 'asc' ? numA - numB : numB - numA;
                } else if (dataType == 'date') {
                    const dateA = new Date(cellA);
                    const dateB = new Date(cellB);
                    return currentDirection === 'asc' ? dateA - dateB : dateB - dateA;
                } else if (dataType == 'string') {
                    // Text comparison using localeCompare for proper alphabetical ordering
                    return currentDirection === 'asc'
                        ? cellA.localeCompare(cellB)
                        : cellB.localeCompare(cellA);
                } else { // progress
                    const matchA = cellA.match(/(?<percent>\d+)\%/);
                    const matchB = cellB.match(/(?<percent>\d+)\%/);

                    const numA = matchA ? +matchA.groups.percent : 0;
                    const numB = matchB ? +matchB.groups.percent : 0;

                    return currentDirection === 'asc' ? numA - numB : numB - numA;
                }
            });

            tbody.innerHTML = "";
            rows.forEach(row => tbody.appendChild(row));
            load_status.style.display = 'none';
            hideLoadingFunc();
        }, 10);
    }

    function initCollapsibleTable(containerId) {
        // Scope our selector to the active injected Emby view context
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
        getMissingMediaRowData,
        getStatistics2026Data,
        getSummaryInfo,
        getLastRunInfo,
        getTabIndex,
        initCollapsibleTable,
        injectSortableTableStyle,
        injectStyleSheet,
        loadTableData,
        loadUsers,
        setupSortability,
        showInfo
    };

})

