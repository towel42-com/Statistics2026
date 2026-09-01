define(['baseView', 'loading', 'emby-input', 'emby-button', 'emby-checkbox', 'emby-scroller', 'emby-select'], function (BaseView, loading) {
    'use strict';

    function loadStats(view, user) {
        // Dashboard.showLoadingMsg();

        // ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {

        // view.querySelector("#UserTitle").innerHTML = "User statistics for " + user;
        //     view.querySelector("#generalStats").innerHTML = "";
        //     view.querySelector("#movieStats").innerHTML = "";
        //     view.querySelector("#showStats").innerHTML = "";

        //     var generalStats = "";
        //     generalStats += Helpers.getSummaryInfo(view, "total_time_watched", user, "?episodes=all");
        //     generalStats += Helpers.getSummaryInfo(view, "total_watchable_time", user, "?episodes=all");
        //     view.querySelector("#generalStats").innerHTML = generalStats;

        //     var movieStats = "";
        //     movieStats += Helpers.getSummaryInfo(view, "total_movie_count", user);
        //     movieStats += Helpers.getSummaryInfo(view, "total_movies_watched", user);
        //     movieStats += Helpers.getSummaryInfo(view, "movie_favorite_years", user);
        //     movieStats += Helpers.getSummaryInfo(view, "movie_favorite_genres", user);
        //     movieStats += Helpers.getSummaryInfo(view, "total_time_watched", user, "?episodes=false", "total_movie_time_watched");
        //     movieStats += Helpers.getSummaryInfo(view, "total_watchable_time", user, "?episodes=false", "total_movie_watchable_time");
        //     movieStats += Helpers.getSummaryInfo(view, "last_seen", user, "?episodes=false", "last_seen_movies");
        //     view.querySelector("#movieStats").innerHTML = movieStats;

        //     var showStats = "";
        //     showStats += Helpers.getSummaryInfo(view, "total_tv_count", user);
        //     showStats += Helpers.getSummaryInfo(view, "total_tv_watched", user);
        //     showStats += Helpers.getSummaryInfo(view, "total_series_finished", user);
        //     showStats += Helpers.getSummaryInfo(view, "tv_favorite_genres", user);
        //     showStats += Helpers.getSummaryInfo(view, "total_time_watched", user, "?episodes=true", "total_episode_time_watched");
        //     showStats += Helpers.getSummaryInfo(view, "total_watchable_time", user, "?episodes=true", "total_episode_watchable_time");
        //     showStats += Helpers.getSummaryInfo(view, "last_seen", user, "?episodes=true", "last_seen_tv");
        //     view.querySelector("#showStats").innerHTML = showStats;

        //     Dashboard.hideLoadingMsg();
        // });
    }

    function View(view, params) {
        BaseView.apply(this, arguments);

        view.addEventListener('viewshow', function (e) {
            // Helpers.injectStyleSheet(e);
        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });

    }

    Object.assign(View.prototype, BaseView.prototype);

    function fetchExistingConfiguration(userId) {
        return ApiClient.getTypedUserSettings(userId, 'Statistics 2026');
    }

    function loadUserInfo(userId) {
        // fetchExistingConfiguration(userId).then(function (currentUserConfig) {
        //     view.querySelector("#UserTitle").innerHTML = "User statistics for " + currentUserConfig.Username || '<unknown>';
        //     loading.hide();
        // });
    }

    View.prototype.onResume = function (options) {

        BaseView.prototype.onResume.apply(this, arguments);

        // if (options.refresh) {
        //     loading.show();

        //     var view = this.view;
        //     var instance = this;

        //     loadUserInfo(instance.params.userId);
        // }
    };
});