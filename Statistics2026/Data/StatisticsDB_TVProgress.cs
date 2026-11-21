using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Responses;
using Statistics2026.Utilities;
using System.Collections.Concurrent;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        public async Task<List<GetTVSeriesProgressResponse>> GetTVSeriesProgress( User? user )
        {
            CheckIsValid( ECheckLevel.eReport );

            if( user == null )
                throw new ArgumentNullException( "user" );

            string getSeriesSQL = "SELECT " +
                "  Series.Name" +
                ", strftime('%Y', Series.PremiereDate) AS PremiereDate" +
                ", Series.NumEpisodes" +
                ", Series.NumSpecials" +
                ", Series.Rating" +
                ", Series.Status" +
                ", Series.ItemId " +
                ", Series.ImageUrl " +
                " FROM " +
                "   Series "
                ;

            var series = new Dictionary<string, GetTVSeriesProgressResponse>();
            await ExecuteCommand( new SQLCmdDef( getSeriesSQL ), statement =>
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

                var curr = new GetTVSeriesProgressResponse()
                {
                    SeriesId = seriesId,
                    Name = name,
                    PremiereYear = premiereYear,
                    Score = score,
                    SeriesStatus = status,
                    ItemUrl = imageUrl
                };
                if( curr.ItemUrl != null && curr.ItemUrl != string.Empty )
                {
                    curr.ItemUrl = ItemImageUrl.ItemUrlFromItemId( seriesId, curr.ItemUrl, curr.Name );
                    curr.Name = curr.ItemUrl;
                }

                curr.Episodes.Total = totalEpisodes;
                curr.Specials.Total = totalSpecials;

                series[ seriesId ] = curr;
                return Task.FromResult( true );
            } );

            string? tableName = TableDefs().getUserTableName( user );
            string sqlBase = "SELECT " +
                $"  SUM({tableName}.NumEpisodes) " +
                $", {tableName}.SeriesId " +
                $" FROM " +
                $"   {tableName} " +
                $" WHERE " +
                $" {tableName}.IsEpisode AND " +
                $" <isTVSpecial> AND " +
                $" {tableName}.IsPlayed AND " +
                $" {tableName}.UserId=@UserId " +
                $" GROUP BY {tableName}.SeriesId "
                ;

            string sqlEpisodes = sqlBase.Replace( "<isTVSpecial>", $"NOT {tableName}.IsTVSpecial" );
            string sqlSpecials = sqlBase.Replace( "<isTVSpecial>", $"{tableName}.IsTVSpecial" );

            List<(string name, object? value)> paramList = [ ("@UserId", user.Id.ToString()) ];

            await ExecuteCommand( new SQLCmdDef( sqlEpisodes, paramList ), statement =>
            {
                IResultSet row = statement.Current;
                int col = 0;
                int watchedCount = row.GetInt( col++ );
                string seriesId = row.GetString( col++ ); // should be true

                if( series.TryGetValue( seriesId, out GetTVSeriesProgressResponse? curr ) )
                {
                    curr.Episodes.Count = watchedCount;
                    series[ seriesId ] = curr;
                }

                return Task.FromResult( true );
            } );

            await ExecuteCommand( new SQLCmdDef( sqlSpecials, paramList ), statement =>
            {
                IResultSet row = statement.Current;
                int col = 0;
                int watchedCount = row.GetInt( col++ );
                string seriesId = row.GetString( col++ ); // should be true

                if( series.TryGetValue( seriesId, out GetTVSeriesProgressResponse? curr ) )
                {
                    curr.Specials.Count = watchedCount;
                    series[ seriesId ] = curr;
                }

                return Task.FromResult( true );
            } );

            var retVal = series.Values.ToList();

            return retVal;
        }
    }
}
