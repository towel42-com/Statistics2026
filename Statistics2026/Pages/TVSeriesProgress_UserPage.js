define(['baseView', 'loading', 'mainTabsManager', ApiClient.getUrl('web/configurationpage?name=Helpers.js'), ApiClient.getUrl('web/configurationpage?name=Helpers_UserPage.js'), 'emby-input', 'emby-button', 'emby-checkbox', 'emby-scroller', 'emby-select'], function (BaseView, loading, mainTabsManager, Helpers, UserPageHelpers) {
    `use strict`;

    Object.assign(View.prototype, BaseView.prototype);

    function loadData(view, userId) {
        loading.show();
        ApiClient.getUser(userId).then(function (user) {
            view.querySelector("#UserTitle").innerHTML = "TV Series Progress for " + user.Name;

            var url = "Statistics2026/tv_series_progress/" + user.Name;
            url = ApiClient.getUrl(url);

            var load_status = view.querySelector('#TVSeriesProgressStatus');
            load_status.innerHTML = "Loading Data...";

            Helpers.getStatistics2026URL(url).then(function (tvSeriesProgressData) {
                load_status.innerHTML = "&nbsp;";
                console.log("tvSeriesProgressData: " + JSON.stringify(tvSeriesProgressData));

                var table_body = view.querySelector('#TVSeriesProgressTable_results');
                var row_html = "";

                for (var index = 0; index < tvSeriesProgressData.length; ++index) {
                    var info = tvSeriesProgressData[index];

                    var row_bg_col = "#BBBBBB00";
                    if (index % 2 == 0) {
                        row_bg_col = "#BBBBBB1C";
                    }

                    row_html += "<tr style='background:" + row_bg_col + ";'>";

                    row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.Name + " (" + info.PremiereYear + ")</td>";
                    row_html += "<td class='center " + Helpers.calculateProgressClass(info.Episodes.Percent) + "' style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.Episodes.String + "</td>";
                    row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.ScoreStr + "/10</td>";
                    row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.SeriesStatus + "</td>";

                    row_html += "</tr>";
                }

                table_body.innerHTML = row_html;
            });
        });
        loading.hide();
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
