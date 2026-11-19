define(function () {
    function getTabs() {
        var tabs = [
            {
                href: getTabPageUrl('Stats2026_UserStats_UserPage'),
                name: 'User Stats'
            },
            {
                href: getTabPageUrl('Stats2026_TVSeriesProgress_UserPage'),
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