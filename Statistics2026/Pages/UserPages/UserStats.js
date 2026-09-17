define([
    'baseView',
    'loading',
    'mainTabsManager',
    ApiClient.getUrl('web/configurationpage?name=UserPageHelpers.js'),
    ApiClient.getUrl('web/configurationPage?name=Helpers.js'),
    'emby-input',
    'emby-button',
    'emby-checkbox',
    'emby-scroller',
    'emby-select'
],
    function (BaseView, loading, mainTabsManager, UserPageHelpers, Helpers) {
        'use strict';

        Object.assign(View.prototype, BaseView.prototype);

        function loadData(view, userId) {
            if (!Helpers.CheckForValidConfig()) {
                Dashboard.hideLoadingMsg();
                return;
            }

            ApiClient.getUser(userId).then(function (user) {
                Helpers.LoadUserStats(view, user.Name, loading.show, loading.hide, Helpers);
            });
        }


        View.prototype.onResume = function (options) {
            BaseView.prototype.onResume.apply(this, arguments);

            if (options.refresh) {
                var view = this.view;
                var instance = this;

                loadData(view, instance.params.userId);
            }
        };

        function View(view, params) {
            BaseView.apply(this, arguments);

            view.addEventListener('viewshow', function (e) {
                mainTabsManager.setTabs(this, Helpers.getTabIndex("UserStats_UserPage", UserPageHelpers.getTabs), UserPageHelpers.getTabs);
                Helpers.injectStyleSheet(e);
            });
        }

        return View;
    }
);