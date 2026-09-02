define(function () {
    const pluginId = "23ADB024-F759-438F-B9A7-D5912A75596C";

    function getTabs() {
        var tabs = [
            {
                href: getTabPageUrl('UserStats_UserPage'),
                name: 'User Stats'
            },
            {
                href: getTabPageUrl('TVSeriesProgress_UserPage'),
                name: 'TV Series Progress'
            }

        ];
        return tabs;
    }

    function getTabPageUrl(pageName) {
        var url = ApiClient.getUrl('configurationpage?name=' + pageName + '&userId=' + ApiClient.getCurrentUserId());
        url = url.replace("/emby", "");
        return url;
    }

    function getConfigPageUrl(pageName) {
        return ApiClient.getUrl('web/configurationpage?name=' + pageName + '&userId=' + ApiClient.getCurrentUserId());
    }

    return {
        getTabs,
        getConfigPageUrl
    };

})