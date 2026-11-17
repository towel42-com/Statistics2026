using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;
using RestSharp;
using ServiceStack;
using Statistics2026.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        public async Task AddAllPeopleTaskImplAsync()
        {
            CheckIsValid( ECheckLevel.eUpdate );

            Logger().Debug( $"Starting People Analysis" );

            Progress?.Report( 0 );
            var videoList = EmbyHelperFuncs.GetAllEpisodesAndMovies( null, LibraryManager() );
            Progress?.Report( 100 );

            if( videoList == null )
            {
                return;
            }

            double count = videoList.Count;
            var completedCount = 0;

            Progress?.Report( 0 );
            var existing = new ConcurrentDictionary<string, bool>();
            ConcurrentBag<object> validIds = [];
            var sqlCmds = new List<SQLCmdDef>();

            using( var timer = new AutoTimer( $"    Analyze People - Getting Commands", Logger() ) )
            {
                using var gatekeeper = HelperFuncs.CreateSemaphoreSlim( true );

                var pool = videoList.Select( ( video, index ) => Task.Run( async () =>
                {
                    await gatekeeper.WaitAsync();
                    try
                    {
                        var retVal = (index: index, video: video, cmds: (List<SQLCmdDef>?)null);
                        if( video == null )
                            return retVal;

                        var videoName = MediaInfo.GetDescName( video ).descName;

                        var actors = EmbyHelperFuncs.GetActorsForVideo( video, LibraryManager() );

                        retVal.cmds = [];
                        foreach( var actor in actors )
                        {
                            if( actor == null )
                                continue;

                            if( existing.ContainsKey( actor.Id.ToString() ) )
                                continue;

                            existing.TryAdd( actor.Id.ToString(), true );
                            validIds.Add( actor.Id.ToString() );

                            var cmd = AddPerson( actor );
                            if( cmd != null )
                                retVal.cmds.Add( cmd );

                            CancellationToken?.ThrowIfCancellationRequested();
                        }
                        int currProgress = Interlocked.Increment( ref completedCount );
                        Progress?.Report( 80.0 * ( 1.0 * currProgress ) / count );
                        Logger().Debug( $"    Processed Video ({currProgress} of {count}) - {videoName}" );

                        return retVal;
                    }
                    finally
                    {
                        gatekeeper.Release();
                    }
                } ) );

                var unsortedResults = await Task.WhenAll( pool );
                var results = unsortedResults.OrderBy( item => item.index ).ToList();
                sqlCmds.Add( SQLCmdDef.CreateSQLCmdDef( "DELETE FROM People WHERE ItemId NOT IN (<KEYLIST>)", "<KEYLIST>", validIds.ToList() ) );
                foreach( var result in results )
                {
                    if( result.cmds != null )
                    {
                        sqlCmds.AddRange( result.cmds );
                    }
                }
            }

            using( var timer = new AutoTimer( $"    Analyze Media - Executing Commands", Logger() ) )
            {
                Progress?.Report( 80 );
                ExecuteCommands( sqlCmds );
                Progress?.Report( 100 );
                Logger().Debug( $"Finished Media Analysis" );
            }
        }

        public SQLCmdDef? AddPerson( Person person )
        {
            CheckIsValid( ECheckLevel.eUpdate );

            if( person == null )
            {
                return null;
            }

            var sql =
                "INSERT INTO People \n" +
                "(\n" +
                    "  ItemId\n" +
                    ", Name\n" +
                ")\n" +
                " VALUES \n" +
                "(\n" +
                    "  @ItemId\n" +
                    ", @Name\n" +
               ")\n" +
                " ON CONFLICT(ItemId) \n" +
                " DO UPDATE \n" +
                " SET \n" +
                    "  Name=@Name\n"
                    ;

            var retVal = new SQLCmdDef( sql,
            [
                ("@ItemId", person.Id.ToString()),
                ("@Name", person.Name)
            ] );

            return retVal;
        }

        private Dictionary<string, (int total, Dictionary<string, int> parentToChildMapping)> ActorCountValues( User? user, bool movies, bool playedOnly )
        {
            return GroupedMediaKeyCountValues( "ActorIds", true, user, movies, playedOnly );
        }

        public StatCard ActorsList( bool movies )
        {
            CheckIsValid( ECheckLevel.eReport );

            var genreMap = ActorCountValues( null, movies, false );

            var title = movies ? Constants.MovieActors : Constants.TVActors;
            var help = movies ? Constants.MovieActorsHelp : Constants.TVActorsHelp;

            var retVal = new GroupedTextBasedStatCard( title, help, EStatCardStyle.eDetailed )
            {
                SubTitle = $"Total Number of Actors: {genreMap.Count()}",
                ListType = EListType.eNumbered
            };

            var sortedGenre = genreMap.OrderByDescending( kvp => kvp.Value.total ).ToList();
            for( int ii = 0; ( ii < sortedGenre.Count() ) && ( ii < Plugin.Instance!.Configuration.numActorsToReport ); ++ii )
            {
                var curr = sortedGenre[ ii ];
                try
                {
                    var person = LibraryManager().GetItemById( new Guid( curr.Key ) );
                    if( person == null )
                        continue;
                    if( movies )
                        retVal.AddLine( person.Name, $"{person.Name}: {curr.Value.total} Movie{( ( curr.Value.total == 1 ) ? "" : "s" )}", false );
                    else
                        retVal.AddLine( person.Name, $"{person.Name}: {curr.Value.parentToChildMapping.Count()} Series and {curr.Value.total} Episode{( ( curr.Value.total == 1 ) ? "" : "s" )}", false );
                }
                catch( Exception )
                {
                }
            }


            return retVal;
        }
    }
}
