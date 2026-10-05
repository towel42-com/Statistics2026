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
                mainTabsManager.setTabs(this, Helpers.getTabIndex("Movies", AdminHelpers.getTabs), AdminHelpers.getTabs);
                Helpers.injectStyleSheet();
                Helpers.injectSortableTableStyle(document);
                view.querySelector('input[name="movieDisplay"][value="All"]').checked = true;

                if (view.getAttribute('data-initialized') === 'true') {
                    return; // Exit and prevent reload
                }
                Helpers.getLastRunInfo(view, ["MediaAnalysis", "MissingMoviesAnalysis"], "lastRunInfo");
                loadTableData();
            });

            view.addEventListener('viewhide', function (e) {
            });

            view.addEventListener('viewdestroy', function (e) {
            });

            function getMediaHeader() {
                var showAll = view.querySelector('input[name="movieDisplay"][value="All"]').checked;
                var showOnServer = showAll || view.querySelector('input[name="movieDisplay"][value="OnServer"]').checked;
                var showMissing = showAll || view.querySelector('input[name="movieDisplay"][value="Missing"]').checked;

                return Helpers.getMediaHeader( showOnServer, showMissing );
            }

            function getMediaRowData(info) {
                var showAll = view.querySelector('input[name="movieDisplay"][value="All"]').checked;
                var showOnServer = showAll || view.querySelector('input[name="movieDisplay"][value="OnServer"]').checked;
                var showMissing = showAll || view.querySelector('input[name="movieDisplay"][value="Missing"]').checked;

                return Helpers.getMediaRowData(info, showOnServer, showMissing);
            }

            function loadTableData() {
                if (!Helpers.CheckForValidConfig()) {
                    Dashboard.hideLoadingMsg();
                    return;
                }

                var showOnServer = view.querySelector('input[name="movieDisplay"][value="OnServer"]').checked;
                var showMissing = view.querySelector('input[name="movieDisplay"][value="Missing"]').checked;
                var showAll = view.querySelector('input[name="movieDisplay"][value="All"]').checked;

                var url = "Statistics2026/movie_list";
                if (showOnServer) {
                    url += "?showOnServer=true";
                }
                if (showMissing) {
                    url += "?showMissing=true";
                }
                if (showAll) {
                    url += "?showOnServer=true&showMissing=true";
                }
                Helpers.loadTableData(view, 'movie_results', url, getMediaHeader, getMediaRowData, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg);
                view.setAttribute('data-initialized', 'true');
            }

            const radioGroup = view.querySelectorAll('input[name="movieDisplay"]');

            radioGroup.forEach(function (radio) {
                radio.addEventListener("change", function () {
                    view.setAttribute('data-initialized', 'false');
                    loadTableData();
                });
            } );
        };
    }
);
