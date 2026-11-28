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

        const baseId = 'movie_results';
        const filterId = 'movieDisplay';
        const baseUrl = "Statistics2026/movie_list";
        const lastRunArray = ["MediaAnalysis", "MissingMoviesAnalysis"];
        let showStreaming = true;

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
                    let found = false;
                    config.TableSortEntries.forEach(entry => {
                        if (entry.TableId == baseId) {
                            if (entry.ExtraFilter != null && entry.ExtraFilter != "") {
                                const radio = view.querySelector(`input[name="${filterId}"][value="${entry.ExtraFilter}"]`);
                                if (radio) {
                                    radio.checked = true;
                                    found = true;
                                }
                            }
                        }
                    });

                    if (!found) {
                        view.querySelector(`input[name="${filterId}"][value="All"]`).checked = true;
                    }

                    Helpers.insertGetLastRunInfo(view, lastRunArray, "lastRunInfo");
                    showStreaming = config.maxNumStreamingProviders != 0;
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

            function getFilterFlags() {
                const showAll = view.querySelector(`input[name="${filterId}"][value="All"]`).checked;
                const showOnServer = showAll || view.querySelector(`input[name="${filterId}"][value="OnServer"]`).checked;
                const showMissing = showAll || view.querySelector(`input[name="${filterId}"][value="Missing"]`).checked;
                return { showAll, showOnServer, showMissing };
            }

            function getTabulatorColumns() {
                const flags = getFilterFlags();
                return Helpers.getMediaTabulatorColumns("Collection", flags.showOnServer, flags.showMissing, showStreaming, true);
            }

            function getExtraQueryParams() {
                const flags = getFilterFlags();
                const queryParams = {};
                if (flags.showOnServer) {
                    queryParams.ShowOnServer = true;
                }
                if (flags.showMissing) {
                    queryParams.ShowMissing = true;
                }
                return queryParams;
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
                        getTabulatorColumns(),
                        getExtraQueryParams
                    );
                } else {
                    table.setColumns(getTabulatorColumns());
                    table.setData();
                }

                view.setAttribute('data-initialized', 'true');
            }

            const radioGroup = view.querySelectorAll(`input[name="${filterId}"]`);

            radioGroup.forEach(function (radio) {
                radio.addEventListener("change", function () {
                    view.setAttribute('data-initialized', 'false');

                    Helpers.getUserPluginConfig().then(function (config) {
                        let found = false;

                        config.TableSortEntries.forEach(entry => {
                            if (entry.TableId == baseId) {
                                entry.ExtraFilter = radio.value;
                                found = true;
                            }
                        });

                        if (!found) {
                            config.TableSortEntries.push({
                                TableId: baseId,
                                ExtraFilter: radio.value
                            });
                        }
                        Helpers.updateUserPluginConfiguration(config);
                    });

                    loadTableData();
                });
            });
        };
    }
);
