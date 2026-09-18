define([
    'mainTabsManager',
    'appRouter',
    Dashboard.getConfigurationResourceUrl('AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Helpers.js'),
    'emby-linkbutton'
],
    function (mainTabsManager, appRouter, AdminHelpers, Helpers) {
        'use strict';

        return function (view, params) {

            view.addEventListener('viewshow', function (e) {

                mainTabsManager.setTabs(this, Helpers.getTabIndex("Episodes", AdminHelpers.getTabs), AdminHelpers.getTabs);

                Helpers.injectStyleSheet(e);
                Helpers.injectSortableTableStyle(document);
                Helpers.setupSortability('episode_results_table', Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg);

                loadTableData();
            });

            view.addEventListener('viewhide', function (e) {
            });

            view.addEventListener('viewdestroy', function (e) {
            });

            function loadTableData() {
                if (!Helpers.CheckForValidConfig()) {
                    Dashboard.hideLoadingMsg();
                    return;
                }
                Helpers.loadTableData(view, 'episode_results_status', 'episode_results', 'Statistics2026/episode_list', Helpers.getMediaRowData, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg, Helpers);
            }
        };
    }
);
