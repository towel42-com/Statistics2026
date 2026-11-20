using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Tasks;
using Statistics2026.Data;
using Statistics2026.Utilities;

namespace Statistics2026.Services
{
    [Route( "/Statistics2026/resolution_summary", "GET", Summary = "Gets Resolution Summary for Library" )]
    [Authenticated( Roles = "admin" )]
    public class GetResolutionSummary : IReturn<object>
    {
    }

    [Route( "/Statistics2026/codec_summary", "GET", Summary = "Gets Codec Summary for Library" )]
    [Authenticated( Roles = "admin" )]
    public class GetCodecSummary : IReturn<object>
    {
    }

    [Route( "/Statistics2026/dvprofile_summary", "GET", Summary = "Gets Dolby Vision Profile Summary for Library" )]
    [Authenticated( Roles = "admin" )]
    public class GetDVProfileSummary : IReturn<object>
    {
    }

    [Route( "/Statistics2026/total_movie_count/{User}", "GET", Summary = "Get the total Movie Count" )]
    [Authenticated]
    public class GetTotalMovieCount : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/total_movie_count", "GET", Summary = "Get the total Movie Count" )]
    [Authenticated( Roles = "admin" )]
    public class GetTotalMovieCountNoUser : IReturn<object>
    {
    }

    [Route( "/Statistics2026/total_movies_watched/{User}", "GET", Summary = "Get the total Movie Count" )]
    [Authenticated]
    public class GetTotalMoviesWatched : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/movie_genre_list", "GET", Summary = "Get the Movie Genre List" )]
    [Authenticated( Roles = "admin" )]
    public class GetMovieGenreList : IReturn<object>
    {
    }

    [Route( "/Statistics2026/movie_actors_list", "GET", Summary = "Get the Movie Actors List" )]
    [Authenticated( Roles = "admin" )]
    public class GetMovieActorsList : IReturn<object>
    {
    }

    [Route( "/Statistics2026/movie_ratings_list", "GET", Summary = "Get the Movie Ratings List" )]
    [Authenticated( Roles = "admin" )]
    public class GetMovieRatingsList : IReturn<object>
    {
    }

    [Route( "/Statistics2026/total_episode_count", "GET", Summary = "Get the total Episode Count" )]
    [Authenticated( Roles = "admin" )]
    public class GetTotalEpisodeCount : IReturn<object>
    {
    }

    [Route( "/Statistics2026/total_series_count", "GET", Summary = "Get the total Series Count" )]
    [Authenticated( Roles = "admin" )]
    public class GetTotalSeriesCount : IReturn<object>
    {
    }

    [Route( "/Statistics2026/total_tv_count/{User}", "GET", Summary = "Get the total TV Count" )]
    [Authenticated]
    public class GetTotalTVCount : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/total_tv_watched/{User}", "GET", Summary = "Get the total Movie Count" )]
    [Authenticated]
    public class GetTotalTVWatched : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/series_genre_list", "GET", Summary = "Get the Series Genre List" )]
    [Authenticated( Roles = "admin" )]
    public class GetSeriesGenreList : IReturn<object>
    {
    }

    [Route( "/Statistics2026/series_actors_list", "GET", Summary = "Get the Series Actors List" )]
    [Authenticated( Roles = "admin" )]
    public class GetSeriesActorsList : IReturn<object>
    {
    }

    [Route( "/Statistics2026/episode_genre_list", "GET", Summary = "Get the Episode Genre List" )]
    [Authenticated( Roles = "admin" )]
    public class GetEpisodeGenreList : IReturn<object>
    {
    }

    [Route( "/Statistics2026/episode_actors_list", "GET", Summary = "Get the Episode Actors List" )]
    [Authenticated( Roles = "admin" )]
    public class GetEpisodeActorsList : IReturn<object>
    {
    }

    [Route( "/Statistics2026/series_ratings_list", "GET", Summary = "Get the Series Ratings List" )]
    [Authenticated( Roles = "admin" )]
    public class GetSeriesRatingsList : IReturn<object>
    {
    }


    [Route( "/Statistics2026/total_movie_studio_count", "GET", Summary = "Get the total Movie Studio Count" )]
    [Authenticated( Roles = "admin" )]
    public class GetTotalMovieStudioCount : IReturn<object>
    {
    }

    [Route( "/Statistics2026/total_episode_studio_count", "GET", Summary = "Get the total Episode Studio Count" )]
    [Authenticated( Roles = "admin" )]
    public class GetTotalEpisodeStudioCount : IReturn<object>
    {
    }

    [Route( "/Statistics2026/total_series_studio_count", "GET", Summary = "Get the total Series Studio Count" )]
    [Authenticated( Roles = "admin" )]
    public class GetTotalSeriesStudioCount : IReturn<object>
    {
    }

