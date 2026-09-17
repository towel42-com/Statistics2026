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

                mainTabsManager.setTabs(this, Helpers.getTabIndex("Episodes", AdminHelpers.getTabs), AdminHelpers.getTabs);

                Helpers.injectStyleSheet(e);
                Helpers.injectSortableTableStyle(document);

                loadTableData();

                document.querySelectorAll('#episode_results_table thead th').forEach((header, index) => {
                    header.addEventListener('click', () => {
                        // const columnName = header.getAttribute('data-column');
                        const columnType = header.getAttribute('data-type');

                        console.log(`Sorting index: ${index}, Column Type: ${columnType}`);

                        // Pass these variables straight into your sort function
                        Helpers.sortTable(index, columnType, 'episode_results_table');
                    });
                });

                function loadTableData() {
                    if (!Helpers.CheckForValidConfig()) {
                        Dashboard.hideLoadingMsg();
                        return;
                    }
                    Helpers.loadTableData(view, 'episode_results_status', 'episode_results', 'Statistics2026/episode_list', Helpers.getMediaRowData, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg, Helpers);
                }
            });

            view.addEventListener('viewhide', function (e) {
            });

            view.addEventListener('viewdestroy', function (e) {
            });
        };
    }
);
