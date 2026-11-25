define([
    'mainTabsManager',
    'appRouter',
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_AdminPages_AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_Helpers.js'),
    'emby-linkbutton'
],
    function (mainTabsManager, appRouter, AdminHelpers, Helpers) {
        'use strict';

        const baseId = 'movie_results';
        const filterId = 'movieDisplay';
        const baseUrl = "Statistics2026/movie_list";
        const tabName = "Statistics2026_Pages_Movies";
        const lastRunArray = ["MediaAnalysis", "MissingMoviesAnalysis"];
        let showStreaming = true;

        return function (view, params) {
            let movieTable = null;

            view.addEventListener('viewshow', function (e) {
                Helpers.initTabSystem(view, AdminHelpers.getTabHierarchy, AdminHelpers.getTabs);

                if (view.getAttribute('data-initialized') === 'true') {
                    return; // Exit and prevent reload
                }

                Helpers.getUserPluginConfig().then(function (config) {
                    var found = false;
                    config.TableSortEntries.forEach(entry => {
                        if (entry.TableId == baseId) {
                            if (entry.ExtraFilter != null && entry.ExtraFilter != "") {
                                view.querySelector(`input[name="${filterId}"][value="${entry.ExtraFilter}"]`).checked = true;
                                found = true;
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
                if (movieTable) {
                    movieTable.destroy();
                    movieTable = null;
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
                return Helpers.getMediaTabulatorColumns("Collection", flags.showOnServer, flags.showMissing, showStreaming);
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
                    Dashboard.hideLoadingMsg();
                    return;
                }

                const container = view.querySelector(`#${baseId}_table`);

                if (!movieTable) {
                    movieTable = Helpers.loadTabulatorTable(
                        view,
                        baseId,
                        baseUrl,
                        getTabulatorColumns(),
                        getExtraQueryParams
                    );
                } else {
                    movieTable.setColumns(getTabulatorColumns());
                    movieTable.setData();
                }

                view.setAttribute('data-initialized', 'true');
            }

            // function getMediaHeader() {
            //     var showAll = view.querySelector(`input[name="${filterId}"][value="All"]`).checked;
            //     var showOnServer = showAll || view.querySelector(`input[name="${filterId}"][value="OnServer"]`).checked;
            //     var showMissing = showAll || view.querySelector(`input[name="${filterId}"][value="Missing"]`).checked;

            //     return Helpers.getMediaHeader("Collection", showOnServer, showMissing, showStreaming);
            // }

            // function getMediaRowData(info) {
            //     var showAll = view.querySelector(`input[name="${filterId}"][value="All"]`).checked;
            //     var showOnServer = showAll || view.querySelector(`input[name="${filterId}"][value="OnServer"]`).checked;
            //     var showMissing = showAll || view.querySelector(`input[name="${filterId}"][value="Missing"]`).checked;

            //     return Helpers.getMediaRowData(info, showOnServer, showMissing, showStreaming);
            // }

            // async function loadTableData() {
            //     let configValid = await Helpers.CheckForValidConfig();
            //     if (!configValid) {
            //         view.querySelector(`#lastRunInfo`).style.display = 'none';
            //         view.querySelector(`#${baseId}_status`).style.display = 'none';
            //         Dashboard.hideLoadingMsg();
            //         return;
            //     }

            //     var showOnServer = view.querySelector(`input[name="${filterId}"][value="OnServer"]`).checked;
            //     var showMissing = view.querySelector(`input[name="${filterId}"][value="Missing"]`).checked;
            //     var showAll = view.querySelector(`input[name="${filterId}"][value="All"]`).checked;

            //     var url = baseUrl;
            //     if (showOnServer) {
            //         url += "?showOnServer=true";
            //     }
            //     if (showMissing) {
            //         url += "?showMissing=true";
            //     }
            //     if (showAll) {
            //         url += "?showOnServer=true&showMissing=true";
            //     }

            //     Helpers.loadTableData(view, baseId, url, getMediaHeader, getMediaRowData, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg);
            //     view.setAttribute('data-initialized', 'true');
            // }

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
