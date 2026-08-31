define(['mainTabsManager', 'appRouter', Dashboard.getConfigurationResourceUrl('Helpers.js'), 'emby-linkbutton'], function (mainTabsManager, appRouter, Helpers) {
    'use strict';

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            mainTabsManager.setTabs(this, Helpers.getTabIndex("Episodes"), Helpers.getTabs);

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

                var url = "Statistics2026/episode_list";
                url = ApiClient.getUrl(url);

                var load_status = view.querySelector('#episode_results_status');
                load_status.innerHTML = "Loading Data...";

                Helpers.getStatistics2026URL(url).then(function (videoData) {
                    load_status.innerHTML = "&nbsp;";
                    console.log("videoData: " + JSON.stringify(videoData));

                    var table_body = view.querySelector('#episode_results');
                    var row_html = "";

                    for (var index = 0; index < videoData.length; ++index) {
                        var info = videoData[index];

                        var row_bg_col = "#BBBBBB00";
                        if (index % 2 == 0) {
                            row_bg_col = "#BBBBBB1C";
                        }

                        row_html += "<tr style='background:" + row_bg_col + ";'>";

                            var episodeName = info.PrimaryName + " - S" + String(info.Season).padStart(2, '0') + "E" + String(info.Episode).padStart(2, '0') + " - " + info.SecondaryName;
                            row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + episodeName + "</td>";
                            row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='right'>" + info.StartYear + "</td>";
                            row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.ResolutionDetail + "</td>";
                            row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.Codec + "</td>";
                            row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.DolbyVisionProfile + "</td>";
                            row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.ServerLocation+ "</td>";

                        row_html += "</tr>";
                    }

                    table_body.innerHTML = row_html;

                },
                function (response) {
                    load_status.innerHTML = response.status + ":" + response.statusText;
                });
            }
        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});