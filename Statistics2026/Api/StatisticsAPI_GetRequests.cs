using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Services;
using Statistics2026.Data;
using Statistics2026.Utilities;
using System;
using System.Net.NetworkInformation;
using static Statistics2026.Data.StatisticsDB;

namespace Statistics2026.Api
{
    public partial class Statistics2026API : IService, IRequiresRequest
    {
        private object GetRequest( string requestName, Func<AutoTimer, object> requestFunc )
        {
            using( var timer = new AutoTimer( $"Request: {requestName}", _embyInterfaces._logger ) )
            {
                try
                {
                    return requestFunc( timer );
                }
                catch( Exception )
                {
                    throw;
                }
            }
        }

        public object Get( GetLastRun request )
        {
            return GetRequest( "GetLastRun", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                if( Enum.TryParse( request.WhichRun, true, out StatisticsDB.EAction whichRun ) )
                {
                    var retVal = db.GetLastRunInfo( whichRun );
                    return retVal;
                }
                else
                {
                    return new object();
                }

            } );
        }
        public object Get( GetTVSeriesProgress request )
        {
            return GetRequest( "GetTVSeriesProgress", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var userName = request.user;
                var user = GetUserByName( userName );
                if( user == null )
                    return new StatCardResponse();

                var retVal = db.GetTVSeriesProgress( user );
                return retVal;
            } );
        }

        public object Get( GetEpisodeList request )
        {
            var retVal = GetRequest( "GetEpisodeList", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var showOnServer = request.showOnServer;
                var showMissing = request.showMissing;

                if( !showOnServer && !showMissing )
                {
                    showOnServer = true;
                    showMissing = true;
                }

                var whichMedia = EWhichMediaList.eEpisodes;
                if( showOnServer )
                    whichMedia = whichMedia | EWhichMediaList.eOnServer;
                if( showMissing )
                    whichMedia = whichMedia | EWhichMediaList.eMissing;

                var episodes = db.getMediaListResponse( whichMedia );

                return episodes ?? new object();
            } );
            return retVal;
        }

        public object Get( GetMovieList request )
        {
            var retVal = GetRequest( "GetEpisodeList", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var showOnServer = request.showOnServer;
                var showMissing = request.showMissing;

                if( !showOnServer && !showMissing )
                {
                    showOnServer = true;
                    showMissing = true;
                }

                var whichMedia = EWhichMediaList.eMovies;
                if( showOnServer )
                    whichMedia = whichMedia | EWhichMediaList.eOnServer;
                if( showMissing )
                    whichMedia = whichMedia | EWhichMediaList.eMissing;

                var episodes = db.getMediaListResponse( whichMedia );

                return episodes ?? new object();
            } );
            return retVal;
        }

        public object Get( GetCodecSummary request )
        {
            return GetRequest( "GetCodecSummary", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                var groupData = db.MediaCodecs();
                if( groupData == null )
                    return new StatCardResponse();
                groupData.SortByKey = true;

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetResolutionSummary request )
        {
            return GetRequest( "GetResolutionSummary", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                var groupData = db.MediaResolutions();
                if( groupData == null )
                    return new StatCardResponse();
                groupData.SortByKey = false;

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetDVProfileSummary request )
        {
            return GetRequest( "GetDVProfileSummary", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                var groupData = db.DVProfileInfo();
                if( groupData == null )
                    return new StatCardResponse();
                groupData.SortByKey = true;

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetUserCount request )
        {
            return GetRequest( "GetUserCount", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                var groupData = db.UserCount();
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetPlayedUserMedia request )
        {
            return GetRequest( "GetPlayedUserMedia", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                var groupData = db.PlayedUserMedia();
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetUserWatchMediaIssues request )
        {
            return GetRequest( "GetUserWatchMediaIssues", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                var groupData = db.UserWatchMediaIssues();
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetMostActiveUsers request )
        {
            return GetRequest( "GetMostActiveUsers", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                var groupData = db.MostActiveUsers();
                if( groupData == null )
                    return new StatCardResponse();
                groupData.SortByKey = false;

                var vgReponse = groupData.createStat();

                return vgReponse;
            } );
        }

        public object TotalMovieCount( User? user )
        {
            var db = StatisticsDB.GetInstance( _embyInterfaces );

            var groupData = db.TotalMovieCount( user, false );
            if( groupData == null )
                return new StatCardResponse();

            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get( GetTotalMovieCount request )
        {
            return GetRequest( "GetTotalMovieCount", timer =>
            {
                var userName = request.user;
                var user = GetUserByName( userName );
                return user == null ? new object() : TotalMovieCount( user );
            } );
        }

        public object Get( GetTotalMovieCountNoUser request )
        {
            return GetRequest( "GetTotalMovieCountNoUser", timer =>
            {
                return TotalMovieCount( null );
            } );
        }

        public object Get( GetTotalMoviesWatched request )
        {
            return GetRequest( "GetTotalMoviesWatched", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var userName = request.user;
                var user = GetUserByName( userName );
                if( user == null )
                    return new StatCardResponse();

                var groupData = db.TotalMovieCount( user, true );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetTotalMissingMovies request )
        {
            return GetRequest( "GetTotalMissingMovies", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                var groupData = db.TotalMissingMovies();
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetTotalMissingEpisodes request )
        {
            return GetRequest( "GetTotalMissingEpisodes", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                var groupData = db.TotalMissingEpisodes();
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetTotalCollectionCount request )
        {
            return GetRequest( "GetTotalCollectionCount", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                var groupData = db.TotalCollectionCount();
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetTotalMovieStudioCount request )
        {
            return GetRequest( "GetTotalMovieStudioCount", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                var groupData = db.TotalMovieStudioCount( null );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object TotalTVCount( User? user, bool watched )
        {
            var db = StatisticsDB.GetInstance( _embyInterfaces );

            var groupData = db.TotalTVCount( user, watched );
            if( groupData == null )
                return new StatCardResponse();

            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get( GetTotalTVCount request )
        {
            return GetRequest( "GetTotalTVCount", timer =>
            {
                var userName = request.user;
                var user = GetUserByName( userName );
                return user == null ? new object() : TotalTVCount( user, false );
            } );
        }

        public object Get( GetTotalTVCountNoUser request )
        {
            return GetRequest( "GetTotalTVCountNoUser", timer =>
            {
                return TotalTVCount( null, false );
            } );
        }

        public object Get( GetTotalTVWatched request )
        {
            return GetRequest( "GetTotalTVWatched", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var userName = request.user;
                var user = GetUserByName( userName );
                if( user == null )
                    return new StatCardResponse();

                var groupData = db.TotalTVCount( user, true );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetTotalSeriesFinished request )
        {
            return GetRequest( "GetTotalSeriesFinished", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var userName = request.user;
                var user = GetUserByName( userName );
                if( user == null )
                    return new StatCardResponse();

                var groupData = db.TotalFinishedSeries( user );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetTotalTVStudioCount request )
        {
            return GetRequest( "GetTotalTVStudioCount", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var groupData = db.TotalTVStudioCount( null );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetLeastWatchedMovies request )
        {
            return GetRequest( "GetLeastWatchedMovies", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var groupData = db.WatchedMedia( null, true, EMediaType.eMovie );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetMostWatchedMovies request )
        {
            return GetRequest( "GetMostWatchedMovies", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                var userName = request.user;
                var user = GetUserByName( userName );
                if( user == null )
                    return new StatCardResponse();

                var groupData = db.WatchedMedia( user, false, EMediaType.eMovie );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetMostWatchedMoviesNoUser request )
        {
            return GetRequest( "GetMostWatchedMoviesNoUser", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                var groupData = db.WatchedMedia( null, false, EMediaType.eMovie );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetLeastWatchedShows request )
        {
            return GetRequest( "GetLeastWatchedShows", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                var groupData = db.WatchedMedia( null, true, EMediaType.eSeries );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetMostWatchedShows request )
        {
            return GetRequest( "GetMostWatchedShows", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var userName = request.user;
                var user = GetUserByName( userName );
                if( user == null )
                    return new StatCardResponse();

                var groupData = db.WatchedMedia( user, false, EMediaType.eSeries );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetMostWatchedShowsNoUser request )
        {
            return GetRequest( "GetMostWatchedShows", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                var groupData = db.WatchedMedia( null, false, EMediaType.eSeries );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetMultiCollectionMovies request )
        {
            return GetRequest( "GetMultiCollectionMovies", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                var groupData = db.MultiCollectionMovies();
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetTotalTimeWatched request )
        {
            return GetRequest( "GetTotalTimeWatched", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var userName = request.user;
                var user = GetUserByName( userName );
                if( user == null )
                    return new StatCardResponse();

                bool? showEpisodes = null;
                if( request.episodes == "true" )
                    showEpisodes = true;
                else if( request.episodes == "all" )
                    showEpisodes = null;
                else
                    showEpisodes = false;

                var groupData = db.TotalTimeWatched( user, showEpisodes );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetTotalWatchableTime request )
        {
            return GetRequest( "GetTotalWatchableTime", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var userName = request.user;
                var user = GetUserByName( userName );
                if( user == null )
                    return new StatCardResponse();

                bool? showEpisodes = null;
                if( request.episodes == "true" )
                    showEpisodes = true;
                else if( request.episodes == "all" )
                    showEpisodes = null;
                else
                    showEpisodes = false;

                var groupData = db.TotalWatchableTime( user, showEpisodes );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetLastSeen request )
        {
            return GetRequest( "GetLastSeen", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var userName = request.user;
                var user = GetUserByName( userName );
                if( user == null )
                    return new StatCardResponse();

                var episodes = request.episodes;

                var groupData = db.LastSeen( user, !episodes );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetDatabaseStatus request )
        {
            return GetRequest( "GetDatabaseStatus", timer =>
            {
                if( Plugin.Instance == null )
                    return new GetDatabaseStatusReponse();

                return new GetDatabaseStatusReponse( Plugin.Instance.Configuration );
            } );
        }

        public object Get( GetMovieFavoriteYears request )
        {
            return GetRequest( "GetMovieFavoriteYears", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var userName = request.user;
                var user = GetUserByName( userName );
                if( user == null )
                    return new StatCardResponse();

                var groupData = db.FavoriteYears( user, true );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetMovieFavoriteGenres request )
        {
            return GetRequest( "GetMovieFavoriteGenres", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var userName = request.user;
                var user = GetUserByName( userName );
                if( user == null )
                    return new StatCardResponse();

                var groupData = db.FavoriteGenre( user, true );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetTVFavoriteGenres request )
        {
            return GetRequest( "GetTVFavoriteGenres", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var userName = request.user;
                var user = GetUserByName( userName );
                if( user == null )
                    return new StatCardResponse();

                var groupData = db.FavoriteGenre( user, false );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetMovie request )
        {
            return GetRequest( "GetMovie", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var whichStatistic = request.whichStatistic;
                timer.Text += $" - {whichStatistic}";

                var groupData = db.StatisticFor( null, whichStatistic, StatGen.EVideoType.Movie );
                if( groupData == null )
                    return new StatCardResponse();

                return groupData.createStat();
            } );
        }

        public object Get( GetSeries request )
        {
            return GetRequest( "GetSeries", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var whichStatistic = request.whichStatistic;
                timer.Text += $" - {whichStatistic}";

                var groupData = db.StatisticFor( null, whichStatistic, StatGen.EVideoType.Series );
                if( groupData == null )
                    return new StatCardResponse();

                return groupData.createStat();
            } );
        }

        public object Get( GetEpisode request )
        {
            return GetRequest( "GetEpisode", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );
                var whichStatistic = request.whichStatistic;
                timer.Text += $" - {whichStatistic}";

                var groupData = db.StatisticFor( null, whichStatistic, StatGen.EVideoType.Episode );
                if( groupData == null )
                    return new StatCardResponse();

                return groupData.createStat();
            } );
        }
    }
}
