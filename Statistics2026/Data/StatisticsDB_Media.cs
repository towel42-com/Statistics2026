using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public void AddAllMedia()
        {
            CheckIsValid();

            _embyInterfaces!._logger?.Debug($"AddAllMedia - Starting Video Analysis");

            _dbHelper!.Progress?.Report(0);
            var videoList = _dbHelper.GetLibraryItems<Episode>().Cast<Video>().ToList();
            _dbHelper!.Progress?.Report(50);
            videoList.AddRange(_dbHelper.GetLibraryItems<Movie>().Cast<Video>().ToList());
            _dbHelper!.Progress?.Report(100);

            double count = videoList.Count;
            double curr = 0.0;

            _dbHelper!.Progress?.Report(0);
            var sqlCmds = new List<SQLCmdDef>();
            var existing = new Dictionary<string, bool>();

            foreach (var video in videoList)
            {
                if (video == null)
                    continue;

                _dbHelper!.Progress?.Report(80.0 * (++curr) / count);

                if (existing.ContainsKey(video.Id.ToString()))
                    continue;
                existing.Add(video.Id.ToString(), true);


                using (var mediaInfo = new MediaInfo(video))
                {

                    sqlCmds.AddRange(AddMediaInfo(mediaInfo));
                    _embyInterfaces!._logger?.Debug($"AddAllMedia -     Processed Video ({curr} of {count}) - {mediaInfo.DescriptiveName}");
                }

                _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();
            }
            _dbHelper!.Progress?.Report(80);
            _dbHelper.ExecuteCommands(sqlCmds);
            _dbHelper!.Progress?.Report(100);
            _embyInterfaces!._logger?.Debug($"AddAllMedia - Finished Video Analysis");
        }

        public List<SQLCmdDef> AddMediaInfo(MediaInfo mediaInfo)
        {
            CheckIsValid();

            var sqlCmds = new List<SQLCmdDef>();
            if (mediaInfo == null || mediaInfo.ItemId == null)
            {
                _embyInterfaces!._logger?.Error($"AddMediaInfo '{mediaInfo?.SortName}': is missing ItemId");
                return sqlCmds;
            }

            string sql =
                "INSERT INTO Media " +
                "(" +
                    "  ItemId" +
                    ", PrimaryName" +
                    ", SortName" +
                    ", SecondaryName" +
                    ", StartYear" +
                    ", IsEpisode" +
                    ", IsTVSpecial" +
                    ", SeriesId" +
                    ", Season" +
                    ", Episode" +
                    ", NumEpisodes" +
                    ", ResolutionBase" +
                    ", ResolutionDetail" +
                    ", Codec" +
                    ", DolbyVisionProfile" +
                    ", StudioNames " +
                    ", Genres " +
                    ", ServerLocation" +
                    ", FileSize" +
                    ", ImageUrl" +
                    ", RunTimeTicks" +
                    ", Rating" +
                    ", TotalBitrate" +
                    ", PremiereDate" +
                    ", DateAdded" +
                ")" +
                " VALUES " +
                "(" +
                    "  @ItemId" +
                    ", @PrimaryName" +
                    ", @SortName" +
                    ", @SecondaryName" +
                    ", @StartYear" +
                    ", @IsEpisode" +
                    ", @IsTVSpecial" +
                    ", @SeriesId" +
                    ", @Season" +
                    ", @Episode" +
                    ", @NumEpisodes" +
                    ", @ResolutionBase" +
                    ", @ResolutionDetail" +
                    ", @Codec" +
                    ", @DolbyVisionProfile" +
                    ", @StudioNames " +
                    ", @Genres " +
                    ", @ServerLocation" +
                    ", @FileSize" +
                    ", @ImageUrl" +
                    ", @RunTimeTicks" +
                    ", @Rating" +
                    ", @TotalBitrate" +
                    ", @PremiereDate" +
                    ", @DateAdded" +
               ")" +
                " ON CONFLICT(ItemId) " +
                " DO UPDATE " +
                " SET " +
                    "  PrimaryName=@PrimaryName" +
                    ", SortName=@SortName" +
                    ", SecondaryName=@SecondaryName" +
                    ", StartYear=@StartYear" +
                    ", IsEpisode=@IsEpisode" +
                    ", IsTVSpecial=@IsTVSpecial" +
                    ", SeriesId=@SeriesId" +
                    ", Season=@Season" +
                    ", Episode=@Episode" +
                    ", NumEpisodes=@NumEpisodes" +
                    ", ResolutionBase=@ResolutionBase" +
                    ", ResolutionDetail=@ResolutionDetail" +
                    ", Codec=@Codec" +
                    ", DolbyVisionProfile=@DolbyVisionProfile" +
                    ", StudioNames =@StudioNames " +
                    ", Genres =@Genres " +
                    ", ServerLocation=@ServerLocation" +
                    ", FileSize=@FileSize" +
                    ", ImageUrl=@ImageUrl" +
                    ", RunTimeTicks=@RunTimeTicks" +
                    ", Rating=@Rating" +
                    ", TotalBitrate=@TotalBitrate" +
                    ", PremiereDate=@PremiereDate" +
                    ", DateAdded=@DateAdded"
                    ;
            ;

            sqlCmds.Add(new SQLCmdDef(sql, new List<(string name, object? value)>()
            {
                ("@ItemId", mediaInfo.ItemId),
                ("@PrimaryName", mediaInfo.PrimaryName),
                ("@SortName", mediaInfo.SortName),
                ("@SecondaryName", mediaInfo.SecondaryName),
                ("@StartYear", mediaInfo.StartYear),
                ("@IsEpisode", mediaInfo.IsEpisode),
                ("@IsTVSpecial", mediaInfo.IsTVSpecial),
                ("@SeriesId", mediaInfo.SeriesId),
                ("@Season", mediaInfo.Season),
                ("@Episode", mediaInfo.Episode),
                ("@NumEpisodes", mediaInfo.NumEpisodes),
                ("@ResolutionBase", mediaInfo.ResolutionBase),
                ("@ResolutionDetail", mediaInfo.ResolutionDetail),
                ("@Codec", mediaInfo.Codec),
                ("@DolbyVisionProfile", mediaInfo.DolbyVisionProfile),
                ("@StudioNames", string.Join(",", mediaInfo.StudioNames)),
                ("@Genres", string.Join(",", mediaInfo.Genres)),
                ("@ServerLocation", mediaInfo.ServerLocation),
                ("@FileSize", mediaInfo.FileSize),
                ("@ImageUrl", (mediaInfo.ImageUrl == null) ? "" : mediaInfo.ImageUrl),
                ("@RunTimeTicks", mediaInfo.RunTimeTicks),
                ("@Rating", mediaInfo.Rating),
                ("@TotalBitrate", mediaInfo.TotalBitrate),
                ("@PremiereDate", _dbHelper.ToDateTimeParamValue( mediaInfo.PremiereDate ) ),
                ("@DateAdded", _dbHelper.ToDateTimeParamValue( mediaInfo.DateAdded ) ),
            }));

            return sqlCmds;
        }
    }
}
