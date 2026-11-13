#if DEBUG
#define DEBUG_PLUGIN
#define DEBUG_PLUGIN_ASYNC
//#define DEBUG_DB
#endif
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Data;
using Statistics2026.Secrets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using static Statistics2026.Data.Statistics2026DB;

namespace Statistics2026.Utilities
{
    public static class EmbyHelperFuncs
    {
        public static async Task MoveMediaLocations( EmbyInterfaces embyInterfaces )
        {
#if DEBUG_PLUGIN
            bool copyImages( BaseItem item )
            {
                if( item == null )
                    return false;

                if( item is Video videoItem )
                {
                    if( ( videoItem is Movie )
                        || ( videoItem is Episode )
                        || ( videoItem is MusicVideo ) )
                        return true;
                    return false;
                }

                if( item is Audio )
                    return true;

                if( item is Folder )
                    return true;

                //embyInterfaces.Logger().Debug( $"Item: {item.Name} - {item.MediaType} - {item.GetType()}" );

                var allParents = item.GetParents().ToList();
                foreach( var parent in allParents )
                {
                    var parentType = parent.MediaType;
                    //embyInterfaces.Logger().Debug( $"CollectionFolder parent: {parent.Name} - {parentType} - {parent.GetType()}" );
                    if( parent is PhotoAlbum photoAlbum )
                    {
                        return false;
                    }
                    else if( parent is Folder folder )
                    {
                        if( parent.Parent is AggregateFolder )
                        {
                            // top level folder
                            if( parent.Name == "photos" )
                            {
                                return false;
                            }
                            break;
                        }
                    }
                }
                return true;
            }

            var query = new InternalItemsQuery
            {
                IsVirtualItem = false,
                Recursive = true
            };

            embyInterfaces.Logger().Debug( $"Updating Library paths: From '{DBLocation.getOldLocations().Join( ", " )}' to '{DBLocation.getNewLocation()}'" );

            BaseItem[] allItems = embyInterfaces.LibraryManager().GetItemList( query );

            var items = allItems.Where( item =>
                   DBLocation.isOldLocation( item.Path )
                || ( item.ImageInfos != null && item.ImageInfos.Any( imageInfo => DBLocation.isOldLocation( imageInfo.Path ) ) )
            ).ToList();

            ConcurrentBag<BaseItem> modifiedItems = [];
            long numItemsModified = 0;
            long numImagesModified = 0;
            long count = -1;

            var pool = items.Select( ( item, index ) => Task.Run( () =>
            {
                if( item == null )
                    return;

                bool copyFile = copyImages( item );

                var isModified = false;
                var numCurrImageModified = 0;
                if( DBLocation.isOldLocation( item.Path ) )
                {
                    numCurrImageModified++;
                    item.Path = DBLocation.updateLocation( item.Path, false, embyInterfaces.Logger() ).path;
                    isModified = true;
                }
                var allImageInfos = item.ImageInfos;
                for( int jj = 0; jj < allImageInfos.Count(); ++jj )
                {
                    var imageInfo = allImageInfos[ jj ];
                    if( imageInfo == null )
                        continue;

                    if( DBLocation.isOldLocation( imageInfo.Path ) )
                    {
                        bool aOK = false;
                        (imageInfo.Path, aOK) = DBLocation.updateLocation( imageInfo.Path, copyFile, embyInterfaces.Logger() );
                        if( aOK )
                        {
                            numCurrImageModified++;
                            item.SetImage( imageInfo, 0 );
                            allImageInfos[ jj ] = imageInfo;
                        }
                    }
                }

                var imagesModified = Interlocked.Read( ref numImagesModified );
                if( numCurrImageModified != 0 )
                {
                    imagesModified = Interlocked.Add( ref numImagesModified, numCurrImageModified );
                    item.ImageInfos = allImageInfos;
                    isModified = true;
                }

                var numModified = Interlocked.Read( ref numItemsModified );
                if( isModified )
                {
                    numModified = Interlocked.Increment( ref numItemsModified );
                    modifiedItems.Add( item );
                }

                var ii = Interlocked.Increment( ref count );
                if( ( ii % 1000 ) == 0 )
                {
                    embyInterfaces.Logger().Debug( $"Updating Library paths ({ii} of {items.Count()} - {numModified} items, {imagesModified} images modified)" );
                }
            } ) );

            await Task.WhenAll( pool );
            embyInterfaces.Logger().Debug( $"Updating Library paths - {numItemsModified} items, {numImagesModified} images modified)" );
            count = 0;
            foreach( var item in modifiedItems )
            {
                if( ( ( count++ ) % 1000 ) == 0 )
                {
                    embyInterfaces.Logger().Debug( $"Updating Library Items - ({count} of {modifiedItems.Count}" );
                }
                embyInterfaces.LibraryManager().UpdateItem(
                    item,
                    item.GetParent(),
                    ItemUpdateType.None,
                    null
                );
            }
#endif
        }

