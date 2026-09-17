define([
    'mainTabsManager',
    Dashboard.getConfigurationResourceUrl('AdminHelpers.js'),
    Dashboard.getConfigurationResourceUrl('Helpers.js')
],
    function (mainTabsManager, AdminHelpers, Helpers) {
        'use strict';

        function loadStats(view, userName) {
            if (!Helpers.CheckForValidConfig()) {
                view.querySelector(`#lastRunInfo`).style.display = 'none';
                Dashboard.hideLoadingMsg();
                return;
            }

            Helpers.LoadUserStats(view, userName, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg, Helpers);
        }

        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                mainTabsManager.setTabs(this, Helpers.getTabIndex("UserStats", AdminHelpers.getTabs), AdminHelpers.getTabs);
                Helpers.injectStyleSheet(e);
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            view.querySelector("#selectUser").addEventListener(`change`, function () {
                const user = this.options[this.selectedIndex].innerHTML;
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
    }
);