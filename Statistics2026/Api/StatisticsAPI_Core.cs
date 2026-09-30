using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Tasks;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statistics2026.Api
{
    public partial class Statistics2026API : IService, IRequiresRequest
    {
        private readonly EmbyInterfaces _embyInterfaces;

        public Statistics2026API(
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
            IAuthenticationRepository authenticationRepository
        )
        {
            _embyInterfaces = new EmbyInterfaces( appHost )
            {
                _fileSystem = fileSystem,
                _libraryManager = libraryManager,
                _logManager = logManager,
                _logger = logManager.GetLogger( "Statistics2026 - Statistics2026API" ),
                _serverApplicationPaths = serverApplicationPaths,
                _userDataManager = userDataManager,
                _userManager = userManager,
                _appHost = appHost,
                _apiService = this,
                _jsonSerializer = jsonSerializer,
                _providerManager = providerManager,
                _configManager = configManager,
                _taskManager = taskManager,
                _sessionManager = sessionManager,
                _httpClient = httpClient,
                _authenticationRepository = authenticationRepository
            };
        }

        public IRequest? Request { get; set; } = null;

        private IEnumerable<T>? GetItems<T>( User? user )
        {
            return DBHelper.GetUserItems<T>( user, _embyInterfaces._libraryManager! );
        }

        public static (IEnumerable<Video>? forUser, IEnumerable<Video>? forAll) GetAllEpisodesAndMovies( User? user, ILibraryManager? libManager, bool computeAll )
        {
            if( libManager == null )
                return (null, null);

            IEnumerable<Video>? forUser = ( user != null ) ? GetAllEpisodesAndMoviesForUser( user, libManager ) : null;

            IEnumerable<Video>? all = null;
            if( computeAll )
            {
                var allEpisodes = DBHelper.GetUserItems<Episode>( null, libManager ).OfType<Video>().ToList();
                var allMovies = DBHelper.GetUserItems<Movie>( null, libManager ).OfType<Video>().ToList();
                all = allEpisodes.Concat( allMovies );
            }

            return (forUser, all);
        }

        public static IEnumerable<Video> GetAllEpisodesAndMoviesForUser( User user, ILibraryManager? libManager )
        {
            if( libManager == null )
                return Enumerable.Empty<Video>();
            var episodesForUser = DBHelper.GetUserItems<Episode>( user, libManager ).OfType<Video>().ToList();
            var moviesForUser = DBHelper.GetUserItems<Movie>( user, libManager ).OfType<Video>().ToList();
            var forUser = episodesForUser.Concat( moviesForUser );

            return forUser;
        }

        public static IEnumerable<BoxSet> GetAllBoxSets( User user, ILibraryManager libManager )
        {
            var boxSets = DBHelper.GetUserItems<BoxSet>( user, libManager ).OfType<BoxSet>().ToList();
            return boxSets;
        }

        private List<MediaInfo>? GetVideos<T>( User? user ) where T : Video
        {
            List<MediaInfo> mediaInfos = [];
            var items = GetItems<T>( user );
            if( items == null )
                return null;
            foreach( var item in items )
            {
                var curr = new MediaInfo( item );
                if( !curr.aOK )
                    continue;

                mediaInfos.Add( curr );
            }

            return mediaInfos;
        }

        private User? GetUserByName( string userName )
        {
            if( _embyInterfaces == null )
                throw new ArgumentNullException( "_embyInterfaces is null." );

            return _embyInterfaces.GetUserByName( userName );
        }

        private User? GetUserById( Guid id )
        {
            if( _embyInterfaces == null )
                throw new ArgumentNullException( "_embyInterfaces is null." );

            var user = _embyInterfaces.GetUserById( id );
            return user;
        }
    }
}
