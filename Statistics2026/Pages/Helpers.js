/*
Copyright(C) 2018

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see<http://www.gnu.org/licenses/>.
*/

define(function () {
    const pluginId = "23ADB024-F759-438F-B9A7-D5912A75596C";

    getStatistics2026URL = function (url_to_get) {
        console.log("getStatistics2026URL Url = " + url_to_get);
        return ApiClient.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };

    function getTabs() {
        var tabs = [
            {
                href: Dashboard.getConfigurationPageUrl('Summary'),
                name: 'Summary'
            }
            , {
                href: Dashboard.getConfigurationPageUrl('UserStats'),
                name: 'User Stats'
            }
            , {
                href: Dashboard.getConfigurationPageUrl('TVSeriesProgress'),
                name: 'TV Series Progress'
            }
            , {
                href: Dashboard.getConfigurationPageUrl('Episodes'),
                name: 'Episodes'
            }
            , {
                href: Dashboard.getConfigurationPageUrl('Movies'),
                name: 'Movies'
            }
            , {
                href: Dashboard.getConfigurationPageUrl('Settings'),
                name: 'Settings'
            }
        ];
        return tabs;
    }

    if (!String.prototype.endsWith2) {
        String.prototype.endsWith2 = function (searchString, position) {
            var subjectString = this.toString();
            if (typeof position !== 'number' || !isFinite(position) || Math.floor(position) !== position || position > subjectString.length) {
                position = subjectString.length;
            }
            position -= searchString.length;
            var lastIndex = subjectString.indexOf(searchString, position);
            return lastIndex !== -1 && lastIndex === position;
        };
    }


    function getTabIndex(tab_name) {
        var tabs = getTabs();
        var index = 0;
        for (index = 0; index < tabs.length; ++index) {
            var path = tabs[index].href;
            if (path.endsWith2("=" + tab_name)) {
                return index;
            }
        }
        return -1;
    }

    function getSummaryInfo(view, whichSummary, user, parameters = "", div = "") {
        var urlText = "/emby/Statistics2026/" + whichSummary;
        if (user != "")
            urlText += "/" + user;
        urlText += parameters;
        console.info("getSummaryInfo - '" + urlText + "'");
        var url = ApiClient.getUrl(urlText);

        if (div == "")
            div = whichSummary;

        ApiClient.getJSON(url).then(response => {
            view.querySelector("#" + div).innerHTML = response.html;

            response.dynamicButtons.forEach((v) => {
                view.querySelector("#" + v.id).addEventListener("click",
                    function () {
                        showInfo(v.info, v.title);
                    });
            });
        }).catch(error => {
            var errorMessage = "'" + error + "' - '" + div + "' - '" + urlText + "'";
            console.error("getSummaryInfo failed:", errorMessage);
        });

        return `<div name="${div}" id="${div}"></div>`;
    }

    function showInfo(text, title) {
        Dashboard.alert({ message: text, title: title });
    }

    const STYLE_ID = 'my-plugin-stylesheet';
    function injectStyleSheet(e) {

        if (document.getElementById(STYLE_ID))  // already added
            return;

        const link = document.createElement('link');
        link.id = STYLE_ID;
        link.rel = 'stylesheet';
        link.type = 'text/css';
        link.href = Dashboard.getConfigurationPageUrl('style.css')

        document.head.appendChild(link);
    }

    function calculateProgressClass(value) {
        if (value == 0)
            return ``;
        else if (value < 40)
            return `progress-20`;
        else if (value < 60)
            return `progress-40`;
        else if (value < 80)
            return `progress-60`;
        else if (value < 100)
            return `progress-80`;
        else
            return `progress-100`;
    };


    return {
        pluginId,
        getTabs,
        getTabIndex,
        getSummaryInfo,
        showInfo,
        getStatistics2026URL,
        injectStyleSheet,
        calculateProgressClass
    };

})