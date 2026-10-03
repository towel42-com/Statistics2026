define(function () {
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
                href: Dashboard.getConfigurationPageUrl('MissingEpisodes'),
                name: 'Missing Episodes'
            }
            , {
                href: Dashboard.getConfigurationPageUrl('Movies'),
                name: 'Movies'
            }
            , {
                href: Dashboard.getConfigurationPageUrl('MissingMovies'),
                name: 'Missing Movies'
            }
            , {
                href: Dashboard.getConfigurationPageUrl('Settings'),
                name: 'Settings'
            }
        ];
        return tabs;
    }

    return {
        getTabs
    };

})