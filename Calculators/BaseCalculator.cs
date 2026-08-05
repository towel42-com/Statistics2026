using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;

namespace CodecInfoPlugin.Calculators
{
    public abstract class BaseCalculator : IDisposable
    {
        private IEnumerable<Movie> _movieCache;
        private IEnumerable<Episode> _episodeCache;

        protected readonly IUserManager UserManager;
        protected readonly ILibraryManager LibraryManager;
        protected readonly IUserDataManager UserDataManager;
        protected readonly IProviderManager ProviderManager;
        protected readonly ILogger _logger;


        protected BaseCalculator(IUserManager userManager, ILibraryManager libraryManager,
            IUserDataManager userDataManager, IProviderManager providerManager, ILogger Logger, CancellationToken cancellationToken)
        {
            UserManager = userManager;
            LibraryManager = libraryManager;
            UserDataManager = userDataManager;
            ProviderManager = providerManager;
            _logger = Logger;
        }

        #region Helpers

        protected IEnumerable<Movie> GetAllMovies()
        {
            return _movieCache ?? (_movieCache = GetItems<Movie>());
        }
        
        protected IEnumerable<Episode> GetAllEpisodes()
        {
            return _episodeCache ?? (_episodeCache = GetItems<Episode>());
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

            return LibraryManager.GetItemList(query).OfType<T>();
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
                _episodeCache = null;
                _movieCache = null;
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString());
            }
        }
    }
}