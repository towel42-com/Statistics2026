using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Querying;
using ServiceStack;
using ServiceStack.Text;
using SQLitePCL.pretty;
using Statistics2026.Responses;
using Statistics2026.Services;
using Statistics2026.Utilities;
using System.Text.RegularExpressions;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        private const string kSQLAddToMissing =
            "INSERT INTO Missing" +
            "(" +
                "  Key" +
                ", TmdbId" +
                ", Title" +
                ", OriginalTitle" +
                ", ReleaseDate" +
                ", Overview" +
                ", PosterPath" +
                ", ParentId" +
                ", IsEpisode" +
                ", SeasonNum" +
                ", EpisodeNum" +
                ", StudioNames" +
                ", StreamingProviders" +
            ")" +
            " VALUES " +
            "(" +
                "  @Key" +
                ", @TmdbId" +
                ", @Title" +
                ", @OriginalTitle" +
                ", @ReleaseDate" +
                ", @Overview" +
                ", @PosterPath" +
                ", @ParentId" +
                ", @IsEpisode" +
                ", @SeasonNum" +
                ", @EpisodeNum" +
                ", @StudioNames" +
                ", @StreamingProviders" +
            ")" +
            " ON CONFLICT(Key) " +
            " DO UPDATE " +
            " SET " +
                "  TmdbId=@TmdbId" +
                ", Title=@Title" +
                ", OriginalTitle=@OriginalTitle" +
                ", ReleaseDate=@ReleaseDate" +
                ", Overview=@Overview" +
                ", PosterPath=@PosterPath" +
                ", ParentId=@ParentId" +
                ", IsEpisode=@IsEpisode" +
                ", SeasonNum=@SeasonNum" +
                ", EpisodeNum=@EpisodeNum" +
                ", StudioNames=@StudioNames" +
                ", StreamingProviders=@StreamingProviders"
                ;

        private static List<Regex> GetCompiledCollectionFilterRegexes()
        {
            if( Plugin.Instance?.Configuration?.movieSettings?.CollectionFiltersForMissingMovies is not { Count: > 0 } patterns )
                return [];

            return patterns
                .Where( pattern => !string.IsNullOrWhiteSpace( pattern ) )
                .Select( pattern => new Regex( pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled ) )
                .ToList();
        }

        // return isCollectionFiltered( name ) || ( ( sortName != name ) && isCollectionFiltered( sortName ) ) || isCollectionFiltered( Missing.Title );

        private bool IsFilteredCollection( string collectionName, string collectionSortName, string missingTitle, List<Regex> compiledRegexes )
        {
            if( Plugin.Instance == null )
                return false;

            if( Plugin.Instance.Configuration == null )
                return false;

            if( collectionName.IsNullOrEmpty() && collectionSortName.IsNullOrEmpty() && missingTitle.IsNullOrEmpty() )
                return false;

            foreach( Regex regex in compiledRegexes )
            {
                if( ( !collectionName.IsNullOrEmpty() && regex.IsMatch( collectionName ) ) || ( !collectionSortName.IsNullOrEmpty() && regex.IsMatch( collectionSortName ) ) || ( !missingTitle.IsNullOrEmpty() && regex.IsMatch( missingTitle ) ) )
                {
                    return true;
                }
            }
            return false;
        }

        private async Task<bool> IsFilteredSeriesAsync( string seriesId, int seasonNum )
        {
            if( Plugin.Instance == null )
                return false;

            if( Plugin.Instance.Configuration == null )
                return false;

            if( seriesId.IsNullOrEmpty() )
                return false;

            string sql = "SELECT " +
            "    TrackSeries" +
            "  , TrackSpecials" +
            "  , MinSeason" +
            "  , MaxSeason" +
            " FROM SeriesFilterList " +
            " WHERE SeriesId=@SeriesId";

            var cmd = new SQLCmdDef( sql,
                     [
                        ("@SeriesId", seriesId ),
                         ] );

            bool isSpecial = ( seasonNum == 0 );
            bool filtered = false;
            await ExecuteCommandAsync( cmd, statement =>
            {
                IResultSet row = statement.Current;
                int col = 0;
                bool trackSeries = row.GetBoolean( col++ );
                if( !trackSeries )
                {
                    filtered = true;
                    return Task.FromResult( false );
                }

                bool trackSpecials = row.GetBoolean( col++ );
                if( isSpecial && !trackSpecials )
                {
                    filtered = true;
                    return Task.FromResult( false );
                }

                int minSeason = row.GetInt( col++ );
                if( ( minSeason > 0 ) && ( seasonNum < minSeason ) )
                {
                    filtered = true;
                    return Task.FromResult( false );
                }

                int maxSeason = row.GetInt( col++ );
                if( ( maxSeason > 0 ) && ( seasonNum > maxSeason ) )
                {
                    filtered = true;
                    return Task.FromResult( false );
                }

                filtered = false;
                return Task.FromResult( true );
            }, locked: true ).ConfigureAwait( false );
            return filtered;
        }

        private async Task<(int numItems, int numParents)> MediaMissingInfoAsync( bool episodes, bool filteredResults )
        {
            CheckIsValid( ECheckLevel.eReport );

            int numItems = 0;
            int numParents = 0;
            if( filteredResults )
            {
                numItems = 0;
                HashSet<string> filteredParents = [];
                string sql = getMediaListResponseMissingCmd( episodes ? EWhichMediaList.eEpisodesMissing : EWhichMediaList.eMoviesMissing, true );

                await RunSQLOnRowAsync( sql, async ( row ) =>
                {
                    HashSet<string> currParentIds = [];
                    bool filtered = false;

                    string name = row.GetString( (int)EMediaListResponseColumns.eTitle );
                    string parentInfoJSON = row.GetString( (int)EMediaListResponseColumns.eParentInfoJSON );
                    string title = row.GetString( (int)EMediaListResponseColumns.eTitle );

                    List<(string id, string name, string sortName, string displayText, string url)>? parentInfos = await MediaItemResponse.GetParentInfoAsync( parentInfoJSON, episodes,
                        async ( string id, string parentName, string parentSortName ) =>
                        {
                            if( parentName.IsEmpty() )
                            {
                                return true;
                            }

                            if( !episodes )
                            {
                                return IsFilteredCollection( parentName, parentSortName, title, GetCompiledCollectionFilterRegexes() );
                            }
                            return false;
                        } ).ConfigureAwait( false );

                    filtered = ( parentInfos == null ) || ( parentInfos.Count == 0 );

                    if( !filtered )
                    {
                        DateTimeOffset premiereDate = HelperFuncs.ReadDateTime( row.GetString( (int)EMediaListResponseColumns.ePremiereDate ) ) ?? DateTimeOffset.MinValue;
                        if( ( premiereDate != DateTimeOffset.MinValue ) && ( premiereDate.LocalDateTime.Date > DateTimeOffset.Now.Date ) )
                            filtered = true;
                    }

                    int seasonNum = -1;

                    if( !filtered )
                    {
                        if( episodes )
                        {
                            seasonNum = row.GetInt( (int)EMediaListResponseColumns.eSeasonNum );
                            string parentId = row.GetString( (int)EMediaListResponseColumns.eParentId );
                            filtered = filtered || ( await IsFilteredSeriesAsync( parentId, seasonNum ).ConfigureAwait( false ) );
                            currParentIds = filtered ? [] : [ parentId ];
                        }
                        else
                        {
                            currParentIds = parentInfos?.Select( pi => pi.id ).ToHashSet() ?? [];
                        }
                    }

                    Logger().Debug( $"{( episodes ? "Series" : "Movie" )} : {name}{( episodes ? $" Season: {seasonNum}" : "" )} - {( filtered ? "" : "NOT " )}Filtered" );

                    if( !filtered )
                    {
                        numItems++;
                        filteredParents.UnionWith( currParentIds );
                    }
                    return true;
                } ).ConfigureAwait( false );
                numParents = filteredParents.Count;
            }
            else
            {
                string whereClause = episodes ? "IsEpisode" : "NOT IsEpisode";
                string sql =
                    $"SELECT COUNT( * ),COUNT( DISTINCT ParentId ) FROM Missing WHERE {( episodes ? "" : "NOT" )} IsEpisode AND ( ReleaseDate < date('now', '+1 day') )";
                var cmd = new SQLCmdDef( sql );

                await ExecuteCommandAsync( cmd, statement =>
                {
                    IResultSet row = statement.Current;
                    numItems = row.GetInt( 0 );
                    numParents = row.GetInt( 1 );
                    return Task.FromResult( true );
                } ).ConfigureAwait( false );
            }
            return (numItems, numParents);
        }

        private async Task<StatCard> TotalMissingMediaAsync( bool episodes )
        {
            CheckIsValid( ECheckLevel.eReport );

            (int numItems, int numParents) info = await MediaMissingInfoAsync( episodes, Plugin.Instance!.Configuration.tvSettings.showFilteredMissingSummaries ).ConfigureAwait( false );

            string title = episodes ? Constants.MissingEpisodes : Constants.MissingMovies;
            string infoFormat = episodes ? "{0} Episodes from {1} Series" : "{0} Movies from {1} Collections";
            string help = episodes ? Constants.MissingEpisodesHelp : Constants.MissingMoviesHelp;

            var retVal = new TextBasedStatCard( title, help, EStatCardStyle.eCompact );
            retVal.AddLine( string.Format( infoFormat, info.numItems, info.numParents ), false );

            return retVal;
        }

        public Task<StatCard> TotalMissingMoviesAsync()
        {
            return TotalMissingMediaAsync( false );
        }

        public Task<StatCard> TotalMissingEpisodesAsync()
        {
            return TotalMissingMediaAsync( true );
        }

        public async Task<List<SeriesFilterResponse>> GetSeriesFilterAsync( bool updateIfEmpty )
        {
            QueryResult<SeriesFilterResponse> result = await GetSeriesFilterAsync( updateIfEmpty,
                new TabulatorTableOptions()
                {
                    StartIndex = -1,
                    Limit = -1,
                    SortBy = string.Empty,
                    SortOrder = string.Empty
                } ).ConfigureAwait( false );
            return result.Items.ToList();
        }

        public async Task<QueryResult<SeriesFilterResponse>> GetSeriesFilterAsync( bool updateIfEmpty, TabulatorTableOptions tableOptions )
        {
            CheckIsValid( ECheckLevel.eReport );

            string fromAndJoin =
                " FROM SeriesFilterList" +
                " JOIN Series ON Series.ItemId = SeriesFilterList.SeriesId";

            bool isPaged = tableOptions.Limit > 0;
            int totalRecordCount = 0;

            if( isPaged )
            {
                string countSql = "SELECT COUNT(*)" + fromAndJoin;
                await ExecuteCommandAsync( new SQLCmdDef( countSql ), statement =>
                {
                    totalRecordCount = statement.Current.GetInt( 0 );
                    return Task.FromResult( false );
                } ).ConfigureAwait( false );

                if( totalRecordCount == 0 && updateIfEmpty )
                {
                    var cmds = ( await UpdateSeriesFilterAsync( true ).ConfigureAwait( false ) )
                        .Where( cmd => cmd != null )
                        .Select( cmd => cmd! )
                        .ToList();
                    await ExecuteCommandsAsync( cmds ).ConfigureAwait( false );
                    return await GetSeriesFilterAsync( false, tableOptions ).ConfigureAwait( false );
                }
            }

            string orderByClause = BuildSeriesFilterOrderByClause( tableOptions.SortBy, tableOptions.SortOrder );
            string sql = "SELECT" +
                "  SeriesFilterList.SeriesId" +
                ", SeriesFilterList.TrackSeries" +
                ", SeriesFilterList.TrackSpecials" +
                ", Series.Name" +
                ", Series.SortName" +
                ", Series.PremiereDate" +
                ", SeriesFilterList.MinSeason" +
                ", SeriesFilterList.MaxSeason" +
                ", Series.HasSpecials" +
                ", Series.NumSeasons" +
                fromAndJoin +
                $" {orderByClause}";

            if( isPaged )
            {
                int safeOffset = Math.Max( 0, tableOptions.StartIndex );
                sql += $" LIMIT {tableOptions.Limit} OFFSET {safeOffset}";
            }

            List<SeriesFilterResponse> retVal = [];
            bool found = false;
            await ExecuteCommandAsync( new SQLCmdDef( sql ), statement =>
            {
                found = true;
                IResultSet row = statement.Current;

                int col = 0;
                var currFilter = new SeriesFilterResponse()
                {
                    SeriesId = row.GetString( col++ ),
                    TrackSeries = row.GetBoolean( col++ ),
                    TrackSpecials = row.GetBoolean( col++ ),
                    Name = row.GetString( col++ ),
                    SortName = row.GetString( col++ ),
                    PremiereYear = HelperFuncs.ReadDateTime( row.GetString( col++ ) )?.Year ?? -1,
                    MinSeason = row.GetInt( col++ ),
                    MaxSeason = row.GetInt( col++ ),
                    HasSpecials = row.GetBoolean( col++ ),
                    NumSeasons = row.GetInt( col++ )
                };
                retVal.Add( currFilter );
                return Task.FromResult( true );
            } ).ConfigureAwait( false );

            if( !found && updateIfEmpty )
            {
                var cmds = ( await UpdateSeriesFilterAsync( true ).ConfigureAwait( false ) )
                    .Where( cmd => cmd != null )
                    .Select( cmd => cmd! )
                    .ToList();
                await ExecuteCommandsAsync( cmds ).ConfigureAwait( false );
                return await GetSeriesFilterAsync( false, tableOptions ).ConfigureAwait( false );
            }

            return new QueryResult<SeriesFilterResponse>
            {
                Items = retVal.ToArray(),
                TotalRecordCount = isPaged ? totalRecordCount : retVal.Count
            };
        }

        private static string BuildSeriesFilterOrderByClause( string sortBy, string sortOrder )
        {
            string direction = string.Equals( sortOrder, "Descending", StringComparison.OrdinalIgnoreCase )
                ? "DESC"
                : "ASC";

            return ( sortBy ?? string.Empty ).ToLowerInvariant() switch
            {
                "trackseries" => $"ORDER BY SeriesFilterList.TrackSeries {direction}, Series.SortName COLLATE NOCASE ASC",
                "trackspecials" => $"ORDER BY SeriesFilterList.TrackSpecials {direction}, Series.SortName COLLATE NOCASE ASC",
                _ => $"ORDER BY Series.SortName COLLATE NOCASE {direction}"
            };
        }

        public async Task<SQLCmdDef> SaveSeriesFilterAsync( SeriesFilterResponse seriesFilter, bool andExecute )
        {
            string sql = "INSERT INTO SeriesFilterList" +
                "(" +
                "    SeriesId" +
                "  , TrackSeries" +
                "  , TrackSpecials" +
                "  , MinSeason" +
                "  , MaxSeason" +
                ")" +
                " VALUES " +
                "(" +
                "  @SeriesId" +
                ", @TrackSeries" +
                ", @TrackSpecials" +
                ", @MinSeason" +
                ", @MaxSeason" +
                ")" +
                " ON CONFLICT(SeriesId) " +
                " DO UPDATE" +
                " SET" +
                "  TrackSeries=@TrackSeries" +
                ", TrackSpecials=@TrackSpecials" +
                ", MinSeason=@MinSeason" +
                ", MaxSeason=@MaxSeason"
                ;

            var cmd = new SQLCmdDef( sql,
                         [
                            ("@SeriesId", seriesFilter.SeriesId ),
                            ("@TrackSeries", seriesFilter.TrackSeries ),
                            ("@TrackSpecials", seriesFilter.TrackSpecials ),
                            ("@MinSeason", seriesFilter.MinSeason ),
                            ("@MaxSeason", seriesFilter.MaxSeason ),
                        ] );

            if( andExecute )
            {
                await ExecuteCommandAsync( cmd ).ConfigureAwait( false );
            }
            return cmd;
        }

        private async Task<List<SQLCmdDef>> UpdateSeriesFilterAsync( bool nested = false )
        {
            bool filterChanged( ref string msg, ref SeriesFilterResponse lhs, int numSeasons, bool hasSpecials )
            {
                bool retVal = false;
                if( lhs.HasSpecials != hasSpecials )
                {
                    msg += $" - HasSpecials : old: {lhs.HasSpecials} new: {hasSpecials}";
                    lhs.HasSpecials = lhs.HasSpecials || hasSpecials;
                    retVal = true;
                }
                int newNumSeasons = ( numSeasons > lhs.NumSeasons ) ? numSeasons : lhs.NumSeasons;
                if( newNumSeasons != lhs.NumSeasons )
                {
                    msg += $" - NumSeasons : old: {lhs.NumSeasons} new: {newNumSeasons}";
                    lhs.NumSeasons = newNumSeasons;
                    retVal = true;
                }
                return retVal;
            }

            if( !nested )
                CheckIsValid( ECheckLevel.eUpdate );

            List<SeriesFilterResponse> seriesFilterList = await GetSeriesFilterAsync( false ).ConfigureAwait( false );
            Dictionary<string, List<SeriesFilterResponse>> oldFilterMap = [];
            foreach( SeriesFilterResponse series in seriesFilterList )
            {
                string key = series.key();
                if( !oldFilterMap.TryGetValue( key, out List<SeriesFilterResponse>? list ) )
                {
                    list = [];
                    oldFilterMap[ key ] = list;
                }
                list.Add( series );
            }

            var currFilterMap = ( await GetSeriesFilterAsync( false ).ConfigureAwait( false ) ).ToDictionary( p => p.SeriesId );

            var changedFilterMap = new List<SeriesFilterResponse>();

            List<Series> seriesList = EmbyHelperFuncs.GetAllSeries( null, LibraryManager(), false );

            foreach( Series series in seriesList )
            {
                string key = series.Id.ToString();
                string oldKey = $"{series.SortName}";
                int premiereYear = series.PremiereDate?.Year ?? -1;
                if( premiereYear != -1 )
                {
                    oldKey += $"_{premiereYear}";
                }

                (int numSeasons, bool hasSpecials) = GetSeasonCount( series );

                if( currFilterMap.TryGetValue( key, out SeriesFilterResponse? currFilter ) )
                {
                    string msg = $"Updating filter for Series: {series.Name}";
                    bool changed = filterChanged( ref msg, ref currFilter, numSeasons, hasSpecials );
                    if( changed )
                    {
                        currFilterMap[ key ] = currFilter;
                        changedFilterMap.Add( currFilter );
                        Logger().Debug( msg );
                    }
                }
                else if( oldFilterMap.TryGetValue( oldKey, out List<SeriesFilterResponse>? oldFilterList ) )
                {
                    bool found = false;
                    SeriesFilterResponse? reference = null;
                    foreach( SeriesFilterResponse tmp in oldFilterList )
                    {
                        reference = reference ?? tmp;

                        if( series.Id.ToString() == tmp.SeriesId )
                        {
                            string msg = $"Updating filter for Series: {series.Name}";
                            bool changed = filterChanged( ref msg, ref currFilter, numSeasons, hasSpecials );
                            if( changed )
                            {
                                currFilterMap[ key ] = currFilter;
                                changedFilterMap.Add( currFilter );
                                Logger().Debug( msg );
                            }
                            found = true;
                            break;
                        }
                    }

                    if( !found )
                    {
                        var newFilter = new SeriesFilterResponse()
                        {
                            SeriesId = series.Id.ToString(),
                            TrackSeries = true,
                            TrackSpecials = false,
                            Name = series.Name,
                            SortName = series.SortName,
                            PremiereYear = premiereYear,
                            MinSeason = -1,
                            MaxSeason = -1,
                            HasSpecials = hasSpecials,
                            NumSeasons = numSeasons,
                        };
                        if( reference != null )
                        {
                            newFilter.TrackSeries = reference.TrackSeries;
                            newFilter.TrackSpecials = reference.TrackSpecials;
                            newFilter.MinSeason = reference.MinSeason;
                            newFilter.MaxSeason = reference.MaxSeason;
                        }
                        changedFilterMap.Add( newFilter );
                    }
                }
                else
                {
                    var newFilter = new SeriesFilterResponse()
                    {
                        SeriesId = series.Id.ToString(),
                        TrackSeries = true,
                        Name = series.Name,
                        SortName = series.SortName,
                        PremiereYear = premiereYear,
                        TrackSpecials = false,
                        MinSeason = -1,
                        MaxSeason = -1,
                        HasSpecials = hasSpecials,
                        NumSeasons = numSeasons,
                    };
                    currFilterMap[ key ] = newFilter;
                    Logger().Debug( $"Adding default filter for Series: {series.Name} - HasSpecials: {hasSpecials} - NumSeasons: {numSeasons}" );
                    changedFilterMap.Add( newFilter );
                }
            }

            List<SQLCmdDef> sqlCmds = [];
            foreach( SeriesFilterResponse entry in changedFilterMap )
            {
                sqlCmds.Add( await SaveSeriesFilterAsync( entry, false ).ConfigureAwait( false ) );
            }
            return sqlCmds;
        }
    }
}
