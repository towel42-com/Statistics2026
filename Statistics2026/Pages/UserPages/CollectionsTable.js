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

        const baseId = 'collection_results';
        const baseUrl = "Statistics2026/collection_list";
        const lastRunArray = ["MediaAnalysis", "CollectionAnalysis"];

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
                return [
                    {
                        title: "Name",
                        field: "Name",
                        sorter: "string",
                        hozAlign: "left",
                        headerFilter: "input",
                        headerFilterPlaceholder: "Filter Collection...",
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
