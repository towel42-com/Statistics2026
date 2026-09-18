using MediaBrowser.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Tasks;
using Statistics2026.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statistics2026.Api
{
    public partial class Statistics2026API : IService, IRequiresRequest
    {
        private readonly EmbyInterfaces _embyInterfaces;

        public Statistics2026API(
            ILogManager logManager,
            IServerConfigurationManager configManager,
            IUserManager userManager,
            IUserDataManager userDataManager,
            ILibraryManager libraryManager,
            IFileSystem fileSystem,
            IJsonSerializer jsonSerializer,
            IServerApplicationPaths serverApplicationPaths,
            IApplicationHost appHost,
            IProviderManager providerManager,
            ITaskManager taskManager
            )
        {
            _embyInterfaces = new EmbyInterfaces( fileSystem, libraryManager, logManager, logManager.GetLogger( "Statistics2026 - Statistics2026API" ), serverApplicationPaths, userDataManager, userManager, appHost, this, jsonSerializer, providerManager, configManager, taskManager );
        }

        public IRequest? Request { get; set; } = null;

        private IEnumerable<T>? GetItems<T>( User? user )
        {
            return DBHelper.GetUserItems<T>( user, _embyInterfaces._libraryManager! );
        }

        public static (IEnumerable<Video> forUser, IEnumerable<Video>? forAll) GetAllEpisodesAndMovies( User? user, ILibraryManager libManager, bool computeAll )
        {
            var episodesForUser = DBHelper.GetUserItems<Episode>( user, libManager ).OfType<Video>().ToList();
            var moviesForUser = DBHelper.GetUserItems<Movie>( user, libManager ).OfType<Video>().ToList();
            var forUser = episodesForUser.Concat( moviesForUser );

            IEnumerable<Video>? all = null;
            if( computeAll )
            {
                var allEpisodes = DBHelper.GetUserItems<Episode>( null, libManager ).OfType<Video>().ToList();
                var allMovies = DBHelper.GetUserItems<Movie>( null, libManager ).OfType<Video>().ToList();
                all = allEpisodes.Concat( allMovies );
            }

            return (forUser, all);
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
