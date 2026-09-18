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

            // init code here
            view.addEventListener('viewshow', function (e) {

                mainTabsManager.setTabs(this, Helpers.getTabIndex("Movies", AdminHelpers.getTabs), AdminHelpers.getTabs);

                Helpers.injectStyleSheet(e);
                Helpers.injectSortableTableStyle(document);

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
                Helpers.loadTableData(view, 'movie_results_status', 'movie_results', 'Statistics2026/movie_list', Helpers.getMediaRowData, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg, Helpers);
            }

        };
    }
);
