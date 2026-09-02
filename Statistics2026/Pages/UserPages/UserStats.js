define(['baseView', 'loading', 'mainTabsManager', ApiClient.getUrl('web/configurationpage?name=Helpers.js'), ApiClient.getUrl('web/configurationpage?name=Helpers_UserPage.js'), ApiClient.getUrl('web/configurationPage?name=LoadingHelpers.js'), 'emby-input', 'emby-button', 'emby-checkbox', 'emby-scroller', 'emby-select'], function (BaseView, loading, mainTabsManager, Helpers, UserPageHelpers, LoadingHelpers) {
    'use strict';

    Object.assign(View.prototype, BaseView.prototype);

    function loadData(view, userId) {
        ApiClient.getUser(userId).then(function (user) {
            LoadingHelpers.LoadUserStats(view, user.Name, loading.show, loading.hide, Helpers);
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
            mainTabsManager.setTabs(this, Helpers.getTabIndexEX("UserStats_UserPage", UserPageHelpers.getTabs()), UserPageHelpers.getTabs);
            Helpers.injectStyleSheetEX(e, UserPageHelpers.getConfigPageUrl('style.css'));
        });
}

    return View;
});