define(['mainTabsManager', Dashboard.getConfigurationResourceUrl('Helpers.js'), Dashboard.getConfigurationResourceUrl('LoadingHelpers.js')], function (mainTabsManager, Helpers,LoadingHelpers) {
    `use strict`;

    function loadStats(view, user) {
        LoadingHelpers.LoadTVProgress(view, user, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg, Helpers);
    }

    return function (view, params) {
        view.addEventListener('viewshow', function (e) {
            mainTabsManager.setTabs(this, Helpers.getTabIndex("TVSeriesProgress"), Helpers.getTabs);
            Helpers.injectStyleSheet(e);

            var style = document.createElement('style');
            style.innerHTML =
                '.tooltip {position: relative;display: inline-block;border-bottom: 1px dotted black;} ' +
                '.tooltip .tooltiptext {visibility: hidden; background-color: black; color: #fff; border-radius: 6px; padding: 5px 0; position: absolute;z-index: 1;} ' +
                '.tooltip:hover .tooltiptext {visibility: visible;} ' +
                '.info_cell {white-space: nowrap; padding-left:45px; padding-right:20px; font-size:smaller;}' +
                '.info_cell_heading {white-space: nowrap; padding-left:20px; padding-right:20px;font-size:smaller;}';
            var ref = document.querySelector('script');
            ref.parentNode.insertBefore(style, ref);

            process_click();

            function process_click() {
                const selectElement = document.getElementById("selectUser");
                const user = selectElement.options[selectElement.selectedIndex].text;
                loadStats(view, user)
            }
        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });

        view.querySelector("#selectUser").addEventListener(`change`, function () {
            const user = this.options[this.selectedIndex].text;
            loadStats(view, user);
        });

        view.querySelector("#episodesInfo").addEventListener(`click`, function () {
            Helpers.showInfo('This column displays the number of watched episodes and the number of total episodes. You will have 100% when you viewed all normal episodes (no specials, only aired)<br/>. ', 'Watched Episodes');
        });
        // view.querySelector("#specialsInfo").addEventListener(`click`, function () {
        //     Helpers.showInfo('This column displays the number of watched specials and the number of total specials. You will have 100% when you viewed all specials<br/>. ', 'Watched Specials');
        // });

        ApiClient.getUsers().then(function (users) {
            var select = view.querySelector(`#selectUser`);

            loadStats(view, users[0].Name);

            users.forEach((user) => {
                var option = document.createElement(`option`);
                option.value = user.Id;
                option.innerHTML = user.Name;
                select.appendChild(option);
            });
        });
    }
});
