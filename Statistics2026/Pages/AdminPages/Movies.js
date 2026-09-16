define([
        'mainTabsManager',
        'appRouter',
        Dashboard.getConfigurationResourceUrl('AdminHelpers.js'),
        Dashboard.getConfigurationResourceUrl('SharedHelpers.js'),
        'emby-linkbutton'
],
    function (mainTabsManager, appRouter, AdminHelpers, UserPageHelpers, SharedHelpers) {
        'use strict';

        return function (view, params) {

            // init code here
            view.addEventListener('viewshow', function (e) {

                mainTabsManager.setTabs(this, SharedHelpers.getTabIndex("Movies", AdminHelpers.getTabs), AdminHelpers.getTabs);

                SharedHelpers.injectStyleSheetEX(e, Dashboard.getConfigurationResourceUrl('style.css'));
                var style = document.createElement('style');
                style.innerHTML = SharedHelpers.sortableTableStyle();
                var ref = document.querySelector('script');
                ref.parentNode.insertBefore(style, ref);

                loadTableData();

                document.querySelectorAll('#movie_results_table thead th').forEach((header, index) => {
                    header.addEventListener('click', () => {
                        // const columnName = header.getAttribute('data-column');
                        const columnType = header.getAttribute('data-type');

                        console.log(`Sorting index: ${index}, Column Type: ${columnType}`);

                        // Pass these variables straight into your sort function
                        SharedHelpers.sortTable(index, columnType, 'movie_results_table');
                    });
                });

                function loadTableData() {
                    SharedHelpers.loadTableData(view, 'movie_results_status', 'movie_results', 'Statistics2026/movie_list', SharedHelpers.getMediaRowData, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg, AdminHelpers, SharedHelpers);
                }
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });
        };
    }
);
