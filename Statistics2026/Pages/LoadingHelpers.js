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

            Helpers.getStatistics2026URL(url).then(function (tvSeriesProgressData) {
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

            movieStats += Helpers.getSummaryInfo(view, "most_watched_movies", userName, "?serverId=" + config.ServerId + "&numMovies=" + config.numWatchedShows);

            movieStats += Helpers.getSummaryInfo(view, "total_time_watched", userName, "?episodes=false", "total_movie_time_watched");
            movieStats += Helpers.getSummaryInfo(view, "total_watchable_time", userName, "?episodes=false", "total_movie_watchable_time");
            movieStats += Helpers.getSummaryInfo(view, "last_seen", userName, "?episodes=false", "last_seen_movies");
            view.querySelector("#movieStats").innerHTML = movieStats;

            var showStats = "";
            showStats += Helpers.getSummaryInfo(view, "total_tv_count", userName);
            showStats += Helpers.getSummaryInfo(view, "total_tv_watched", userName);
            showStats += Helpers.getSummaryInfo(view, "total_series_finished", userName);
            showStats += Helpers.getSummaryInfo(view, "tv_favorite_genres", userName);
            showStats += Helpers.getSummaryInfo(view, "most_watched_shows", userName, "?serverId=" + config.ServerId + "&numShows=" + config.numWatchedShows);
            showStats += Helpers.getSummaryInfo(view, "total_time_watched", userName, "?episodes=true", "total_episode_time_watched");
            showStats += Helpers.getSummaryInfo(view, "total_watchable_time", userName, "?episodes=true", "total_episode_watchable_time");
            showStats += Helpers.getSummaryInfo(view, "last_seen", userName, "?episodes=true", "last_seen_tv");
            view.querySelector("#showStats").innerHTML = showStats;

            hideLoadingFunc();
        });
    }

    return {
        LoadTVProgress,
        LoadUserStats
    };

})