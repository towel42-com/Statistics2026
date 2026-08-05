define([`baseView`, `emby-button`, `emby-select`],
    function (BaseView) {
        `use strict`;

        const pluginId = "4BFE2894-AEA3-4D3C-A429-503B56D61711";

        function View(view, params) {
            BaseView.apply(this, arguments);
            loadStats(view);
        };

        function loadStats(view) {
            Dashboard.showLoadingMsg();

            ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                for (var h = 0, len = config.MovieDVProfileItems.Count; h < len; h++) {
                    var currGroup = config.MovieDVProfileItems.MediaItemGroups[h];
                    if (!config.showUnknownDVProfileCount && currGroup.IsUnknownDolbyProfile)
                        continue;

                    var innerText = ``
                    currGroup.MediaItems.forEach((v) => {
                        innerText += makeTable(v, config.ServerId);
                    });

                    var currHtml = `<h2 id = "` + currGroup.Title + `Title">` + currGroup.Title + `</h2>` +
                                   `<div><table id="` + currGroup.Title + `">` + innerText + `</table></div>`;

                    view.querySelector("#pagestart").innerHTML += currHtml;
                }

                Dashboard.hideLoadingMsg();
            });
        };

        function makeTable(movie, serverId) {
            var html = `<tr>`;
            html += `<td><a is="emby-linkbutton" href="/item?id=` + movie.Id + `&serverId=` + serverId + `">` + movie.Title + `</a></td>`;
            html += `<td>` + movie.Year + `</td>`;
            return html + `</tr>`;
        }

        Object.assign(View.prototype, BaseView.prototype);

        View.prototype.onResume = function (options) {
            BaseView.prototype.onResume.apply(this, arguments);
        };

        return View;
    });