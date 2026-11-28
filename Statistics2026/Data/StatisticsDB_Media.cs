using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Configuration;
using Statistics2026.Utilities;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        public async Task AddAllMediaTaskImplAsync()
        {
            CheckIsValid( ECheckLevel.eUpdate );

            Logger().Debug( $"Starting Video Analysis" );

            SetProgress( 0 );
            List<Video> videoList = EmbyHelperFuncs.GetAllEpisodesAndMovies( null, LibraryManager() );
            SetProgress( 100 );

            if( videoList == null )
            {
                return;
            }

            double count = videoList.Count;
            int completedCount = 0;

            SetProgress( 0 );

            await DeleteAllInvalidAsync( videoList.ConvertAll( item => (BaseItem)item ), "Media", "ItemId" ).ConfigureAwait( false );

            var sqlCmds = new List<SQLCmdDef>();
            var existing = new ConcurrentDictionary<string, bool>();
            sqlCmds.Add( new SQLCmdDef( "DELETE FROM Media WHERE PremiereDate IS NULL" ) );

            using( var timer = new AutoTimer( $"    Analyze Media - Getting Commands", Logger() ) )
            {
                using SemaphoreSlim gatekeeper = HelperFuncs.CreateSemaphoreSlim();

                IEnumerable<Task<(int index, Video video, List<SQLCmdDef?> cmds)>> pool = videoList.Select( ( video, index ) => Task.Run( async () =>
                {
                    await gatekeeper.WaitAsync().ConfigureAwait( false );
                    try
                    {
                        (int index, Video video, List<SQLCmdDef?> cmds) retVal = (index: index, video: video, cmds: new List<SQLCmdDef?>());
                        if( video == null )
                            return retVal;

                        if( existing.ContainsKey( video.Id.ToString() ) )
                            return retVal;

                        existing.TryAdd( video.Id.ToString(), true );

                        var mediaInfo = MediaInfo.Create( Logger(), LibraryManager(), video );
                        if( ( mediaInfo == null ) || !mediaInfo.aOK )
                            return retVal;

                        retVal.cmds.Add( AddMediaInfo( mediaInfo ) );
                        CancellationToken?.ThrowIfCancellationRequested();
                        int currProgress = Interlocked.Increment( ref completedCount );

                        SetProgress( 80.0 * ( 1.0 * currProgress ) / count );

                        Logger().Debug( $"    Processed Video ({currProgress} of {count}) - {mediaInfo.DescriptiveName}" );
                        return retVal;
                    }
                    finally
                    {
                        gatekeeper.Release();
                    }
                } ) );

                (int index, Video video, List<SQLCmdDef?> cmds)[] unsortedResults = await Task.WhenAll( pool ).ConfigureAwait( false );
                sqlCmds = unsortedResults
                            .Where( item => item.cmds != null )
                            .OrderBy( item => item.index )
                            .SelectMany( item => item.cmds! )
                            .Where( cmd => cmd != null )
                            .Select( cmd => cmd! )
                            .ToList();
            }

            using( var timer = new AutoTimer( $"    Analyze Media - Executing Commands", Logger() ) )
            {
                SetProgress( 80 );
                await ExecuteCommandsAsync( sqlCmds ).ConfigureAwait( false );
                SetProgress( 100 );
                Logger().Debug( $"Finished Media Analysis" );
            }
        }

        public SQLCmdDef? AddMediaInfo( MediaInfo mediaInfo )
        {
            CheckIsValid( ECheckLevel.eUpdate );

            if( mediaInfo == null || !mediaInfo.aOK )
            {
                Logger().Error( $"AddMediaInfo '{mediaInfo?.SortName}': is missing ItemId" );
                return null;
            }

            string sql =
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
                    ", ActorIds \n" +
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
                    ", @ActorIds \n" +
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
                    ", ActorIds=@ActorIds \n" +
                    ", ServerLocation=@ServerLocation\n" +
                    ", FileSize=@FileSize\n" +
                    ", ImageUrl=@ImageUrl\n" +
                    ", RunTimeTicks=@RunTimeTicks\n" +
                    ", Rating=@Rating\n" +
                    ", TotalBitrate=@TotalBitrate\n" +
                    ", PremiereDate=@PremiereDate\n" +
                    ", DateAdded=@DateAdded"
                    ;

            var retVal = new SQLCmdDef( sql,
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
                ("@ActorIds", JsonSerializer.Serialize(mediaInfo.ActorIds)),
                ("@ServerLocation", mediaInfo.ServerLocation),
                ("@FileSize", mediaInfo.FileSize),
                ("@ImageUrl",  mediaInfo.ImageUrl ?? string.Empty ),
                ("@RunTimeTicks", mediaInfo.RunTimeTicks),
                ("@Rating", mediaInfo.Rating),
                ("@TotalBitrate", mediaInfo.TotalBitrate),
                ("@PremiereDate", HelperFuncs.ToDateTimeParamValue( mediaInfo.PremiereDate ) ),
                ("@DateAdded", HelperFuncs.ToDateTimeParamValue( mediaInfo.DateAdded ) ),
            ] );

            return retVal;
        }

        public async Task<StatCard> MediaResolutionsAsync()
        {
            CheckIsValid( ECheckLevel.eReport );

            var retVal = new TableBasedStatCard( Constants.MediaResolutions, Constants.HelpMediaResolutions, [ "Movies", "Episodes" ] );

            if( Plugin.Instance!.Configuration.systemSettings.showAllResolutions )
            {
                retVal.addRow( Constants.HD, [ 0, 0 ] );
                retVal.addRow( Constants._4k, [ 0, 0 ] );
                retVal.addRow( Constants._8k, [ 0, 0 ] );
                retVal.addRow( Constants._720p, [ 0, 0 ] );
                retVal.addRow( Constants.SD, [ 0, 0 ] );
            }

            string sql =
                "SELECT \n" +
                "ResolutionBase as Resolution, \n" +
                "sum(IsEpisode) AS Episodes, \n" +
                "sum(NOT IsEpisode) AS Movies \n" +
                "FROM Media \n" +
                "GROUP BY Resolution \n" +
                "ORDER BY Resolution ASC"
                ;
            await ExecuteCommandAsync( new SQLCmdDef( sql ), statement =>
            {
                IResultSet row = statement.Current;
                string resolution = row.GetString( 0 );
                int episodeCount = row.GetInt( 1 );
                int movieCount = row.GetInt( 2 );
                retVal.addRow( resolution, [ movieCount, episodeCount ] );
                return Task.FromResult( true );
            } ).ConfigureAwait( false );

            return retVal;
        }

        public async Task<StatCard> MediaCodecsAsync()
        {
            CheckIsValid( ECheckLevel.eReport );

            var retVal = new TableBasedStatCard( Constants.MediaCodecs, Constants.HelpMediaCodecs, [ "Movies", "Episodes" ] );
            string sql =
                "SELECT \n" +
                "Codec as Codec, \n" +
                "sum(IsEpisode) AS Episodes, \n" +
                "sum(NOT IsEpisode) AS Movies \n" +
                "FROM Media \n" +
                "GROUP BY Codec \n" +
                "ORDER BY Codec ASC"
                ;

            await ExecuteCommandAsync( new SQLCmdDef( sql ), statement =>
            {
                IResultSet row = statement.Current;
                string codec = row.GetString( 0 );
                int episodeCount = row.GetInt( 1 );
                int movieCount = row.GetInt( 2 );
                retVal.addRow( codec, [ movieCount, episodeCount ] );
                return Task.FromResult( true );
            } ).ConfigureAwait( false );

            return retVal;
        }

        public async Task<StatCard> DVProfileInfoAsync()
        {
            CheckIsValid( ECheckLevel.eReport );

            string sql =
                "SELECT \n" +
                "DolbyVisionProfile as DVProfile, \n" +
                "sum(IsEpisode) AS Episodes, \n" +
                "sum(NOT IsEpisode) AS Movies \n" +
                "FROM Media ";

            if( !Plugin.Instance!.Configuration.systemSettings.showUnknownDVProfiles )
                sql += $"WHERE DolbyVisionProfile NOT IN ({string.Join( ",", Constants.UnknownDolbyProfiles.Select( p => $"'{p}'" ) )}) ";

            sql += "GROUP BY DolbyVisionProfile \n" +
                   "ORDER BY DolbyVisionProfile ASC"
                   ;

            var retVal = new TableBasedStatCard( Constants.DolbyVisionProfiles, Constants.HelpDolbyVisionProfile, [ "Movies", "Episodes" ] );
            if( Plugin.Instance!.Configuration.systemSettings.showUnknownDVProfiles )
                retVal.addRow( "Unknown Dolby Profile", [ 0, 0 ] );

            await ExecuteCommandAsync( new SQLCmdDef( sql ), statement =>
            {
                IResultSet row = statement.Current;
                string dvProfile = row.GetString( 0 );
                int episodeCount = row.GetInt( 1 );
                int movieCount = row.GetInt( 2 );
                retVal.addRow( dvProfile, [ movieCount, episodeCount ] );
                return Task.FromResult( true );
            } ).ConfigureAwait( false );

            return retVal;
        }

        public async Task<StatCard> TotalMovieCountAsync( User? user, bool watched )
        {
            CheckIsValid( ECheckLevel.eReport );

            string sql = string.Empty;
            var parameters = new List<(string, object?)>();
            string title = Constants.TotalMovies;
            string help = Constants.HelpTotalMovies;
            long total = 0;
            long totalTicks = 0;
            if( user == null )
            {
                sql = "SELECT SUM(NOT IsEpisode) FROM Media";
                totalTicks = ( await GetSingleValueFromSQLAsync( $"SELECT SUM(RunTimeTicks) FROM MEDIA WHERE NOT IsEpisode" ).ConfigureAwait( false ) ).ToLong();
            }
            else
            {
                title = watched ? Constants.TotalUserMoviesWatched : Constants.TotalUserMovies;
                help = watched ? Constants.HelpTotalUserMoviesWatched : Constants.HelpTotalUserMovies;

                sql = $"SELECT SUM(NOT IsEpisode) FROM {TableDefs().getUserTableName( user )} WHERE UserId=@UserId";
                if( watched )
                    sql += " AND IsPlayed";
                parameters.Add( ("@UserId", user.Id.ToString()) );

                if( watched )
                {
                    total = ( await GetSingleValueFromSQLAsync( $"SELECT SUM(NOT IsEpisode) FROM {TableDefs().getUserTableName( user )} WHERE UserId=@UserId", parameters ).ConfigureAwait( false ) ).ToLong();
                }
            }

            TextBasedStatCard retVal = await ValueGroupForSingleItemAsync( title, help, sql, parameters, count =>
                {
                    string retVal = count.ToString();

                    if( watched && total != 0 )
                    {
                        double value = 100.0 * count / ( 1.0 * total );
                        retVal += $" ({value:F1})%";
                    }

                    return retVal;
                } ).ConfigureAwait( false );

            if( totalTicks != 0 )
            {
                var rt = new RunTime( totalTicks );
                retVal.AddLine( FormatTicks( totalTicks ), false );
            }

            return retVal;
        }

        public async Task<StatCard> TotalTVCountAsync( User? user, bool watched, bool forSeries )
        {
            CheckIsValid( ECheckLevel.eReport );

            string runtimeColumn = string.Empty;

            List<(string name, object? value)>? paramList = null;

            string? tableName = TableDefs().getUserTableName( user );

            string? seriesColumn;

            string? seriesFrom;

            string? episodeColumn;
            string? episodeFrom;
            string? titleSeries;

            string? titleEpisodes;

            string? helpEpisodes;
            string? helpSeries;
            if( user == null )
            {
                seriesColumn = "COUNT(DISTINCT(PrimaryName))";
                episodeColumn = "SUM(NumEpisodes)";
                episodeFrom = seriesFrom = "Media WHERE IsEpisode AND NOT IsTVSpecial";

                titleSeries = Constants.TotalTVShows;
                titleEpisodes = Constants.TotalTVEpisodes;
                helpEpisodes = Constants.HelpTotalTVEpisodes;
                helpSeries = Constants.HelpTotalTVShows;
            }
            else
            {
                paramList = [ ("@UserId", user.Id.ToString()) ];

                seriesColumn = "COUNT(DISTINCT(Media.PrimaryName))";
                episodeColumn = $"SUM({tableName}.NumEpisodes)";

                titleSeries = Constants.TotalUserTVShows;
                titleEpisodes = Constants.TotalUserTVEpisodes;
                helpEpisodes = Constants.HelpTotalUserTVEpisodes;
                helpSeries = Constants.HelpTotalUserTVShows;

                string from = $"{tableName} LEFT JOIN Media ON {tableName}.ItemId=Media.ItemId WHERE Media.IsEpisode AND NOT Media.IsTVSpecial AND ( {tableName}.UserId=@UserId )";

                if( watched )
                {
                    from += $" AND ( {tableName}.IsPlayed )";

                    titleSeries = Constants.TotalTVShowsWatched;
                    titleEpisodes = Constants.TotalUserTVEpisodesWatched;
                    helpEpisodes = Constants.HelpTotalTVShowsWatched;
                }

                seriesFrom = episodeFrom = from;
            }

            TextBasedStatCard retVal;
            if( forSeries )
            {
                retVal = await ValueGroupForSingleItemAsync( titleSeries, helpSeries, $"SELECT {seriesColumn} FROM {seriesFrom}", paramList ).ConfigureAwait( false );
            }
            else
            {
                retVal = await ValueGroupForSingleItemAsync( titleEpisodes, helpEpisodes, $"SELECT {episodeColumn} FROM {episodeFrom}", paramList ).ConfigureAwait( false );
            }

            if( user == null )
            {
                string value = await GetSingleValueFromSQLAsync( "SELECT SUM(RunTimeTicks) FROM MEDIA WHERE IsEpisode" ).ConfigureAwait( false );
                value = FormatTicks( value.ToInt64() );
                retVal.AddLine( value, false );
            }

            return retVal;
        }

        private async Task<long> TotalStudioCountValueAsync( User? user, bool movies, bool forSeries )
        {
            CheckIsValid( ECheckLevel.eReport );

            string sql = "SELECT DISTINCT StudioNames FROM Media WHERE ";
            if( movies )
                sql += "NOT ";
            sql += "IsEpisode AND StudioNames IS NOT NULL AND StudioNames<>''";

            // Create an unordered set of strings
            HashSet<string> studios = [];

            var cmd = new SQLCmdDef( sql );
            await ExecuteCommandAsync( new SQLCmdDef( sql ), statement =>
            {
                IResultSet row = statement.Current;
                List<string> currStudios = HelperFuncs.JsonToStringArray( row.GetString( 0 ) );
                studios.UnionWith( currStudios );
                return Task.FromResult( true );
            } ).ConfigureAwait( false );

            return studios.Count();
        }

        private async Task<StatCard> TotalStudioCountAsync( User? user, bool movies, bool forSeries )
        {
            CheckIsValid( ECheckLevel.eReport );

            var retVal = new TextBasedStatCard( movies ? Constants.TotalStudios : Constants.TotalTVNetworks, movies ? Constants.HelpTotalStudios : Constants.HelpTotalTVNetworks, EStatCardStyle.eCompact );
            long value = await TotalStudioCountValueAsync( user, movies, forSeries ).ConfigureAwait( false );
            retVal.AddLine( value.ToString(), false );
            return retVal;
        }

        public Task<StatCard> TotalMovieStudioCountAsync( User? user )
        {
            CheckIsValid( ECheckLevel.eReport );

            return TotalStudioCountAsync( user, true, false );
        }

        public Task<StatCard> TotalTVStudioCountAsync( User? user, bool forSeries )
        {
            CheckIsValid( ECheckLevel.eReport );

            return TotalStudioCountAsync( user, false, forSeries );
        }

        private async Task<List<(int year, long count)>> FavoriteYearValuesAsync( User? user, bool movies )
        {
            CheckIsValid( ECheckLevel.eReport );

            if( user == null )
                throw new ArgumentNullException( "user" );

            string? tableName = TableDefs().getUserTableName( user );
            string sql =
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
              + $"LIMIT {Plugin.Instance!.Configuration.GetNumFavoriteYears(user)} "
              ;

            var sqlCmd = new SQLCmdDef( sql,
                                        [
                                            ( "@UserId", user.Id.ToString())
                                        ] );

            var retVal = new List<(int year, long count)>();
            await ExecuteCommandAsync( sqlCmd, statement =>
            {
                IResultSet row = statement.Current;
                long count = row.GetInt64( 0 );
                int year = row.GetInt( 1 );
                retVal.Add( (year, count) );
                return Task.FromResult( true );
            } ).ConfigureAwait( false );

            return retVal;
        }

        public async Task<StatCard> FavoriteYearsAsync( User? user, bool movies )
        {
            string videoType = movies ? "Movies" : "Episodes";
            var retVal = new TableBasedStatCard( Constants.FavoriteMovieYears, "Genre", [ $"# of {videoType} Watched" ] );
            retVal.SetDataColumnAlignment( 0, StatCard.EAlignment.eCenter );
            List<(int year, long count)> values = await FavoriteYearValuesAsync( user, movies ).ConfigureAwait( false );

            foreach( (int year, long count) in values )
            {
                retVal.addRow( year.ToString(), [ count ] );
            }

            return retVal;
        }


        private async Task<Dictionary<string, (int total, Dictionary<string, int> parentToChildMapping)>> GroupedMediaKeyCountValuesAsync( string mediaColumnName, bool isJSONArray, User? user, bool movies, bool playedOnly )
        {
            CheckIsValid( ECheckLevel.eReport );

            string? tableName = ( user != null ) ? TableDefs().getUserTableName( user ) : string.Empty;
            string sql =
                $"SELECT PrimaryName, {mediaColumnName}, COUNT({mediaColumnName}) From Media ";
            if( user != null )
            {
                sql += $"INNER JOIN {tableName} On Media.ItemId={tableName}.ItemId ";
            }

            List<string> whereClauses = [];
            if( movies )
            {
                whereClauses.Add( "NOT Media.IsEpisode" );
            }
            else
            {
                whereClauses.Add( "Media.IsEpisode" );
            }

            if( user != null )
            {
                whereClauses.Add( "UserId=@UserId" );
            }

            if( playedOnly && user != null )
            {
                whereClauses.Add( $"{tableName}.IsPlayed" );
            }

            sql += HelperFuncs.JoinWhereClauses( whereClauses );

            sql += " GROUP BY Media.PrimaryName";

            List<(string name, object? value)>? parameters = null;
            if( user != null )
                parameters = [ ("@UserId", user.Id.ToString()) ];
            var sqlCmd = new SQLCmdDef( sql, parameters );

            Dictionary<string, (int total, Dictionary<string, int> parentToChildMapping)> retVal = [];
            await ExecuteCommandAsync( sqlCmd, statement =>
            {
                IResultSet row = statement.Current;
                int pos = 0;
                string name = row.GetString( pos++ );
                string valueString = row.GetString( pos++ );
                List<string> values = [];
                if( isJSONArray )
                    values = HelperFuncs.JsonToStringArray( valueString );
                else
                    values.Add( valueString );
                int num = row.GetInt( pos++ );
                foreach( string value in values )
                {
                    if( !retVal.ContainsKey( value ) )
                        retVal[ value ] = (0, new Dictionary<string, int>());

                    (int total, Dictionary<string, int> parentToChildMapping) curr = retVal[ value ];
                    curr.total += movies ? 1 : num; // if there are multiple copies of the same movie, only count it once
                    curr.parentToChildMapping[ name ] = movies ? 1 : num;
                    retVal[ value ] = curr;
                }

                return Task.FromResult( true );
            } ).ConfigureAwait( false );

            return retVal;
        }

        // genre to series/moviename -> number of episodes/movies
        private Task<Dictionary<string, (int total, Dictionary<string, int> parentToChildMapping)>> GenreCountValuesAsync( User? user, bool movies, bool playedOnly, bool? series = null )
        {
            return GroupedMediaKeyCountValuesAsync( "Genres", true, user, movies, playedOnly );
        }

        public async Task<StatCard> FavoriteGenreAsync( User? user, bool movies )
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
                videoType = "Series";
                title = Constants.FavoriteTVGenres;
            }

            var retVal = new TableBasedStatCard( title, "Genre", [ $"# of {videoType} Watched" ] );
            retVal.SetDataColumnAlignment( 0, StatCard.EAlignment.eCenter );

            Dictionary<string, (int total, Dictionary<string, int> parentToChildMapping)> genreMap = await GenreCountValuesAsync( user, movies, true, true ).ConfigureAwait( false );
            var sortedGenre = genreMap.OrderByDescending( kvp => kvp.Value.total ).ToList();

            EReportType reportType = user != null ? EReportType.eUserReport : ( movies ? EReportType.eMovieReport : EReportType.eTVReport );
            for( int ii = 0; ( ii < sortedGenre.Count() ) && ( ii < Plugin.Instance!.Configuration.NumGenresToReport( user, reportType ) ); ++ii )
            {
                KeyValuePair<string, (int total, Dictionary<string, int> parentToChildMapping)> curr = sortedGenre[ ii ];
                retVal.addRow( curr.Key, [ curr.Value.total ] );
            }

            return retVal;
        }

        public async Task<(int current, int inDB)> TotalMediaCountAsync()
        {
            CheckIsValid( ECheckLevel.eReport );

            List<Video> videoList = EmbyHelperFuncs.GetAllEpisodesAndMovies( null, LibraryManager() );

            int currentCount = 0;
            if( videoList != null )
            {
                var existing = new Dictionary<string, bool>();
                foreach( Video video in videoList )
                {
                    if( video == null )
                        continue;
                    if( existing.ContainsKey( video.Id.ToString() ) )
                        continue;
                    existing.Add( video.Id.ToString(), true );
                }
                currentCount = existing.Count();
            }

            string sql = "SELECT COUNT(*) FROM Media";
            int inDBCount = 0;
            await ExecuteCommandAsync( new SQLCmdDef( sql ), statement =>
            {
                IResultSet row = statement.Current;
                inDBCount = row.GetInt( 0 );
                return Task.FromResult( true );
            } ).ConfigureAwait( false );

            return (currentCount, inDBCount);
        }

        public async Task<StatCard> GenreListAsync( User? user, bool movies, bool? series = null )
        {
            CheckIsValid( ECheckLevel.eReport );

            Dictionary<string, (int total, Dictionary<string, int> parentToChildMapping)> genreMap = await GenreCountValuesAsync( null, movies, false, series ).ConfigureAwait( false );

            string title = movies ? Constants.MovieGenres : ( ( series == null || series! == true ) ? Constants.TVSeriesGenres : Constants.TVEpisodeGenres );
            string help = movies ? Constants.MovieGenresHelp : ( ( series == null || series! == true ) ? Constants.TVSeriesGenresHelp : Constants.TVEpisodeGenresHelp );

            var retVal = new GroupedTextBasedStatCard( title, help, EStatCardStyle.eDetailed )
            {
                SubTitle = $"Total Number of Genres: {genreMap.Count()}",
                ListType = EListType.eNumbered
            };

            var sortedGenre = genreMap.OrderByDescending( kvp =>
                {
                    if( !movies && ( series == null || series! == true ) )
                        return kvp.Value.parentToChildMapping.Count();
                    return kvp.Value.total;
                }
            ).ToList();

            for( int ii = 0; ( ii < sortedGenre.Count() ) && ( ii < Plugin.Instance!.Configuration.NumGenresToReport( user, movies ? EReportType.eMovieReport : EReportType.eTVReport ) ); ++ii )
            {
                KeyValuePair<string, (int total, Dictionary<string, int> parentToChildMapping)> curr = sortedGenre[ ii ];

                Genre? genre = EmbyHelperFuncs.GetGenreByName( curr.Key, _embyInterfaces );

                string text = string.Empty;

                if( movies )
                    text = $"{curr.Key}: {curr.Value.total} Movie{( ( curr.Value.total == 1 ) ? "" : "s" )}";
                else if( series == null || series! == true )
                    text = $"{curr.Key}: {curr.Value.parentToChildMapping.Count()} Series";
                else
                    text = $"{curr.Key}: {curr.Value.total} Episode{( ( curr.Value.total == 1 ) ? "" : "s" )}";

                text = GetItemImageUrl.ItemHtmlFromItem( genre, text, Plugin.Instance!.Configuration.ShowImages( user, movies ? EReportType.eMovieReport : EReportType.eTVReport ) );

                retVal.AddLine( curr.Key, text, false );
            }

            return retVal;
        }

        // range 0->.5, .5->1, 1->1.5
        private async Task<List<((double start, double end) range, int count)>> RatingsCountValuesAsync( bool movies, bool playedOnly )
        {
            CheckIsValid( ECheckLevel.eReport );

            string sql =
                $"SELECT \n" +
                $"   FLOOR(Rating/.5)*.5 as RangeStart,\n" +
                $"   (FLOOR(Rating/.5)+1)*.5 as RangeEnd,\n" +
                $"   COUNT(*) as Count\n" +
                $"FROM Media\n";


            List<string> whereClauses = [];
            if( movies )
            {
                whereClauses.Add( "NOT Media.IsEpisode" );
            }
            else
            {
                whereClauses.Add( "Media.IsEpisode" );
            }

            if( playedOnly )
            {
                whereClauses.Add( "Media.IsPlayed" );
            }

            sql += HelperFuncs.JoinWhereClauses( whereClauses );

            sql +=
                $"GROUP BY FLOOR(Rating/.5)\n" +
                $"ORDER BY RangeStart DESC\n";

            var sqlCmd = new SQLCmdDef( sql );

            Dictionary<(double start, double end), int> map = [];
            await ExecuteCommandAsync( sqlCmd, statement =>
            {
                IResultSet row = statement.Current;
                int pos = 0;

                double rangeStart = row.GetDouble( pos++ );
                double rangeEnd = row.GetDouble( pos++ );
                int count = row.GetInt( pos++ );

                map[ (rangeStart, rangeEnd) ] = count;
                return Task.FromResult( true );
            } ).ConfigureAwait( false );

            for( int ii = 0; ii < 10; ++ii )
            {
                if( ( ii > 0 ) && !map.TryGetValue( (ii - .5, ii), out int _ ) )
                {
                    map[ (ii - .5, ii) ] = 0;
                }
                if( !map.TryGetValue( (ii, ii + 0.5), out int _ ) )
                {
                    map[ (ii, ii + .5) ] = 0;
                }
            }

            var retVal = map.OrderByDescending( kvp => kvp.Key.start ).Select( pair => (pair.Key, pair.Value) ).ToList();
            return retVal;
        }

        public async Task<StatCard> RatingsListAsync( bool movies )
        {
            CheckIsValid( ECheckLevel.eReport );

            string title = movies ? Constants.MovieRatings : Constants.TVRatings;
            string help = movies ? Constants.MovieRatingsHelp : Constants.TVRatingsHelp;

            var retVal = new TextBasedStatCard( title, help, EStatCardStyle.eDetailed )
            {
                ListType = EListType.eRangeOrdered
            };
            string star = "<i class=\"md-icon md-icon-fill starIcon\">&#xE838;</i>";
            retVal.SubTitle = $"Ratings from 0.0 to 10.0{star}";

            List<((double start, double end) range, int count)> ratings = await RatingsCountValuesAsync( movies, false ).ConfigureAwait( false );
            foreach( ((double start, double end) range, int count) curr in ratings )
            {
                (double start, double end) range = curr.range;
                int count = curr.count;
                string value = $"{count} {( movies ? "Movies" : "Episodes" )}";
                if( count == 1 )
                    value = value[ ..^1 ];

                retVal.AddLine( ($"{range.start:F1}", $"{range.end:F1}{star}"), value, false );
            }

            return retVal;
        }
    }
}
