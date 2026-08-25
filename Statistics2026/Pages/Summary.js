define(['mainTabsManager', Dashboard.getConfigurationResourceUrl('Helpers.js')], function (mainTabsManager, Helpers) {
    'use strict';

    ApiClient.getStatistics2026URL = function (url_to_get) {
        console.log("getStatistics2026URL Url = " + url_to_get);
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };

    const pluginId = "4BFE2894-AEA3-4D3C-A429-503B56D61711";

    function loadDebugInfo(view) {
        var url = ApiClient.getUrl("/emby/System/Configuration");
        ApiClient.getJSON(url).then(response => {

            if (response.EnableDebugLevelLogging) {
                ApiClient.getPluginConfiguration(pluginId).then(function (config) {
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

        ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            var lastRunInfo = "Last Codec Analysis finished at <b> " + config.LastUpdated + "</b>";

            view.querySelector("#lastRunInfo").innerHTML = lastRunInfo;
            view.querySelector(`#debugInfo`).style.display = 'none';
            loadDebugInfo(view);

            if (!Helpers.dataLoaded(config))
            {
                view.querySelector(`#lastRunInfo`).style.display = 'none';
                return;
            } 

            view.querySelector("#pageIntro").innerHTML =
                "This plugin will calculate media and user statistics "
                + "from this Emby server instance.";


            var userInfo = "";
            userInfo += Helpers.getSummaryInfo(view, "most_active_users", "", "?hasConnectUserID=" + config.hasConnectUserID + "&numUsers=" + config.numMostActiveUsers + "&excludeAdmin=" + config.excludeAdmin);
            userInfo += Helpers.getSummaryInfo(view, "user_count", "", "?hasConnectUserID=" + config.hasConnectUserID + "&excludeAdmin=" + config.excludeAdmin);
            view.querySelector("#userInfo").innerHTML = (userInfo);

            var mediaInfo = "";
            mediaInfo += Helpers.getSummaryInfo(view, "codec_summary", "", "?showAllCodecs=" + config.showAllCodecs);
            mediaInfo += Helpers.getSummaryInfo(view, "resolution_summary", "", "?showAllResolutions =" + config.showAllResolutions);
            mediaInfo += Helpers.getSummaryInfo(view, "dvprofile_summary", "", "?showUnknownDVProfiles=" + config.showUnknownDVProfiles + "&showAllDVProfiles =" + config.showAllDVProfiles);
            view.querySelector("#mediaInfo").innerHTML = mediaInfo;

            var movieStats = "";
            movieStats += Helpers.getSummaryInfo(view, "total_movie_count", "");
            movieStats += Helpers.getSummaryInfo(view, "total_collection_count", "");
            movieStats += Helpers.getSummaryInfo(view, "total_movie_studio_count", "");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/Largest", "", "?serverId=" + config.ServerId, "largest_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/Smallest", "", "?serverId=" + config.ServerId, "smallest_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/Longest", "", "?serverId=" + config.ServerId, "longest_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/Shortest", "", "?serverId=" + config.ServerId, "shortest_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/HighestRated", "", "?serverId=" + config.ServerId, "highest_rated_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/LowestRated", "", "?serverId=" + config.ServerId, "lowest_rated_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/HighestBitrate", "", "?serverId=" + config.ServerId, "highest_bitrate_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/LowestBitrate", "", "?serverId=" + config.ServerId, "lowest_bitrate_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/OldestPremiereDate", "", "?serverId=" + config.ServerId, "oldest_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/LatestPremiereDate", "", "?serverId=" + config.ServerId, "latest_movie");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/OldestAddition", "", "?serverId=" + config.ServerId, "oldest_movie_addition");
            movieStats += Helpers.getSummaryInfo(view, "get_movie/LatestAddition", "", "?serverId=" + config.ServerId, "latest_movie_addition");
            view.querySelector("#movieStats").innerHTML = movieStats;

            var seriesStats = "";
            seriesStats += Helpers.getSummaryInfo(view, "total_tv_count", "");
            seriesStats += Helpers.getSummaryInfo(view, "total_tv_studio_count", "");
            seriesStats += Helpers.getSummaryInfo(view, "least_watched_shows", "", "?serverId=" + config.ServerId);
            seriesStats += Helpers.getSummaryInfo(view, "most_watched_shows", "", "?serverId=" + config.ServerId);
            seriesStats += Helpers.getSummaryInfo(view, "get_series/Largest", "", "?serverId=" + config.ServerId, "largest_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/Smallest", "", "?serverId=" + config.ServerId, "smallest_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/Longest", "", "?serverId=" + config.ServerId, "longest_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/Shortest", "", "?serverId=" + config.ServerId, "shortest_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/HighestRated", "", "?serverId=" + config.ServerId, "highest_rated_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/LowestRated", "", "?serverId=" + config.ServerId, "lowest_rated_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/HighestBitrate", "", "?serverId=" + config.ServerId, "highest_bitrate_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/LowestBitrate", "", "?serverId=" + config.ServerId, "lowest_bitrate_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/OldestPremiereDate", "", "?serverId=" + config.ServerId, "oldest_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/LatestPremiereDate", "", "?serverId=" + config.ServerId, "latest_series");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/OldestAddition", "", "?serverId=" + config.ServerId, "oldest_series_addition");
            seriesStats += Helpers.getSummaryInfo(view, "get_series/LatestAddition", "", "?serverId=" + config.ServerId, "latest_series_addition");
            view.querySelector("#seriesStats").innerHTML = seriesStats;

            var episodeStats = "";
            episodeStats += Helpers.getSummaryInfo(view, "get_episode/OldestPremiereDate", "", "?serverId=" + config.ServerId, "oldest_episode");
            episodeStats += Helpers.getSummaryInfo(view, "get_episode/LatestPremiereDate", "", "?serverId=" + config.ServerId, "latest_episode");
            episodeStats += Helpers.getSummaryInfo(view, "get_episode/OldestAddition", "", "?serverId=" + config.ServerId, "oldest_episode_addition");
            episodeStats += Helpers.getSummaryInfo(view, "get_episode/LatestAddition", "", "?serverId=" + config.ServerId, "latest_episode_addition");
            view.querySelector("#episodeStats").innerHTML = episodeStats;

            Dashboard.hideLoadingMsg();
        });
    }

    return function (view, params) {
        view.addEventListener('viewshow', function (e) {

            mainTabsManager.setTabs(this, Helpers.getTabIndex("Summary"), Helpers.getTabs);
        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });

        loadStats(view);
    }
});