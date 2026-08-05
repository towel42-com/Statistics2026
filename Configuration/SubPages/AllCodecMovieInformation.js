define([`baseView`, 'appRouter', `emby-button`, 'emby-linkbutton', `emby-select`],
    function (BaseView) {
        `use strict`;

        const pluginId = `4BFE2894-AEA3-4D3C-A429-503B56D61711`;

        function showInfo(text, title) {
            Dashboard.alert({ message: text, title: title });
        }

        function stupidTable(element) {
            // Attach click event to the span inside the th elements
            element.addEventListener(`click`, function (event) {
                if (event.target.tagName === `SPAN`) {
                    stupidSort(event.target.parentElement);
                }
            });

            return element;
        }

        function stupidSort(element, customSortFn) {
            var table = element.closest(`table`);
            var columnIndex = Array.from(element.parentElement.children).indexOf(element);

            var sortColumn = element.dataset.sort || null;

            if (sortColumn !== null) {
                var cumulativeColspan = 0;
                element.parentElement.querySelectorAll(`th`).forEach(function (th, index) {
                    if (index < columnIndex) {
                        var colspan = parseInt(th.getAttribute(`colspan`)) || 1;
                        cumulativeColspan += colspan;
                    }
                });

                var sortDirection;
                if (arguments.length === 2) {
                    sortDirection = customSortFn;
                } else {
                    sortDirection =
                        customSortFn ||
                        element.dataset.sortDefault ||
                        stupidTable.dir.ASC;

                    if (element.dataset.sortDir && element.dataset.sortDir === stupidTable.dir.ASC) {
                        sortDirection = stupidTable.dir.DESC;
                    }
                }

                if (element.dataset.sortDir !== sortDirection) {
                    element.dataset.sortDir = sortDirection;
                    var beforeTableSortEvent = new CustomEvent(`beforetablesort`, {
                        detail: { column: cumulativeColspan, direction: sortDirection },
                    });
                    table.dispatchEvent(beforeTableSortEvent);
                    table.style.display;

                    setTimeout(function () {
                        var rows = [];
                        var sortFn = stupidTable.defaultSortFns[sortColumn];
                        var tbodyRows = table.tBodies[0].querySelectorAll(`tr`);

                        tbodyRows.forEach(function (row, index) {
                            var cell = row.children[cumulativeColspan];
                            var sortValue = cell.dataset.sortValue || cell.textContent;
                            rows.push([sortValue, row]);
                        });

                        rows.sort(function (a, b) {
                            return sortFn(a[0] === 'undefined' ? -1000 : a[0], b[0] === 'undefined' ? -1000 : b[0]);
                        });

                        if (sortDirection !== stupidTable.dir.ASC) {
                            rows.reverse();
                        }

                        var sortedRows = rows.map(function (row) {
                            return row[1];
                        });

                        table.tBodies[0].append(...sortedRows);

                        table.querySelectorAll(`th`).forEach(function (th) {
                            th.dataset.sortDir = null;
                            th.classList.remove(`sorting-desc`, `sorting-asc`);
                        });

                        element.dataset.sortDir = sortDirection;
                        element.classList.add(`sorting-` + sortDirection);

                        var afterTableSortEvent = new CustomEvent(`aftertablesort`, {
                            detail: { column: cumulativeColspan, direction: sortDirection },
                        });
                        table.dispatchEvent(afterTableSortEvent);
                        table.style.display;
                    }, 10);

                    return element;
                }
            }
        }

        stupidTable.dir = { ASC: `asc`, DESC: `desc` };

        stupidTable.defaultSortFns = {
            int: function (a, b) {
                return parseInt(a, 10) - parseInt(b, 10);
            },
            float: function (a, b) {
                return parseFloat(a) - parseFloat(b);
            },
            string: function (a, b) {
                return a.toString().localeCompare(b.toString());
            },
            "string-ins": function (a, b) {
                a = a.toString().toLocaleLowerCase();
                b = b.toString().toLocaleLowerCase();
                return a.localeCompare(b);
            },
        };

        function loadStats(view) {
            Dashboard.showLoadingMsg();
            ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                var tbl = view.querySelector(`#MediaTable > tbody`);
                var mediaStats = config.MediaInfoList;

                for (var i = 0; i < tbl.rows.length;) {
                    tbl.deleteRow(i);
                }

                mediaStats.forEach((video) => {
                    if (video.IsEpisode) {
                        return;
                    }

                    var cell = 0;
                    var newRow = tbl.insertRow(-1);
                    var newCell = newRow.insertCell(cell++);
                    var link = document.createElement("a");
                    link.setAttribute("is", 'emby-linkbutton');
                    link.setAttribute("href", '/item?id=' + video.Id + '&serverId=' + config.ServerId);
                    var newText = document.createTextNode(video.PrimaryName);
                    link.appendChild(newText);
                    newCell.setAttribute("data-sort-value", video.SortName);
                    newCell.appendChild(link);

                    newCell = newRow.insertCell(cell++);
                    newCell.className = (`center`);
                    newText = document.createTextNode(video.StartYear);
                    newCell.setAttribute("data-sort-value", video.StartYear);
                    newCell.appendChild(newText);

                    newCell = newRow.insertCell(cell++);
                    newCell.className = (`center`);
                    newText = document.createTextNode(video.Resolution);
                    newCell.setAttribute("data-sort-value", video.Resolution);
                    newCell.appendChild(newText);

                    newCell = newRow.insertCell(cell++);
                    newCell.className = (`center`);
                    newText = document.createTextNode(video.CodecName);
                    newCell.setAttribute("data-sort-value", video.CodecName);
                    newCell.appendChild(newText);

                    newCell = newRow.insertCell(cell++);
                    newCell.className = (`center`);
                    newText = document.createTextNode(video.DolbyVisionProfile);
                    newCell.setAttribute("data-sort-value", video.DolbyVisionProfile);
                    newCell.appendChild(newText);

                    newCell = newRow.insertCell(cell++);
                    newCell.className = (`center`);
                    newText = document.createTextNode(video.ServerLocation);
                    newCell.setAttribute("data-sort-value", video.ServerLocation);
                    newCell.appendChild(newText);

                    tbl.innerHTML += '';
                });

                Dashboard.hideLoadingMsg();
            });
        };

        function View(view, params) {
            BaseView.apply(this, arguments);

            var table = stupidTable(view.querySelector(`#MediaTable`));

            view.querySelector(`#MediaTable`).addEventListener(`aftertablesort`, function (event, data) {
                var th = view.querySelector(`#MediaTable`).getElementsByTagName(`th`);
                for (var i = 0; i < th.length; i++) {
                    th[i].classList.remove(`selectLabelFocused`);
                };
                th[event.detail.column].classList.add(`selectLabelFocused`);
            });

            stupidSort(view.querySelector(`#defaultColumn span`).parentElement, `asc`);

            loadStats(view);
        };

        Object.assign(View.prototype, BaseView.prototype);

        View.prototype.onResume = function (options) {
            BaseView.prototype.onResume.apply(this, arguments);
        };

        return View;
    });