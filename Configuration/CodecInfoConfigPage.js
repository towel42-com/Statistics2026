define([`baseView`, `emby-button`, `emby-select`],
    function (BaseView) {
        `use strict`;

        var dynamicbuttons = [];


        const pluginId = "4BFE2894-AEA3-4D3C-A429-503B56D61711";

        function showInfo(text, title) {
            Dashboard.alert({ message: text, title: title });
        }
        function createStat(valueGroup, view, serverId = undefined, rootDivName="") {
            if (!valueGroup || !view)
                return "";

            var html = `<div class="col {valueGroup.Size}"`;
            if (rootDivName != "") {
                html += ` id="${rootDivName}"`;
            }
            html += "\">";
            html += "<div class=\"statCard\">";
            html += "<div class=\"statCard-content\">";

            if (valueGroup.ExtraInformation !== undefined) {
                var id = valueGroup.Title.replace(/\s/g, "");
                html += `<div id="${id}" class="infoBlock"><i class="md-icon">info</i></div>`;

                dynamicbuttons.push({ id: id, info: valueGroup.ExtraInformation, title: valueGroup.Title });
            }

            var showImage = (serverId !== undefined) && (valueGroup.Id !== undefined);
            if (showImage) {
                var imageUrl = ApiClient.getImageUrl(valueGroup.Id, { type: "Primary", quality: 90 });
                html += `<a is="emby-linkbutton" href="/item?id=${valueGroup.Id}&serverId=${serverId}"><img src="${imageUrl}" height="105px"/></a>`;    
                html += "<div>";

                if (valueGroup.Title !== undefined) {
                    html += `<div class="statCard-stats-title-left">${valueGroup.Title}</div>`;
                }
            }
            else if (valueGroup.Title !== undefined) {
                html += "<div style=\"width: 100%;\">";
                html += `<div class="statCard-stats-title">${valueGroup.Title}</div>`;
            }
            if (valueGroup.ValueLineOne !== undefined) {
                html += `<div class="statCard-stats-number">${valueGroup.ValueLineOne}</div>`
            }

            if (valueGroup.ValueLineTwo !== undefined) {
                html += `<div class="statCard-stats-number">${valueGroup.ValueLineTwo}</div>`
            }

            if (valueGroup.ValueLineThree !== undefined) {
                html += `<div class="statCard-stats-number">${valueGroup.ValueLineThree}</div>`
            }

            html += "</div>";
            html += "</div>";
            html += "</div>";
            html += "</div>";
            html += "</div>"; // not sure why this is here, but it was in the original code.

            return html;
        };

        function loadStats(view) {
            Dashboard.showLoadingMsg();

            ApiClient.getPluginConfiguration(pluginId).then(function (config) {

                view.querySelector("#showUnknownDVProfileCount").checked = config.showUnknownDVProfileCount;

                view.querySelector("#lastRunInfo").innerHTML = "Last Codec Analysis finished at <b> " + config.LastUpdated + "</b> - Version <b>" + config.Version + "</b> - Build Date - <b>" + config.BuildDate + "</b>";
                if (config.LastUpdated === undefined) {
                    Dashboard.alert({
                        message:
                            "No configuration found, please run the 'Media Codec Infromation' task on the Scheduled Tasks page and come back for the results."
                    });

                    view.querySelector(`#GotoAllCodecMovieInformationPage`).disabled = true;
                    view.querySelector(`#GotoAllCodecEpisodeInformationPage`).disabled = true;
                    view.querySelector(`#showUnknownDVProfileCount`).disabled = true;
                    view.querySelector(`#lastRunInfo`).style.display = 'none'; 

                    
                    Dashboard.hideLoadingMsg();
                } else {
                    view.querySelector("#pageIntro").innerHTML = "This plugin will calculate codec and dolby profile information "
                        + "from this Emby server instance.";


                    var mediaStats = "";
                    mediaStats += createStat(config.MediaCodecs, view);
                    mediaStats += createStat(config.MediaResolutions, view);
                    mediaStats += createStat(config.DolbyVisionProfiles, view, undefined, "dvProfileStats");

                    view.querySelector("#mediaStats").innerHTML = (mediaStats);

                    Dashboard.hideLoadingMsg();

                    dynamicbuttons.forEach((v) => {
                        view.querySelector("#" + v.id).addEventListener("click",
                            function () {
                                showInfo(v.info, v.title);
                            });
                    });
                }
            });
        }

        function View(view, params) {
            BaseView.apply(this, arguments);
            dynamicbuttons = [];
            loadStats(view);

            view.querySelector("#showUnknownDVProfileCount").addEventListener("click",
                function () {
                    ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                        config.showUnknownDVProfileCount = view.querySelector("#showUnknownDVProfileCount").checked;
                        ApiClient.updatePluginConfiguration(pluginId, config);
                    });
                }
            );

            view.querySelector("#GotoAllCodecMovieInformationPage").addEventListener("click",
                function () {
                    var href = Dashboard.getConfigurationPageUrl("AllCodecMovieInformationPage");
                    Dashboard.navigate(href);
                });

            view.querySelector("#GotoAllCodecEpisodeInformationPage").addEventListener("click",
                function () {
                    var href = Dashboard.getConfigurationPageUrl("AllCodecEpisodeInformationPage");
                    Dashboard.navigate(href);
                });
        }


        Object.assign(View.prototype, BaseView.prototype);

        View.prototype.onResume = function (options) {

            BaseView.prototype.onResume.apply(this, arguments);
        }

        return View;
    });