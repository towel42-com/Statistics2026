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
                Helpers.injectSortableTableStyle(document);

                Helpers.getLastRunInfo(view, ["UserWatchDataAnalysis"], "lastRunInfo");
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

            function loadData(view, userId) {
                if (!Helpers.CheckForValidConfig()) {
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
