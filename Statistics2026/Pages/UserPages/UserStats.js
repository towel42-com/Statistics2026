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
        'use strict';

        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                mainTabsManager.setTabs(this, Helpers.getTabIndex("UserStats_UserPage", UserPageHelpers.getTabs), UserPageHelpers.getTabs);
                Helpers.injectStyleSheet(e);

                loadData(view, instance.params.userId);
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            function loadData(view, userId) {
                if (!Helpers.CheckForValidConfig()) {
                    Dashboard.hideLoadingMsg();
                    return;
                }

                ApiClient.getUser(userId).then(function (user) {
                    Helpers.LoadUserStats(view, user.Name, loading.show, loading.hide, Helpers);
                });
            }
        };
    }
);