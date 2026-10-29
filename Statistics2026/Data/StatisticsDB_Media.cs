using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;
using ServiceStack;
using Statistics2026.Api;
using Statistics2026.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public async Task AddAllMediaTaskImplAsync()
        {
            CheckIsValid( ECheckLevel.eUpdate );

            EmbyInterfaces().Logger().Debug( $"Starting Media Analysis" );

            _dbHelper!.Progress?.Report( 0 );
            var videoList = EmbyHelperFuncs.GetAllEpisodesAndMovies( null, LibraryManager() );
            _dbHelper!.Progress?.Report( 100 );

            if( videoList == null )
            {
                return;
            }

            double count = videoList.Count;
            var completedCount = 0;

            _dbHelper!.Progress?.Report( 0 );
            var existing = new ConcurrentDictionary<string, bool>();
            ConcurrentBag<object> paramBag = [];
            var sqlCmds = new List<SQLCmdDef>();
            sqlCmds.Add( new SQLCmdDef( "DELETE FROM Media WHERE PremiereDate IS NULL" ) );

            using( var timer = new AutoTimer( $"    Analyze Media - Getting Commands", Logger() ) )
            {
                using var gatekeeper = new SemaphoreSlim( 10, 10 );

                var pool = videoList.Select( ( video, index ) => Task.Run( async () =>
                {
                    await gatekeeper.WaitAsync();
                    try
                    {
                        var retVal = (index: index, video: video, cmd: (SQLCmdDef?)null);
                        if( video == null )
                            return retVal;

                        if( existing.ContainsKey( video.Id.ToString() ) )
                            return retVal;

                        existing.TryAdd( video.Id.ToString(), true );
                        paramBag.Add( video.Id.ToString() );

                        var mediaInfo = await MediaInfo.CreateAsync( Logger(), video ).ConfigureAwait( false );
                        if( !mediaInfo.aOK )
                            return retVal;

                        retVal.cmd = AddMediaInfo( mediaInfo );
                        _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();
                        int currProgress = Interlocked.Increment( ref completedCount );

                        _dbHelper!.Progress?.Report( 80.0 * ( 1.0 * currProgress ) / count );

                        EmbyInterfaces().Logger().Debug( $"    Processed Video ({currProgress} of {count}) - {mediaInfo.DescriptiveName}" );
                        //await Task.Delay( 10, CancellationToken.None );
                        return retVal;
                    }
                    finally
                    {
                        gatekeeper.Release();
                    }
                } ) );

                var unsortedResults = await Task.WhenAll( pool );
                var results = unsortedResults.OrderBy( item => item.index ).ToList();
                foreach( var result in results )
                {
                    if( result.cmd != null )
                    {
                        sqlCmds.Add( result.cmd );
                    }
                }
            }

            using( var timer = new AutoTimer( $"    Analyze Media - Executing Commands", Logger() ) )
            {
                _dbHelper!.Progress?.Report( 80 );
                var paramList = paramBag.ToList();
                sqlCmds.Insert( 1, SQLCmdDef.CreateSQLCmdDef( "DELETE FROM Media WHERE ItemId NOT IN (<KEYLIST>)", "<KEYLIST>", paramList ) );
                _dbHelper.ExecuteCommands( sqlCmds );
                _dbHelper!.Progress?.Report( 100 );
                EmbyInterfaces().Logger().Debug( $"Finished Media Analysis" );
            }
        }

        public SQLCmdDef? AddMediaInfo( MediaInfo mediaInfo )
        {
            CheckIsValid( ECheckLevel.eUpdate );

            if( mediaInfo == null || !mediaInfo.aOK )
            {
                EmbyInterfaces().Logger().Error( $"AddMediaInfo '{mediaInfo?.SortName}': is missing ItemId" );
                return null;
            }

            var sql =
                "INSERT INTO Media \n" +
                "(\n" +
                    "  ItemId\n" +
                    ", PrimaryName\n" +
                    ", SortName\n" +
                    ", SecondaryName\n" +
                    ", StartYear\n" +
                    ", IsEpisode\n" +
                    ", IsTVSpecial\n" +
                    ", SeriesId\n" +
                    ", Season\n" +
                    ", Episode\n" +
                    ", NumEpisodes\n" +
                    ", ResolutionBase\n" +
                    ", ResolutionDetail\n" +
                    ", Codec\n" +
                    ", DolbyVisionProfile\n" +
                    ", StudioNames \n" +
                    ", Genres \n" +
                    ", StreamingServices \n" +
                    ", ServerLocation\n" +
                    ", FileSize\n" +
                    ", ImageUrl\n" +
                    ", RunTimeTicks\n" +
                    ", Rating\n" +
                    ", TotalBitrate\n" +
                    ", PremiereDate\n" +
                    ", DateAdded\n" +
                ")\n" +
                " VALUES \n" +
                "(\n" +
                    "  @ItemId\n" +
                    ", @PrimaryName\n" +
                    ", @SortName\n" +
                    ", @SecondaryName\n" +
                    ", @StartYear\n" +
                    ", @IsEpisode\n" +
                    ", @IsTVSpecial\n" +
                    ", @SeriesId\n" +
                    ", @Season\n" +
                    ", @Episode\n" +
                    ", @NumEpisodes\n" +
                    ", @ResolutionBase\n" +
                    ", @ResolutionDetail\n" +
                    ", @Codec\n" +
                    ", @DolbyVisionProfile\n" +
                    ", @StudioNames \n" +
                    ", @Genres \n" +
                    ", @StreamingServices \n" +
                    ", @ServerLocation\n" +
                    ", @FileSize\n" +
                    ", @ImageUrl\n" +
                    ", @RunTimeTicks\n" +
                    ", @Rating\n" +
                    ", @TotalBitrate\n" +
                    ", @PremiereDate\n" +
                    ", @DateAdded\n" +
               ")\n" +
                " ON CONFLICT(ItemId) \n" +
                " DO UPDATE \n" +
                " SET \n" +
                    "  PrimaryName=@PrimaryName\n" +
                    ", SortName=@SortName\n" +
                    ", SecondaryName=@SecondaryName\n" +
                    ", StartYear=@StartYear\n" +
                    ", IsEpisode=@IsEpisode\n" +
                    ", IsTVSpecial=@IsTVSpecial\n" +
                    ", SeriesId=@SeriesId\n" +
                    ", Season=@Season\n" +
                    ", Episode=@Episode\n" +
                    ", NumEpisodes=@NumEpisodes\n" +
                    ", ResolutionBase=@ResolutionBase\n" +
                    ", ResolutionDetail=@ResolutionDetail\n" +
                    ", Codec=@Codec\n" +
                    ", DolbyVisionProfile=@DolbyVisionProfile\n" +
                    ", StudioNames=@StudioNames \n" +
                    ", Genres=@Genres \n" +
                    ", StreamingServices=@StreamingServices \n" +
                    ", ServerLocation=@ServerLocation\n" +
                    ", FileSize=@FileSize\n" +
                    ", ImageUrl=@ImageUrl\n" +
                    ", RunTimeTicks=@RunTimeTicks\n" +
                    ", Rating=@Rating\n" +
                    ", TotalBitrate=@TotalBitrate\n" +
                    ", PremiereDate=@PremiereDate\n" +
                    ", DateAdded=@DateAdded"
                    ;

            var services = JsonSerializer.Serialize( mediaInfo.StreamingServices );

            return new SQLCmdDef( sql,
            [
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
                ("@StudioNames", JsonSerializer.Serialize(mediaInfo.StudioNames)),
                ("@Genres", JsonSerializer.Serialize(mediaInfo.Genres)),
                ("@StreamingServices", JsonSerializer.Serialize(mediaInfo.StreamingServices)),
                ("@ServerLocation", mediaInfo.ServerLocation),
                ("@FileSize", mediaInfo.FileSize),
                ("@ImageUrl",  mediaInfo.ImageUrl ?? string.Empty ),
                ("@RunTimeTicks", mediaInfo.RunTimeTicks),
                ("@Rating", mediaInfo.Rating),
                ("@TotalBitrate", mediaInfo.TotalBitrate),
                ("@PremiereDate", HelperFuncs.ToDateTimeParamValue( mediaInfo.PremiereDate ) ),
                ("@DateAdded", HelperFuncs.ToDateTimeParamValue( mediaInfo.DateAdded ) ),
            ] );
        }

        public StatCard MediaResolutions()
        {
            CheckIsValid( ECheckLevel.eReport );

            var retVal = new TableBasedStatCard( Constants.MediaResolutions, Constants.HelpMediaResolutions, [ "Movies", "Episodes" ] );

            if( Plugin.Instance!.Configuration.showAllResolutions )
            {
                retVal.addRow( Constants.HD, [ 0, 0 ] );
                retVal.addRow( Constants._4k, [ 0, 0 ] );
                retVal.addRow( Constants._8k, [ 0, 0 ] );
                retVal.addRow( Constants._720p, [ 0, 0 ] );
                retVal.addRow( Constants.SD, [ 0, 0 ] );
            }

            var sql =
                "SELECT \n" +
                "ResolutionBase as Resolution, \n" +
                "sum(IsEpisode) AS Episodes, \n" +
                "sum(NOT IsEpisode) AS Movies \n" +
                "FROM Media \n" +
                "GROUP BY Resolution \n" +
                "ORDER BY Resolution ASC"
                ;
            _dbHelper.ExecuteCommand( new SQLCmdDef( sql ), statement =>
            {
                var row = statement.Current;
                var resolution = row.GetString( 0 );
                var episodeCount = row.GetInt( 1 );
                var movieCount = row.GetInt( 2 );
                retVal.addRow( resolution, [ movieCount, episodeCount ] );
                return true;
            } );

            return retVal;
        }

        public StatCard MediaCodecs()
        {
            CheckIsValid( ECheckLevel.eReport );

            var retVal = new TableBasedStatCard( Constants.MediaCodecs, Constants.HelpMediaCodecs, [ "Movies", "Episodes" ] );
            var sql =
                "SELECT \n" +
                "Codec as Codec, \n" +
                "sum(IsEpisode) AS Episodes, \n" +
                "sum(NOT IsEpisode) AS Movies \n" +
                "FROM Media \n" +
                "GROUP BY Codec \n" +
                "ORDER BY Codec ASC"
                ;

            _dbHelper.ExecuteCommand( new SQLCmdDef( sql ), statement =>
            {
                var row = statement.Current;
                var codec = row.GetString( 0 );
                var episodeCount = row.GetInt( 1 );
                var movieCount = row.GetInt( 2 );
                retVal.addRow( codec, [ movieCount, episodeCount ] );
                return true;
            } );

            return retVal;
        }

        public StatCard DVProfileInfo()
        {
            CheckIsValid( ECheckLevel.eReport );

            var sql =
                "SELECT \n" +
                "DolbyVisionProfile as DVProfile, \n" +
                "sum(IsEpisode) AS Episodes, \n" +
                "sum(NOT IsEpisode) AS Movies \n" +
                "FROM Media ";

            if( !Plugin.Instance!.Configuration.showUnknownDVProfiles )
                sql += $"WHERE DolbyVisionProfile NOT IN ({string.Join( ",", Constants.UnknownDolbyProfiles.Select( p => $"'{p}'" ) )}) ";

            sql += "GROUP BY DolbyVisionProfile \n" +
                   "ORDER BY DolbyVisionProfile ASC"
                   ;

            var retVal = new TableBasedStatCard( Constants.DolbyVisionProfiles, Constants.HelpDolbyVisionProfile, [ "Movies", "Episodes" ] );
            if( Plugin.Instance!.Configuration.showUnknownDVProfiles )
                retVal.addRow( "Unknown Dolby Profile", [ 0, 0 ] );

            _dbHelper.ExecuteCommand( new SQLCmdDef( sql ), statement =>
            {
                var row = statement.Current;
                var dvProfile = row.GetString( 0 );
                var episodeCount = row.GetInt( 1 );
                var movieCount = row.GetInt( 2 );
                retVal.addRow( dvProfile, [ movieCount, episodeCount ] );
                return true;
            } );

            return retVal;
        }

        public StatCard TotalMovieCount( User? user, bool watched )
        {
            CheckIsValid( ECheckLevel.eReport );

            var sql = string.Empty;
            var parameters = new List<(string, object?)>();
            var title = Constants.TotalMovies;
            var help = Constants.HelpTotalMovies;
            long total = 0;
            long totalTicks = 0;
            if( user == null )
            {
                sql = "SELECT SUM(NOT IsEpisode) FROM Media";
                totalTicks = GetSingleValueFromSQL( $"SELECT SUM(RunTimeTicks) FROM MEDIA WHERE NOT IsEpisode" ).ToLong();
            }
            else
            {
                title = watched ? Constants.TotalUserMoviesWatched : Constants.TotalUserMovies;
                help = watched ? Constants.HelpTotalUserMoviesWatched : Constants.HelpTotalUserMovies;

                sql = $"SELECT SUM(NOT IsEpisode) FROM {getUserTableName( user )} WHERE UserId=@UserId";
                if( watched )
                    sql += " AND IsPlayed";
                parameters.Add( ("@UserId", user.Id.ToString()) );

                if( watched )
                {
                    total = GetSingleValueFromSQL( $"SELECT SUM(NOT IsEpisode) FROM {getUserTableName( user )} WHERE UserId=@UserId", parameters ).ToLong();
                }
            }

            var retVal = ValueGroupForSingleItem( title, help, sql, parameters, count =>
            {
                var retVal = count.ToString();

                if( watched && total != 0 )
                {
                    var value = 100.0 * count / ( 1.0 * total );
                    retVal += $" ({value:F1})%";
                }

                return retVal;
            } );

            if( totalTicks != 0 )
            {
                var rt = new RunTime( totalTicks );
                retVal.AddLine( DBHelper.FormatTicks( totalTicks ), false );
            }

            return retVal;
        }

        public StatCard TotalTVCount( User? user, bool watched )
        {
            CheckIsValid( ECheckLevel.eReport );

            var runtimeColumn = string.Empty;

            List<(string name, object? value)>? paramList = null;

            var tableName = getUserTableName( user );

            string? seriesColumn;

            string? seriesFrom;

            string? episodeColumn;
            string? episodeFrom;
            string? titleSeries;

            string? titleEpisodes;

            string? helpEpisodes;
            if( user == null )
            {
                seriesColumn = "COUNT(DISTINCT(PrimaryName))";
                episodeColumn = "SUM(NumEpisodes)";
                episodeFrom = seriesFrom = "Media WHERE IsEpisode";

                titleSeries = Constants.TotalTVShows;
                titleEpisodes = Constants.TotalTVEpisodes;
                helpEpisodes = Constants.HelpTotalTVShows;
            }
            else
            {
                paramList = [ ("@UserId", user.Id.ToString()) ];

                seriesColumn = "COUNT(DISTINCT(Media.PrimaryName))";
                episodeColumn = $"SUM({tableName}.NumEpisodes)";

                titleSeries = Constants.TotalUserTVShows;
                titleEpisodes = Constants.TotalUserTVEpisodes;
                helpEpisodes = Constants.HelpTotalUserTVShows;

                var from = $"{tableName} LEFT JOIN Media ON {tableName}.ItemId=Media.ItemId WHERE Media.IsEpisode AND NOT Media.IsTVSpecial AND ( {tableName}.UserId=@UserId )";

                if( watched )
                {
                    from += $" AND ( {tableName}.IsPlayed )";

                    titleSeries = Constants.TotalTVShowsWatched;
                    titleEpisodes = Constants.TotalUserTVEpisodesWatched;
                    helpEpisodes = Constants.HelpTotalTVShowsWatched;
                }

                seriesFrom = episodeFrom = from;
            }

            var sqlEpisodes = $"SELECT {episodeColumn} FROM {episodeFrom}";
            var retVal = ValueGroupForSingleItem( titleEpisodes, helpEpisodes, sqlEpisodes, paramList );

            retVal.AddLine( titleSeries, true );
            var sqlSeries = $"SELECT {seriesColumn} FROM {seriesFrom}";
            var value = GetSingleValueFromSQL( sqlSeries, paramList );
            retVal.AddLine( value, false );

            if( user == null )
            {
                value = GetSingleValueFromSQL( "SELECT SUM(RunTimeTicks) FROM MEDIA WHERE IsEpisode" );
                value = DBHelper.FormatTicks( value.ToInt64() );
                retVal.AddLine( value, false );
            }

            return retVal;
        }

        public long TotalStudioCountValue( User? user, bool movies )
        {
            CheckIsValid( ECheckLevel.eReport );

            var sql = "SELECT DISTINCT StudioNames FROM Media WHERE ";
            if( movies )
                sql += "NOT ";
            sql += "IsEpisode AND StudioNames IS NOT NULL AND StudioNames<>''";

            // Create an unordered set of strings
            HashSet<string> studios = [];

            var cmd = new SQLCmdDef( sql );
            _dbHelper.ExecuteCommand( new SQLCmdDef( sql ), statement =>
            {
                var row = statement.Current;
                var currStudios = HelperFuncs.JsonToStringArray( row.GetString( 0 ) );
                studios.UnionWith( currStudios );
                return true;
            } );

            return studios.Count();
        }

        public StatCard TotalStudioCount( User? user, bool movies )
        {
            CheckIsValid( ECheckLevel.eReport );

            var retVal = new TextBasedStatCard( movies ? Constants.TotalStudios : Constants.TotalTVNetworks, movies ? Constants.HelpTotalStudios : Constants.HelpTotalTVNetworks, EStatCardStyle.eCompact );
            var value = TotalStudioCountValue( user, movies );
            retVal.AddLine( value.ToString(), false );
            return retVal;
        }

        public StatCard TotalMovieStudioCount( User? user )
        {
            CheckIsValid( ECheckLevel.eReport );

            return TotalStudioCount( user, true );
        }

        public StatCard TotalTVStudioCount( User? user )
        {
            CheckIsValid( ECheckLevel.eReport );

            return TotalStudioCount( user, false );
        }

        public List<(int year, long count)> FavoriteYearValues( User? user, bool movies )
        {
            CheckIsValid( ECheckLevel.eReport );

            if( user == null )
                throw new ArgumentNullException( "user" );

            var tableName = getUserTableName( user );
            var sql =
                "SELECT COUNT(*) as NumVideos, StartYear From Media "
                + $"INNER JOIN {tableName} On Media.ItemId={tableName}.ItemId "
                + "WHERE "
                ;
            if( movies )
            {
                sql += "NOT Media.IsEpisode ";
            }
            else
            {
                sql += "Media.IsEpisode ";
            }

            sql +=
                "AND UserId=@UserId AND IsPlayed "
              + "GROUP BY StartYear "
              + "ORDER BY NumVideos DESC, StartYear ASC "
              + "LIMIT 5 "
              ;

            var sqlCmd = new SQLCmdDef( sql,
                                        [
                                            ( "@UserId", user.Id.ToString())
                                        ] );

            var retVal = new List<(int year, long count)>();
            _dbHelper.ExecuteCommand( sqlCmd, statement =>
            {
                var row = statement.Current;
                var count = row.GetInt64( 0 );
                var year = row.GetInt( 1 );
                retVal.Add( (year, count) );
                return true;
            } );

            return retVal;
        }

        public StatCard FavoriteYears( User? user, bool movies )
        {
            var videoType = movies ? "Movies" : "Episodes";
            var retVal = new TableBasedStatCard( Constants.FavoriteMovieYears, "Genre", [ $"# of {videoType} Watched" ] );
            retVal.SetDataColumnAlignment( 0, StatCard.EAlignment.eCenter );
            var values = FavoriteYearValues( user, movies );

            foreach( var (year, count) in values )
            {
                retVal.addRow( year.ToString(), [ count ] );
            }

            return retVal;
        }

        public List<(string genre, long count)> FavoriteGenreValues( User? user, bool movies )
        {
            CheckIsValid( ECheckLevel.eReport );

            if( user == null )
                throw new ArgumentNullException( "user" );

            var tableName = getUserTableName( user );
            var sql =
                "SELECT Genres From Media "
                + $"INNER JOIN {tableName} On Media.ItemId={tableName}.ItemId "
                + "WHERE "

                ;
            if( movies )
            {
                sql += "NOT Media.IsEpisode ";
            }
            else
            {
                sql += "Media.IsEpisode ";
            }

            sql +=
                "AND UserId=@UserId AND IsPlayed "
              ;

            var sqlCmd = new SQLCmdDef( sql,
                                        [
                                            ( "@UserId", user.Id.ToString())
                                        ] );

            Dictionary<string, int> genreMap = [];
            _dbHelper.ExecuteCommand( sqlCmd, statement =>
            {
                var row = statement.Current;
                var genres = HelperFuncs.JsonToStringArray( row.GetString( 0 ) );
                foreach( var genre in genres )
                {
                    if( !genreMap.ContainsKey( genre ) )
                        genreMap[ genre ] = 0;
                    genreMap[ genre ]++;
                }

                return true;
            } );

            var sortedGenre = genreMap.OrderByDescending( kvp => kvp.Value ).ToList();

            var retVal = new List<(string genre, long count)>();
            for( var ii = 0; ii < sortedGenre.Count() && ii < 5; ++ii )
            {
                retVal.Add( (sortedGenre[ ii ].Key, sortedGenre[ ii ].Value) );
            }

            return retVal;
        }

        public StatCard FavoriteGenre( User? user, bool movies )
        {
            CheckIsValid( ECheckLevel.eReport );

            if( user == null )
                throw new ArgumentNullException( "user" );

            string? videoType;

            string? title;
            if( movies )
            {
                videoType = "Movies";
                title = Constants.FavoriteMovieGenres;
            }
            else
            {
                videoType = "Episodes";
                title = Constants.FavoriteTVGenres;
            }

            var retVal = new TableBasedStatCard( title, "Genre", [ $"# of {videoType} Watched" ] );
            retVal.SetDataColumnAlignment( 0, StatCard.EAlignment.eCenter );

            var values = FavoriteGenreValues( user, movies );
            foreach( var (genre, count) in values )
            {
                retVal.addRow( genre, [ count ] );
            }

            return retVal;
        }
    }
}
