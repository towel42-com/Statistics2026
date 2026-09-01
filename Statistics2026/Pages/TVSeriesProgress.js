define(['mainTabsManager', Dashboard.getConfigurationResourceUrl('Helpers.js')], function (mainTabsManager, Helpers) {
    `use strict`;

    function loadStats(view, user) {
        Dashboard.showLoadingMsg();

        ApiClient.getPluginConfiguration(Helpers.pluginId).then( function (config) {
            if (config.LastUpdated === undefined) {
                Dashboard.alert({
                    message:
                        "No configuration found, please run the 'Statistics 2026' task on the Scheduled Tasks page and come back for the results."
                });

                view.querySelector(`#lastRunInfo`).style.display = 'none';

                Dashboard.hideLoadingMsg();
                return;
            }
            var url = "Statistics2026/tv_series_progress/" + user;
            url = ApiClient.getUrl(url);
            console.log("tvSeriesProgressData: Url: " + url);

            view.querySelector("#UserTitle").innerHTML = "TV Series Progress for " + user;

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

                    row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.Name + " (" + info.PremiereYear  + ")</td>";
                    row_html += "<td class='center " + Helpers.calculateProgressClass(info.Episodes.Percent) + "' style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.Episodes.String + "</td>";
                    row_html += "<td class='center " + Helpers.calculateProgressClass(info.Specials.Percent) + "' style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.Specials.String + "</td>";
                    row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.ScoreStr + "/10</td>";
                    row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.SeriesStatus + "</td>";

                    row_html += "</tr>";
                }

                table_body.innerHTML = row_html;
                Dashboard.hideLoadingMsg();
            } );
        } );
    }

    return function (view, params) {
        view.addEventListener('viewshow', function (e) {
            mainTabsManager.setTabs(this, Helpers.getTabIndex("TVSeriesProgress"), Helpers.getTabs);
            Helpers.injectStyleSheet(e);

            var style = document.createElement('style');
            style.innerHTML =
                '.tooltip {position: relative;display: inline-block;border-bottom: 1px dotted black;} ' +
                '.tooltip .tooltiptext {visibility: hidden; background-color: black; color: #fff; border-radius: 6px; padding: 5px 0; position: absolute;z-index: 1;} ' +
                '.tooltip:hover .tooltiptext {visibility: visible;} ' +
                '.info_cell {white-space: nowrap; padding-left:45px; padding-right:20px; font-size:smaller;}' +
                '.info_cell_heading {white-space: nowrap; padding-left:20px; padding-right:20px;font-size:smaller;}';
            var ref = document.querySelector('script');
            ref.parentNode.insertBefore(style, ref);

            process_click();

            function process_click() {
                const selectElement = document.getElementById("selectUser");
                const user = selectElement.options[selectElement.selectedIndex].text;
                loadStats(view, user)
            }
        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });

        view.querySelector("#selectUser").addEventListener(`change`, function () {
            const user = this.options[this.selectedIndex].text;
            loadStats(view, user);
        });

        view.querySelector("#episodesInfo").addEventListener(`click`, function () {
            Helpers.showInfo('This column displays the number of watched episodes and the number of total episodes. You will have 100% when you viewed all normal episodes (no specials, only aired)<br/>. ', 'Watched Episodes');
        });
        view.querySelector("#specialsInfo").addEventListener(`click`, function () {
            Helpers.showInfo('This column displays the number of watched specials and the number of total specials. You will have 100% when you viewed all specials<br/>. ', 'Watched Specials');
        });

        ApiClient.getUsers().then(function (users) {
            var select = view.querySelector(`#selectUser`);

            loadStats(view, users[0].Name);

            users.forEach((user) => {
                var option = document.createElement(`option`);
                option.value = user.Id;
                option.innerHTML = user.Name;
                select.appendChild(option);
            });
        });
    }
});
