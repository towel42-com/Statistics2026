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

        return function (view, params) {
            view.addEventListener('viewshow', async function (e) {
                Helpers.initTabSystem(view, UserPageHelpers.getTabHierarchy, UserPageHelpers.getRelativeTabPageUrl);

                let configValid = await Helpers.CheckForValidConfig();
                if (!configValid) {
                    view.querySelector(`#lastRunInfo`).style.display = 'none';
                    loading.hide();
                    return;
                }

                Helpers.insertGetLastRunInfo(view, ["UserAnalysis", "UserWatchDataAnalysis"], "lastRunInfo");
                loadData(view, params.userId);
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            async function loadData(view, userId) {
                ApiClient.getUser(userId).then(function (user) {
                    Helpers.LoadUserSummaryStats(view, user.Name, loading.show, loading.hide);
                });
            }
        };
    }
);