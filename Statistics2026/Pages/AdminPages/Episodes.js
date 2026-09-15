define([
    'mainTabsManager',
    'appRouter',
    Dashboard.getConfigurationResourceUrl('Helpers.js'),
    ApiClient.getUrl('web/configurationpage?name=Helpers_UserPage.js'),
    Dashboard.getConfigurationResourceUrl('SharedHelpers.js'),
    'emby-linkbutton'
],
    function (mainTabsManager, appRouter, Helpers, UserPageHelpers, SharedHelpers) {
    'use strict';

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            mainTabsManager.setTabs(this, Helpers.getTabIndex("Episodes"), Helpers.getTabs);

            Helpers.injectStyleSheetEX(e, UserPageHelpers.getConfigPageUrl('style.css'));
            var style = document.createElement('style');
            style.innerHTML = SharedHelpers.sortableTableStyle();

            var ref = document.querySelector('script');
            ref.parentNode.insertBefore(style, ref);

            loadTableData();

            document.querySelectorAll('#episode_results_table thead th').forEach((header, index) => {
                header.addEventListener('click', () => {
                    // const columnName = header.getAttribute('data-column');
                    const columnType = header.getAttribute('data-type');

                    console.log(`Sorting index: ${index}, Column Type: ${columnType}`);

                    // Pass these variables straight into your sort function
                    SharedHelpers.sortTable(index, columnType, 'episode_results_table');
                });
            });

            function loadTableData() {
                SharedHelpers.loadTableData(view, 'episode_results_status', 'episode_results', 'Statistics2026/episode_list', SharedHelpers.getMediaRowData, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg, Helpers);
            }
        });

        view.addEventListener('viewhide', function (e) {
        });

        view.addEventListener('viewdestroy', function (e) {
        });
    };
});

//# sourceURL=AdminPages/Episodes.js