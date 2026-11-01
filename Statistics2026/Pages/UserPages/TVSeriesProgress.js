define([
    'baseView',
    'loading',
    'mainTabsManager',
    ApiClient.getUrl('web/configurationpage?name=UserPageHelpers.js'),
    ApiClient.getUrl('web/configurationpage?name=Helpers.js'),
    'emby-input',
    'emby-button',
    'emby-checkbox',
    'emby-scroller',
    'emby-select'
],
    function (BaseView, loading, mainTabsManager, UserPageHelpers, Helpers) {
        `use strict`;

        return function (view, params) {

            view.addEventListener('viewshow', function (e) {
                mainTabsManager.setTabs(this, Helpers.getTabIndex("TVSeriesProgress_UserPage", UserPageHelpers.getTabs), UserPageHelpers.getTabs);
                Helpers.injectStyleSheet();

                Helpers.insertGetLastRunInfo(view, ["UserWatchDataAnalysis"], "lastRunInfo");
                loadData(view, params.userId);
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            async function loadData(view, userId) {
                let configValid = await Helpers.CheckForValidConfig();
                if (!configValid) {
                    view.querySelector(`#lastRunInfo`).style.display = 'none';
                    view.querySelector(`#TVSeriesProgress_status`).style.display = 'none';
                    loading.hide;
                    return;
                }

                ApiClient.getUser(userId).then(function (user) {
                    Helpers.LoadTVProgress(view, user.Name, loading.show, loading.hide);
                });
            }
        };
    }
);
