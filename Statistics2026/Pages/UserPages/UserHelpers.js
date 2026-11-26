define(function () {
    function getTabs() {
        var tabs = [
            {
                href: getTabPageUrl('Statistics2026_Pages_UserPages_User_UserStats'),
                name: 'User Stats'
            },
            {
                href: getTabPageUrl('Statistics2026_Pages_UserPages_User_TVSeriesProgress'),
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