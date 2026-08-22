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

    function loadStats(view, user) {
        Dashboard.showLoadingMsg();

        ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            if (config.LastUpdated === undefined) {
                Dashboard.alert({
                    message:
                        "No configuration found, please run the 'Statistics 2026' task on the Scheduled Tasks page and come back for the results."
                });

                view.querySelector(`#lastRunInfo`).style.display = 'none';

                Dashboard.hideLoadingMsg();
                return;
            } 

            view.querySelector("#UserTitle").innerHTML = "User statistics for " + user;
            view.querySelector("#userStats").innerHTML = "";
            view.querySelector("#movieStats").innerHTML = "";
            view.querySelector("#showStats").innerHTML = "";

            var userStats = "";
            userStats += Helpers.getSummaryInfo(view, "total_time_watched", user);
            userStats += Helpers.getSummaryInfo(view, "total_watchable_time", user);
            view.querySelector("#userStats").innerHTML = userStats;

            var movieStats = "";
            movieStats += Helpers.getSummaryInfo(view, "total_movie_count", user);
            movieStats += Helpers.getSummaryInfo(view, "total_movies_watched", user);
            movieStats += Helpers.getSummaryInfo(view, "movie_favorite_years", user);
            movieStats += Helpers.getSummaryInfo(view, "movie_favorite_genres", user);
            view.querySelector("#movieStats").innerHTML = movieStats;

            var showStats = "";
            view.querySelector("#showStats").innerHTML = showStats;
            // var userStat = config.UserStats.find(v => v.UserName === user);

            // userStat.OverallStats.forEach((v) => { createStatDiv(v, "#overallStat", view); });
            // userStat.MovieStats.forEach((v) => { createStatDiv(v, "#movieStat", view); });
            // userStat.ShowStats.forEach((v) => { createStatDiv(v, "#showStat", view); });

            // dynamicbuttons.forEach((v) => {
            //     view.querySelector(`#` + v.id).addEventListener("click",
            //         function () {
            //             showInfo(v.info, v.title);
            //         });
            // });

            Dashboard.hideLoadingMsg();
        });
    }

    return function (view, params) {
        view.addEventListener('viewshow', function (e) {

            mainTabsManager.setTabs(this, Helpers.getTabIndex("UserStats"), Helpers.getTabs);
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