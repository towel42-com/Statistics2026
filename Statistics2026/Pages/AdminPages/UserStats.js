define(['mainTabsManager', Dashboard.getConfigurationResourceUrl('Helpers.js'), Dashboard.getConfigurationResourceUrl('LoadingHelpers.js')], function (mainTabsManager, Helpers, LoadingHelpers) {
    'use strict';

    function loadStats(view, userName) {
        LoadingHelpers.LoadUserStats(view, userName, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg, Helpers);
    }

    return function (view, params) {
        view.addEventListener('viewshow', function (e) {
            mainTabsManager.setTabs(this, Helpers.getTabIndex("UserStats"), Helpers.getTabs);
            Helpers.injectStyleSheet(e);
        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });

        view.querySelector("#selectUser").addEventListener(`change`, function () {
            const user = this.options[this.selectedIndex].text;
            loadStats(view, user);
        });

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