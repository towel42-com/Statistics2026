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

        Object.assign(View.prototype, BaseView.prototype);

        function loadData(view, userId) {
            ApiClient.getUser(userId).then(function (user) {
                Helpers.LoadTVProgress(view, user.Name, loading.show, loading.hide, Helpers);
            });
        }

        View.prototype.onResume = function (options) {
            BaseView.prototype.onResume.apply(this, arguments);

            if (options.refresh) {
                var view = this.view;
                var instance = this;

                loadData(view, instance.params.userId);

                document.querySelectorAll('#TVSeriesProgressTable thead th').forEach((header, index) => {
                    header.addEventListener('click', () => {
                        // const columnName = header.getAttribute('data-column');
                        const columnType = header.getAttribute('data-type');

                        console.log(`Sorting index: ${index}, Column Type: ${columnType}`);

                        // Pass these variables straight into your sort function
                        Helpers.sortTable(index, columnType, 'TVSeriesProgressTable');
                    });
                });
            }
        };

        function View(view, params) {
            BaseView.apply(this, arguments);

            view.addEventListener('viewshow', function (e) {
                mainTabsManager.setTabs(this, Helpers.getTabIndex("TVSeriesProgress_UserPage", UserPageHelpers.getTabs), UserPageHelpers.getTabs);
                Helpers.injectStyleSheet(e);
                Helpers.injectSortableTableStyle(document);
            });

            view.querySelector("#episodesInfo").addEventListener(`click`, function () {
                Helpers.showInfo('This column displays the number of watched episodes and the number of total episodes. You will have 100% when you viewed all normal episodes (no specials, only aired)<br/>. ', 'Watched Episodes');
            });
        }

        return View;
    }
);
