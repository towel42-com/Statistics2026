define(['mainTabsManager', Dashboard.getConfigurationResourceUrl('Helpers.js')], function (mainTabsManager) {
    'use strict';

    const pluginId = "4BFE2894-AEA3-4D3C-A429-503B56D61711";

    function loadPage(view, params) {
        ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            view.querySelector("#showUnknownDVProfileCount").checked = config.showUnknownDVProfileCount;
        });
    }

    return function (view, params) {

        loadPage(view, params);
        

        // init code here
        view.addEventListener('viewshow', function (e) {

            mainTabsManager.setTabs(this, getTabIndex("Settings"), getTabs);
        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });

        view.querySelector("#showUnknownDVProfileCount").addEventListener("click",
            function () {
                ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                    config.showUnknownDVProfileCount = view.querySelector("#showUnknownDVProfileCount").checked;
                    ApiClient.updatePluginConfiguration(pluginId, config);
                });
            }
        );

    };
});

