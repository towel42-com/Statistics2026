using MediaBrowser.Model.Querying;
using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Responses;
using Statistics2026.Services;
using Statistics2026.Utilities;
using System.Text.RegularExpressions;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
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

        public enum EMediaListResponseColumns
        {
            eMissing,
            eTitle,
            eSortName,
            ePremiereDate,
            eStudioNames,
            eStreamingProviders,
            eResolutionDetail,
            eCodec,
            eDolbyVisionProfile,
            eServerLocation,
            eItemId,
            eItemImageUrl,
            eParentInfoJSON,
            ePosterPath,
            eSeasonNum,
            eEpisodeNum,
            eParentId, // empty if on server
        };

        private string getMediaListResponseMissingCmd( EWhichMediaList whichMedia, ref List<string> orderBy, bool filterOutFuture )
        {
            bool episodes = ( whichMedia & EWhichMediaList.eEpisodes ) != 0;

            List<string> columns = [];
            columns.Add( "1 as IsMissing" );
            columns.Add( "Missing.Title AS Title" );
            columns.Add( "Missing.Title AS SortName" );
            columns.Add( "Missing.ReleaseDate AS PremiereDate" );
            columns.Add( "Missing.StudioNames" );
            columns.Add( "Missing.StreamingProviders AS StreamingProviders" );
            columns.Add( "'' AS ResolutionDetail" );
            columns.Add( "'' AS Codec" );
            columns.Add( "'' AS DolbyVisionProfile" );
            columns.Add( "'' AS ServerLocation" );
            columns.Add( "'' AS ItemId" );
            columns.Add( "'' AS ImageURL" );

            string parentTableName = episodes ? "Series" : "Collections";
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

            columns.Add( "Missing.PosterPath AS PosterPath" );
            columns.Add( "Missing.SeasonNum AS SeasonNum" );
            columns.Add( "Missing.EpisodeNum AS EpisodeNum" );
            columns.Add( "Missing.ParentId AS ParentId" );

            string from = "Missing";
            List<string> joins = [];
            List<string> whereClauses = [];

            if( episodes )
            {
                joins.Add( "LEFT JOIN Series ON Series.ItemId=Missing.ParentId" );
                whereClauses.Add( "Missing.IsEpisode" );
            }
            else
            {
                joins.Add( "LEFT JOIN Collections ON Collections.ItemId=Missing.ParentId" );
                whereClauses.Add( "NOT Missing.IsEpisode" );
            }

            if( filterOutFuture )
            {
                whereClauses.Add( "ReleaseDate < date('now', '+1 day')" );
            }

            orderBy.Add( "SortName ASC" );
            if( episodes )
            {
                orderBy.Add( "SeasonNum ASC" );
                orderBy.Add( "EpisodeNum ASC" );
            }
            var groupByClauses = new List<string>() { "Missing.Title" };
            string retVal = SQLCmdDef.CreateCmd( from, columns, joins, whereClauses, groupByClauses, null );
            return retVal;
        }

        private string getMediaListResponseMissingCmd( EWhichMediaList whichMedia, bool filterOutFuture )
        {
            List<string> orderBy = [];
            return getMediaListResponseMissingCmd( whichMedia, ref orderBy, filterOutFuture );
        }

        private string getSeriesListResponseOnServerCmd( string orderByClause )
        {
            List<string> columns = [];
            columns.Add( "0 AS IsMissing" );
            columns.Add( "Series.Name AS Title" );
            columns.Add( "Series.SortName as SortName" );
            columns.Add( "Series.PremiereDate AS PremiereDate" );
            columns.Add( "'' AS StudioNames" );
            columns.Add( "Series.StreamingProviders AS StreamingProviders" );
            columns.Add( "'' AS ResolutionDetail" );
            columns.Add( "'' AS Codec" );
            columns.Add( "'' AS DolbyVisionProfile" );
            columns.Add( "ServerLocation AS ServerLocation" );
            columns.Add( "Series.ItemId AS ItemId" );
            columns.Add( "Series.ImageUrl AS ImageUrl" );
            columns.Add( "'' AS ParentInfo" );

            columns.Add( "'' AS PosterPath" );
            columns.Add( "'' AS SeasonNum" );
            columns.Add( "'' AS EpisodeNum" );
            columns.Add( "'' AS ParentId" );
            columns.Add( "NumEpisodes as NumEpisodes" );
            columns.Add( "(SELECT SUM(NumEpisodes) FROM Media WHERE Series.ItemId=Media.SeriesId AND Media.IsEpisode AND NOT Media.IsTVSpecial AND Media.Season > 0 AND Media.Episode > 0 AND ServerLocation NOT like '%featurettes%') AS collectedEpisodes" );
            columns.Add( "NumSpecials as NumSpecials" );
            columns.Add( "(SELECT SUM(NumEpisodes) FROM Media WHERE Series.ItemId=Media.SeriesId AND Media.IsEpisode AND     ( Media.IsTVSpecial OR Media.Season == 0 OR Media.Episode <= 0 ) ) AS collectedSpecials" );

            string from = "Series";
            List<string> joins = [];
            List<string> whereClauses = [];
            List<string> groupByClauses = [];
            List<string> orderBy = new() { orderByClause };

            return SQLCmdDef.CreateCmd( from, columns, joins, whereClauses, groupByClauses, orderBy );
        }

        private string getCollectionListResponseOnServerCmd( string orderByClause )
        {
            List<string> columns = [];
            columns.Add( "0 AS IsMissing" );
            columns.Add( "Collections.Name AS Title" );
            columns.Add( "Collections.SortName as SortName" );
            columns.Add( "'''' AS PremiereDate" );
            columns.Add( "'' AS StudioNames" );
            columns.Add( "'' AS StreamingProviders" );
            columns.Add( "'' AS ResolutionDetail" );
            columns.Add( "'' AS Codec" );
            columns.Add( "'' AS DolbyVisionProfile" );
            columns.Add( "'' AS ServerLocation" );
            columns.Add( "Collections.ItemId AS ItemId" );
            columns.Add( "Collections.ImageUrl AS ImageUrl" );
            columns.Add( "'' AS ParentInfo" );

            columns.Add( "'' AS PosterPath" );
            columns.Add( "'' AS SeasonNum" );
            columns.Add( "'' AS EpisodeNum" );
            columns.Add( "'' AS ParentId" );
            columns.Add( "NumMovies as NumMovies" );
            columns.Add( "(SELECT COUNT(*) FROM CollectionMembership WHERE Collections.ItemId=CollectionMembership.CollectionId ) AS collectedMovies" );

            string from = "Collections";
            List<string> joins = [];
            List<string> whereClauses = [];
            List<string> groupByClauses = [];
            List<string> orderBy = string.IsNullOrEmpty( orderByClause ) ? [] : [ orderByClause ];

            return SQLCmdDef.CreateCmd( from, columns, joins, whereClauses, groupByClauses, orderBy );
        }

        private string getMediaListResponseOnServerCmd( EWhichMediaList whichMedia, ref List<string> orderBy )
        {
            bool episodes = ( whichMedia & EWhichMediaList.eEpisodes ) != 0;

            List<string> columns = [];
            columns.Add( "0 as IsMissing" );
            columns.Add( episodes ? "Media.PrimaryName || ' - S' || printf( '%02d', Media.Season ) || 'E' || printf('%02d', Media.Episode) || ' - ' || Media.SecondaryName AS Title" : "Media.PrimaryName AS Title" );
            columns.Add( "Media.SortName as SortName" );
            columns.Add( "Media.PremiereDate AS PremiereDate" );
            columns.Add( "Media.StudioNames AS StudioNames" );
            if( episodes )
                columns.Add( "Series.StreamingProviders AS StreamingProviders" );
            else
                columns.Add( "Media.StreamingProviders AS StreamingProviders" );
            columns.Add( "Media.ResolutionDetail AS ResolutionDetail" );
            columns.Add( "Media.Codec AS Codec" );
            columns.Add( "Media.DolbyVisionProfile AS DolbyVisionProfile" );
            columns.Add( "Media.ServerLocation AS ServerLocation" );
            columns.Add( "Media.ItemId AS ItemId" );
            columns.Add( "Media.ImageUrl AS ImageUrl" );

            string parentTableName = episodes ? "Series" : "Collections";
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

            columns.Add( "'' AS PosterPath" );
            columns.Add( "Media.Season AS SeasonNum" );
            columns.Add( "Media.Episode AS EpisodeNum" );
            columns.Add( "'' AS ParentId" );

            string from = "Media";
            List<string> joins = [];
            List<string> whereClauses = [];
            if( episodes )
            {
                joins.Add( "FULL OUTER JOIN Series ON Series.ItemId = Media.SeriesId" );
                whereClauses.Add( "Media.IsEpisode" );
                whereClauses.Add( "NOT Media.IsTVSpecial" );
            }
            else
            {
                joins.Add( "FULL OUTER JOIN CollectionMembership ON Media.ItemId = CollectionMembership.ItemId" );
                joins.Add( "FULL OUTER JOIN Collections ON CollectionMembership.CollectionId = Collections.ItemId" );
                whereClauses.Add( "NOT Media.IsEpisode" );
            }

            var groupByClauses = new List<string>() { "Media.ItemId" };

            orderBy = new List<string>() { "SortName ASC", "SeasonNum ASC", "EpisodeNum ASC" };

            return SQLCmdDef.CreateCmd( from, columns, joins, whereClauses, groupByClauses, null );
        }

        private string getMediaListResponseOnServerCmd( EWhichMediaList whichMedia )
        {
            List<string> orderBy = [];
            return getMediaListResponseOnServerCmd( whichMedia, ref orderBy );
        }

        private string CleanSortName( string sortName )
        {
            string retVal = Regex.Replace( sortName, @"^[^a-zA-Z0-9]+", "" );
            return retVal;
        }

        public async Task<QueryResult<MediaItemResponse>> GetCollectionListResponseAsync( TabulatorTableOptions tableOptions )
        {
            if( _embyInterfaces == null )
                return new QueryResult<MediaItemResponse>();

            string orderByClause = BuildCollectionOrderByClause( tableOptions.SortBy, tableOptions.SortOrder );
            string baseSql = getCollectionListResponseOnServerCmd( orderByClause );
            bool isPaged = tableOptions.Limit > 0;

            // 1. Get total count excluding empty collections (NumMovies == 0 && collectedMovies == 0)
            int totalRecordCount = 0;
            if( isPaged )
            {
                string countSql = $"SELECT COUNT(*) FROM (\n{getCollectionListResponseOnServerCmd( string.Empty )}\n) AS count_subquery\nWHERE NumMovies > 0 OR collectedMovies > 0";
                await ExecuteCommandAsync( new SQLCmdDef( countSql ), ( statement ) =>
                {
                    totalRecordCount = statement.Current.GetInt( 0 );
                    return Task.FromResult( false );
                } ).ConfigureAwait( false );
            }

            // 2. Filter empty collections and apply LIMIT/OFFSET
            string pagedSql = $"SELECT * FROM (\n{baseSql}\n) AS paged_subquery\nWHERE NumMovies > 0 OR collectedMovies > 0";

            if( isPaged )
            {
                int safeOffset = Math.Max( 0, tableOptions.StartIndex );
                pagedSql += $"\nLIMIT {tableOptions.Limit} OFFSET {safeOffset}";
            }

            string isoCode = HelperFuncs.GetCountryCode( _embyInterfaces!.HttpClient(), CancellationToken );

            List<MediaItemResponse> retVal = [];
            await ExecuteCommandAsync( new SQLCmdDef( pagedSql ), async ( statement ) =>
            {
                IResultSet row = statement.Current;
                MediaItemResponse? mediaInfoResponse = await MediaItemResponse.CreateFromStatementRowAsync(
                    row,
                    false,
                    isoCode,
                    null,
                    _embyInterfaces,
                    this ).ConfigureAwait( false );

                if( mediaInfoResponse != null )
                {
                    int pos = ( (int)EMediaListResponseColumns.eParentId ) + 1;
                    int numMovies = row.GetInt( pos++ );
                    int moviesCollected = row.GetInt( pos++ );

                    if( numMovies == 0 && moviesCollected == 0 )
                    {
                        return true;
                    }

                    if( numMovies == 0 )
                        numMovies = moviesCollected;

                    double moviesPercent = ( numMovies == 0 ) ? 100.0 : ( 100.0 * moviesCollected / ( 1.0 * numMovies ) );

                    string collectionStatus = $"{moviesCollected}/{numMovies} ({moviesPercent:F0}%)";
                    mediaInfoResponse.Extra = collectionStatus;

                    retVal.Add( mediaInfoResponse );
                }

                return true;
            } ).ConfigureAwait( false );

            return new QueryResult<MediaItemResponse>
            {
                Items = retVal.ToArray(),
                TotalRecordCount = isPaged ? totalRecordCount : retVal.Count
            };
        }

        private static string BuildCollectionOrderByClause( string sortBy, string sortOrder )
        {
            string direction = string.Equals( sortOrder, "Descending", StringComparison.OrdinalIgnoreCase )
                ? "DESC"
                : "ASC";

            if( string.Equals( sortBy, "CollectionProgress", StringComparison.OrdinalIgnoreCase ) )
            {
                return $"(CASE WHEN NumMovies = 0 THEN 100.0 ELSE (100.0 * collectedMovies / CAST(NumMovies AS REAL)) END) {direction}, SortName COLLATE NOCASE ASC";
            }

            return $"SortName COLLATE NOCASE {direction}";
        }

        public async Task<QueryResult<MediaItemResponse>> GetSeriesListResponseAsync( TabulatorTableOptions tableOptions )
        {
            if( _embyInterfaces == null )
                return new QueryResult<MediaItemResponse>();

            string orderByClause = BuildSeriesOrderByClause( tableOptions.SortBy, tableOptions.SortOrder );
            bool isPaged = tableOptions.Limit > 0;

            // 1. Get total record count when paging is active
            int totalRecordCount = 0;
            if( isPaged )
            {
                string countSql = $"SELECT COUNT(*) FROM (\n{getSeriesListResponseOnServerCmd( string.Empty )}\n) AS count_subquery";
                await ExecuteCommandAsync( new SQLCmdDef( countSql ), ( statement ) =>
                {
                    totalRecordCount = statement.Current.GetInt( 0 );
                    return Task.FromResult( false );
                } ).ConfigureAwait( false );
            }

            // 2. Build ordered query and apply LIMIT/OFFSET
            string pagedSql = getSeriesListResponseOnServerCmd( orderByClause );
            if( isPaged )
            {
                int safeOffset = Math.Max( 0, tableOptions.StartIndex );
                pagedSql += $"\nLIMIT {tableOptions.Limit} OFFSET {safeOffset}";
            }

            string isoCode = HelperFuncs.GetCountryCode( _embyInterfaces!.HttpClient(), CancellationToken );

            List<MediaItemResponse> retVal = [];
            await ExecuteCommandAsync( new SQLCmdDef( pagedSql ), async ( statement ) =>
            {
                IResultSet row = statement.Current;
                MediaItemResponse? mediaInfoResponse = await MediaItemResponse.CreateFromStatementRowAsync(
                    row,
                    null,
                    isoCode,
                    null,
                    _embyInterfaces,
                    this ).ConfigureAwait( false );

                if( mediaInfoResponse != null )
                {
                    int pos = ( (int)EMediaListResponseColumns.eParentId ) + 1;
                    int numEpisodes = row.GetInt( pos++ );
                    int episodesCollected = row.GetInt( pos++ );
                    int numSpecials = row.GetInt( pos++ );
                    int specialsCollected = row.GetInt( pos++ );

                    double episodesPercent = ( numEpisodes == 0 ) ? 100.0 : ( 100.0 * episodesCollected / ( 1.0 * numEpisodes ) );
                    string episodeStatus = $"{episodesCollected}/{numEpisodes} ({episodesPercent:F0}%)";
                    mediaInfoResponse.Extra = episodeStatus;

                    double specialsPercent = ( numSpecials == 0 ) ? 100.0 : ( 100.0 * specialsCollected / ( 1.0 * numSpecials ) );
                    string specialsStatus = $"{specialsCollected}/{numSpecials} ({specialsPercent:F0}%)";
                    mediaInfoResponse.Extra2 = specialsStatus;

                    retVal.Add( mediaInfoResponse );
                }

                return true;
            } ).ConfigureAwait( false );

            return new QueryResult<MediaItemResponse>
            {
                Items = retVal.ToArray(),
                TotalRecordCount = isPaged ? totalRecordCount : retVal.Count
            };
        }

        private static string BuildSeriesOrderByClause( string sortBy, string sortOrder )
        {
            string direction = string.Equals( sortOrder, "Descending", StringComparison.OrdinalIgnoreCase )
                ? "DESC"
                : "ASC";

            return ( sortBy ?? string.Empty ).ToLowerInvariant() switch
            {
                "premieredate" => $"PremiereDate {direction}, SortName COLLATE NOCASE ASC",
                "collectedinfoep" => $"(CASE WHEN NumEpisodes = 0 THEN 100.0 ELSE (100.0 * EpisodesCollected / CAST(NumEpisodes AS REAL)) END) {direction}, SortName COLLATE NOCASE ASC",
                "collectedinfosp" => $"(CASE WHEN NumSpecials = 0 THEN 100.0 ELSE (100.0 * SpecialsCollected / CAST(NumSpecials AS REAL)) END) {direction}, SortName COLLATE NOCASE ASC",
                "serverlocation" => $"ServerLocation COLLATE NOCASE {direction}, SortName COLLATE NOCASE ASC",
                "streamingservies" => $"StreamingProviders COLLATE NOCASE {direction}, SortName COLLATE NOCASE ASC",
                _ => $"SortName COLLATE NOCASE {direction}"
            };
        }

        private static string BuildMediaListOrderByClause( string sortBy, string sortOrder )
        {
            string direction = string.Equals( sortOrder, "Descending", StringComparison.OrdinalIgnoreCase )
                ? "DESC"
                : "ASC";

            // Map Tabulator's field names to your SQL SELECT column aliases
            string sqlColumn = ( sortBy ?? string.Empty ).ToLowerInvariant() switch
            {
                "parentname" => "ParentName",
                "premieredate" => "PremiereDate",
                "resolution" => "Resolution",
                "codec" => "Codec",
                "dolbyvisionprofile" => "DolbyVisionProfile",
                "serverlocation" => "Path",
                _ => "SortName" // Default sort (also matches "name")
            };

            return $"ORDER BY {sqlColumn} COLLATE NOCASE {direction}";
        }

        private static readonly Dictionary<string, string> MediaListFilterColumnMap = new( StringComparer.OrdinalIgnoreCase )
        {
            [ "Name" ] = "Title",
            [ "ParentName" ] = "ParentInfo",
            [ "Resolution" ] = "ResolutionDetail",
            [ "Codec" ] = "Codec",
            [ "DolbyVisionProfile" ] = "DolbyVisionProfile",
            [ "ServerLocation" ] = "ServerLocation"
        };

        private static List<string> BuildWhereClausesFromFilterMap(
            Dictionary<string, string> filterMap,
            Dictionary<string, string> columnMap,
            out List<(string name, object? value)> paramList )
        {
            List<string> conditions = [];
            paramList = [];
            int paramIndex = 0;

            foreach( KeyValuePair<string, string> kvp in filterMap )
            {
                if( string.IsNullOrWhiteSpace( kvp.Value ) )
                    continue;

                if( columnMap.TryGetValue( kvp.Key, out string? sqlColumn ) )
                {
                    string paramName = $"@Filter{paramIndex++}";
                    conditions.Add( $"{sqlColumn} LIKE {paramName} COLLATE NOCASE" );
                    paramList.Add( (paramName, $"%{kvp.Value.Trim()}%") );
                }
            }

            return conditions;
        }


        public async Task<QueryResult<MediaItemResponse>> GetMediaListResponseAsync( EWhichMediaList whichMedia, TabulatorTableOptions tableOptions )
        {
            if( _embyInterfaces == null )
                return new QueryResult<MediaItemResponse>();

            List<string> sqlCmds = [];
            if( ( whichMedia & EWhichMediaList.eMissing ) != 0 )
            {
                sqlCmds.Add( getMediaListResponseMissingCmd( whichMedia, false ) );
            }

            if( ( whichMedia & EWhichMediaList.eOnServer ) != 0 )
            {
                sqlCmds.Add( getMediaListResponseOnServerCmd( whichMedia ) );
            }

            if( sqlCmds.Count == 0 )
            {
                return new QueryResult<MediaItemResponse>();
            }

            string unionSqlCmd = sqlCmds.Join( "\nUNION\n" );
            List<string> whereClauses = BuildWhereClausesFromFilterMap(
                tableOptions.GetFilterMap(),
                MediaListFilterColumnMap,
                out List<(string name, object? value)> filterParams );

            if( ( whichMedia & EWhichMediaList.eEpisodes ) != 0 && Plugin.Instance?.Configuration != null )
            {
                whereClauses.Add(
                    "NOT EXISTS (" +
                    "  SELECT 1 FROM SeriesFilterList sfl" +
                    "  WHERE sfl.SeriesId = ParentId" +
                    "    AND (" +
                    "      sfl.TrackSeries = 0" +
                    "      OR (SeasonNum = 0 AND sfl.TrackSpecials = 0)" +
                    "      OR (sfl.MinSeason > 0 AND SeasonNum < sfl.MinSeason)" +
                    "      OR (sfl.MaxSeason > 0 AND SeasonNum > sfl.MaxSeason)" +
                    "    )" +
                    ")"
                );
            }

            if( ( whichMedia & EWhichMediaList.eMovies ) != 0 && ( whichMedia & EWhichMediaList.eMissing ) != 0 )
            {
                List<Regex> compiledRegexes = GetCompiledCollectionFilterRegexes();
                if( compiledRegexes.Count > 0 )
                {
                    List<string> excludedCollectionIds = [];
                    await ExecuteCommandAsync( new SQLCmdDef( "SELECT ItemId, Name, SortName FROM Collections" ), statement =>
                    {
                        IResultSet row = statement.Current;
                        string colId = row.GetString( 0 );
                        string colName = row.GetString( 1 );
                        string colSortName = row.GetString( 2 );

                        if( IsFilteredCollection( colName, colSortName, string.Empty, compiledRegexes ) )
                        {
                            excludedCollectionIds.Add( colId );
                        }
                        return Task.FromResult( true );
                    } ).ConfigureAwait( false );

                    List<string> excludedMissingTitles = [];
                    await ExecuteCommandAsync( new SQLCmdDef( "SELECT DISTINCT Title FROM Missing WHERE NOT IsEpisode" ), statement =>
                    {
                        IResultSet row = statement.Current;
                        string title = row.GetString( 0 );

                        if( IsFilteredCollection( string.Empty, string.Empty, title, compiledRegexes ) )
                        {
                            excludedMissingTitles.Add( title );
                        }
                        return Task.FromResult( true );
                    } ).ConfigureAwait( false );

                    List<string> exclusionParts = [];

                    if( excludedCollectionIds.Count > 0 )
                    {
                        List<string> colParams = [];
                        for( int ii = 0; ii < excludedCollectionIds.Count; ii++ )
                        {
                            string paramName = $"@ExCol{ii}";
                            colParams.Add( paramName );
                            filterParams.Add( (paramName, excludedCollectionIds[ ii ]) );
                        }
                        exclusionParts.Add( $"ParentId IN ({string.Join( ", ", colParams )})" );
                    }

                    if( excludedMissingTitles.Count > 0 )
                    {
                        List<string> titleParams = [];
                        for( int ii = 0; ii < excludedMissingTitles.Count; ii++ )
                        {
                            string paramName = $"@ExTitle{ii}";
                            titleParams.Add( paramName );
                            filterParams.Add( (paramName, excludedMissingTitles[ ii ]) );
                        }
                        exclusionParts.Add( $"Title IN ({string.Join( ", ", titleParams )})" );
                    }

                    if( exclusionParts.Count > 0 )
                    {
                        whereClauses.Add( $"(IsMissing = 0 OR NOT ({string.Join( " OR ", exclusionParts )}))" );
                    }
                }
            }

            string whereClause = HelperFuncs.JoinWhereClauses( whereClauses );

            bool isPaged = tableOptions.Limit > 0;

            int totalRecordCount = 0;
            if( isPaged )
            {
                string countSql = $"SELECT COUNT(*) FROM (\n{unionSqlCmd}\n) AS count_subquery{whereClause}";
                await ExecuteCommandAsync( new SQLCmdDef( countSql, filterParams ), ( statement ) =>
                {
                    totalRecordCount = statement.Current.GetInt( 0 );
                    return Task.FromResult( false );
                } ).ConfigureAwait( false );
            }

            string orderByClause = BuildMediaListOrderByClause( tableOptions.SortBy, tableOptions.SortOrder );
            string pagedSql = $"SELECT * FROM (\n{unionSqlCmd}\n) AS paged_subquery{whereClause}\n{orderByClause}";

            if( isPaged )
            {
                int safeOffset = Math.Max( 0, tableOptions.StartIndex );
                pagedSql += $"\nLIMIT {tableOptions.Limit} OFFSET {safeOffset}";
            }

            bool episodes = ( whichMedia & EWhichMediaList.eEpisodes ) != 0;
            string isoCode = HelperFuncs.GetCountryCode( _embyInterfaces!.HttpClient(), CancellationToken );

            List<MediaItemResponse> retVal = [];
            await ExecuteCommandAsync( new SQLCmdDef( pagedSql, filterParams ), async ( statement ) =>
            {
                IResultSet row = statement.Current;
                MediaItemResponse? mediaInfoResponse = await MediaItemResponse.CreateFromStatementRowAsync(
                    row,
                    episodes,
                    isoCode,
                    GetCollectionNameAsync,
                    _embyInterfaces,
                    this ).ConfigureAwait( false );

                if( mediaInfoResponse != null )
                {
                    retVal.Add( mediaInfoResponse );
                }

                return true;
            } ).ConfigureAwait( false );

            return new QueryResult<MediaItemResponse>
            {
                Items = retVal.ToArray(),
                TotalRecordCount = isPaged ? totalRecordCount : retVal.Count
            };
        }
    }
}
