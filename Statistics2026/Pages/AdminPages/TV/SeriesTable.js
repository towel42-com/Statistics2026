define([
    'mainTabsManager',
    'appRouter',
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_AdminPages_AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_Helpers.js'),
    'emby-linkbutton'
],
    function (mainTabsManager, appRouter, AdminHelpers, Helpers) {
        'use strict';

        const baseId = 'series_results';
        const filterId = 'seriesDisplay';
        const baseUrl = "Statistics2026/series_list";
        const tabName = "Statistics2026_Pages_Series";
        const lastRunArray = ["MediaAnalysis", "SeriesAnalysis"];
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
                <th data-column="PremiereDate" data-type="date" class="info_cell_heading">Premiere Date<span class="sort-icon"></span></th>
                <th data-column="CollectedInfoEp" data-type="progress" class="info_cell_heading">Episode Collection Progress<span class="sort-icon"></span></th>
                <th data-column="CollectedInfoSp" data-type="progress" class="info_cell_heading">Specials Collection Progress<span class="sort-icon"></span></th>
                <th data-column="ServerLocation" data-type="string" class="info_cell_heading">Location on Server<span class="sort-icon"></span></th>
                `;

                if (showStreaming) {
                    retVal += `<th data-column="StreamingServies" data-type="string" class="info_cell_heading">Streaming Services(s)<span class="sort-icon"></span></th>`;
                }

                retVal += "</tr>";
                return retVal;
            }


            function getMediaRowData(info) {
                var retVal = `
                    <td style="text-align:left;" sort-value="${info.DisplayName.SortBy}">${info.DisplayName.Text}</td>
                    <td style="text-align:left;" sort-value="${info.PremiereDate}">${info.PremiereDate}</td>
                    <td style="text-align:left;" sort-value="${info.Extra}">${info.Extra}</td>
                    <td style="text-align:left;" sort-value="${info.Extra2}">${info.Extra2}</td>
                    <td style="text-align:left;" sort-value="${info.ServerLocation.SortBy}">${info.ServerLocation.Text}</td>`
                    ;

                if (showStreaming) {
                    retVal += `<td style="text-align:left;"  sort-value="${info.StreamingProviders.SortBy}">${info.StreamingProviders.Text}</td>`;
                }

                // if (showMissing) {
                //     retVal += `<td style="text-align:left;" sort-value="${info.SearchLocation.SortBy}">${info.SearchLocation.Text}</td>`;
                // }

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
