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
        const baseUrl = "Statistics2026/collection_list";
        const lastRunArray = ["MediaAnalysis", "CollectionAnalysis"];

        return function (view, params) {
            let table = null;

            view.addEventListener('viewshow', function (e) {
                Helpers.initTabSystem(view, AdminHelpers.getTabHierarchy, AdminHelpers.getTabs);

                if (view.getAttribute('data-initialized') === 'true') {
                    return;
                }

                Helpers.getUserPluginConfig().then(function (config) {
                    Helpers.insertGetLastRunInfo(view, lastRunArray, "lastRunInfo");
                    loadTableData();
                });
            });

            view.addEventListener('viewhide', function (e) {
            });

            view.addEventListener('viewdestroy', function (e) {
                if (table) {
                    table.destroy();
                    table = null;
                }
            });

            function getTabulatorColumns() {
                return [
                    {
                        title: "Name",
                        field: "Name",
                        sorter: "string",
                        hozAlign: "left",
                        tooltip: Helpers.createTextTooltip("DisplayName"),
                        formatter: function (cell) {
                            const data = cell.getRow().getData();
                            return Helpers.formatHtmlWithImages(cell, data.DisplayName ? data.DisplayName.Text : "");
                        }
                    },
                    {
                        title: "Collection Progress",
                        field: "CollectionProgress",
                        sorter: "number",
                        hozAlign: "left",
                        tooltip: Helpers.createTextTooltip("Extra"),
                        formatter: function (cell) {
                            const data = cell.getRow().getData();
                            return data.Extra ? data.Extra : "";
                        }
                    }
                ];
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

                if (!table) {
                    table = Helpers.loadTabulatorTable(
                        view,
                        baseId,
                        baseUrl,
                        getTabulatorColumns()
                    );
                } else {
                    table.setColumns(getTabulatorColumns());
                    table.setData();
                }

                view.setAttribute('data-initialized', 'true');
            }
        };
    }
);