        public static List<T> GetLibraryItems<T>( User? user, ILibraryManager libManager, bool getImages = true )
        {
            if( libManager == null )
                return [];

            var query = new InternalItemsQuery( user )
            {
                IncludeItemTypes = new[] { typeof( T ).Name },
                Recursive = true,
                IsVirtualItem = false
            };
            if( getImages )
            {
                query.DtoOptions = new DtoOptions( true )
                {
                    ImageTypes = new[] { ImageType.Thumb, ImageType.Thumbnail },
                    EnableImages = true
                };
            }

            return libManager.GetItemList( query ).OfType<T>().ToList();
        }

        public static List<Video> GetAllEpisodesAndMovies( User? user, ILibraryManager libManager )
        {
            if( libManager == null )
                return [];

            var query = new InternalItemsQuery( user )
            {
                IncludeItemTypes = new[] { typeof( Movie ).Name, typeof( Episode ).Name },
                Recursive = true,
                IsVirtualItem = false
            };
            query.DtoOptions = new DtoOptions( true )
            {
                ImageTypes = new[] { ImageType.Thumb, ImageType.Thumbnail },
                EnableImages = true
            };

            return libManager.GetItemList( query ).OfType<Video>().ToList();
        }

        public static (List<Video>? forUser, List<Video>? forAll) GetAllEpisodesAndMovies( User? user, ILibraryManager libManager, bool computeForNonUsers )
        {
            if( libManager == null )
                return (null, null);

            List<Video>? forUser = ( user != null ) ? GetAllEpisodesAndMovies( user, libManager ) : null;
            List<Video>? all = computeForNonUsers ? GetAllEpisodesAndMovies( null, libManager ) : null;

            return (forUser, all);
        }

        public static List<BoxSet> GetAllBoxSets( User? user, ILibraryManager libManager )
        {
            var boxSets = GetLibraryItems<BoxSet>( user, libManager );
            return boxSets;
        }

        public static List<Episode> GetAllEpisodesForSeries( User? user, ILibraryManager libManager, Series series )
        {
            var query = new InternalItemsQuery( user )
            {
                IncludeItemTypes = new[] { typeof( Episode ).Name },
                Recursive = true,
                IsVirtualItem = false,
                Parent = series
            };

            var episodes = libManager.GetItemList( query ).OfType<Episode>().ToList();
            return episodes;
        }

        public static List<Series> GetAllSeries( User? user, ILibraryManager libManager, bool includeEmptySeries )
        {
            var series = GetLibraryItems<Series>( user, libManager );
            if( !includeEmptySeries )
                return series;

            series = series.Where( series =>
            {
                var episodes = GetAllEpisodesForSeries( user, libManager, series );
                return ( episodes.Count() != 0 );
            } ).ToList();

            if( series == null )
                return [];

            return series;
        }

        public static Season? GetSeasonFromSeries( Series series, int seasonNum )
        {
            var result = series.GetItems( new InternalItemsQuery
            {
                IncludeItemTypes = new[] { nameof( Season ) },
                Recursive = true
            } );

            var seasons = result.Items.OfType<Season>().ToList();
            foreach( var season in seasons )
            {
                if( season.IndexNumber == seasonNum )
                    return season;
            }

            return null;
        }

        public static Movie? GetMovieByTmdbId( ILibraryManager libraryManager, string tmdbId )
        {
            var query = new InternalItemsQuery
            {
                // Target only Movie types to ensure accuracy
                IncludeItemTypes = new[] { typeof( Movie ).Name },
                // Look up by the formatted provider string
                AnyProviderIdEquals = new[]
                {
                    new KeyValuePair<string, string>("Tmdb", tmdbId)
                },
                // Search across all libraries recursively
                Recursive = true
            };

            // Execute query and grab the first match
            var result = libraryManager.GetItemList( query );
            if( result == null )
                return null;

            return result.FirstOrDefault() as Movie;
        }

