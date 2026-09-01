define(['mainTabsManager', 'appRouter', Dashboard.getConfigurationResourceUrl('Helpers.js'), 'emby-linkbutton'], function (mainTabsManager, appRouter, Helpers) {
    'use strict';

    function displayTime(ticks) {
        var ticksInSeconds = ticks / 10000000;
        var hh = Math.floor(ticksInSeconds / 3600);
        var mm = Math.floor((ticksInSeconds % 3600) / 60);
        var ss = Math.floor(ticksInSeconds % 60);

        return pad(hh, 2) + ":" + pad(mm, 2) + ":" + pad(ss, 2);
    }

    function pad(n, width) {
        n = n + '';
        return n.length >= width ? n : new Array(width - n.length + 1).join('0') + n;
    }

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            mainTabsManager.setTabs(this, Helpers.getTabIndex("Movies"), Helpers.getTabs);

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

                var url = "Statistics2026/movie_list";
                url = ApiClient.getUrl(url);

                var load_status = view.querySelector('#movie_results_status');
                load_status.innerHTML = "Loading Data...";

                Helpers.getStatistics20URL(url, this).then(function (videoData) {
                    load_status.innerHTML = "&nbsp;";
                    console.log("videoData: " + JSON.stringify(videoData));

                    var table_body = view.querySelector('#movie_results');
                    var row_html = "";

                    for (var index = 0; index < videoData.length; ++index) {
                        var info = videoData[index];

                        var row_bg_col = "#BBBBBB00";
                        if (index % 2 == 0) {
                            row_bg_col = "#BBBBBB1C";
                        }

                        row_html += "<tr style='background:" + row_bg_col + ";'>";

                            row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.PrimaryName + "</td>";
                            row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='right'>" + info.StartYear + "</td>";
                            row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.ResolutionDetail + "</td>";
                            row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.Codec + "</td>";
                            row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.DolbyVisionProfile + "</td>";
                            row_html += "<td style='vertical-align: middle; white-space: nowrap;' align='left'>" + info.ServerLocation+ "</td>";

                        row_html += "</tr>";
                    }

                    table_body.innerHTML = row_html;

                }, function (response) { load_status.innerHTML = response.status + ":" + response.statusText; });
            }
        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});