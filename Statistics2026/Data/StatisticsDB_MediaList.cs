using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Responses;
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

        private string getSeriesListResponseOnServerCmd()
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
            List<string> orderBy = new() { "SortName ASC" };

            return SQLCmdDef.CreateCmd( from, columns, joins, whereClauses, groupByClauses, orderBy );
        }

        private string getCollectionListResponseOnServerCmd()
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
            List<string> orderBy = new() { "SortName ASC" };

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

        public async Task<List<MediaItemResponse>> GetCollectionListResponseAsync()
        {
            if( _embyInterfaces == null )
                return [];

            string sqlCmd = getCollectionListResponseOnServerCmd();

            string isoCode = HelperFuncs.GetCountryCode( _embyInterfaces!.HttpClient(), CancellationToken );

            List<MediaItemResponse> retVal = [];
            await ExecuteCommandAsync( new SQLCmdDef( sqlCmd ), async ( statement ) =>
            {
                IResultSet row = statement.Current;
                MediaItemResponse? mediaInfoResponse = await MediaItemResponse.CreateFromStatementRowAsync( row, false, isoCode, null, null, null, _embyInterfaces, this ).ConfigureAwait( false );
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

                    double moviesPercent = (numMovies == 0) ? 100.0 : ( 100.0 * moviesCollected / ( 1.0 * numMovies ) );

                    string collectionStatus = $"{moviesCollected}/{numMovies} ({moviesPercent:F0}%)";
                    mediaInfoResponse.Extra = collectionStatus;

                    retVal.Add( mediaInfoResponse );
                }

                return true;
            } ).ConfigureAwait( false );


            retVal.Sort(
                ( x, y ) =>
                {
                    string lhs = x.DisplayName.SortBy;
                    string rhs = y.DisplayName.SortBy;
                    return lhs.CompareTo( rhs );
                } );
            return retVal;
        }

        public async Task<List<MediaItemResponse>> GetSeriesListResponseAsync()
        {
            if( _embyInterfaces == null )
                return [];

            string sqlCmd = getSeriesListResponseOnServerCmd();

            string isoCode = HelperFuncs.GetCountryCode( _embyInterfaces!.HttpClient(), CancellationToken );

            List<MediaItemResponse> retVal = [];
            await ExecuteCommandAsync( new SQLCmdDef( sqlCmd ), async ( statement ) =>
            {
                IResultSet row = statement.Current;
                MediaItemResponse? mediaInfoResponse = await MediaItemResponse.CreateFromStatementRowAsync( row, null, isoCode, null, null, null, _embyInterfaces, this ).ConfigureAwait( false );
                if( mediaInfoResponse != null )
                {
                    int pos = ( (int)EMediaListResponseColumns.eParentId ) + 1;
                    int numEpisodes = row.GetInt( pos++ );
                    int episodesCollected = row.GetInt( pos++ );
                    int numSpecials = row.GetInt( pos++ );
                    int specialsCollected = row.GetInt( pos++ );

                    double episodesPercent = 100.0 * episodesCollected / ( 1.0 * numEpisodes );

                    string episodeStatus = $"{episodesCollected}/{numEpisodes} ({episodesPercent:F0}%)";
                    mediaInfoResponse.Extra = episodeStatus;

                    double specialsPercent = 100.0 * specialsCollected / ( 1.0 * numSpecials );
                    string specialsStatus = $"{specialsCollected}/{numSpecials} ({specialsPercent:F0}%)";
                    mediaInfoResponse.Extra2 = specialsStatus;

                    retVal.Add( mediaInfoResponse );
                }

                return true;
            } ).ConfigureAwait( false );


            retVal.Sort(
                ( x, y ) =>
                {
                    string lhs = x.DisplayName.SortBy;
                    string rhs = y.DisplayName.SortBy;
                    return lhs.CompareTo( rhs );
                } );
            return retVal;
        }

        public async Task<List<MediaItemResponse>> GetMediaListResponseAsync( EWhichMediaList whichMedia )
        {
            if( _embyInterfaces == null )
                return [];

            List<string> sqlCmds = [];
            if( ( whichMedia & EWhichMediaList.eMissing ) != 0 )
            {
                sqlCmds.Add( getMediaListResponseMissingCmd( whichMedia, false ) );
            }

            if( ( whichMedia & EWhichMediaList.eOnServer ) != 0 )
            {
                sqlCmds.Add( getMediaListResponseOnServerCmd( whichMedia ) );
            }

            string sqlCmd = sqlCmds.Join( "\nUNION\n" );

            bool episodes = ( whichMedia & EWhichMediaList.eEpisodes ) != 0;

            string isoCode = HelperFuncs.GetCountryCode( _embyInterfaces!.HttpClient(), CancellationToken );

            List<MediaItemResponse> retVal = [];
            await ExecuteCommandAsync( new SQLCmdDef( sqlCmd ), async ( statement ) =>
            {
                IResultSet row = statement.Current;
                MediaItemResponse? mediaInfoResponse = await MediaItemResponse.CreateFromStatementRowAsync( row, episodes, isoCode, IsFilteredSeriesAsync, IsFilteredCollection, GetCollectionNameAsync, _embyInterfaces, this ).ConfigureAwait( false );
                if( mediaInfoResponse != null )
                {
                    retVal.Add( mediaInfoResponse );
                }

                return true;
            } ).ConfigureAwait( false );


            retVal.Sort(
                ( x, y ) =>
                {
                    string lhs = x.DisplayName.SortBy;
                    string rhs = y.DisplayName.SortBy;
                    return lhs.CompareTo( rhs );
                } );
            return retVal;
        }
    }
}
