define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Helpers.js')
],
    function (mainTabsManager, AdminHelpers, Helpers) {
        `use strict`;

        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                mainTabsManager.setTabs(this, Helpers.getTabIndex("TVSeriesProgress", AdminHelpers.getTabs), AdminHelpers.getTabs);
                Helpers.injectStyleSheet();

                Helpers.insertGetLastRunInfo(view, ["UserWatchDataAnalysis"], "lastRunInfo");
                Helpers.loadUsers(view, `#selectUser_tvprogress`, loadData);
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            view.querySelector("#selectUser_tvprogress").addEventListener(`change`, function () {
                const userId = this.options[this.selectedIndex].value;
                loadData(view, userId);
            });

            async function loadData(view, userId) {
                let configValid = await Helpers.CheckForValidConfig();
                if (!configValid) {
                    view.querySelector(`#lastRunInfo`).style.display = 'none';
                    view.querySelector(`#TVSeriesProgress_status`).style.display = 'none';
                    Dashboard.hideLoadingMsg();
                    return;
                }

                ApiClient.getUser(userId).then(function (user) {
                    Helpers.LoadTVProgress(view, user.Name, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg);
                });
            }
        }
    }
);
