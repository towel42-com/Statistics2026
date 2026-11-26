define([
    'mainTabsManager',
    'appRouter',
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_AdminPages_AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Statistics2026_Pages_Helpers.js'),
    'emby-linkbutton'
],
    function (mainTabsManager, appRouter, AdminHelpers, Helpers) {
        'use strict';

        const baseId = 'episode_results';
        const filterId = 'episodeDisplay';
        const baseUrl = "Statistics2026/episode_list";
        const lastRunArray = ["MediaAnalysis", "MissingEpisodesAnalysis", "NotifyAvailableToday"];
        let showStreaming = true;

        return function (view, params) {
            let episodeTable = null;

            view.addEventListener('viewshow', function (e) {
                Helpers.initTabSystem(view, AdminHelpers.getTabHierarchy, AdminHelpers.getTabs);

                if (view.getAttribute('data-initialized') === 'true') {
                    return;
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
                if (episodeTable) {
                    episodeTable.destroy();
                    episodeTable = null;
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
                return Helpers.getMediaTabulatorColumns("Series", flags.showOnServer, flags.showMissing, showStreaming);
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

                if (!episodeTable) {
                    episodeTable = Helpers.loadTabulatorTable(
                        view,
                        baseId,
                        baseUrl,
                        getTabulatorColumns(),
                        getExtraQueryParams
                    );
                } else {
                    episodeTable.setColumns(getTabulatorColumns());
                    episodeTable.setData();
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