        public static Episode? GetEpisodeFromTmdbId( ILibraryManager libraryManager, Season embySeason, string tmdbId, int episodeNum )
        {
            if( embySeason == null )
                return null;

            var query = new InternalItemsQuery
            {
                Parent = embySeason,
                // Target only Movie types to ensure accuracy
                IncludeItemTypes = new[] { typeof( Episode ).Name },
                // Look up by the formatted provider string
                AnyProviderIdEquals = new[]
                {
                    new KeyValuePair<string, string>("Tmdb", tmdbId)
                },
                // Search across all libraries recursively
                Recursive = true
            };

            // Execute query and grab the first match
            var result = libraryManager.GetItemList( query );
            if( result != null && result.Count() != 0 )
            {
                var episode = result.FirstOrDefault() as Episode;
                if( episode != null )
                    return episode;
            }

            var queryResult = embySeason.GetItems( new InternalItemsQuery
            {
                IncludeItemTypes = new[] { nameof( Episode ) },
                Recursive = true
            } );

            // Extract the episode numbers
            var retVal = queryResult.Items
                .OfType<Episode>()
                .FirstOrDefault( ep =>
                ep.IndexNumber == episodeNum ||
                ( ep.IndexNumberEnd != null && ( ep.IndexNumber <= episodeNum ) && ( ep.IndexNumberEnd >= episodeNum ) ) )
                ;

            return retVal;
        }

        public static BaseItem? GetLastMovieOrEpisodeAdded( EmbyInterfaces? embyInterfaces, EWhichMediaList whichMedia = EWhichMediaList.eMovies | EWhichMediaList.eEpisodes )
        {
            if( embyInterfaces == null )
                return null;

            List<string> itemTypes = [];
            if( ( ( whichMedia & EWhichMediaList.eMovies ) != 0 ) )
            {
                itemTypes.Add( "Movie" );
            }
            if( ( ( whichMedia & EWhichMediaList.eEpisodes ) != 0 ) )
            {
                itemTypes.Add( "Episode" );
            }

            var limit = 10;
            while( true )
            {
                var query = new InternalItemsQuery
                {
                    IsFolder = false,
                    OrderBy = new[] { new ValueTuple<string, SortOrder>( ItemSortBy.DateCreated, SortOrder.Descending ) },
                    IsVirtualItem = false,
                    IncludeItemTypes = itemTypes.ToArray(),
                    Recursive = true,
                    Limit = limit
                };

                var items = embyInterfaces.LibraryManager().GetItemList( query );
                foreach( var item in items )
                {
                    if( item.DateCreated == DateTimeOffset.MinValue )
                        continue;
                    return item;
                }
                // no valid dates found double the limit;
                if( items.Count() < limit )
                    break;
                limit *= 2;
            }
            return null;
        }

        public static Video? GetLastMovieAdded( EmbyInterfaces? embyInterfaces )
        {
            return (Video?)GetLastMovieOrEpisodeAdded( embyInterfaces, EWhichMediaList.eMovies );
        }

        public static Video? GetLastEpisodeAdded( EmbyInterfaces? embyInterfaces )
        {
            return (Video?)GetLastMovieOrEpisodeAdded( embyInterfaces, EWhichMediaList.eEpisodes );
        }

        public static User? GetLastAddedUser( EmbyInterfaces? embyInterfaces )
        {
            if( embyInterfaces == null )
                return null;

            var queryResult = EmbyHelperFuncs.GetAllUsers( embyInterfaces, false );
            if( queryResult != null )
            {
                // 3. Find the newest user by sorting their creation date
                var users = queryResult
                    .OrderByDescending( u => u.DateCreated );

                foreach( var user in users )
                {
                    if( user == null )
                        continue;

                    if( user.DateCreated == DateTimeOffset.MinValue )
                        continue;

                    return user;
                }
            }
            return null;
        }


