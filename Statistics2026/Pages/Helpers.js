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

function getDefautColours() {
    return ["#d98880", "#c39bd3", "#7fb3d5", "#76d7c4", "#7dcea0", "#f7dc6f", "#f0b27a", "#d7dbdd", "#85c1e9", "#f1948a"];
}

function getTabs() {
    var tabs = [
        {
            href: Dashboard.getConfigurationPageUrl('Summary'),
            name: 'Summary'
        }
        , {
            href: Dashboard.getConfigurationPageUrl('UserStats'),
            name: 'UserStats'
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

Date.prototype.toDateInputValue = function () {
    var local = new Date(this);
    local.setMinutes(this.getMinutes() - this.getTimezoneOffset());
    return local.toJSON().slice(0, 10);
};

Date.daysBetween = function (date1, date2) {
    //Get 1 day in milliseconds
    var one_day = 1000 * 60 * 60 * 24;

    // Convert both dates to milliseconds
    var date1_ms = date1.getTime();
    var date2_ms = date2.getTime();

    // Calculate the difference in milliseconds
    var difference_ms = date2_ms - date1_ms;

    // Convert back to days and return
    return Math.round(difference_ms / one_day);
};

function getSummaryInfo(view, whichSummary, user, parameters = "", div = "") {
    var urlText = "/emby/Statistics2026/" + whichSummary;
    if (user != "")
        urlText += "/" + user;
    urlText += parameters;
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
        var errorMessage = "getSummaryInfo call failed: '" + error + "' - '" + urlText + "'";
        console.error("API call failed:", errorMessage);
    });

    return `<div name="${div}" id="${div}"></div>`;
}

function showInfo(text, title) {
    Dashboard.alert({ message: text, title: title });
}

