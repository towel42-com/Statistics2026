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
                Helpers.injectStyleSheet(e);
                Helpers.injectSortableTableStyle(document);

                view.querySelector("#episodesInfo").addEventListener(`click`, function () {
                    Helpers.showInfo('This column displays the number of watched episodes and the number of total episodes. You will have 100% when you viewed all normal episodes (no specials, only aired)<br/>. ', 'Watched Episodes');
                });

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
                    Helpers.LoadTVProgress(view, user.Name, loading.show, loading.hide, Helpers);
                });
            }
        };
    }
);
