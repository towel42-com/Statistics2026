define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('Helpers.js'),
    Dashboard.getConfigurationResourceUrl('SharedHelpers.js')
],
    function (mainTabsManager, Helpers, SharedHelpers) {
        'use strict';

        function loadDebugInfo(view) {
            var url = ApiClient.getUrl("/emby/System/Configuration");
            ApiClient.getJSON(url).then(response => {

                if (response.EnableDebugLevelLogging) {
                    ApiClient.getPluginConfiguration(SharedHelpers.pluginId).then(function (config) {
                        var debugInfo = "Version <b>" + config.Version + "</b> - Build Date - <b>" + config.BuildDate + "</b>";
                        view.querySelector(`#debugInfo`).style.display = '';
                        view.querySelector("#debugInfo").innerHTML = debugInfo;
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

            ApiClient.getPluginConfiguration(SharedHelpers.pluginId).then(function (config) {
                var lastRunInfo = "Last Codec Analysis finished at <b> " + config.LastUpdated + "</b>";

                view.querySelector("#lastRunInfo").innerHTML = lastRunInfo;
                view.querySelector(`#debugInfo`).style.display = 'none';
                loadDebugInfo(view);

                if (!SharedHelpers.CheckForValidConfig(config)) {
                    view.querySelector(`#lastRunInfo`).style.display = 'none';
                    Dashboard.hideLoadingMsg();
                    return;
                }

                view.querySelector("#pageIntro").innerHTML =
                    "This plugin will calculate media and user statistics "
                    + "from this Emby server instance.";


                var userInfo = "";
                userInfo += SharedHelpers.getSummaryInfo(view, "most_active_users", "", "");
                userInfo += SharedHelpers.getSummaryInfo(view, "user_count", "", "");
                view.querySelector("#userInfo").innerHTML = (userInfo);

                var mediaInfo = "";
                mediaInfo += SharedHelpers.getSummaryInfo(view, "codec_summary");
                mediaInfo += SharedHelpers.getSummaryInfo(view, "resolution_summary", "", "");
                mediaInfo += SharedHelpers.getSummaryInfo(view, "dvprofile_summary", "", "");
                view.querySelector("#mediaInfo").innerHTML = mediaInfo;

                var movieStats = "";
                movieStats += SharedHelpers.getSummaryInfo(view, "total_movie_count", "");
                movieStats += SharedHelpers.getSummaryInfo(view, "total_collection_count", "");
                movieStats += SharedHelpers.getSummaryInfo(view, "total_movie_studio_count", "");
                movieStats += SharedHelpers.getSummaryInfo(view, "get_movie/Largest", "", "", "largest_movie");
                movieStats += SharedHelpers.getSummaryInfo(view, "get_movie/Smallest", "", "", "smallest_movie");
                movieStats += SharedHelpers.getSummaryInfo(view, "get_movie/Longest", "", "", "longest_movie");
                movieStats += SharedHelpers.getSummaryInfo(view, "get_movie/Shortest", "", "", "shortest_movie");
                movieStats += SharedHelpers.getSummaryInfo(view, "get_movie/HighestRated", "", "", "highest_rated_movie");
                movieStats += SharedHelpers.getSummaryInfo(view, "get_movie/LowestRated", "", "", "lowest_rated_movie");
                movieStats += SharedHelpers.getSummaryInfo(view, "get_movie/HighestBitrate", "", "", "highest_bitrate_movie");
                movieStats += SharedHelpers.getSummaryInfo(view, "get_movie/LowestBitrate", "", "", "lowest_bitrate_movie");
                movieStats += SharedHelpers.getSummaryInfo(view, "get_movie/OldestPremiereDate", "", "", "oldest_movie");
                movieStats += SharedHelpers.getSummaryInfo(view, "get_movie/LatestPremiereDate", "", "", "latest_movie");
                movieStats += SharedHelpers.getSummaryInfo(view, "get_movie/FirstAdditionToServer", "", "", "oldest_movie_addition");
                movieStats += SharedHelpers.getSummaryInfo(view, "get_movie/LatestAdditionToServer", "", "", "latest_movie_addition");
                view.querySelector("#movieStats").innerHTML = movieStats;

                var movieMostLeastWatchedStats = "";
                movieMostLeastWatchedStats += SharedHelpers.getSummaryInfo(view, "least_watched_movies", "", "");
                movieMostLeastWatchedStats += SharedHelpers.getSummaryInfo(view, "most_watched_movies", "", "");
                view.querySelector("#movieMostLeastWatchedStats").innerHTML = movieMostLeastWatchedStats;

                var seriesSummaryStats = "";
                seriesSummaryStats += SharedHelpers.getSummaryInfo(view, "total_tv_count", "");
                seriesSummaryStats += SharedHelpers.getSummaryInfo(view, "total_tv_studio_count", "");
                view.querySelector("#seriesSummaryStats").innerHTML = seriesSummaryStats;

                var seriesMostLeastWatchedStats = "";
                seriesMostLeastWatchedStats += SharedHelpers.getSummaryInfo(view, "least_watched_shows", "", "");
                seriesMostLeastWatchedStats += SharedHelpers.getSummaryInfo(view, "most_watched_shows", "", "");
                view.querySelector("#seriesMostLeastWatchedStats").innerHTML = seriesMostLeastWatchedStats;

                var seriesStats = "";
                seriesStats += SharedHelpers.getSummaryInfo(view, "get_series/Largest", "", "", "largest_series");
                seriesStats += SharedHelpers.getSummaryInfo(view, "get_series/Smallest", "", "", "smallest_series");
                seriesStats += SharedHelpers.getSummaryInfo(view, "get_series/Longest", "", "", "longest_series");
                seriesStats += SharedHelpers.getSummaryInfo(view, "get_series/Shortest", "", "", "shortest_series");
                seriesStats += SharedHelpers.getSummaryInfo(view, "get_series/HighestRated", "", "", "highest_rated_series");
                seriesStats += SharedHelpers.getSummaryInfo(view, "get_series/LowestRated", "", "", "lowest_rated_series");
                seriesStats += SharedHelpers.getSummaryInfo(view, "get_series/HighestBitrate", "", "", "highest_bitrate_series");
                seriesStats += SharedHelpers.getSummaryInfo(view, "get_series/LowestBitrate", "", "", "lowest_bitrate_series");
                seriesStats += SharedHelpers.getSummaryInfo(view, "get_series/OldestPremiereDate", "", "", "oldest_series");
                seriesStats += SharedHelpers.getSummaryInfo(view, "get_series/LatestPremiereDate", "", "", "latest_series");
                seriesStats += SharedHelpers.getSummaryInfo(view, "get_series/FirstAdditionToServer", "", "", "oldest_series_addition");
                seriesStats += SharedHelpers.getSummaryInfo(view, "get_series/LatestAdditionToServer", "", "", "latest_series_addition");
                view.querySelector("#seriesStats").innerHTML = seriesStats;

                var episodeAgeStats = "";
                episodeAgeStats += SharedHelpers.getSummaryInfo(view, "get_episode/OldestPremiereDate", "", "", "oldest_episode");
                episodeAgeStats += SharedHelpers.getSummaryInfo(view, "get_episode/LatestPremiereDate", "", "", "latest_episode");
                view.querySelector("#episodeAgeStats").innerHTML = episodeAgeStats;

                var episodeAdditionStats = "";
                episodeAdditionStats += SharedHelpers.getSummaryInfo(view, "get_episode/FirstAdditionToServer", "", "", "oldest_episode_addition");
                episodeAdditionStats += SharedHelpers.getSummaryInfo(view, "get_episode/LatestAdditionToServer", "", "", "latest_episode_addition");
                view.querySelector("#episodeAdditionStats").innerHTML = episodeAdditionStats;

                Dashboard.hideLoadingMsg();
            });
        }

        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                mainTabsManager.setTabs(this, SharedHelpers.getTabIndex("Summary", Helpers.getTabs), Helpers.getTabs);
                SharedHelpers.injectStyleSheet(e);
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            loadStats(view);
        }
    }
);