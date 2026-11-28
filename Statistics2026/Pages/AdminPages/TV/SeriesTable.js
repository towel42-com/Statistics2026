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
        const baseUrl = "Statistics2026/series_list";
        const lastRunArray = ["MediaAnalysis", "SeriesAnalysis"];
        let showStreaming = true;

        return function (view, params) {
            let seriesTable = null;

            view.addEventListener('viewshow', function (e) {
                Helpers.initTabSystem(view, AdminHelpers.getTabHierarchy, Dashboard.getConfigurationPageUrl);

                if (view.getAttribute('data-initialized') === 'true') {
                    return;
                }

                Helpers.getUserPluginConfig().then(function (config) {
                    Helpers.insertGetLastRunInfo(view, lastRunArray, "lastRunInfo");
                    showStreaming = config.maxNumStreamingProviders != 0;
                    loadTableData();
                });
            });

            view.addEventListener('viewhide', function (e) {
            });

            view.addEventListener('viewdestroy', function (e) {
                if (seriesTable) {
                    seriesTable.destroy();
                    seriesTable = null;
                }
            });

            function getTabulatorColumns() {
                const columns = [
                    {
                        title: "Name",
                        field: "Name",
                        sorter: "string",
                        hozAlign: "left",
                        headerFilter: "input",
                        headerFilterPlaceholder: "Filter Series...",
                        tooltip: Helpers.createTextTooltip("DisplayName"),
                        formatter: function (cell) {
                            const data = cell.getRow().getData();
                            return data.DisplayName ? data.DisplayName.Text : "";
                        }
                    },
                    {
                        title: "Premiere Date",
                        field: "PremiereDate",
                        sorter: "date",
                        hozAlign: "left",
                        tooltip: Helpers.createTextTooltip("PremiereDate"),
                        formatter: function (cell) {
                            return cell.getRow().getData().PremiereDate || "";
                        }
                    },
                    {
                        title: "Episode Collection Progress",
                        field: "CollectedInfoEp",
                        sorter: "number",
                        hozAlign: "left",
                        tooltip: Helpers.createTextTooltip("Extra"),
                        formatter: function (cell) {
                            return cell.getRow().getData().Extra || "";
                        }
                    },
                    {
                        title: "Specials Collection Progress",
                        field: "CollectedInfoSp",
                        sorter: "number",
                        hozAlign: "left",
                        tooltip: Helpers.createTextTooltip("Extra2"),
                        formatter: function (cell) {
                            return cell.getRow().getData().Extra2 || "";
                        }
                    }
                ];

                if (showStreaming) {
                    columns.push({
                        title: "Streaming Services(s)",
                        field: "StreamingServies",
                        sorter: "string",
                        hozAlign: "left",
                        variableHeight: true,
                        tooltip: Helpers.createTextTooltip("StreamingProviders"),
                        formatter: function (cell) {
                            const data = cell.getRow().getData();
                            return Helpers.formatHtmlWithImages(cell, data.StreamingProviders ? data.StreamingProviders.Text : "");
                        }
                    });
                }

                columns.push(
                    {
                        title: "Location on Server",
                        field: "ServerLocation",
                        sorter: "string",
                        hozAlign: "left",
                        headerFilter: "input",
                        headerFilterPlaceholder: "Filter location...",
                        tooltip: Helpers.createTextTooltip("ServerLocation"),
                        formatter: function (cell) {
                            const data = cell.getRow().getData();
                            return data.ServerLocation ? data.ServerLocation.Text : "";
                        }
                    });

                return columns;
            }

            async function loadTableData() {
                const configValid = await Helpers.CheckForValidConfig();
                if (!configValid) {
                    view.querySelector(`#lastRunInfo`).style.display = 'none';
                    const statusEl = view.querySelector(`#${baseId}_status`);
                    if (statusEl) {
                        statusEl.style.display = 'none';
                    }
                    Dashboard.hideLoadingMsg();
                    return;
                }

                if (!seriesTable) {
                    seriesTable = Helpers.loadTabulatorTable(
                        view,
                        baseId,
                        baseUrl,
                        getTabulatorColumns()
                    );
                } else {
                    seriesTable.setColumns(getTabulatorColumns());
                    seriesTable.setData();
                }

                view.setAttribute('data-initialized', 'true');
            }
        };
    }
);
