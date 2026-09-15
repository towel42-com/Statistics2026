define([
    'baseView',
    'loading',
    'mainTabsManager',
    ApiClient.getUrl('web/configurationpage?name=Helpers.js'),
    ApiClient.getUrl('web/configurationpage?name=Helpers_UserPage.js'),
    ApiClient.getUrl('web/configurationpage?name=SharedHelpers.js'),
    'emby-input',
    'emby-button',
    'emby-checkbox',
    'emby-scroller',
    'emby-select'
],
    function (BaseView, loading, mainTabsManager, Helpers, UserPageHelpers, SharedHelpers) {
        `use strict`;

        Object.assign(View.prototype, BaseView.prototype);

        function loadData(view, userId) {
            ApiClient.getUser(userId).then(function (user) {
                SharedHelpers.LoadTVProgress(view, user.Name, loading.show, loading.hide, SharedHelpers);
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
                        SharedHelpers.sortTable(index, columnType, 'TVSeriesProgressTable');
                    });
                });
            }
        };

        function View(view, params) {
            BaseView.apply(this, arguments);

            view.addEventListener('viewshow', function (e) {
                mainTabsManager.setTabs(this, SharedHelpers.getTabIndexEX("TVSeriesProgress_UserPage", UserPageHelpers.getTabs), UserPageHelpers.getTabs);

                SharedHelpers.injectStyleSheetEX(e, UserPageHelpers.getConfigPageUrl('style.css'));
                var style = document.createElement('style');
                style.innerHTML = SharedHelpers.sortableTableStyle();
                var ref = document.querySelector('script');
                ref.parentNode.insertBefore(style, ref);

            });

            view.querySelector("#episodesInfo").addEventListener(`click`, function () {
                Helpers.showInfo('This column displays the number of watched episodes and the number of total episodes. You will have 100% when you viewed all normal episodes (no specials, only aired)<br/>. ', 'Watched Episodes');
            });
        }

        return View;
    });
