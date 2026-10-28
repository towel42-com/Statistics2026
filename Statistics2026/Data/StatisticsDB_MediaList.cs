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
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
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

        private string getMediaListResponseMissingCmd( EWhichMediaList whichMedia, ref List<string> orderBy )
        {
            var episodes = ( whichMedia & EWhichMediaList.eEpisodes ) != 0;

            List<string> columns = [];
            columns.Add( "1 as IsMissing" );
            columns.Add( "Missing.Title AS Title" );
            columns.Add( "Missing.Title AS SortName" );
            columns.Add( "Missing.ReleaseDate AS PremiereDate" );
            columns.Add( "Missing.StudioNames" );
            columns.Add( "'' AS ResolutionDetail" );
            columns.Add( "'' AS Codec" );
            columns.Add( "'' AS DolbyVisionProfile" );
            columns.Add( "'' AS ServerLocation" );
            columns.Add( "'' AS ItemId" );
            columns.Add( "'' AS ImageURL" );

            var parentTableName = episodes ? "Series" : "Collections";
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

            var from = "Missing";
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

            orderBy.Add( "SortName ASC" );
            if( episodes )
            {
                orderBy.Add( "SeasonNum ASC" );
                orderBy.Add( "EpisodeNum ASC" );
            }
            var groupByClauses = new List<string>() { "Missing.Title" };
            return SQLCmdDef.CreateCmd( from, columns, joins, whereClauses, groupByClauses, null );
        }

        private string getMediaListResponseMissingCmd( EWhichMediaList whichMedia )
        {
            List<string> orderBy = [];
            return getMediaListResponseMissingCmd( whichMedia, ref orderBy );
        }

        private string getMediaListResponseOnServerCmd( EWhichMediaList whichMedia, ref List<string> orderBy )
        {
            var episodes = ( whichMedia & EWhichMediaList.eEpisodes ) != 0;

            List<string> columns = [];
            columns.Add( "0 as IsMissing" );
            columns.Add( episodes ? "Media.PrimaryName || ' - S' || printf( '%02d', Media.Season ) || 'E' || printf('%02d', Media.Episode) || ' - ' || Media.SecondaryName AS Title" : "Media.PrimaryName AS Title" );
            columns.Add( "Media.SortName as SortName" );
            columns.Add( "Media.PremiereDate AS PremiereDate" );
            columns.Add( "Media.StudioNames AS StudioNames" );
            columns.Add( "Media.ResolutionDetail AS ResolutionDetail" );
            columns.Add( "Media.Codec AS Codec" );
            columns.Add( "Media.DolbyVisionProfile AS DolbyVisionProfile" );
            columns.Add( "Media.ServerLocation AS ServerLocation" );
            columns.Add( "Media.ItemId AS ItemId" );
            columns.Add( "Media.ImageUrl AS ImageUrl" );

            var parentTableName = episodes ? "Series" : "Collections";
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

            var from = "Media";
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
            var retVal = Regex.Replace( sortName, @"^[^a-zA-Z0-9]+", "" );
            return retVal;
        }

        public List<MediaItemResponse> getMediaListResponse( EWhichMediaList whichMedia )
        {
            List<string> sqlCmds = [];
            if( ( whichMedia & EWhichMediaList.eMissing ) != 0 )
            {
                sqlCmds.Add( getMediaListResponseMissingCmd( whichMedia ) );
            }

            if( ( whichMedia & EWhichMediaList.eOnServer ) != 0 )
            {
                sqlCmds.Add( getMediaListResponseOnServerCmd( whichMedia ) );
            }

            var sqlCmd = sqlCmds.Join( "\nUNION\n" );

            var episodes = ( whichMedia & EWhichMediaList.eEpisodes ) != 0;

            List<MediaItemResponse> retVal = [];
            _dbHelper.ExecuteCommand( new SQLCmdDef( sqlCmd ), statement =>
            {
                var row = statement.Current;
                var response = MediaItemResponse.CreateFromStatementRow( row, episodes, IsFilteredSeries, IsFilteredCollection, GetCollectionName );
                if ( response != null )
                    retVal.Add( response );
                return true;
            } );


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
