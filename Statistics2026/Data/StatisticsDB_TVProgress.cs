using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Querying;
using SQLitePCL.pretty;
using Statistics2026.Configuration;
using Statistics2026.Responses;
using Statistics2026.Services;
using Statistics2026.Utilities;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        public async Task<QueryResult<GetTVSeriesProgressResponse>> GetTVSeriesProgressAsync(
            User? user,
            TabulatorTableOptions tableOptions )
        {
            CheckIsValid( ECheckLevel.eReport );

            if( user == null )
                throw new ArgumentNullException( nameof( user ) );

            string? tableName = TableDefs().getUserTableName( user );
            List<string> whereClauses = BuildWhereClausesFromFilterMap(
                tableOptions.GetFilterMap(),
                TVSeriesProgressFilterColumnMap,
                out List<(string name, object? value)> paramList );

            paramList.Add( ("@UserId", user.Id.ToString()) );
            string whereClause = HelperFuncs.JoinWhereClauses( whereClauses );

            bool isPaged = tableOptions.Limit > 0;
            int totalRecordCount = 0;

            // 1. Get total series count with active filters
            if( isPaged )
            {
                string countSql = $"SELECT COUNT(*) FROM Series{whereClause}";
                await ExecuteCommandAsync( new SQLCmdDef( countSql, paramList ), statement =>
                {
                    totalRecordCount = statement.Current.GetInt( 0 );
                    return Task.FromResult( false );
                } ).ConfigureAwait( false );
            }

            // 2. Join Series with grouped episode & special counts, apply WHERE, ORDER BY, and LIMIT/OFFSET
            string orderByClause = BuildTVSeriesProgressOrderByClause( tableOptions.SortBy, tableOptions.SortOrder );
            string sql =
                "SELECT " +
                "  Series.Name" +
                ", strftime('%Y', Series.PremiereDate) AS PremiereYear" +
                ", Series.NumEpisodes" +
                ", Series.NumSpecials" +
                ", Series.Rating" +
                ", Series.Status" +
                ", Series.ItemId" +
                ", Series.ImageUrl" +
                ", COALESCE(ep.WatchedEpisodes, 0) AS WatchedEpisodes" +
                ", COALESCE(sp.WatchedSpecials, 0) AS WatchedSpecials" +
                " FROM Series " +
                " LEFT JOIN ( " +
                $"   SELECT SeriesId, SUM(NumEpisodes) AS WatchedEpisodes " +
                $"   FROM {tableName} " +
                $"   WHERE IsEpisode AND NOT IsTVSpecial AND IsPlayed AND UserId = @UserId " +
                "   GROUP BY SeriesId " +
                " ) ep ON ep.SeriesId = Series.ItemId " +
                " LEFT JOIN ( " +
                $"   SELECT SeriesId, SUM(NumEpisodes) AS WatchedSpecials " +
                $"   FROM {tableName} " +
                $"   WHERE IsEpisode AND IsTVSpecial AND IsPlayed AND UserId = @UserId " +
                "   GROUP BY SeriesId " +
                " ) sp ON sp.SeriesId = Series.ItemId " +
                $"{whereClause} " +
                $"{orderByClause}";

            if( isPaged )
            {
                int safeOffset = Math.Max( 0, tableOptions.StartIndex );
                sql += $" LIMIT {tableOptions.Limit} OFFSET {safeOffset}";
            }

            bool showImages = Plugin.Instance!.Configuration.ShowImages( EReportType.eUserReport );
            List<GetTVSeriesProgressResponse> retVal = [];

            await ExecuteCommandAsync( new SQLCmdDef( sql, paramList ), statement =>
            {
                IResultSet row = statement.Current;
                int col = 0;
                string name = row.GetString( col++ );
                int premiereYear = row.GetInt( col++ );
                int totalEpisodes = row.GetInt( col++ );
                int totalSpecials = row.GetInt( col++ );
                double score = row.GetDouble( col++ );
                string status = row.GetString( col++ );
                string seriesId = row.GetString( col++ );
                string imageUrl = row.GetString( col++ );
                int watchedEpisodes = row.GetInt( col++ );
                int watchedSpecials = row.GetInt( col++ );

                var curr = new GetTVSeriesProgressResponse()
                {
                    SeriesId = seriesId,
                    Name = name,
                    PremiereYear = premiereYear,
                    Score = score,
                    SeriesStatus = status,
                    ItemUrl = imageUrl
                };

                if( !string.IsNullOrEmpty( curr.ItemUrl ) )
                {
                    curr.ItemUrl = GetItemImageUrl.ItemHtmlFromItemId( seriesId, curr.ItemUrl, text: curr.Name, showImages );
                    curr.Name = curr.ItemUrl;
                }

                curr.Episodes.Total = totalEpisodes;
                curr.Episodes.Count = watchedEpisodes;
                curr.Specials.Total = totalSpecials;
                curr.Specials.Count = watchedSpecials;

                retVal.Add( curr );
                return Task.FromResult( true );
            } ).ConfigureAwait( false );

            return new QueryResult<GetTVSeriesProgressResponse>
            {
                Items = retVal.ToArray(),
                TotalRecordCount = isPaged ? totalRecordCount : retVal.Count
            };
        }

        private static readonly Dictionary<string, string> TVSeriesProgressFilterColumnMap = new( StringComparer.OrdinalIgnoreCase )
        {
            [ "Name" ] = "Series.Name",
            [ "Status" ] = "Series.Status"
        };

        private static string BuildTVSeriesProgressOrderByClause( string sortBy, string sortOrder )
        {
            string direction = string.Equals( sortOrder, "Descending", StringComparison.OrdinalIgnoreCase )
                ? "DESC"
                : "ASC";

            return ( sortBy ?? string.Empty ).ToLowerInvariant() switch
            {
                "episodeprogess" or "episodeprogress" =>
                    $"ORDER BY (CASE WHEN Series.NumEpisodes = 0 THEN 0.0 ELSE (100.0 * COALESCE(ep.WatchedEpisodes, 0) / CAST(Series.NumEpisodes AS REAL)) END) {direction}, Series.SortName COLLATE NOCASE ASC",
                "score" =>
                    $"ORDER BY Series.Rating {direction}, Series.SortName COLLATE NOCASE ASC",
                "status" =>
                    $"ORDER BY Series.Status COLLATE NOCASE {direction}, Series.SortName COLLATE NOCASE ASC",
                _ =>
                    $"ORDER BY Series.SortName COLLATE NOCASE {direction}"
            };
        }
    }
}
