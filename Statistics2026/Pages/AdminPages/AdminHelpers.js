define(function () {
    function getTabs() {
        var tabs = [
            {
                href: Dashboard.getConfigurationPageUrl('Stats2026_Summary'),
                name: 'Summary'
            }
            , {
                href: Dashboard.getConfigurationPageUrl('Stats2026_MovieSummary'),
                name: 'Movie Summary'
            }
            , {
                href: Dashboard.getConfigurationPageUrl('Stats2026_Movies'),
                name: 'Movies'
            }
            , {
                href: Dashboard.getConfigurationPageUrl('Stats2026_EpisodeSummary'),
                name: 'Episode/Series Summary'
            }
            , {
                href: Dashboard.getConfigurationPageUrl('Stats2026_Episodes'),
                name: 'Episodes'
            }
            , {
                href: Dashboard.getConfigurationPageUrl('Stats2026_TVSeriesProgress'),
                name: 'TV Series Progress'
            }
            , {
                href: Dashboard.getConfigurationPageUrl('Stats2026_UserStats'),
                name: 'User Stats'
            }
            , {
                href: Dashboard.getConfigurationPageUrl('Stats2026_SystemStatus'),
                name: 'System Status'
            }
            , {
                href: Dashboard.getConfigurationPageUrl('Stats2026_Settings'),
                name: 'Settings'
            }
        ];
        return tabs;
    }

    return {
        getTabs
    };

})