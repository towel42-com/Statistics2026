define(['mainTabsManager', Dashboard.getConfigurationResourceUrl('Helpers.js'), Dashboard.getConfigurationResourceUrl('LoadingHelpers.js')], function (mainTabsManager, Helpers, LoadingHelpers) {
    'use strict';

    function loadStats(view, user) {
        Dashboard.showLoadingMsg();

        ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
            if (!Helpers.CheckForValidConfig(config)) {
                Dashboard.hideLoadingMsg();
                return;
            }

            view.querySelector("#UserTitle").innerHTML = "User statistics for " + user;
            view.querySelector("#generalStats").innerHTML = "";
            view.querySelector("#movieStats").innerHTML = "";
            view.querySelector("#showStats").innerHTML = "";

            var generalStats = "";
            generalStats += Helpers.getSummaryInfo(view, "total_time_watched", user, "?episodes=all");
            generalStats += Helpers.getSummaryInfo(view, "total_watchable_time", user, "?episodes=all");
            view.querySelector("#generalStats").innerHTML = generalStats;

            var movieStats = "";
            movieStats += Helpers.getSummaryInfo(view, "total_movie_count", user);
            movieStats += Helpers.getSummaryInfo(view, "total_movies_watched", user);
            movieStats += Helpers.getSummaryInfo(view, "movie_favorite_years", user);
            movieStats += Helpers.getSummaryInfo(view, "movie_favorite_genres", user);
            movieStats += Helpers.getSummaryInfo(view, "total_time_watched", user, "?episodes=false", "total_movie_time_watched");
            movieStats += Helpers.getSummaryInfo(view, "total_watchable_time", user, "?episodes=false", "total_movie_watchable_time");
            movieStats += Helpers.getSummaryInfo(view, "last_seen", user, "?episodes=false", "last_seen_movies");
            view.querySelector("#movieStats").innerHTML = movieStats;

            var showStats = "";
            showStats += Helpers.getSummaryInfo(view, "total_tv_count", user);
            showStats += Helpers.getSummaryInfo(view, "total_tv_watched", user);
            showStats += Helpers.getSummaryInfo(view, "total_series_finished", user);
            showStats += Helpers.getSummaryInfo(view, "tv_favorite_genres", user);
            showStats += Helpers.getSummaryInfo(view, "total_time_watched", user, "?episodes=true", "total_episode_time_watched");
            showStats += Helpers.getSummaryInfo(view, "total_watchable_time", user, "?episodes=true", "total_episode_watchable_time");
            showStats += Helpers.getSummaryInfo(view, "last_seen", user, "?episodes=true", "last_seen_tv");
            view.querySelector("#showStats").innerHTML = showStats;

            Dashboard.hideLoadingMsg();
        });
    }

    return function (view, params) {
        view.addEventListener('viewshow', function (e) {
            mainTabsManager.setTabs(this, Helpers.getTabIndex("UserStats"), Helpers.getTabs);
            Helpers.injectStyleSheet(e);
        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });

        view.querySelector("#selectUser").addEventListener(`change`, function () {
            const user = this.options[this.selectedIndex].text;
            loadStats(view, user);
        });

        ApiClient.getUsers().then(function (users) {
            var select = view.querySelector(`#selectUser`);

            loadStats(view, users[0].Name);

            users.forEach((user) => {
                var option = document.createElement(`option`);
                option.value = user.Id;
                option.innerHTML = user.Name;
                select.appendChild(option);
            });
        });
    }
});