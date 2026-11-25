define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_AdminPages_AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_Helpers.js')
],
    function (mainTabsManager, AdminHelpers, Helpers) {
        `use strict`;

        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                Helpers.initTabSystem(view, AdminHelpers.getTabHierarchy, AdminHelpers.getTabs);

                Helpers.insertGetLastRunInfo(view, ["UserAnalysis", "UserWatchDataAnalysis"], "lastRunInfo");
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
