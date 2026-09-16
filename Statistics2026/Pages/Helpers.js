define(function () {
    const pluginId = "23ADB024-F759-438F-B9A7-D5912A75596C";

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
        var row_html = "";
        row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.Name + " (" + info.PremiereYear + ")</td>";
        row_html += "<td class='center " + calculateProgressClass(info.Episodes.Percent) + "' style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.Episodes.String + "</td>";
        row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.ScoreStr + "/10</td>";
        row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.SeriesStatus + "</td>";
        return row_html;
    }

    function LoadTVProgress(view, userName, showLoadingFunc, hideLoadingFunc, Helpers) {
        ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            view.querySelector("#UserTitle").innerHTML = "TV Series Progress for " + userName;

            var url = "Statistics2026/tv_series_progress/" + userName;
            loadTableData(view, 'TVSeriesProgressStatus', 'TVSeriesProgressTable_results', url, getTVProgressRowData, showLoadingFunc, hideLoadingFunc, Helpers);
        });
    }

    function LoadUserStats(view, userName, showLoadingFunc, hideLoadingFunc, Helpers) {
        showLoadingFunc();

        try {
            ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                if (!CheckForValidConfig(config)) {
                    hideLoadingFunc();
                    return;
                }

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
            });
        } finally {
            hideLoadingFunc();
        }

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
    const sortDirections = new Map();

    function sortTable(columnIndex, dataType, tableId) {
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
        if (currentDirection === undefined) {
            currentDirection = 'asc';
        } else {
            currentDirection = currentDirection === 'asc' ? 'desc' : 'asc';
        }

        sortDirections.get(tableId).set(columnIndex, currentDirection);

        // Reset indicator classes on all headers
        table.querySelectorAll("th").forEach(th => th.classList.remove("asc", "desc"));
        // Add current sorting indicator class to the active header
        table.querySelectorAll("th")[columnIndex].classList.add(currentDirection);

        // Sort the row elements
        rows.sort((rowA, rowB) => {
            const cellA = rowA.children[columnIndex].textContent.trim();
            const cellB = rowB.children[columnIndex].textContent.trim();

            if (dataType === 'number') {
                // Strip out currency symbols or non-numeric formatting characters if present
                const numA = parseFloat(cellA.replace(/[^0-9.-]+/g, ""));
                const numB = parseFloat(cellB.replace(/[^0-9.-]+/g, ""));
                return currentDirection === 'asc' ? numA - numB : numB - numA;
            } else if (dataType == 'string') {
                // Text comparison using localeCompare for proper alphabetical ordering
                return currentDirection === 'asc'
                    ? cellA.localeCompare(cellB)
                    : cellB.localeCompare(cellA);
            } else { // progress
                var numA = +cellA.match(/(?<percent>\d+)\%/).groups.percent;
                var numB = +cellB.match(/(?<percent>\d+)\%/).groups.percent;

                return currentDirection === 'asc' ? numA - numB : numB - numA;
            }
        });

        // Re-append sorted rows to empty the body and place elements in new order
        tbody.innerHTML = "";
        rows.forEach(row => tbody.appendChild(row));
    }

    function getMediaRowData(info) {
        var row_html = "";

        row_html += "<td style='align='left'>" + info.ListDisplayName + "</td>";
        row_html += "<td style='align='right'>" + info.StartYear + "</td>";
        row_html += "<td style='align='left'>" + info.ResolutionDetail + "</td>";
        row_html += "<td style='align='left'>" + info.Codec + "</td>";
        row_html += "<td style='align='left'>" + info.DolbyVisionProfile + "</td>";
        row_html += "<td style='align='left'>" + info.ServerLocation + "</td>";
        return row_html;
    }

    function CheckForValidConfig(config) {
        if (config.LastUpdated === undefined ) {
            showInfo("No configuration found, please run the 'Statistics 2026' task on the Scheduled Tasks page and come back for the results.", "No Configuration Found");
            return false;
        }
        if (config.dbStateOK == false) {
            showInfo("The database has not been initialized, please run the 'Statistics 2026' task on the Scheduled Tasks page and come back for the results.", "No Configuration Found");
            return false;
        }
        return true;
    }


    function loadTableData(view, statusElementId, resultsElementId, apiEndpoint, getRowDataFunc, showLoadingFunc, hideLoadingFunc, Helpers) {
        ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            if (!CheckForValidConfig(config)) {
                hideLoadingFunc();
                return;
            }
            var url = ApiClient.getUrl(apiEndpoint);

            var load_status = view.querySelector('#' + statusElementId);
            load_status.innerHTML = "Loading Data...";

            showLoadingFunc();
            try {
                getStatistics2026Data(url).then(function (resultData) {
                    load_status.innerHTML = "&nbsp;";
                    console.log("resultData: " + JSON.stringify(resultData));

                    var table_body = view.querySelector('#' + resultsElementId);
                    var row_html = "";

                    for (var index = 0; index < resultData.length; ++index) {
                        var info = resultData[index];

                        var row_bg_col = "#BBBBBB00";
                        if (index % 2 == 0) {
                            row_bg_col = "#BBBBBB1C";
                        }

                        row_html += "<tr style='background:" + row_bg_col + ";'>";

                        row_html += getRowDataFunc(info);

                        row_html += "</tr>";
                    }

                    table_body.innerHTML = row_html;
                    hideLoadingFunc();
                },
                    function (response) {
                        load_status.innerHTML = response.status + ":" + response.statusText;
                    });
            } finally {
                hideLoadingFunc();
            }
        });
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

    const STYLE_ID = 'my-plugin-stylesheet';
    function injectStyleSheet(e) {

        const cssUrl = Dashboard.getConfigurationPageUrl('style.css');
        return injectStyleSheetEX(e, cssUrl);
    }

    function injectStyleSheetEX(e, cssUrl) {

        if (document.getElementById(STYLE_ID))  // already added
            return;

        const link = document.createElement('link');
        link.id = STYLE_ID;
        link.rel = 'stylesheet';
        link.type = 'text/css';
        link.href = cssUrl;

        document.head.appendChild(link);
    }

    getStatistics2026Data = function (url_to_get) {
        console.log("getStatistics2026Data Url = " + url_to_get);
        return ApiClient.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };

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

    function showInfo(text, title) {
        Dashboard.alert({ message: text, title: title });
    }

    return {
        pluginId,
        LoadTVProgress,
        LoadUserStats,
        sortTable,
        sortableTableStyle,
        loadTableData,
        getMediaRowData,
        CheckForValidConfig,
        getTabIndex,
        injectStyleSheet,
        injectStyleSheetEX,
        getStatistics2026Data,
        getSummaryInfo,
        showInfo
    };

})

//# sourceURL=js