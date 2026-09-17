define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Helpers.js')
],
    function (mainTabsManager, AdminHelpers, Helpers) {
        'use strict';

        function loadDebugInfo(view) {
            var url = ApiClient.getUrl("/emby/System/Configuration");
            ApiClient.getJSON(url).then(response => {
                if (response.EnableDebugLevelLogging) {
                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        var debugInfo = "Version <b>" + config.Version + "</b> - Build Date - <b>" + config.BuildDate + "</b>";

                        var urlText = "/emby/Statistics2026/database_status";
                        var url = ApiClient.getUrl(urlText);
                        ApiClient.getJSON(url).then(response => {
                            debugInfo += " - Database Status <b>" + response.DBState + " (";
                            if (!response.DBStateOK)
                                debugInfo += "not ";
                            debugInfo += "OK)</b>";

                            view.querySelector(`#debugInfo`).style.display = '';
                            view.querySelector("#debugInfo").innerHTML = debugInfo;
                        }).catch(error => {
                            console.error("database_status failed:", errorMessage);
                        });
                    });

                }
                else
                    view.querySelector(`#debugInfo`).style.display = 'none';
            }).catch(error => {
                console.error("API call failed:", error);
            });
        }

        function loadStats(view) {
            Dashboard.showLoadingMsg();

            ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                var lastRunInfo = "Last Codec Analysis finished at <b> " + config.LastUpdated + "</b>";

                view.querySelector("#lastRunInfo").innerHTML = lastRunInfo;
                view.querySelector(`#debugInfo`).style.display = 'none';
                loadDebugInfo(view);

                if (!Helpers.CheckForValidConfig()) {
                    view.querySelector(`#lastRunInfo`).style.display = 'none';
                    Dashboard.hideLoadingMsg();
                    return;
                }
            });

            view.querySelector("#pageIntro").innerHTML =
                "This plugin will calculate media and user statistics "
                + "from this Emby server instance.";


            var userInfo = "";
            userInfo += Helpers.getSummaryInfo(view, "most_active_users", "", "");
            userInfo += Helpers.getSummaryInfo(view, "user_count", "", "");
            view.querySelector("#userInfo").innerHTML = (userInfo);

            var mediaInfo = "";
            mediaInfo += Helpers.getSummaryInfo(view, "codec_summary");
            mediaInfo += Helpers.getSummaryInfo(view, "resolution_summary", "", "");
            mediaInfo += Helpers.getSummaryInfo(view, "dvprofile_summary", "", "");
            view.querySelector("#mediaInfo").innerHTML = mediaInfo;

            var movieStats = "";
            movieStats += Helpers.getSummaryInfo(view, "total_movie_count", "");
            movieStats += Helpers.getSummaryInfo(view, "total_collection_count", "");
            movieStats += Helpers.getSummaryInfo(view, "total_movie_studio_count", "");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/Largest", "", "", "largest_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/Smallest", "", "", "smallest_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/Longest", "", "", "longest_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/Shortest", "", "", "shortest_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/HighestRated", "", "", "highest_rated_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/LowestRated", "", "", "lowest_rated_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/HighestBitrate", "", "", "highest_bitrate_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/LowestBitrate", "", "", "lowest_bitrate_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/OldestPremiereDate", "", "", "oldest_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/LatestPremiereDate", "", "", "latest_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/FirstAdditionToServer", "", "", "oldest_movie_addition");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/LatestAdditionToServer", "", "", "latest_movie_addition");
            view.querySelector("#movieStats").innerHTML = movieStats;

            var movieMostLeastWatchedStats = "";
            movieMostLeastWatchedStats += Helpers.getSummaryInfo(view, "least_watched_movies", "", "");
            movieMostLeastWatchedStats += Helpers.getSummaryInfo(view, "most_watched_movies", "", "");
            view.querySelector("#movieMostLeastWatchedStats").innerHTML = movieMostLeastWatchedStats;

            var seriesSummaryStats = "";
            seriesSummaryStats += Helpers.getSummaryInfo(view, "total_tv_count", "");
            seriesSummaryStats += Helpers.getSummaryInfo(view, "total_tv_studio_count", "");
            view.querySelector("#seriesSummaryStats").innerHTML = seriesSummaryStats;

            var seriesMostLeastWatchedStats = "";
            seriesMostLeastWatchedStats += Helpers.getSummaryInfo(view, "least_watched_shows", "", "");
            seriesMostLeastWatchedStats += Helpers.getSummaryInfo(view, "most_watched_shows", "", "");
            view.querySelector("#seriesMostLeastWatchedStats").innerHTML = seriesMostLeastWatchedStats;

            var seriesStats = "";
            seriesStats += Helpers.getSummaryInfo(view, "get_series/Largest", "", "", "largest_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/Smallest", "", "", "smallest_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/Longest", "", "", "longest_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/Shortest", "", "", "shortest_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/HighestRated", "", "", "highest_rated_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/LowestRated", "", "", "lowest_rated_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/HighestBitrate", "", "", "highest_bitrate_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/LowestBitrate", "", "", "lowest_bitrate_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/OldestPremiereDate", "", "", "oldest_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/LatestPremiereDate", "", "", "latest_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/FirstAdditionToServer", "", "", "oldest_series_addition");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/LatestAdditionToServer", "", "", "latest_series_addition");
            view.querySelector("#seriesStats").innerHTML = seriesStats;

            var episodeAgeStats = "";
            episodeAgeStats += Helpers.getSummaryInfo(view, "get_episode/OldestPremiereDate", "", "", "oldest_episode");
            episodeAgeStats += Helpers.getSummaryInfo(view, "get_episode/LatestPremiereDate", "", "", "latest_episode");
            view.querySelector("#episodeAgeStats").innerHTML = episodeAgeStats;

            var episodeAdditionStats = "";
            episodeAdditionStats += Helpers.getSummaryInfo(view, "get_episode/FirstAdditionToServer", "", "", "oldest_episode_addition");
            episodeAdditionStats += Helpers.getSummaryInfo(view, "get_episode/LatestAdditionToServer", "", "", "latest_episode_addition");
            view.querySelector("#episodeAdditionStats").innerHTML = episodeAdditionStats;

            Dashboard.hideLoadingMsg();
        }

        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                mainTabsManager.setTabs(this, Helpers.getTabIndex("Summary", AdminHelpers.getTabs), AdminHelpers.getTabs);
                Helpers.injectStyleSheet(e);
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            loadStats(view);
        }
    }
);