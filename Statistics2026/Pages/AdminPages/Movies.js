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

                Helpers.injectStyleSheetEX(e, Dashboard.getConfigurationResourceUrl('style.css'));
                var style = document.createElement('style');
                style.innerHTML = Helpers.sortableTableStyle();
                var ref = document.querySelector('script');
                ref.parentNode.insertBefore(style, ref);

                loadTableData();

                document.querySelectorAll('#movie_results_table thead th').forEach((header, index) => {
                    header.addEventListener('click', () => {
                        // const columnName = header.getAttribute('data-column');
                        const columnType = header.getAttribute('data-type');

                        console.log(`Sorting index: ${index}, Column Type: ${columnType}`);

                        // Pass these variables straight into your sort function
                        Helpers.sortTable(index, columnType, 'movie_results_table');
                    });
                });

                function loadTableData() {
                    Helpers.loadTableData(view, 'movie_results_status', 'movie_results', 'Statistics2026/movie_list', Helpers.getMediaRowData, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg, AdminHelpers, Helpers);
                }
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });
        };
    }
);