        public static void UpdateLastAdded( EmbyInterfaces? embyInterfaces, List<string> reason, Statistics2026DB db )
        {
            var lastMediaAdded = EmbyHelperFuncs.GetLastMovieOrEpisodeAdded( embyInterfaces );
            DateTimeOffset? lastAdded = null;
            if( lastMediaAdded != null && lastMediaAdded!.DateCreated != DateTimeOffset.MinValue )
            {
                lastAdded = lastMediaAdded!.DateCreated;
            }
            var lastUserAdded = EmbyHelperFuncs.GetLastAddedUser( embyInterfaces );
            if( lastUserAdded != null && lastUserAdded!.DateCreated != DateTimeOffset.MinValue )
            {
                if( lastAdded == null )
                    lastAdded = lastUserAdded!.DateCreated;
                else if( lastAdded < lastUserAdded!.DateCreated )
                    lastAdded = lastUserAdded!.DateCreated;
            }
            if( lastAdded != null && lastAdded != DateTimeOffset.MinValue )
            {
                db.SetLastUpdated( Statistics2026DB.EAction.LastMediaOrUserAdded, reason, 0, lastAdded.Value );
            }
        }

        public static BaseItem? getLatestItem( BaseItem? lhs, BaseItem? rhs )
        {
            if( rhs == null || ( rhs.DateCreated == null ) )
            {
                return lhs;
            }
            if( lhs == null || ( lhs.DateCreated == null ) )
            {
                return rhs;
            }

            if( lhs.DateCreated! >= rhs.DateCreated! )
                return lhs;
            return rhs;
        }

        public static (BaseItem? item, string reason) getLastItemOfConsequenceAdded( EmbyInterfaces? embyInterfaces )
        {
            BaseItem? latestItem = null;

            // all items analyzed are tied to new episodes, movies or users

            var lastMovieAddedToServer = EmbyHelperFuncs.GetLastMovieAdded( embyInterfaces );
            if( lastMovieAddedToServer != null && lastMovieAddedToServer!.DateCreated != DateTimeOffset.MinValue )
            {
                embyInterfaces?.Logger().Info( $"Last Movie added: '{MediaInfo.GetDisplayName( lastMovieAddedToServer )}' on '{lastMovieAddedToServer!.DateCreated.LocalDateTime}'" );
                latestItem = getLatestItem( latestItem, lastMovieAddedToServer );
            }

            var lastEpisodeAddedToServer = EmbyHelperFuncs.GetLastEpisodeAdded( embyInterfaces );
            if( lastEpisodeAddedToServer != null && lastEpisodeAddedToServer!.DateCreated != DateTimeOffset.MinValue )
            {
                embyInterfaces?.Logger().Info( $"Last Episode added: '{MediaInfo.GetDisplayName( lastEpisodeAddedToServer )}' on '{lastEpisodeAddedToServer!.DateCreated.LocalDateTime}'" );
                latestItem = getLatestItem( latestItem, lastEpisodeAddedToServer );
            }

            var lastUserAddedToServer = EmbyHelperFuncs.GetLastAddedUser( embyInterfaces );
            if( lastUserAddedToServer != null && lastUserAddedToServer!.DateCreated != DateTimeOffset.MinValue )
            {
                embyInterfaces?.Logger().Info( $"Last User added: '{lastUserAddedToServer.Name}' on '{lastUserAddedToServer!.DateCreated.LocalDateTime}'" );
                latestItem = getLatestItem( latestItem, lastUserAddedToServer );
            }

            if( latestItem == null )
            {
                return (null, "<No Item Found>");
            }

            var name = string.Empty;
            var dateCreated = latestItem?.DateCreated;
            if( latestItem is Video )
            {

                name = MediaInfo.GetDisplayName( latestItem as Video );
                if( name.IsNullOrEmpty() )
                    name = latestItem!.Name;
            }
            else
                name = latestItem!.Name;

            var msg = $"Last Item Added: {latestItem.GetType().Name} - '{name}'";
            if( dateCreated != null )
                msg += $" on '{dateCreated?.LocalDateTime}'";
            embyInterfaces?.Logger().Info( msg );

            return (latestItem, msg);
        }

