using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodecInfo.API
{
    public abstract class IBaseCalculator : IDisposable
    {
        private IEnumerable<Movie> fMovieCache;
        private IEnumerable<Episode> fEpisodeCache;

        protected readonly IUserManager fUserManager;
        protected readonly ILibraryManager fLibraryManager;
        protected readonly IUserDataManager fUserDataManager;
        protected readonly IProviderManager fProviderManager;
        protected readonly IFileSystem fFileSystem;
        protected readonly ILogger fLogger;
        protected readonly CancellationToken fCancellationToken;


        protected IBaseCalculator(IUserManager userManager, ILibraryManager libraryManager,
            IUserDataManager userDataManager, IFileSystem fileSystem, ILogger logger,
            IProviderManager providerManager, CancellationToken cancellationToken)
        {
            fUserManager = userManager;
            fLibraryManager = libraryManager;
            fUserDataManager = userDataManager;
            fProviderManager = providerManager;
            fFileSystem = fileSystem;
            fLogger = logger;
            fCancellationToken = cancellationToken;
        }

        #region Helpers

        protected IEnumerable<Movie> GetAllMovies()
        {
            return fMovieCache ?? (fMovieCache = GetItems<Movie>());
        }
        
        protected IEnumerable<Episode> GetAllEpisodes()
        {
            return fEpisodeCache ?? (fEpisodeCache = GetItems<Episode>());
        }

        private IEnumerable<T> GetItems<T>()
        {
            var query = new InternalItemsQuery(null)
            {
                IncludeItemTypes = new[] { typeof(T).Name },
                Recursive = true,
                IsVirtualItem = false,
                DtoOptions = new DtoOptions(true)
                {
                    EnableImages = false
                }
            };

            return fLibraryManager.GetItemList(query).OfType<T>();
        }

        #endregion

        public void Dispose()
        {
            ClearCache();
        }

        public void ClearCache()
        {
            try
            {
                fEpisodeCache = null;
                fMovieCache = null;
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString());
            }
        }
    }
}