define([
    'mainTabsManager',
    'appRouter',
    Dashboard.getConfigurationResourceUrl('Helpers.js'),
    Dashboard.getConfigurationResourceUrl('LoadingHelpers.js'),
    'emby-linkbutton'
],
    function (mainTabsManager, appRouter, Helpers, LoadingHelpers) {
    'use strict';

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            mainTabsManager.setTabs(this, Helpers.getTabIndex("Episodes"), Helpers.getTabs);

            var style = document.createElement('style');
            style.innerHTML = LoadingHelpers.sortableTableStyle();

            var ref = document.querySelector('script');
            ref.parentNode.insertBefore(style, ref);

            loadTableData();

            document.querySelectorAll('#episode_results_table thead th').forEach((header, index) => {
                header.addEventListener('click', () => {
                    // const columnName = header.getAttribute('data-column');
                    const columnType = header.getAttribute('data-type');

                    console.log(`Sorting index: ${index}, Column Type: ${columnType}`);

                    // Pass these variables straight into your sort function
                    LoadingHelpers.sortTable(index, columnType, 'episode_results_table');
                });
            });

            function loadTableData() {
                LoadingHelpers.loadTableData(view, 'episode_results_status', 'episode_results', 'Statistics2026/episode_list', LoadingHelpers.getMediaRowData, Helpers);
            }
        });

        view.addEventListener('viewhide', function (e) {
        });

        view.addEventListener('viewdestroy', function (e) {
        });
    };
});

//# sourceURL=AdminPages/Episodes.js