using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using static Statistics2026.Data.StatisticsDB;

namespace Statistics2026.Utilities
{
    public static class EmbyHelperFuncs
    {
        public static List<T> GetLibraryItems<T>( User? user, ILibraryManager? libManager, bool getImages = true )
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


            return libManager.GetItemList( query ).OfType<T>().Cast<T>().ToList();
        }

        public static List<Video>? GetAllEpisodesAndMovies( User? user, ILibraryManager? libManager )
        {
            if( libManager == null )
                return null;

            List<Video> retVal = [];

            retVal.Concat( GetLibraryItems<Episode>( user, libManager ).Cast<Video>() );
            retVal.Concat( GetLibraryItems<Movie>( user, libManager ).Cast<Video>() );

            return retVal;
        }

        public static (List<Video>? forUser, List<Video>? forAll) GetAllEpisodesAndMovies( User? user, ILibraryManager? libManager, bool computeForNonUsers )
        {
            if( libManager == null )
                return (null, null);

            List<Video>? forUser = ( user != null ) ? GetAllEpisodesAndMovies( user, libManager ) : null;
            List<Video>? all = computeForNonUsers ? GetAllEpisodesAndMovies( null, libManager ) : null;

            return (forUser, all);
        }

        public static List<BoxSet> GetAllBoxSets( User user, ILibraryManager libManager )
        {
            var boxSets = GetLibraryItems<BoxSet>( user, libManager );
            return boxSets;
        }

        public static Season? GetSeasonFromSeries( Series series, int seasonNum )
        {
            var result = series.GetItems( new InternalItemsQuery
            {
                IncludeItemTypes = new[] { nameof( Season ) },
                Recursive = true
            } );

            var seasons = result.Items.Cast<Season>().ToList();
            foreach( var season in seasons )
            {
                if( season.IndexNumber == seasonNum )
                    return season;
            }

            return null;
        }

