define(function () {
    function getTabHierarchy() {
        return [
            {
                id: 'summary',
                name: 'Summary',
                href: getTabPageUrl('Statistics2026_Pages_UserPages_UserSummary'),
                subTabs: [
                    { page: 'Statistics2026_Pages_UserPages_UserSummary', name: 'Summary' }
                ]
            },
            {
                id: 'movies',
                name: 'Movies',
                href: getTabPageUrl('Statistics2026_Pages_UserPages_MovieSummary'),
                subTabs: [
                    { page: 'Statistics2026_Pages_UserPages_MovieSummary', name: 'Movies' }
                ]
            },
            {
                id: 'tv',
                name: 'TV Shows',
                href: getTabPageUrl('Statistics2026_Pages_UserPages_TVSummary'),
                subTabs: [
                    { page: 'Statistics2026_Pages_UserPages_TVSummary', name: 'TV Summary' },
                    { page: 'Statistics2026_Pages_UserPages_TVSeriesProgress', name: 'TV Series Progress' },
                ]
            },
            {
                id: 'settings',
                name: 'Settings',
                href: getTabPageUrl('Statistics2026_Pages_UserPages_UserReportSettings'),
                subTabs: [
                    { page: 'Statistics2026_Pages_UserPages_UserReportSettings', name: 'Settings' }
                ]
            }
        ];
    }

    function getRelativeTabPageUrl(pageName) {
        const userId = ApiClient.getCurrentUserId();
        const url = `configurationpage?name=${pageName}&userId=${userId}`;
        return url;
    }    

    function getTabPageUrl(pageName) {
        let url = ApiClient.getUrl(getRelativeTabPageUrl(pageName));
        url = url.replace("/emby", "");
        return url;
    }

    function getConfigPageUrl(pageName) {
        const relUrl = getRelativeTabPageUrl(pageName);
        return ApiClient.getUrl(`web/${relUrl}`);
    }

    return {
        getRelativeTabPageUrl,
        getTabHierarchy,
        getTabPageUrl,
        getConfigPageUrl
    };

})