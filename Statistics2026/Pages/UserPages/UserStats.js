define(['baseView', 'loading', 'mainTabsManager', ApiClient.getUrl('web/configurationpage?name=Helpers.js'), ApiClient.getUrl('web/configurationpage?name=Helpers_UserPage.js') , 'emby-input', 'emby-button', 'emby-checkbox', 'emby-scroller', 'emby-select'], function (BaseView, loading, mainTabsManager, Helpers, UserPageHelpers) {
    'use strict';

    Object.assign(View.prototype, BaseView.prototype);

    function loadData(view, userId) {
        loading.show();
        ApiClient.getUser(userId).then(function (user) {
            view.querySelector("#UserTitle").innerHTML = "User statistics for " + user.Name;

            view.querySelector("#generalStats").innerHTML = "";
            view.querySelector("#movieStats").innerHTML = "";
            view.querySelector("#showStats").innerHTML = "";

            var generalStats = "";
            generalStats += Helpers.getSummaryInfo(view, "total_time_watched", user.Name, "?episodes=all");
            generalStats += Helpers.getSummaryInfo(view, "total_watchable_time", user.Name, "?episodes=all");
            view.querySelector("#generalStats").innerHTML = generalStats;

            var movieStats = "";
            movieStats += Helpers.getSummaryInfo(view, "total_movie_count", user.Name);
            movieStats += Helpers.getSummaryInfo(view, "total_movies_watched", user.Name);
            movieStats += Helpers.getSummaryInfo(view, "movie_favorite_years", user.Name);
            movieStats += Helpers.getSummaryInfo(view, "movie_favorite_genres", user.Name);
            movieStats += Helpers.getSummaryInfo(view, "total_time_watched", user.Name, "?episodes=false", "total_movie_time_watched");
            movieStats += Helpers.getSummaryInfo(view, "total_watchable_time", user.Name, "?episodes=false", "total_movie_watchable_time");
            movieStats += Helpers.getSummaryInfo(view, "last_seen", user.Name, "?episodes=false", "last_seen_movies");
            view.querySelector("#movieStats").innerHTML = movieStats;

            var showStats = "";
            showStats += Helpers.getSummaryInfo(view, "total_tv_count", user.Name);
            showStats += Helpers.getSummaryInfo(view, "total_tv_watched", user.Name);
            showStats += Helpers.getSummaryInfo(view, "total_series_finished", user.Name);
            showStats += Helpers.getSummaryInfo(view, "tv_favorite_genres", user.Name);
            showStats += Helpers.getSummaryInfo(view, "total_time_watched", user.Name, "?episodes=true", "total_episode_time_watched");
            showStats += Helpers.getSummaryInfo(view, "total_watchable_time", user.Name, "?episodes=true", "total_episode_watchable_time");
            showStats += Helpers.getSummaryInfo(view, "last_seen", user.Name, "?episodes=true", "last_seen_tv");
            view.querySelector("#showStats").innerHTML = showStats;
        });

        loading.hide()
    }


    View.prototype.onResume = function (options) {
        BaseView.prototype.onResume.apply(this, arguments);

        if (options.refresh) {
            var view = this.view;
            var instance = this;

            loadData(view, instance.params.userId);
        }
    };

    function View(view, params) {
        BaseView.apply(this, arguments);

        view.addEventListener('viewshow', function (e) {
            mainTabsManager.setTabs(this, Helpers.getTabIndexEX("UserStats_UserPage", UserPageHelpers.getTabs()), UserPageHelpers.getTabs);
            Helpers.injectStyleSheetEX(e, UserPageHelpers.getConfigPageUrl('style.css'));
        });
}

    return View;
});