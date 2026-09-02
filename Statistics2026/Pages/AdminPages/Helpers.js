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

    function getTabIndexEX(tab_name, tabs) {
        var index = 0;
        for (index = 0; index < tabs.length; ++index) {
            var path = tabs[index].href;
            if (path.endsWith2("=" + tab_name)) {
                return index;
            }
        }
        return -1;
    }


    function getTabIndex(tab_name) {
        var tabs = getTabs();
        return getTabIndexEX(tab_name, tabs);
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

        const cssUrl = Dashboard.getConfigurationPageUrl('style.css');
        return injectStyleSheetEX(e, cssUrl);
    }

    function injectStyleSheetEX(e, cssUrl) {

        if (document.getElementById(STYLE_ID))  // already added
            return;

        const link = document.createElement('link');
        link.id = STYLE_ID;
        link.rel = 'stylesheet';
        link.type = 'text/css';
        link.href = cssUrl;

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
        getTabIndexEX,
        getTabIndex,
        getSummaryInfo,
        showInfo,
        getStatistics2026URL,
        injectStyleSheet,
        injectStyleSheetEX,
        calculateProgressClass
    };

})