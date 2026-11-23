define(function () {
    'use strict';

    function getTabHierarchy() {
        return [
            {
                id: 'overview',
                name: 'Overview',
                href: Dashboard.getConfigurationPageUrl('Stats2026_SystemSummary'),
                subTabs: [
                    { page: 'Stats2026_SystemSummary', name: 'Summary' },
                    { page: 'Stats2026_SystemStatus', name: 'System Status' },
                    { page: 'Stats2026_SystemSettings', name: 'Settings' }
                ]
            },
            {
                id: 'movies',
                name: 'Movies',
                href: Dashboard.getConfigurationPageUrl('Stats2026_MovieSummary'),
                subTabs: [
                    { page: 'Stats2026_MovieSummary', name: 'Movie Summary' },
                    { page: 'Stats2026_CollectionsTable', name: 'Collections' },
                    { page: 'Stats2026_MovieTable', name: 'Movies' },
                    { page: 'Stats2026_MovieSettings', name: 'Settings' }
                ]
            },
            {
                id: 'tv',
                name: 'TV Shows',
                href: Dashboard.getConfigurationPageUrl('Stats2026_SeriesSummary'),
                subTabs: [
                    { page: 'Stats2026_SeriesSummary', name: 'Series Summary' },
                    { page: 'Stats2026_EpisodeSummary', name: 'Episode Summary' },
                    { page: 'Stats2026_SeriesTable', name: 'Series' },
                    { page: 'Stats2026_EpisodeTable', name: 'Episodes' },
                    { page: 'Stats2026_TVSettings', name: 'Settings' }
                ]
            },
            {
                id: 'users',
                name: 'User Info',
                href: Dashboard.getConfigurationPageUrl('Stats2026_UserStats'),
                subTabs: [
                    { page: 'Stats2026_UserStats', name: 'User Stats' },
                    { page: 'Stats2026_TVSeriesProgress', name: 'TV Series Progress' },
                    { page: 'Stats2026_UserReportSettings', name: 'Settings' }
                ]
            },
        ];
    }

    function getTabs() {
        return getTabHierarchy().map(function (group) {
            return {
                href: group.href,
                name: group.name
            };
        });
    }

    return {
        getTabHierarchy,
        getTabs
    };
});
