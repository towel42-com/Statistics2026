define([
    'mainTabsManager', 
    Dashboard.getConfigurationResourceUrl('AdminHelpers.js'), 
    Dashboard.getConfigurationResourceUrl('SharedHelpers.js')
], 
    function (mainTabsManager, AdminHelpers, SharedHelpers) {
        `use strict`;

        function loadData(view, user) {
            SharedHelpers.LoadTVProgress(view, user, Dashboard.showLoadingMsg, Dashboard.hideLoadingMsg, SharedHelpers);
        }

        return function (view, params) {
            view.addEventListener('viewshow', function (e) {
                mainTabsManager.setTabs(this, SharedHelpers.getTabIndex("TVSeriesProgress", AdminHelpers.getTabs), AdminHelpers.getTabs);
                SharedHelpers.injectStyleSheet(e);

                SharedHelpers.injectStyleSheetEX(e, Dashboard.getConfigurationResourceUrl('style.css'));
                var style = document.createElement('style');
                style.innerHTML = SharedHelpers.sortableTableStyle();
                var ref = document.querySelector('script');
                ref.parentNode.insertBefore(style, ref);

                const selectElement = document.getElementById("selectUser");
                const user = selectElement.options[selectElement.selectedIndex].innerHTML;
                loadData(view, user)

                function process_click() {
                    const selectElement = document.getElementById("selectUser");
                    const user = selectElement.options[selectElement.selectedIndex].innerHTML;
                    loadData(view, user)
                }
            });

            view.addEventListener('viewhide', function (e) {

            });

            view.addEventListener('viewdestroy', function (e) {

            });

            view.querySelector("#selectUser").addEventListener(`change`, function () {
                const user = this.options[this.selectedIndex].innerHTML;
                loadData(view, user);
            });

            view.querySelector("#episodesInfo").addEventListener(`click`, function () {
                SharedHelpers.showInfo('This column displays the number of watched episodes and the number of total episodes. You will have 100% when you viewed all normal episodes (no specials, only aired)<br/>. ', 'Watched Episodes');
            });

            ApiClient.getUsers().then(function (users) {
                var select = view.querySelector(`#selectUser`);

                loadData(view, users[0].Name);

                document.querySelectorAll('#TVSeriesProgressTable thead th').forEach((header, index) => {
                    header.addEventListener('click', () => {
                        // const columnName = header.getAttribute('data-column');
                        const columnType = header.getAttribute('data-type');

                        console.log(`Sorting index: ${index}, Column Type: ${columnType}`);

                        // Pass these variables straight into your sort function
                        SharedHelpers.sortTable(index, columnType, 'TVSeriesProgressTable');
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