    [Route( "/Statistics2026/movie_favorite_years/{User}", "GET", Summary = "Get the Favorite Movie Years" )]
    [Authenticated]
    public class GetMovieFavoriteYears : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/movie_favorite_genres/{User}", "GET", Summary = "Get the Favorite Movie Genres" )]
    [Authenticated]
    public class GetMovieFavoriteGenres : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
    }

    [Route( "/Statistics2026/tv_favorite_genres/{User}", "GET", Summary = "Get the Favorite Movie Genres" )]
    [Authenticated]
    public class GetTVFavoriteGenres : IReturn<object>
    {
        public string user { get; set; } = string.Empty;
    }

    public class MediaService : BaseService
    {
        public MediaService(
           IFileSystem fileSystem,
           ILibraryManager libraryManager,
           ILogManager logManager,
           IServerApplicationPaths serverApplicationPaths,
           IUserDataManager userDataManager,
           IUserManager userManager,
           IApplicationHost appHost,
           IJsonSerializer jsonSerializer,
           IProviderManager providerManager,
           IServerConfigurationManager configManager,
           ITaskManager taskManager,
           ISessionManager sessionManager,
           IHttpClient httpClient,
           IAuthenticationRepository authenticationRepository,
           INotificationManager notificationManager
        ) : base(
               fileSystem,
               libraryManager,
               logManager,
               serverApplicationPaths,
               userDataManager,
               userManager,
               appHost,
               jsonSerializer,
               providerManager,
               configManager,
               taskManager,
               sessionManager,
               httpClient,
               authenticationRepository,
               notificationManager
            )
        {
        }

        public object Get( GetResolutionSummary request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                var groupData = db.MediaResolutions();
                if( groupData == null )
                    return new StatCardResponse();
                groupData.SortByKey = false;

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetCodecSummary request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                var groupData = db.MediaCodecs();
                if( groupData == null )
                    return new StatCardResponse();
                groupData.SortByKey = true;

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetDVProfileSummary request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                var groupData = db.DVProfileInfo();
                if( groupData == null )
                    return new StatCardResponse();
                groupData.SortByKey = true;

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object TotalMovieCount( User? user )
        {
            using var db = Statistics2026DB.GetInstance( _embyInterfaces );

            var groupData = db.TotalMovieCount( user, false );
            if( groupData == null )
                return new StatCardResponse();

            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get( GetTotalMovieCount request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                var userName = request.user;
                var user = EmbyHelperFuncs.GetUserByName( userName, _embyInterfaces );
                return user == null ? new object() : TotalMovieCount( user );
            } );
        }

        public object Get( GetTotalMovieCountNoUser request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                return TotalMovieCount( null );
            } );
        }

        public object Get( GetTotalMoviesWatched request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );
                var userName = request.user;
                var user = EmbyHelperFuncs.GetUserByName( userName, _embyInterfaces );
                if( user == null )
                    return new StatCardResponse();

                var groupData = db.TotalMovieCount( user, true );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetMovieGenreList request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                var groupData = db.GenreList( true );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetSeriesGenreList request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                var groupData = db.GenreList( false, true );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetEpisodeGenreList request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                var groupData = db.GenreList( false, false );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }


        //GetEpisodeActorsList
        public object Get( GetMovieActorsList request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                var groupData = db.ActorsList( true, null );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetSeriesActorsList request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                var groupData = db.ActorsList( false, true );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }


        public object Get( GetEpisodeActorsList request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                var groupData = db.ActorsList( false, false );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetMovieRatingsList request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                var groupData = db.RatingsList( true );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetSeriesRatingsList request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                var groupData = db.RatingsList( false );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object TotalTVCount( User? user, bool watched, bool forSeries )
        {
            using var db = Statistics2026DB.GetInstance( _embyInterfaces );

            var groupData = db.TotalTVCount( user, watched, forSeries );
            if( groupData == null )
                return new StatCardResponse();

            var vgReponse = groupData.createStat();
            return vgReponse;
        }

        public object Get( GetTotalTVCount request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                var userName = request.user;
                var user = EmbyHelperFuncs.GetUserByName( userName, _embyInterfaces );
                return user == null ? new object() : TotalTVCount( user, false, false );
            } );
        }

        public object Get( GetTotalSeriesCount request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                return TotalTVCount( null, false, true );
            } );
        }

        public object Get( GetTotalEpisodeCount request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                return TotalTVCount( null, false, false );
            } );
        }

        public object Get( GetTotalTVWatched request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );
                var userName = request.user;
                var user = EmbyHelperFuncs.GetUserByName( userName, _embyInterfaces );
                if( user == null )
                    return new StatCardResponse();

                var groupData = db.TotalTVCount( user, true, false );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetTotalMovieStudioCount request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );

                var groupData = db.TotalMovieStudioCount( null );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetTotalSeriesStudioCount request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );
                var groupData = db.TotalTVStudioCount( null, false );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetTotalEpisodeStudioCount request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );
                var groupData = db.TotalTVStudioCount( null, true );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }

        public object Get( GetMovieFavoriteYears request )
        {
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );
                var userName = request.user;
                var user = EmbyHelperFuncs.GetUserByName( userName, _embyInterfaces );
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
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );
                var userName = request.user;
                var user = EmbyHelperFuncs.GetUserByName( userName, _embyInterfaces );
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
            return HelperFuncs.HandleRequest( request.GetType().Name, _embyInterfaces, timer =>
            {
                using var db = Statistics2026DB.GetInstance( _embyInterfaces );
                var userName = request.user;
                var user = EmbyHelperFuncs.GetUserByName( userName, _embyInterfaces );
                if( user == null )
                    return new StatCardResponse();

                var groupData = db.FavoriteGenre( user, false );
                if( groupData == null )
                    return new StatCardResponse();

                var vgReponse = groupData.createStat();
                return vgReponse;
            } );
        }
    }
}
