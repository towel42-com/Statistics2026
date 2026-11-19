using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using ServiceStack;
using Statistics2026.Responses;
using Statistics2026.Utilities;
using System.Collections.Concurrent;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        public List<GetTVSeriesProgressResponse> GetTVSeriesProgress( User? user )
        {
            CheckIsValid( ECheckLevel.eReport );

            if( user == null )
                throw new ArgumentNullException( "user" );

            var getSeriesSQL = "SELECT " +
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
            ExecuteCommand( new SQLCmdDef( getSeriesSQL ), statement =>
            {
                var row = statement.Current;
                var col = 0;
                var name = row.GetString( col++ );
                var premiereYear = row.GetInt( col++ );
                var totalEpisodes = row.GetInt( col++ );
                var totalSpecials = row.GetInt( col++ );
                var score = row.GetDouble( col++ );
                var status = row.GetString( col++ );
                var seriesId = row.GetString( col++ );
                var imageUrl = row.GetString( col++ );

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
                return true;
            } );

            var tableName = TableDefs().getUserTableName( user );
            var sqlBase = "SELECT " +
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

            var sqlEpisodes = sqlBase.Replace( "<isTVSpecial>", $"NOT {tableName}.IsTVSpecial" );
            var sqlSpecials = sqlBase.Replace( "<isTVSpecial>", $"{tableName}.IsTVSpecial" );

            List<(string name, object? value)> paramList = [ ("@UserId", user.Id.ToString()) ];

            ExecuteCommand( new SQLCmdDef( sqlEpisodes, paramList ), statement =>
            {
                var row = statement.Current;
                var col = 0;
                var watchedCount = row.GetInt( col++ );
                var seriesId = row.GetString( col++ ); // should be true

                if( series.TryGetValue( seriesId, out var curr ) )
                {
                    curr.Episodes.Count = watchedCount;
                    series[ seriesId ] = curr;
                }

                return true;
            } );

            ExecuteCommand( new SQLCmdDef( sqlSpecials, paramList ), statement =>
            {
                var row = statement.Current;
                var col = 0;
                var watchedCount = row.GetInt( col++ );
                var seriesId = row.GetString( col++ ); // should be true

                if( series.TryGetValue( seriesId, out var curr ) )
                {
                    curr.Specials.Count = watchedCount;
                    series[ seriesId ] = curr;
                }

                return true;
            } );

            var retVal = series.Values.ToList();

            return retVal;
        }
    }
}
