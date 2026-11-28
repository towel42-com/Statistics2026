define(function () {
    'use strict';

    function getTabHierarchy() {
        return [
            {
                id: 'overview',
                name: 'Overview',
                href: Dashboard.getConfigurationPageUrl('Statistics2026_Pages_AdminPages_System_SystemSummary'),
                subTabs: [
                    { page: 'Statistics2026_Pages_AdminPages_System_SystemSummary', name: 'Summary' },
                    { page: 'Statistics2026_Pages_AdminPages_System_SystemStatus', name: 'System Status' },
                    { page: 'Statistics2026_Pages_AdminPages_System_SystemSettings', name: 'Settings' }
                ]
            },
            {
                id: 'movies',
                name: 'Movies',
                href: Dashboard.getConfigurationPageUrl('Statistics2026_Pages_AdminPages_Movies_MovieSummary'),
                subTabs: [
                    { page: 'Statistics2026_Pages_AdminPages_Movies_MovieSummary', name: 'Movie Summary' },
                    { page: 'Statistics2026_Pages_AdminPages_Movies_CollectionsTable', name: 'Collections' },
                    { page: 'Statistics2026_Pages_AdminPages_Movies_MovieTable', name: 'Movies' },
                    { page: 'Statistics2026_Pages_AdminPages_Movies_MovieSettings', name: 'Settings' }
                ]
            },
            {
                id: 'tv',
                name: 'TV Shows',
                href: Dashboard.getConfigurationPageUrl('Statistics2026_Pages_AdminPages_TV_SeriesSummary'),
                subTabs: [
                    { page: 'Statistics2026_Pages_AdminPages_TV_SeriesSummary', name: 'Series Summary' },
                    { page: 'Statistics2026_Pages_AdminPages_TV_EpisodeSummary', name: 'Episode Summary' },
                    { page: 'Statistics2026_Pages_AdminPages_TV_SeriesTable', name: 'Series' },
                    { page: 'Statistics2026_Pages_AdminPages_TV_EpisodeTable', name: 'Episodes' },
                    { page: 'Statistics2026_Pages_AdminPages_TV_TVSettings', name: 'Settings' }
                ]
            },
            {
                id: 'users',
                name: 'User Info',
                href: Dashboard.getConfigurationPageUrl('Statistics2026_Pages_AdminPages_User_UserSummary'),
                subTabs: [
                    { page: 'Statistics2026_Pages_AdminPages_User_UserSummary', name: 'Summary' },
                    { page: 'Statistics2026_Pages_AdminPages_User_MovieSummary', name: 'Movie Summary' },
                    { page: 'Statistics2026_Pages_AdminPages_User_TVSummary', name: 'TV Summary' },
                    { page: 'Statistics2026_Pages_AdminPages_User_TVSeriesProgress', name: 'TV Series Progress' },
                    { page: 'Statistics2026_Pages_AdminPages_User_UserReportSettings', name: 'Settings' }
                ]
            },
        ];
    }

    return {
        getTabHierarchy
    };
});
