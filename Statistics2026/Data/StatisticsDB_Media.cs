using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using ServiceStack;
using Statistics2026.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public void AddAllMedia()
        {
            CheckIsValid( ECheckType.eUpdate );

            _embyInterfaces!._logger?.Debug( $"AddAllMedia - Starting Video Analysis" );

            _dbHelper!.Progress?.Report( 0 );
            var videoList = _dbHelper.GetLibraryItems<Episode>().Cast<Video>().ToList();
            _dbHelper!.Progress?.Report( 50 );
            videoList.AddRange( _dbHelper.GetLibraryItems<Movie>().Cast<Video>().ToList() );
            _dbHelper!.Progress?.Report( 100 );

            double count = videoList.Count;
            var curr = 0.0;

            _dbHelper!.Progress?.Report( 0 );
            var sqlCmds = new List<SQLCmdDef>();
            var existing = new Dictionary<string, bool>();

            foreach( var video in videoList )
            {
                if( video == null )
                    continue;

                _dbHelper!.Progress?.Report( 80.0 * ( ++curr ) / count );

                if( existing.ContainsKey( video.Id.ToString() ) )
                    continue;
                existing.Add( video.Id.ToString(), true );

                using( var mediaInfo = new MediaInfo( video ) )
                {
                    if( !mediaInfo.aOK )
                        continue;

                    sqlCmds.AddRange( AddMediaInfo( mediaInfo ) );
                    _embyInterfaces!._logger?.Debug( $"AddAllMedia -     Processed Video ({curr} of {count}) - {mediaInfo.DescriptiveName}" );
                }

                _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();
            }

            _dbHelper!.Progress?.Report( 80 );
            _dbHelper.ExecuteCommands( sqlCmds );
            _dbHelper!.Progress?.Report( 100 );
            _embyInterfaces!._logger?.Debug( $"AddAllMedia - Finished Video Analysis" );
        }

        public List<SQLCmdDef> AddMediaInfo( MediaInfo mediaInfo )
        {
            CheckIsValid( ECheckType.eUpdate );

            var sqlCmds = new List<SQLCmdDef>();
            if( mediaInfo == null || !mediaInfo.aOK )
            {
                _embyInterfaces!._logger?.Error( $"AddMediaInfo '{mediaInfo?.SortName}': is missing ItemId" );
                return sqlCmds;
            }

            var sql =
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

            sqlCmds.Add( new SQLCmdDef( sql,
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
                ("@StudioNames", string.Join(",", mediaInfo.StudioNames)),
                ("@Genres", string.Join(",", mediaInfo.Genres)),
                ("@ServerLocation", mediaInfo.ServerLocation),
                ("@FileSize", mediaInfo.FileSize),
                ("@ImageUrl",  mediaInfo.ImageUrl ??  string.Empty ),
                ("@RunTimeTicks", mediaInfo.RunTimeTicks),
                ("@Rating", mediaInfo.Rating),
                ("@TotalBitrate", mediaInfo.TotalBitrate),
                ("@PremiereDate", _dbHelper.ToDateTimeParamValue( mediaInfo.PremiereDate ) ),
                ("@DateAdded", _dbHelper.ToDateTimeParamValue( mediaInfo.DateAdded ) ),
            ] ) );

            return sqlCmds;
        }

        public StatCard MediaResolutions()
        {
            CheckIsValid( ECheckType.eReport );

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
                "SELECT " +
                "ResolutionBase as Resolution, " +
                "sum(IsEpisode) AS Episodes, " +
                "sum(NOT IsEpisode) AS Movies " +
                "FROM Media " +
                "GROUP BY Resolution " +
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
            CheckIsValid( ECheckType.eReport );

            var retVal = new TableBasedStatCard( Constants.MediaCodecs, Constants.HelpMediaCodecs, [ "Movies", "Episodes" ] );
            var sql =
                "SELECT " +
                "Codec as Codec, " +
                "sum(IsEpisode) AS Episodes, " +
                "sum(NOT IsEpisode) AS Movies " +
                "FROM Media " +
                "GROUP BY Codec " +
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
            CheckIsValid( ECheckType.eReport );

            var sql =
                "SELECT " +
                "DolbyVisionProfile as DVProfile, " +
                "sum(IsEpisode) AS Episodes, " +
                "sum(NOT IsEpisode) AS Movies " +
                "FROM Media ";

            if( !Plugin.Instance!.Configuration.showUnknownDVProfiles )
                sql += $"WHERE DolbyVisionProfile NOT IN ({string.Join( ",", Constants.UnknownDolbyProfiles.Select( p => $"'{p}'" ) )}) ";

            sql += "GROUP BY DolbyVisionProfile " +
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
            CheckIsValid( ECheckType.eReport );

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
                retVal.AddLine( DBHelper.FormatTicks( totalTicks ) );
            }

            return retVal;
        }

        public StatCard TotalTVCount( User? user, bool watched )
        {
            CheckIsValid( ECheckType.eReport );

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

            retVal.AddLine( titleSeries );
            var sqlSeries = $"SELECT {seriesColumn} FROM {seriesFrom}";
            var value = GetSingleValueFromSQL( sqlSeries, paramList );
            retVal.AddLine( value );

            if( user == null )
            {
                value = GetSingleValueFromSQL( "SELECT SUM(RunTimeTicks) FROM MEDIA WHERE IsEpisode" );
                value = DBHelper.FormatTicks( value.ToInt64() );
                retVal.AddLine( value );
            }

            return retVal;
        }

        public long TotalStudioCountValue( User? user, bool movies )
        {
            CheckIsValid( ECheckType.eReport );

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
                var currStudios = row.GetString( 0 )?.Split( ',' ) ?? Array.Empty<string>();
                ;
                studios.UnionWith( currStudios );
                return true;
            } );

            return studios.Count();
        }

        public StatCard TotalStudioCount( User? user, bool movies )
        {
            CheckIsValid( ECheckType.eReport );

            var retVal = new TextBasedStatCard( movies ? Constants.TotalStudios : Constants.TotalTVNetworks, movies ? Constants.HelpTotalStudios : Constants.HelpTotalTVNetworks, EStatCardSize.eSmall );
            var value = TotalStudioCountValue( user, movies );
            retVal.AddLine( value.ToString() );
            return retVal;
        }

        public StatCard TotalMovieStudioCount( User? user )
        {
            CheckIsValid( ECheckType.eReport );

            return TotalStudioCount( user, true );
        }

        public StatCard TotalTVStudioCount( User? user )
        {
            CheckIsValid( ECheckType.eReport );

            return TotalStudioCount( user, false );
        }

        public List<(int year, long count)> FavoriteYearValues( User? user, bool movies )
        {
            CheckIsValid( ECheckType.eReport );

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
            CheckIsValid( ECheckType.eReport );

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
                var genres = row.GetString( 0 )?.Split( ',' ) ?? Array.Empty<string>();
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
            CheckIsValid( ECheckType.eReport );

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

        private List<MediaItemResponse> getMediaListResponse( bool episodes )
        {
            var retVal = new List<MediaItemResponse>();

            var sql = "SELECT ";
            if( episodes )
                sql += "  PrimaryName || ' - S' || printf( '%02d', Season ) || 'E' || printf('%02d', Episode) || ' - ' || SecondaryName AS ListDisplayName";
            else
                sql += "  PrimaryName AS ListDisplayName";

            sql +=
                ", StartYear" +
                ", ResolutionDetail" +
                ", Codec" +
                ", DolbyVisionProfile" +
                ", ServerLocation" +
                ", ItemId" +
                ", ImageUrl" +
                " FROM " +
                "   Media ";
            if( episodes )
                sql += " WHERE IsEpisode ";
            else
                sql += " WHERE NOT IsEpisode ";

            sql += " ORDER BY PrimaryName ASC, Season ASC, Episode ASC ";
            _dbHelper.ExecuteCommand( new SQLCmdDef( sql ), statement =>
            {
                var row = statement.Current;
                var col = 0;
                var curr = new MediaItemResponse()
                {
                    ListDisplayName = row.GetString( col++ ),
                    StartYear = row.GetString( col++ ),
                    ResolutionDetail = row.GetString( col++ ),
                    Codec = row.GetString( col++ ),
                    DolbyVisionProfile = row.GetString( col++ ),
                    ServerLocation = row.GetString( col++ )
                };
                var itemId = row.GetString( col++ );
                var itemUrl = row.GetString( col++ );
                curr.ItemUrl = ItemImageUrl.ItemUrl( itemId, itemUrl, curr.ListDisplayName );
                if( curr.ItemUrl != null && curr.ItemUrl != string.Empty )
                {
                    curr.ListDisplayName = curr.ItemUrl;
                }

                if( curr.Codec != "hevc" && curr.Codec != "av1" )
                    curr.DolbyVisionProfile = string.Empty;

                retVal.Add( curr );
                return true;
            } );

            return retVal;
        }

        public List<MediaItemResponse> GetEpisodeList()
        {
            return getMediaListResponse( true );
        }

        public List<MediaItemResponse> GetMovieList()
        {
            return getMediaListResponse( false );
        }
    }
}
