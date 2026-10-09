define([
    'mainTabsManager',
    'appRouter',
    Dashboard.getConfigurationResourceUrl('AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Helpers.js'),
    'emby-linkbutton'
],
    function (mainTabsManager, appRouter, AdminHelpers, Helpers) {
        'use strict';

        const baseId = 'episode_results';
        const filterId = 'episodeDisplay';
        const baseUrl = "Statistics2026/episode_list";
        const tabName = "Episodes";
        const lastRunArray =  ["MediaAnalysis", "MissingEpisodesAnalysis"];

        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                mainTabsManager.setTabs(this, Helpers.getTabIndex(tabName, AdminHelpers.getTabs), AdminHelpers.getTabs);
                Helpers.injectStyleSheet();
                Helpers.injectSortableTableStyle(document);

                if (view.getAttribute('data-initialized') === 'true') {
                    return; // Exit and prevent reload
                }

                ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                    var found = false;
                    config.SortEntries.forEach(entry => {
                        if (entry.TableId == baseId) {
                            if (entry.ExtraFilter != null && entry.ExtraFilter != "") {
                                view.querySelector(`input[name="${filterId}"][value="${entry.ExtraFilter}"]`).checked = true;
                                found = true;
                            }
                        }
                    });

                    if (found == false) {
                        view.querySelector(`input[name="${filterId}"][value="All"]`).checked = true;
                    }

                    Helpers.getLastRunInfo(view, lastRunArray, "lastRunInfo");
                    loadTableData();
                });
            });

            view.addEventListener('viewhide', function (e) {
            });

            view.addEventListener('viewdestroy', function (e) {
            });

            function getMediaHeader() {
                var showAll = view.querySelector(`input[name="${filterId}"][value="All"]`).checked;
                var showOnServer = showAll || view.querySelector(`input[name="${filterId}"][value="OnServer"]`).checked;
                var showMissing = showAll || view.querySelector(`input[name="${filterId}"][value="Missing"]`).checked;

                return Helpers.getMediaHeader("Series", showOnServer, showMissing);
            }

            function getMediaRowData(info) {
                var showAll = view.querySelector(`input[name="${filterId}"][value="All"]`).checked;
                var showOnServer = showAll || view.querySelector(`input[name="${filterId}"][value="OnServer"]`).checked;
                var showMissing = showAll || view.querySelector(`input[name="${filterId}"][value="Missing"]`).checked;

                return Helpers.getMediaRowData(info, showOnServer, showMissing);
            }

            function loadTableData() {
                if (!Helpers.CheckForValidConfig()) {
                    Dashboard.hideLoadingMsg();
                    return;
                }

                var showOnServer = view.querySelector(`input[name="${filterId}"][value="OnServer"]`).checked;
                var showMissing = view.querySelector(`input[name="${filterId}"][value="Missing"]`).checked;
                var showAll = view.querySelector(`input[name="${filterId}"][value="All"]`).checked;

                var url = baseUrl;
                if (showOnServer) {
                    url += "?showOnServer=true";
                }
                if (showMissing) {
                    url += "?showMissing=true";
                }
                if (showAll) {
                    url += "?showOnServer=true&showMissing=true";
                }

                Helpers.loadTableData(view, baseId, url, getMediaHeader, getMediaRowData, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg);
                view.setAttribute('data-initialized', 'true');
            }

            const radioGroup = view.querySelectorAll(`input[name="${filterId}"]`);

            radioGroup.forEach(function (radio) {
                radio.addEventListener("change", function () {
                    view.setAttribute('data-initialized', 'false');

                    ApiClient.getPluginConfiguration(Helpers.pluginId).then(function (config) {
                        config.SortEntries.forEach(entry => {
                            if (entry.TableId == baseId) {
                                entry.ExtraFilter = radio.value;
                                ApiClient.updatePluginConfiguration(Helpers.pluginId, config);
                            }
                        });
                    });

                    loadTableData();
                });
            });
        };
    }
);
