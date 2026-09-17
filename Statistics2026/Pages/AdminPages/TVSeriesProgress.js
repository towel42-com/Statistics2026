define([
    'mainTabsManager', 
    Dashboard.getConfigurationResourceUrl('AdminHelpers.js'), 
    Dashboard.getConfigurationResourceUrl('Helpers.js')
], 
    function (mainTabsManager, AdminHelpers, Helpers) {
        `use strict`;

        function loadData(view, user) {
            if (!Helpers.CheckForValidConfig()) {
                Dashboard.hideLoadingMsg();
                return;
            }

            Helpers.LoadTVProgress(view, user, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg, Helpers);
        }

        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                mainTabsManager.setTabs(this, Helpers.getTabIndex("TVSeriesProgress", AdminHelpers.getTabs), AdminHelpers.getTabs);
                Helpers.injectStyleSheet(e);
                Helpers.injectSortableTableStyle(document);

                const selectElement = document.getElementById("selectUser_tvprogress");
                const user = selectElement.options[selectElement.selectedIndex].innerHTML;
                loadData(view, user)

                function process_click() {
                    const selectElement = document.getElementById("selectUser_tvprogress");
                    const user = selectElement.options[selectElement.selectedIndex].innerHTML;
                    loadData(view, user)
                }
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            view.querySelector("#selectUser_tvprogress").addEventListener(`change`, function () {
                const user = this.options[this.selectedIndex].innerHTML;
                loadData(view, user);
            });

            view.querySelector("#episodesInfo").addEventListener(`click`, function () {
                Helpers.showInfo('This column displays the number of watched episodes and the number of total episodes. You will have 100% when you viewed all normal episodes (no specials, only aired)<br/>. ', 'Watched Episodes');
            });

            ApiClient.getUsers().then(function (users) {
                var select = view.querySelector(`#selectUser_tvprogress`);

                loadData(view, users[0].Name);

                document.querySelectorAll('#TVSeriesProgressTable thead th').forEach((header, index) => {
                    header.addEventListener('click', () => {
                        // const columnName = header.getAttribute('data-column');
                        const columnType = header.getAttribute('data-type');

                        console.log(`Sorting index: ${index}, Column Type: ${columnType}`);

                        // Pass these variables straight into your sort function
                        Helpers.sortTable(index, columnType, 'TVSeriesProgressTable');
                    });
                });

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
