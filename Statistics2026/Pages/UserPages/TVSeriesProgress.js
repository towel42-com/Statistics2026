define(['baseView', 'loading', 'mainTabsManager', ApiClient.getUrl('web/configurationpage?name=Helpers.js'), ApiClient.getUrl('web/configurationpage?name=Helpers_UserPage.js'), ApiClient.getUrl('web/configurationpage?name=LoadingHelpers.js'), 'emby-input', 'emby-button', 'emby-checkbox', 'emby-scroller', 'emby-select'], function (BaseView, loading, mainTabsManager, Helpers, UserPageHelpers, LoadingHelpers) {
    `use strict`;

    Object.assign(View.prototype, BaseView.prototype);

    function loadData(view, userId) {
        ApiClient.getUser(userId).then(function (user) {
            LoadingHelpers.LoadTVProgress( view, user.Name, loading.show, loading.hide, Helpers );
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
            mainTabsManager.setTabs(this, Helpers.getTabIndexEX("TVSeriesProgress_UserPage", UserPageHelpers.getTabs()), UserPageHelpers.getTabs);

            Helpers.injectStyleSheetEX(e, UserPageHelpers.getConfigPageUrl('style.css'));
            var style = document.createElement('style');
            style.innerHTML =
                '.tooltip {position: relative;display: inline-block;border-bottom: 1px dotted black;} ' +
                '.tooltip .tooltiptext {visibility: hidden; background-color: black; color: #fff; border-radius: 6px; padding: 5px 0; position: absolute;z-index: 1;} ' +
                '.tooltip:hover .tooltiptext {visibility: visible;} ' +
                '.info_cell {white-space: nowrap; padding-left:45px; padding-right:20px; font-size:smaller;}' +
                '.info_cell_heading {white-space: nowrap; padding-left:20px; padding-right:20px;font-size:smaller;}';
            var ref = document.querySelector('script');
            ref.parentNode.insertBefore(style, ref);
        });

        view.querySelector("#episodesInfo").addEventListener(`click`, function () {
            Helpers.showInfo('This column displays the number of watched episodes and the number of total episodes. You will have 100% when you viewed all normal episodes (no specials, only aired)<br/>. ', 'Watched Episodes');
        });
    }

    return View;
});
