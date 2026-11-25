define([
    'mainTabsManager',
    'appRouter',
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_AdminPages_AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_Helpers.js'),
    'emby-linkbutton'
],
    function (mainTabsManager, appRouter, AdminHelpers, Helpers) {
        'use strict';

        const baseId = 'collection_results';
        const filterId = 'collectionDisplay';
        const baseUrl = "Statistics2026/collection_list";
        const tabName = "Statistics2026_Pages_AdminPages_Movies_CollectionsTable";
        const lastRunArray = ["MediaAnalysis", "CollectionAnalysis"];
        let showStreaming = true;

        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                Helpers.initTabSystem(view, AdminHelpers.getTabHierarchy, AdminHelpers.getTabs);
                Helpers.injectStyleSheet();

                if (view.getAttribute('data-initialized') === 'true') {
                    return; // Exit and prevent reload
                }

                Helpers.getUserPluginConfig().then(function (config) {
                    Helpers.insertGetLastRunInfo(view, lastRunArray, "lastRunInfo");
                    showStreaming = config.maxNumStreamingProviders != 0;
                    loadTableDataAsync();
                });
            });

            view.addEventListener('viewhide', function (e) {
            });

            view.addEventListener('viewdestroy', function (e) {
            });

            function getMediaHeader() {
                var retVal =
                    `<tr style="text-align: left;">
                        <th data-column="Name" data-type="string" class="info_cell_heading">Name<span class="sort-icon"></span></th>
                        <th data-column="CollectedInfoEp" data-type="progress" class="info_cell_heading">Collection Progress<span class="sort-icon"></span></th>
                    </tr>`;
                return retVal;
            }


            function getMediaRowData(info) {
                var retVal = `
                    <td style="text-align:left;" sort-value="${info.DisplayName.SortBy}">${info.DisplayName.Text}</td>
                    <td style="text-align:left;" sort-value="${info.Extra}">${info.Extra}</td>
                    `;
                return retVal;
            }

            async function loadTableDataAsync() {
                let configValid = await Helpers.CheckForValidConfig();
                if (!configValid) {
                    view.querySelector(`#lastRunInfo`).style.display = 'none';
                    view.querySelector(`#${baseId}_status`).style.display = 'none';
                    Dashboard.hideLoadingMsg();
                    return;
                }

                Helpers.loadTableDataAsync(view, baseId, baseUrl, getMediaHeader, getMediaRowData, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg);
                view.setAttribute('data-initialized', 'true');
            }
        };
    }
);