        public static Movie? GetMovieByTmdbId( ILibraryManager? libraryManager, string tmdbId )
        {
            if( libraryManager == null )
                return null;

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

        public static Episode? GetEpisodeFromTmdbId( ILibraryManager? libraryManager, Season embySeason, string tmdbId, int episodeNum )
        {
            if( ( libraryManager == null ) || ( embySeason == null ) )
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
                .Cast<Episode>()
                .FirstOrDefault( ep =>
                ep.IndexNumber == episodeNum ||
                ( ep.IndexNumberEnd != null && ( ep.IndexNumber <= episodeNum ) && ( ep.IndexNumberEnd >= episodeNum ) ) )
                ;

            return retVal;
        }

        public static BaseItem? GetLastMovieOrEpisodeAdded( EmbyInterfaces? embyInterfaces, EWhichMediaList whichMedia = EWhichMediaList.eMovies | EWhichMediaList.eEpisodes )
        {
            if( embyInterfaces == null || embyInterfaces._libraryManager == null )
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

                var items = embyInterfaces._libraryManager.GetItemList( query );
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
            if( embyInterfaces == null || embyInterfaces._userManager == null )
                return null;

            var query = new UserQuery
            {
                // You can optionally add filters here, such as:
                // IsDisabled = false,
                // IsHidden = false
            };

            var queryResult = embyInterfaces._userManager.GetUserList( query ) ?? null;
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


        public static void UpdateLastAdded( EmbyInterfaces? embyInterfaces, StatisticsDB db )
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
                db.UpdateLastUpdated( StatisticsDB.EAction.LastMediaOrUserAdded, 0, lastAdded.Value );
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

        public static BaseItem? getLastItemOfConsequenceAdded( EmbyInterfaces? embyInterfaces )
        {
            BaseItem? latestItem = null;

            // all items analyzed are tied to new episodes, movies or users

            var lastMovieAddedToServer = EmbyHelperFuncs.GetLastMovieAdded( embyInterfaces );
            if( lastMovieAddedToServer != null && lastMovieAddedToServer!.DateCreated != DateTimeOffset.MinValue )
            {
                embyInterfaces?._logger?.Info( $"Last Movie added: {MediaInfo.GetDisplayName( lastMovieAddedToServer )} on {lastMovieAddedToServer!.DateCreated}" );
                latestItem = getLatestItem( latestItem, lastMovieAddedToServer );
            }

            var lastEpisodeAddedToServer = EmbyHelperFuncs.GetLastEpisodeAdded( embyInterfaces );
            if( lastEpisodeAddedToServer != null && lastEpisodeAddedToServer!.DateCreated != DateTimeOffset.MinValue )
            {
                embyInterfaces?._logger?.Info( $"Last Episode added: {MediaInfo.GetDisplayName( lastEpisodeAddedToServer )} on {lastEpisodeAddedToServer!.DateCreated}" );
                latestItem = getLatestItem( latestItem, lastEpisodeAddedToServer );
            }

            var lastUserAddedToServer = EmbyHelperFuncs.GetLastAddedUser( embyInterfaces );
            if( lastUserAddedToServer != null && lastUserAddedToServer!.DateCreated != DateTimeOffset.MinValue )
            {
                embyInterfaces?._logger?.Info( $"Last User added: {lastUserAddedToServer.Name} on {lastUserAddedToServer!.DateCreated}" );
                latestItem = getLatestItem( latestItem, lastUserAddedToServer );
            }

            return latestItem;
        }

        public static (bool runFullAnalysis, bool runMissingAnalysis) AnalysisOutOfDate( EmbyInterfaces? embyInterfaces, StatisticsDB db )
        {
            if( embyInterfaces == null || embyInterfaces._logger == null )
                return (false, false);

            bool runFullAnalysis = false;
            var lastAnalysisAdded = db.GetLastUpdated( EAction.LastMediaOrUserAdded );
            if( lastAnalysisAdded == null || lastAnalysisAdded.Value == DateTimeOffset.MinValue )
            {
                embyInterfaces._logger?.Info( "Analysis has not been run yet" );
                runFullAnalysis = true;
            }
            else
            {
                embyInterfaces._logger?.Info( $"Last Analysis run: {lastAnalysisAdded}" );
            }

            if( !runFullAnalysis )
            {
                var latestItem = getLastItemOfConsequenceAdded( embyInterfaces );
                if( latestItem != null && latestItem.DateCreated != DateTimeOffset.MinValue )
                    runFullAnalysis = lastAnalysisAdded < latestItem.DateCreated;
            }

            if( !runFullAnalysis )
            {
                embyInterfaces._logger?.Info( "No media or users have been added since last run, not re-running Complete Statistical Analysis" );
            }
            else
            {
                return (runFullAnalysis, runFullAnalysis);
            }

            bool runMissingAnalysis = false;

            DateTimeOffset? lastMissingAnalysis = null;
            var lastMissingEpisodeAnalysis = db.GetLastUpdated( EAction.MissingEpisodesAnalysis );
            if( lastMissingEpisodeAnalysis == null || lastMissingEpisodeAnalysis.Value == DateTimeOffset.MinValue )
            {
                runMissingAnalysis = true;
                embyInterfaces._logger?.Info( "Missing Episode Analysis has not been run yet" );
            }
            else
            {
                lastMissingAnalysis = lastMissingEpisodeAnalysis;
                embyInterfaces._logger?.Info( $"Last Missing Episode Analysis run: {lastMissingEpisodeAnalysis}" );
            }

            var lastMissingMovieAnalysis = db.GetLastUpdated( EAction.MissingMoviesAnalysis );
            if( lastMissingMovieAnalysis == null || lastMissingMovieAnalysis.Value == DateTimeOffset.MinValue )
            {
                runMissingAnalysis = true;
                embyInterfaces._logger?.Info( "Missing Movie Analysis has not been run yet" );
            }
            else
            {
                embyInterfaces._logger?.Info( $"Last Missing Movie Analysis run: {lastMissingMovieAnalysis}" );
                if( lastMissingAnalysis == null )
                    lastMissingAnalysis = lastMissingMovieAnalysis;
                else if( lastMissingMovieAnalysis < lastMissingAnalysis.Value )
                {
                    lastMissingAnalysis = lastMissingMovieAnalysis;
                }
            }

            if( !runMissingAnalysis )
            {
                if( lastMissingAnalysis == null || lastMissingAnalysis == DateTimeOffset.MinValue )
                {
                    runMissingAnalysis = true;
                }
                else if( lastMissingAnalysis.Value.UtcDateTime.Date != DateTimeOffset.UtcNow.Date )
                {
                    embyInterfaces._logger?.Info( "Missing Analysis has not been run today." );
                    runMissingAnalysis = true;
                }
            }
            else
            {
                runMissingAnalysis = true;
            }

            return (runFullAnalysis, runMissingAnalysis);
        }

    }
}
