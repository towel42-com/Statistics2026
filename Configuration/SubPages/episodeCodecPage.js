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
                for (var h = 0, len = config.EpisodeCodecItems.Count; h < len; h++) {
                    var currGroup = config.EpisodeCodecItems.MediaItemGroups[h];

                    var innerText = ``
                    currGroup.MediaItems.forEach((v) => {
                        innerText += makeLink(v, config.ServerId);
                    });

                    var currHtml = `<h2 id = "` + currGroup.Title + `Title">` + currGroup.Title + `</h2>` +
                                   `<div id="` + currGroup.Title + `">` + innerText + `</div>`;
                    view.querySelector("#pagestart").innerHTML += currHtml;
                }

                Dashboard.hideLoadingMsg();
            });
        };

        function makeLink(movie, serverId) {
            var imageUrl = ApiClient.getImageUrl(movie.Id, { type: "Primary", quality: 90 });

            var html = `<a is="emby-linkbutton" href="/item?id=` + movie.Id + `&serverId=` + serverId + `"><img src="` + imageUrl + `" height="200px" alt="` + movie.Name + `" /></a>`;
            return html;
        }

        Object.assign(View.prototype, BaseView.prototype);

        View.prototype.onResume = function (options) {
            BaseView.prototype.onResume.apply(this, arguments);
        };

        return View;
    });