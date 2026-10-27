using Emby.Media.Common.Extensions;
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
        private object HandleRequest( string requestName, Func<AutoTimer, object> requestFunc )
        {
            using( var timer = new AutoTimer( $"Request: {requestName}", _embyInterfaces.Logger() ) )
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
            return HandleRequest( "GetLastRun", timer =>
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
            return HandleRequest( "GetTVSeriesProgress", timer =>
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

        public object Get( GetSeriesFilter request )
        {
            var retVal = HandleRequest( "GetSeriesFilter", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                return db.GetSeriesFilter( true );
            } );
            return retVal;
        }

        public object Post( SetSeriesFilter request )
        {
            var retVal = HandleRequest( "SetSeriesFilter", timer =>
            {
                var db = StatisticsDB.GetInstance( _embyInterfaces );

                var seriesFilter = new SeriesFilterResponse()
                {
                    SeriesId = request.series_id,
                    TrackSeries = request.track_series,
                    TrackSpecials = request.track_specials,
                    MinSeason = request.min_season,
                    MaxSeason = request.max_season
                };

                if( seriesFilter.SeriesId.IsNullOrEmpty() )
                    return new object();

                return db.SaveSeriesFilter( seriesFilter, true );
            } );
            return retVal;
        }

        public object Get( GetEpisodeList request )
        {
            var retVal = HandleRequest( "GetEpisodeList", timer =>
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
            var retVal = HandleRequest( "GetMovieList", timer =>
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

                var movies = db.getMediaListResponse( whichMedia );

                return movies ?? new object();
            } );
            return retVal;
        }

        public object Get( GetCodecSummary request )
        {
            return HandleRequest( "GetCodecSummary", timer =>
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
            return HandleRequest( "GetResolutionSummary", timer =>
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
            return HandleRequest( "GetDVProfileSummary", timer =>
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
            return HandleRequest( "GetUserCount", timer =>
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
            return HandleRequest( "GetPlayedUserMedia", timer =>
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
            return HandleRequest( "GetUserWatchMediaIssues", timer =>
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
            return HandleRequest( "GetMostActiveUsers", timer =>
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
            return HandleRequest( "GetTotalMovieCount", timer =>
            {
                var userName = request.user;
                var user = GetUserByName( userName );
                return user == null ? new object() : TotalMovieCount( user );
            } );
        }

        public object Get( GetTotalMovieCountNoUser request )
        {
            return HandleRequest( "GetTotalMovieCountNoUser", timer =>
            {
                return TotalMovieCount( null );
            } );
        }

        public object Get( GetTotalMoviesWatched request )
        {
            return HandleRequest( "GetTotalMoviesWatched", timer =>
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
            return HandleRequest( "GetTotalMissingMovies", timer =>
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
            return HandleRequest( "GetTotalMissingEpisodes", timer =>
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
            return HandleRequest( "GetTotalCollectionCount", timer =>
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
            return HandleRequest( "GetTotalMovieStudioCount", timer =>
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
            return HandleRequest( "GetTotalTVCount", timer =>
            {
                var userName = request.user;
                var user = GetUserByName( userName );
                return user == null ? new object() : TotalTVCount( user, false );
            } );
        }

        public object Get( GetTotalTVCountNoUser request )
        {
            return HandleRequest( "GetTotalTVCountNoUser", timer =>
            {
                return TotalTVCount( null, false );
            } );
        }

        public object Get( GetTotalTVWatched request )
        {
            return HandleRequest( "GetTotalTVWatched", timer =>
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
            return HandleRequest( "GetTotalSeriesFinished", timer =>
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
            return HandleRequest( "GetTotalTVStudioCount", timer =>
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
            return HandleRequest( "GetLeastWatchedMovies", timer =>
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
            return HandleRequest( "GetMostWatchedMovies", timer =>
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
            return HandleRequest( "GetMostWatchedMoviesNoUser", timer =>
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
            return HandleRequest( "GetLeastWatchedShows", timer =>
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
            return HandleRequest( "GetMostWatchedShows", timer =>
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
            return HandleRequest( "GetMostWatchedShows", timer =>
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
            return HandleRequest( "GetMultiCollectionMovies", timer =>
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
            return HandleRequest( "GetTotalTimeWatched", timer =>
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
            return HandleRequest( "GetTotalWatchableTime", timer =>
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
            return HandleRequest( "GetLastSeen", timer =>
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
            return HandleRequest( "GetDatabaseStatus", timer =>
            {
                if( Plugin.Instance == null )
                    return new GetDatabaseStatusReponse();

                return new GetDatabaseStatusReponse( Plugin.Instance.Configuration );
            } );
        }

        public object Get( GetMovieFavoriteYears request )
        {
            return HandleRequest( "GetMovieFavoriteYears", timer =>
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
            return HandleRequest( "GetMovieFavoriteGenres", timer =>
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
            return HandleRequest( "GetTVFavoriteGenres", timer =>
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
            return HandleRequest( "GetMovie", timer =>
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
            return HandleRequest( "GetSeries", timer =>
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
            return HandleRequest( "GetEpisode", timer =>
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
