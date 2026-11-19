define(function () {
    'use strict';

    function getTabHierarchy() {
        return [
            {
                id: 'overview',
                name: 'Overview',
                href: Dashboard.getConfigurationPageUrl('Stats2026_Summary'),
                subTabs: [
                    { page: 'Stats2026_Summary', name: 'Summary' },
                    { page: 'Stats2026_SystemStatus', name: 'System Status' },
                    { page: 'Stats2026_Settings', name: 'Settings' }
                ]
            },
            {
                id: 'movies',
                name: 'Movies',
                href: Dashboard.getConfigurationPageUrl('Stats2026_MovieSummary'),
                subTabs: [
                    { page: 'Stats2026_MovieSummary', name: 'Movie Summary' },
                    { page: 'Stats2026_Movies', name: 'Movies' }
                ]
            },
            {
                id: 'tv',
                name: 'TV & Episodes',
                href: Dashboard.getConfigurationPageUrl('Stats2026_EpisodeSummary'),
                subTabs: [
                    { page: 'Stats2026_EpisodeSummary', name: 'Episode/Series Summary' },
                    { page: 'Stats2026_Episodes', name: 'Episodes' },
                ]
            },
            {
                id: 'users',
                name: 'User Info',
                href: Dashboard.getConfigurationPageUrl('Stats2026_UserStats'),
                subTabs: [
                    { page: 'Stats2026_UserStats', name: 'User Stats' },
                    { page: 'Stats2026_TVSeriesProgress', name: 'TV Series Progress' }
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
