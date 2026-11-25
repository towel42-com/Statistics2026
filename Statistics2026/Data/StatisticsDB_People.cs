using MediaBrowser.Controller.Entities;
using ServiceStack;
using Statistics2026.Configuration;
using Statistics2026.Utilities;
using System.Collections.Concurrent;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        public async Task AddAllPeopleTaskImplAsync()
        {
            CheckIsValid( ECheckLevel.eUpdate );

            Logger().Debug( $"Starting People Analysis" );

            SetProgress( 0 );
            List<Video> videoList = EmbyHelperFuncs.GetAllEpisodesAndMovies( null, LibraryManager() );
            SetProgress( 100 );

            if( videoList == null )
            {
                return;
            }

            SetProgress( 0 );

            ConcurrentDictionary<Person, bool> allActors = [];
            HashSet<string> allActorIds = [];
            foreach( Video video in videoList )
            {
                Person[] actors = EmbyHelperFuncs.GetActorsForVideo( video, LibraryManager() );
                foreach( Person actor in actors )
                {
                    allActors.TryAdd( actor, true );
                    allActorIds.Add( actor.Id.ToString() );
                }
            }

            await DeleteAllInvalidAsync( allActorIds.ToList(), "People", "ItemId" ).ConfigureAwait( false );

            double count = allActors.Count;
            int completedCount = 0;

            List<SQLCmdDef> sqlCmds = [];
            using( var timer = new AutoTimer( $"    Analyze People - Getting Commands", Logger() ) )
            {
                using SemaphoreSlim gatekeeper = HelperFuncs.CreateSemaphoreSlim();

                IEnumerable<Task<(int index, Person item, List<SQLCmdDef?> cmds)>> pool = allActors.Select( ( kvp, index ) => Task.Run( async () =>
                {
                    await gatekeeper.WaitAsync().ConfigureAwait( false );
                    try
                    {
                        Person actor = kvp.Key;
                        (int index, Person item, List<SQLCmdDef?> cmds) retVal = (index: index, item: actor, cmds: new List<SQLCmdDef?>());
                        if( actor == null )
                            return retVal;

                        retVal.cmds.Add( AddPerson( actor ) );

                        CancellationToken?.ThrowIfCancellationRequested();

                        int currProgress = Interlocked.Increment( ref completedCount );
                        SetProgress( 80.0 * ( 1.0 * currProgress ) / count );
                        Logger().Debug( $"    Processed Person ({currProgress} of {count}) - {actor.Name}" );

                        return retVal;
                    }
                    finally
                    {
                        gatekeeper.Release();
                    }
                } ) );

                (int index, Person item, List<SQLCmdDef?> cmds)[] unsortedResults = await Task.WhenAll( pool ).ConfigureAwait( false );
                sqlCmds = unsortedResults
                            .Where( item => item.cmds != null )
                            .OrderBy( item => item.index )
                            .SelectMany( item => item.cmds! )
                            .Where( cmd => cmd != null )
                            .Select( cmd => cmd! )
                            .ToList();
            }

            using( var timer = new AutoTimer( $"    Analyze Media - Executing Commands", Logger() ) )
            {
                SetProgress( 80 );
                await ExecuteCommandsAsync( sqlCmds ).ConfigureAwait( false );
                SetProgress( 100 );
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

            string sql =
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

        private Task<Dictionary<string, (int total, Dictionary<string, int> parentToChildMapping)>> ActorCountValuesAsync( User? user, bool movies, bool playedOnly )
        {
            return GroupedMediaKeyCountValuesAsync( "ActorIds", true, user, movies, playedOnly );
        }

        public async Task<StatCard> ActorsListAsync( bool movies, bool? series )
        {
            CheckIsValid( ECheckLevel.eReport );

            Dictionary<string, (int total, Dictionary<string, int> parentToChildMapping)> actorsMap = await ActorCountValuesAsync( null, movies, false ).ConfigureAwait( false );

            string title = movies ? Constants.MovieActors : ( ( series == null || series! == true ) ? Constants.TVSeriesActors : Constants.TVEpisodeActors );
            string help = movies ? Constants.MovieActorsHelp : ( ( series == null || series! == true ) ? Constants.TVSeriesActorsHelp : Constants.TVEpisodeActorsHelp );

            var retVal = new GroupedTextBasedStatCard( title, help, EStatCardStyle.eDetailed )
            {
                SubTitle = $"Total Number of Actors: {actorsMap.Count()}",
                ListType = EListType.eNumbered
            };

            var sortedActors = actorsMap.OrderByDescending( kvp =>
                {
                    if( !movies && ( series == null || series! == true ) )
                        return kvp.Value.parentToChildMapping.Count();
                    return kvp.Value.total;
                }
            ).ToList();
            for( int ii = 0; ( ii < sortedActors.Count() ) && ( ii < Plugin.Instance!.Configuration.NumActorsToReport( movies ? EReportType.eMovieReport : EReportType.eTVReport ) ); ++ii )
            {
                KeyValuePair<string, (int total, Dictionary<string, int> parentToChildMapping)> curr = sortedActors[ ii ];
                try
                {
                    BaseItem person = LibraryManager().GetItemById( new Guid( curr.Key ) );
                    if( person == null )
                        continue;

                    string text = string.Empty;
                    if( movies )
                        text = $"{person.Name}: {curr.Value.total} Movie{( ( curr.Value.total == 1 ) ? "" : "s" )}";
                    else if( series == null || series! == true )
                        text = $"{person.Name}: {curr.Value.parentToChildMapping.Count()} Series";
                    else
                        text = $"{person.Name}: {curr.Value.total} Episode{( ( curr.Value.total == 1 ) ? "" : "s" )}";

                    text = GetItemImageUrl.ItemHtmlFromItem( person, text: text, Plugin.Instance!.Configuration.ShowImages( movies ? EReportType.eMovieReport : EReportType.eTVReport ) );
                    retVal.AddLine( person.Name, text, false );
                }
                catch( Exception )
                {
                }
            }


            return retVal;
        }
    }
}
