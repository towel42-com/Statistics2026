define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Helpers.js')
],
    function (mainTabsManager, AdminHelpers, Helpers) {
        'use strict';

        window.MyPluginHelpers = Helpers;

        async function loadDebugInfo(view, pluginConfig) {
            var url = ApiClient.getUrl("/emby/System/Configuration");

            let sysConfig = await ApiClient.getJSON(url).catch(error => {
                console.error("System Config API call failed:", error);
                return;
            });

            if (!sysConfig.EnableDebugLevelLogging && !pluginConfig.showDebugInfo) {
                view.querySelector(`#debugInfo`).style.display = 'none';
                view.querySelector(`#playedUserMedia`).style.display = 'none';
                view.querySelector(`#userWatchMediaIssues`).style.display = 'none';
                return;
            }

            var urlText = "/emby/Statistics2026/database_status";
            var url = ApiClient.getUrl(urlText);
            let dbStatus = await ApiClient.getJSON(url).catch(error => {
                console.error("database_status API call failed:", error);
                return;
            });

            var debugInfo = "Version <b>" + pluginConfig.Version
                + "</b> - Build Date - <b>" + pluginConfig.BuildDate + "</b>"
                + " - Database Status <b>" + dbStatus.DBState + " (";
            if (!dbStatus.DBStateOK)
                debugInfo += "not ";
            debugInfo += "OK)</b>";

            view.querySelector(`#debugInfo`).style.display = '';
            view.querySelector("#debugInfo").innerHTML = debugInfo;

            var playedUserMedia = Helpers.getSummaryInfo(view, "played_user_media", "", "");
            view.querySelector(`#playedUserMedia`).style.display = '';
            view.querySelector("#playedUserMedia").innerHTML = (playedUserMedia);

            var userWatchMediaIssues = Helpers.getSummaryInfo(view, "user_watch_media_issues", "", "");
            view.querySelector(`#userWatchMediaIssues`).style.display = '';
            view.querySelector("#userWatchMediaIssues").innerHTML = (userWatchMediaIssues);
        }

        function loadStats(view) {
            Dashboard.showLoadingMsg();
            ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {

                view.querySelector(`#debugInfo`).style.display = 'none';
                loadDebugInfo(view, config);

                if (!Helpers.CheckForValidConfig()) {
                    view.querySelector(`#lastRunInfo`).style.display = 'none';
                    Dashboard.hideLoadingMsg();
                    return;
                }
            });

            Helpers.getLastRunInfo(view, "System", "lastRunInfo");
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

            var movieStats_totals = "";
            movieStats_totals += Helpers.getSummaryInfo(view, "total_movie_count", "");
            movieStats_totals += Helpers.getSummaryInfo(view, "total_collection_count", "");
            movieStats_totals += Helpers.getSummaryInfo(view, "total_movie_studio_count", "");
            movieStats_totals += Helpers.getSummaryInfo(view, "total_missing_movies", "");
            view.querySelector("#movieStats_totals").innerHTML = movieStats_totals;

            var movieStats_size = "";
            movieStats_size += Helpers.getSummaryInfo(view, "get_movie/Largest", "", "", "largest_movie");
            movieStats_size += Helpers.getSummaryInfo(view, "get_movie/Smallest", "", "", "smallest_movie");
            movieStats_size += Helpers.getSummaryInfo(view, "get_movie/Longest", "", "", "longest_movie");
            movieStats_size += Helpers.getSummaryInfo(view, "get_movie/Shortest", "", "", "shortest_movie");
            view.querySelector("#movieStats_size").innerHTML = movieStats_size;

            var movieStats_ratings = "";
            movieStats_ratings += Helpers.getSummaryInfo(view, "get_movie/HighestRated", "", "", "highest_rated_movie");
            movieStats_ratings += Helpers.getSummaryInfo(view, "get_movie/LowestRated", "", "", "lowest_rated_movie");
            view.querySelector("#movieStats_ratings").innerHTML = movieStats_ratings;

            var movieStats_video_quality = "";
            movieStats_video_quality += Helpers.getSummaryInfo(view, "get_movie/HighestBitrate", "", "", "highest_bitrate_movie");
            movieStats_video_quality += Helpers.getSummaryInfo(view, "get_movie/LowestBitrate", "", "", "lowest_bitrate_movie");
            view.querySelector("#movieStats_video_quality").innerHTML = movieStats_video_quality;

            var movieStats_added_to_server = "";
            movieStats_added_to_server += Helpers.getSummaryInfo(view, "get_movie/FirstAdditionToServer", "", "", "oldest_movie_addition");
            movieStats_added_to_server += Helpers.getSummaryInfo(view, "get_movie/LatestAdditionToServer", "", "", "latest_movie_addition");
            view.querySelector("#movieStats_added_to_server").innerHTML = movieStats_added_to_server;

            var movieStats_age_stats = "";
            movieStats_age_stats += Helpers.getSummaryInfo(view, "get_movie/OldestPremiereDate", "", "", "oldest_movie");
            movieStats_age_stats += Helpers.getSummaryInfo(view, "get_movie/LatestPremiereDate", "", "", "latest_movie");
            view.querySelector("#movieStats_age_stats").innerHTML = movieStats_age_stats;

            var movieStats_mostLeastWatched = "";
            movieStats_mostLeastWatched += Helpers.getSummaryInfo(view, "least_watched_movies", "", "");
            movieStats_mostLeastWatched += Helpers.getSummaryInfo(view, "most_watched_movies", "", "");
            view.querySelector("#movieStats_mostLeastWatched").innerHTML = movieStats_mostLeastWatched;

            var seriesStats_totals = "";
            seriesStats_totals += Helpers.getSummaryInfo(view, "total_tv_count", "");
            seriesStats_totals += Helpers.getSummaryInfo(view, "total_tv_studio_count", "");
            seriesStats_totals += Helpers.getSummaryInfo(view, "total_missing_episodes", "");
            view.querySelector("#seriesStats_totals").innerHTML = seriesStats_totals;

            var seriesStats_size = "";
            seriesStats_size += Helpers.getSummaryInfo(view, "get_series/Largest", "", "", "largest_series");
            seriesStats_size += Helpers.getSummaryInfo(view, "get_series/Smallest", "", "", "smallest_series");
            seriesStats_size += Helpers.getSummaryInfo(view, "get_series/Longest", "", "", "longest_series");
            seriesStats_size += Helpers.getSummaryInfo(view, "get_series/Shortest", "", "", "shortest_series");
            view.querySelector("#seriesStats_size").innerHTML = seriesStats_size;

            var seriesStats_ratings = "";
            seriesStats_ratings += Helpers.getSummaryInfo(view, "get_series/HighestRated", "", "", "highest_rated_series");
            seriesStats_ratings += Helpers.getSummaryInfo(view, "get_series/LowestRated", "", "", "lowest_rated_series");
            view.querySelector("#seriesStats_ratings").innerHTML = seriesStats_ratings;

            var seriesStats_video_quality = "";
            seriesStats_video_quality += Helpers.getSummaryInfo(view, "get_series/HighestBitrate", "", "", "highest_bitrate_series");
            seriesStats_video_quality += Helpers.getSummaryInfo(view, "get_series/LowestBitrate", "", "", "lowest_bitrate_series");
            view.querySelector("#seriesStats_video_quality").innerHTML = seriesStats_video_quality;

            var seriesStats_added_to_server = "";
            seriesStats_added_to_server += Helpers.getSummaryInfo(view, "get_series/FirstAdditionToServer", "", "", "oldest_series_addition");
            seriesStats_added_to_server += Helpers.getSummaryInfo(view, "get_series/LatestAdditionToServer", "", "", "latest_series_addition");
            view.querySelector("#seriesStats_added_to_server").innerHTML = seriesStats_added_to_server;

            var seriesStats_age_stats = "";
            seriesStats_age_stats += Helpers.getSummaryInfo(view, "get_series/OldestPremiereDate", "", "", "oldest_series");
            seriesStats_age_stats += Helpers.getSummaryInfo(view, "get_series/LatestPremiereDate", "", "", "latest_series");
            view.querySelector("#seriesStats_age_stats").innerHTML = seriesStats_age_stats;

            var seriesStats_mostLeastWatched = "";
            seriesStats_mostLeastWatched += Helpers.getSummaryInfo(view, "least_watched_shows", "", "");
            seriesStats_mostLeastWatched += Helpers.getSummaryInfo(view, "most_watched_shows", "", "");
            view.querySelector("#seriesStats_mostLeastWatched").innerHTML = seriesStats_mostLeastWatched;

            var episodeStats_age_stats = "";
            episodeStats_age_stats += Helpers.getSummaryInfo(view, "get_episode/OldestPremiereDate", "", "", "oldest_episode");
            episodeStats_age_stats += Helpers.getSummaryInfo(view, "get_episode/LatestPremiereDate", "", "", "latest_episode");
            view.querySelector("#episodeStats_age_stats").innerHTML = episodeStats_age_stats;

            var episodeStats_added_to_server = "";
            episodeStats_added_to_server += Helpers.getSummaryInfo(view, "get_episode/FirstAdditionToServer", "", "", "oldest_episode_addition");
            episodeStats_added_to_server += Helpers.getSummaryInfo(view, "get_episode/LatestAdditionToServer", "", "", "latest_episode_addition");
            view.querySelector("#episodeStats_added_to_server").innerHTML = episodeStats_added_to_server;

            Dashboard.hideLoadingMsg();
        }


        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                mainTabsManager.setTabs(this, Helpers.getTabIndex("Summary", AdminHelpers.getTabs), AdminHelpers.getTabs);
                Helpers.injectStyleSheet();
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            loadStats(view);
        }
    }
);