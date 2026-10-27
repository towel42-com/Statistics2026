using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack;
using Statistics2026.Api;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public async Task AddAllMediaTaskImpl()
        {
            CheckIsValid( ECheckType.eUpdate );

            _embyInterfaces!.Logger().Debug( $"Starting Video Analysis" );

            _dbHelper!.Progress?.Report( 0 );
            var videoList = EmbyHelperFuncs.GetAllEpisodesAndMovies( null, LibraryManager() );
            _dbHelper!.Progress?.Report( 100 );

            if( videoList == null )
            {
                return;
            }

            double count = videoList.Count;
            var curr = 0.0;

            _dbHelper!.Progress?.Report( 0 );
            var sqlCmds = new List<SQLCmdDef>();
            var existing = new Dictionary<string, bool>();
            List<object> paramList = [];
            sqlCmds.Add( new SQLCmdDef( "DELETE FROM Media WHERE PremiereDate IS NULL" ) );

            foreach( var video in videoList )
            {
                if( video == null )
                    continue;

                _dbHelper!.Progress?.Report( 80.0 * ( ++curr ) / count );

                if( existing.ContainsKey( video.Id.ToString() ) )
                    continue;
                existing.Add( video.Id.ToString(), true );
                paramList.Add( video.Id.ToString() );

                await using( var mediaInfo = await MediaInfo.CreateAsync( video ).ConfigureAwait( false ) )
                {
                    if( !mediaInfo.aOK )
                        continue;

                    sqlCmds.AddRange( AddMediaInfo( mediaInfo ) );
                    _embyInterfaces!.Logger().Debug( $"    Processed Video ({curr} of {count}) - {mediaInfo.DescriptiveName}" );
                }

                _dbHelper!.CancellationToken?.ThrowIfCancellationRequested();
            }

            _dbHelper!.Progress?.Report( 80 );
            sqlCmds.Insert( 1, DBHelper.CreateSQLCmdDef( "DELETE FROM Media WHERE ItemId NOT IN (<KEYLIST>)", "<KEYLIST>", paramList ) );
            _dbHelper.ExecuteCommands( sqlCmds );
            _dbHelper!.Progress?.Report( 100 );
            _embyInterfaces!.Logger().Debug( $"Finished Video Analysis" );
        }

        public List<SQLCmdDef> AddMediaInfo( MediaInfo mediaInfo )
        {
            CheckIsValid( ECheckType.eUpdate );

            var sqlCmds = new List<SQLCmdDef>();
            if( mediaInfo == null || !mediaInfo.aOK )
            {
                _embyInterfaces!.Logger().Error( $"AddMediaInfo '{mediaInfo?.SortName}': is missing ItemId" );
                return sqlCmds;
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
            CheckIsValid( ECheckType.eReport );

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
            CheckIsValid( ECheckType.eReport );

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
                retVal.AddLine( DBHelper.FormatTicks( totalTicks ), false );
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
                var currStudios = HelperFuncs.JsonToStringArray( row.GetString( 0 ) );
                studios.UnionWith( currStudios );
                return true;
            } );

            return studios.Count();
        }

        public StatCard TotalStudioCount( User? user, bool movies )
        {
            CheckIsValid( ECheckType.eReport );

            var retVal = new TextBasedStatCard( movies ? Constants.TotalStudios : Constants.TotalTVNetworks, movies ? Constants.HelpTotalStudios : Constants.HelpTotalTVNetworks, EStatCardStyle.eCompact );
            var value = TotalStudioCountValue( user, movies );
            retVal.AddLine( value.ToString(), false );
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

        public enum EWhichMediaList
        {
            eOnServer = 0x01,
            eMissing = 0x02,
            eEpisodes = 0x10,
            eMovies = 0x20,
            eEpisodesOnServer = eEpisodes | eOnServer,
            eEpisodesMissing = eEpisodes | eMissing,
            eMoviesOnServer = eMovies | eOnServer,
            eMoviesMissing = eMovies | eMissing,
        };

        public string ToString( EWhichMediaList mediaList )
        {
            List<string> retVal = [];
            if( ( mediaList & EWhichMediaList.eEpisodes ) != 0 )
                retVal.Add( "Episodes" );

            if( ( mediaList & EWhichMediaList.eMovies ) != 0 )
                retVal.Add( "Episodes" );

            if( ( mediaList & EWhichMediaList.eOnServer ) != 0 )
                retVal.Add( "OnServer" );

            if( ( mediaList & EWhichMediaList.eMissing ) != 0 )
                retVal.Add( "Missing" );

            return string.Join( ", ", retVal );
        }

        private List<MediaItemResponse> getMediaListResponseOnServer( EWhichMediaList whichMedia )
        {
            var episodes = ( whichMedia & EWhichMediaList.eEpisodes ) != 0;

            var retVal = new List<MediaItemResponse>();
            var sql = string.Empty;

            var parentTableName = episodes ? "Series" : "Collections";
            List<string> columns = [];
            columns.Add( episodes ? "Media.PrimaryName || ' - S' || printf( '%02d', Media.Season ) || 'E' || printf('%02d', Media.Episode) || ' - ' || Media.SecondaryName" : "Media.PrimaryName" );
            columns.Add( "Media.PremiereDate" );
            columns.Add( "Media.StudioNames" );
            columns.Add( "Media.ResolutionDetail" );
            columns.Add( "Media.Codec" );
            columns.Add( "Media.DolbyVisionProfile" );
            columns.Add( "Media.ServerLocation" );
            columns.Add( "Media.ItemId" );
            columns.Add( "Media.ImageUrl" );
            columns.Add(
                $"'[' || \n" +
                $"   GROUP_CONCAT( \n" +
                $"                json_object(  'id',        {parentTableName}.ItemId\n" +
                $"                            , 'name',      {parentTableName}.Name\n" +
                $"                            , 'sort_name', {parentTableName}.SortName\n" +
                $"                            , 'url',       {parentTableName}.ImageUrl\n" +
                $"                           )\n" +
                $"   )\n" +
                "|| ']' AS ParentInfo"
                );

            var from = "Media";
            var joins = new List<string>();
            if( episodes )
            {
                joins.Add( "FULL OUTER JOIN Series ON Series.ItemId = Media.SeriesId" );
            }
            else
            {
                joins.Add( "FULL OUTER JOIN CollectionMembership ON Media.ItemId = CollectionMembership.ItemId" );
                joins.Add( "FULL OUTER JOIN Collections ON CollectionMembership.CollectionId = Collections.ItemId" );
            }

            List<string> whereClauses = [];
            if( episodes )
            {
                whereClauses.Add( "Media.IsEpisode" );
                whereClauses.Add( "NOT Media.IsTVSpecial" );
            }
            else
            {
                whereClauses.Add( "NOT Media.IsEpisode" );
            }

            var groupByClauses = new List<string>() { "Media.ItemId" };

            var orderBy = new List<string>() { "Media.SortName ASC", "Media.Season ASC", "Media.Episode ASC" };
            _dbHelper.ExecuteCommand(
                new SQLCmdDef( from, columns, joins, whereClauses, groupByClauses, orderBy ), statement =>
            {
                var row = statement.Current;
                var col = 0;
                var curr = new MediaItemResponse();
                curr.DisplayName = new SortByText( CleanSortName( row.GetString( col++ ) ), string.Empty );
                curr.PremiereDate = HelperFuncs.ReadDateTime( row.GetString( col++ ) )?.ToTZShortString( true, true ) ?? string.Empty;
                curr.StudioNames = new SortByText( HelperFuncs.JsonToStringArray( row.GetString( col++ ) ).Join( ", " ) );
                curr.ResolutionDetail = row.GetString( col++ );
                curr.Codec = row.GetString( col++ );
                curr.DolbyVisionProfile = row.GetString( col++ );
                if( curr.Codec != "hevc" && curr.Codec != "av1" )
                    curr.DolbyVisionProfile = string.Empty;
                curr.ServerLocation = new SortByText( row.GetString( col ), row.GetString( col ) );
                col++;

                curr.ItemId = row.GetString( col++ );
                var itemUrl = row.GetString( col++ );
                curr.ItemUrl = ItemImageUrl.ItemUrl( curr.ItemId, itemUrl, curr.DisplayName.Text );

                var parentInfo = row.GetString( col++ );
                curr.setParentInfo( parentInfo );

                retVal.Add( curr );
                return true;
            } );

            return retVal;
        }

        private string CleanSortName( string sortName )
        {
            var retVal = Regex.Replace( sortName, @"^[^a-zA-Z0-9]+", "" );
            return retVal;
        }

        public List<MediaItemResponse> getMediaListResponse( EWhichMediaList whichMedia )
        {
            List<MediaItemResponse> retVal = [];

            if( ( whichMedia & EWhichMediaList.eMissing ) != 0 )
            {
                retVal.AddRange( getMediaListResponseMissing( whichMedia ) );
            }

            if( ( whichMedia & EWhichMediaList.eOnServer ) != 0 )
            {
                retVal.AddRange( getMediaListResponseOnServer( whichMedia ) );
            }

            retVal.Sort(
                ( x, y ) =>
                {
                    var lhs = x.DisplayName.SortBy;
                    var rhs = y.DisplayName.SortBy;
                    return lhs.CompareTo( rhs );
                } );
            return retVal;
        }
    }
}
