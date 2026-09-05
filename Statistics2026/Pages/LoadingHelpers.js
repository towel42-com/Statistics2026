define(function () {
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


    function LoadTVProgress(view, userName, showLoadingFunc, hideLoadingFunc, Helpers) {
        showLoadingFunc()
        ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
            if (!Helpers.CheckForValidConfig(config)) {
                hideLoadingFunc();
                return;
            }

            view.querySelector("#UserTitle").innerHTML = "TV Series Progress for " + userName;

            var url = "Statistics2026/tv_series_progress/" + userName;
            url = ApiClient.getUrl(url);
            console.log("tvSeriesProgressData: Url: " + url);

            var load_status = view.querySelector('#TVSeriesProgressStatus');
            load_status.innerHTML = "Loading Data...";

            Helpers.getStatistics2026Data(url).then(function (tvSeriesProgressData) {
                load_status.innerHTML = "&nbsp;";
                console.log("tvSeriesProgressData: " + JSON.stringify(tvSeriesProgressData));

                var table_body = view.querySelector('#TVSeriesProgressTable_results');
                var row_html = "";

                for (var index = 0; index < tvSeriesProgressData.length; ++index) {
                    var info = tvSeriesProgressData[index];

                    var row_bg_col = "#BBBBBB00";
                    if (index % 2 == 0) {
                        row_bg_col = "#BBBBBB1C";
                    }

                    row_html += "<tr style='background:" + row_bg_col + ";'>";

                    row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.Name + " (" + info.PremiereYear + ")</td>";
                    row_html += "<td class='center " + calculateProgressClass(info.Episodes.Percent) + "' style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.Episodes.String + "</td>";
                    row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.ScoreStr + "/10</td>";
                    row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.SeriesStatus + "</td>";

                    row_html += "</tr>";
                }

                table_body.innerHTML = row_html;
                hideLoadingFunc();
            });
        });
    }

    function LoadUserStats(view, userName, showLoadingFunc, hideLoadingFunc, Helpers) {
        showLoadingFunc();
        ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
            if (!Helpers.CheckForValidConfig(config)) {
                hideLoadingFunc();
                return;
            }

            view.querySelector("#UserTitle").innerHTML = "User statistics for " + userName;
            view.querySelector("#generalStats").innerHTML = "";
            view.querySelector("#movieStats").innerHTML = "";
            view.querySelector("#showStats").innerHTML = "";

            var generalStats = "";
            generalStats += Helpers.getSummaryInfo(view, "total_time_watched", userName, "?episodes=all");
            generalStats += Helpers.getSummaryInfo(view, "total_watchable_time", userName, "?episodes=all");
            view.querySelector("#generalStats").innerHTML = generalStats;

            var movieStats = "";
            movieStats += Helpers.getSummaryInfo(view, "total_movie_count", userName);
            movieStats += Helpers.getSummaryInfo(view, "total_movies_watched", userName);
            movieStats += Helpers.getSummaryInfo(view, "movie_favorite_years", userName);
            movieStats += Helpers.getSummaryInfo(view, "movie_favorite_genres", userName);
            movieStats += Helpers.getSummaryInfo(view, "total_time_watched", userName, "?episodes=false", "total_movie_time_watched");
            movieStats += Helpers.getSummaryInfo(view, "total_watchable_time", userName, "?episodes=false", "total_movie_watchable_time");
            view.querySelector("#movieStats").innerHTML = movieStats;

            var movieMostWatchedStats = "";
            movieMostWatchedStats += Helpers.getSummaryInfo(view, "last_seen", userName, "?episodes=false", "last_seen_movies");
            movieMostWatchedStats += Helpers.getSummaryInfo(view, "most_watched_movies", userName, "?serverId=" + config.ServerId + "&numMovies=" + config.numWatchedShows);
            view.querySelector("#movieMostWatchedStats").innerHTML = movieMostWatchedStats;

            var showStats = "";
            showStats += Helpers.getSummaryInfo(view, "total_tv_count", userName);
            showStats += Helpers.getSummaryInfo(view, "total_tv_watched", userName);
            showStats += Helpers.getSummaryInfo(view, "total_series_finished", userName);
            showStats += Helpers.getSummaryInfo(view, "tv_favorite_genres", userName);
            showStats += Helpers.getSummaryInfo(view, "total_time_watched", userName, "?episodes=true", "total_episode_time_watched");
            showStats += Helpers.getSummaryInfo(view, "total_watchable_time", userName, "?episodes=true", "total_episode_watchable_time");
            view.querySelector("#showStats").innerHTML = showStats;

            var seriesMostWatchedStats = "";
            seriesMostWatchedStats += Helpers.getSummaryInfo(view, "last_seen", userName, "?episodes=true", "last_seen_tv");
            seriesMostWatchedStats += Helpers.getSummaryInfo(view, "most_watched_shows", userName, "?serverId=" + config.ServerId + "&numShows=" + config.numWatchedShows);
            view.querySelector("#seriesMostWatchedStats").innerHTML = seriesMostWatchedStats;

            hideLoadingFunc();
        });
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
            } else {
                // Text comparison using localeCompare for proper alphabetical ordering
                return currentDirection === 'asc'
                    ? cellA.localeCompare(cellB)
                    : cellB.localeCompare(cellA);
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
        row_html += "<td style='align='right'>" + info.Count + "</td>";
        return row_html;
    }

    function loadTableData(view, statusElementId, resultsElementId, apiEndpoint, getRowDataFunc, Helpers) {
        var url = ApiClient.getUrl(apiEndpoint);

        var load_status = view.querySelector('#' + statusElementId);
        load_status.innerHTML = "Loading Data...";

        Helpers.getStatistics2026Data(url).then(function (resultData) {
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
        },
            function (response) {
                load_status.innerHTML = response.status + ":" + response.statusText;
            });
    }

    return {
        LoadTVProgress,
        LoadUserStats,
        sortTable,
        sortableTableStyle,
        loadTableData,
        getMediaRowData
    };

})

//# sourceURL=LoadingHelpers.js