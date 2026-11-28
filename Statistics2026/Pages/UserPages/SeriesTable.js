define([
    'baseView',
    'loading',
    'mainTabsManager',
    ApiClient.getUrl('web/configurationpage?name=Statistics2026_Pages_UserPages_UserHelpers.js'),
    ApiClient.getUrl('web/configurationpage?name=Statistics2026_Pages_Helpers.js'),
    'emby-input',
    'emby-button',
    'emby-checkbox',
    'emby-scroller',
    'emby-select'
],
    function (BaseView, loading, mainTabsManager, UserPageHelpers, Helpers) {
        'use strict';

        const baseId = 'series_results';
        const baseUrl = "Statistics2026/series_list";
        const lastRunArray = ["MediaAnalysis", "SeriesAnalysis"];

        return function (view, params) {
            let table = null;

            view.addEventListener('viewshow', async function (e) {
                Helpers.initTabSystem(view, UserPageHelpers.getTabHierarchy, UserPageHelpers.getRelativeTabPageUrl);

                let configValid = await Helpers.CheckForValidConfig();
                if (!configValid) {
                    view.querySelector(`#lastRunInfo`).style.display = 'none';
                    loading.hide();
                    return;
                }

                if (view.getAttribute('data-initialized') === 'true') {
                    return; // Exit and prevent reload
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
                    loading.hide();
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
