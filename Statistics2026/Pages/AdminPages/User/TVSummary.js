define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_AdminPages_AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_Helpers.js')
],
    function (mainTabsManager, AdminHelpers, Helpers) {
        'use strict';

        return function (view, params) {
            view.addEventListener('viewshow', async function (e) {
                Helpers.initTabSystem(view, AdminHelpers.getTabHierarchy, AdminHelpers.getTabs);

                let configValid = await Helpers.CheckForValidConfig();
                if (!configValid) {
                    view.querySelector(`#lastRunInfo`).style.display = 'none';
                    Dashboard.hideLoadingMsg();
                    return;
                }

                Helpers.insertGetLastRunInfo(view, ["UserAnalysis", "UserWatchDataAnalysis"], "lastRunInfo");
                Helpers.loadUsers(view, `#selectUser_TVSummary`, loadData);
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            view.querySelector("#selectUser_TVSummary").addEventListener(`change`, function () {
                const userId = this.options[this.selectedIndex].value;
                Helpers.setAdminPagesSelectedUserId(userId);
                loadData(view, userId);
            });

            async function loadData(view, userId) {
                ApiClient.getUser(userId).then(function (user) {
                    Helpers.LoadUserTVSummaryStats(view, user.Name, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg);
                });
            }
        }
    }
);