        public static (bool runFullAnalysis, bool runMissingAnalysis, List<string> reasons) AnalysisOutOfDate( EmbyInterfaces? embyInterfaces, Statistics2026DB db )
        {
            if( embyInterfaces == null )
                return (false, false, []);

            var logger = embyInterfaces.LogManager().GetLogger( "Statistics2026 - OOD Analysis" ) ?? null;
            embyInterfaces._logger = logger;

            logger?.Info( $"Starting OOD Analysis" );

            List<string> reasons = [];
            bool runFullAnalysis = false;
            var lastAnalysisAdded = db.GetLastUpdated( EAction.LastMediaOrUserAdded ).when;
            if( lastAnalysisAdded == null || lastAnalysisAdded.Value == DateTimeOffset.MinValue )
            {
                var msg = "Analysis has not been run yet";
                logger?.Info( msg );
                reasons.Add( msg );
                runFullAnalysis = true;
            }
            else
            {
                logger?.Info( $"Last Analysis run: {lastAnalysisAdded.Value.LocalDateTime}" );
            }

            if( !runFullAnalysis )
            {
                var (curr, inDB) = db.totalMediaCount();
                if( curr != inDB )
                {
                    var msg = $"Media count has changed since last run, re-running Complete Statistical Analysis";
                    logger?.Info( msg );
                    reasons.Add( msg );
                    runFullAnalysis = true;
                }
            }

            bool runMissingAnalysis = false;
            if( !runFullAnalysis )
            {
                var (latestItem, reason) = getLastItemOfConsequenceAdded( embyInterfaces );
                if( latestItem != null && latestItem.DateCreated != DateTimeOffset.MinValue )
                {
                    if( lastAnalysisAdded < latestItem.DateCreated )
                    {
                        var msg = $"Last analysys was run before the latest item was added, running Missing Analysis";
                        logger?.Info( msg );
                        reasons.Add( reason );
                        reasons.Add( msg );
                        runMissingAnalysis = true;
                    }
                }
            }

            if( runFullAnalysis )
            {
                return (runFullAnalysis, runFullAnalysis, reasons);
            }

            DateTimeOffset? lastMissingAnalysis = null;
            var lastMissingEpisodeAnalysis = db.GetLastUpdated( EAction.MissingEpisodesAnalysis ).when;
            if( lastMissingEpisodeAnalysis == null || lastMissingEpisodeAnalysis.Value == DateTimeOffset.MinValue )
            {
                runMissingAnalysis = true;
                var msg = "Missing Episode Analysis has not been run yet";
                logger?.Info(  msg );
                reasons.Add( msg );
            }
            else
            {
                lastMissingAnalysis = lastMissingEpisodeAnalysis;
                logger?.Info( $"Last Missing Episode Analysis run: {lastMissingEpisodeAnalysis.Value.LocalDateTime}" );
            }

            var lastMissingMovieAnalysis = db.GetLastUpdated( EAction.MissingMoviesAnalysis ).when;
            if( lastMissingMovieAnalysis == null || lastMissingMovieAnalysis.Value == DateTimeOffset.MinValue )
            {
                runMissingAnalysis = true;
                var msg = "Missing Movie Analysis has not been run yet";
                logger?.Info( msg );
                reasons.Add( msg );
            }
            else
            {
                logger?.Info( $"Last Missing Movie Analysis run: {lastMissingMovieAnalysis.Value.LocalDateTime}" );
                if( lastMissingAnalysis == null )
                    lastMissingAnalysis = lastMissingMovieAnalysis;
                else if( lastMissingMovieAnalysis > lastMissingAnalysis.Value )
                {
                    lastMissingAnalysis = lastMissingMovieAnalysis;
                }
            }

            if( !runMissingAnalysis )
            {
                string msg = string.Empty;
                if( lastMissingAnalysis == null || lastMissingAnalysis == DateTimeOffset.MinValue )
                {
                    msg = "Missing Analysis has not been run, running missing analysis.";
                }
                else if( lastMissingAnalysis.Value.UtcDateTime.Date != DateTimeOffset.UtcNow.Date )
                {
                    msg = "Missing Analysis has not been run today, running missing analysis.";
                }
                else
                {
                    logger?.Info( "Missing Analysis has already been run since new items added, not running missing analysis." );
                }
                if( !msg.IsEmpty() )
                {
                    logger?.Info( msg );
                    reasons.Add( msg );
                    runMissingAnalysis = true;
                }
            }

            return (runFullAnalysis, runMissingAnalysis, reasons);
        }

        public static List<User> GetAllUsers( EmbyInterfaces? embyInterfaces, bool limitToRemoteAccess = true )
        {
            if( embyInterfaces == null )
                return [];

            var userQuery = new UserQuery();
            if( limitToRemoteAccess )
            {
                userQuery.EnableRemoteAccess = true;
            }

            var retVal = embyInterfaces.UserManager().GetUserList( userQuery ).ToList();
#if DEBUG_DB
            retVal = retVal.Where( u => u.Name == "scott" ).ToList();
#endif
            return retVal;
        }
    }
}