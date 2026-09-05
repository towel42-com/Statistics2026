define([
        'mainTabsManager',
        'appRouter',
        Dashboard.getConfigurationResourceUrl('Helpers.js'),
        ApiClient.getUrl('web/configurationpage?name=Helpers_UserPage.js'),
        Dashboard.getConfigurationResourceUrl('LoadingHelpers.js'),
        'emby-linkbutton'
],
    function (mainTabsManager, appRouter, Helpers, UserPageHelpers, LoadingHelpers) {
    'use strict';

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            mainTabsManager.setTabs(this, Helpers.getTabIndex("Movies"), Helpers.getTabs);

            Helpers.injectStyleSheetEX(e, UserPageHelpers.getConfigPageUrl('style.css'));
            var style = document.createElement('style');
            style.innerHTML = LoadingHelpers.sortableTableStyle();
            var ref = document.querySelector('script');
            ref.parentNode.insertBefore(style, ref);

            loadTableData();

            document.querySelectorAll('#movie_results_table thead th').forEach((header, index) => {
                header.addEventListener('click', () => {
                    // const columnName = header.getAttribute('data-column');
                    const columnType = header.getAttribute('data-type');

                    console.log(`Sorting index: ${index}, Column Type: ${columnType}`);

                    // Pass these variables straight into your sort function
                    LoadingHelpers.sortTable(index, columnType, 'movie_results_table');
                });
            });

            function loadTableData() {
                LoadingHelpers.loadTableData(view, 'movie_results_status', 'movie_results', 'Statistics2026/movie_list', LoadingHelpers.getMediaRowData, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg, Helpers);
            }
        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});

//# sourceURL=AdminPages/Movies.js