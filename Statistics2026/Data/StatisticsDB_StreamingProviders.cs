using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Utilities;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        public async Task AddAllMediaProvidersTaskImplAsync()
        {
            CheckIsValid( ECheckLevel.eUpdate );

            Logger().Debug( $"Starting Video Analysis" );

            SetProgress( 0 );
            List<Series> seriesList = EmbyHelperFuncs.GetAllSeries( null, LibraryManager(), false );
            List<Video> videoList = EmbyHelperFuncs.GetAllEpisodesAndMovies( null, LibraryManager() );
            SetProgress( 100 );

            List<BaseItem> allItems = [];
            if( seriesList != null )
            {
                allItems.AddRange( seriesList );
            }
            if( videoList != null )
            {
                allItems.AddRange( videoList );
            }

            double count = allItems.Count;
            int completedCount = 0;

            TmdbStreamingProvider.ClearCache();

            SetProgress( 0 );
            var existing = new ConcurrentDictionary<string, bool>();
            var sqlCmds = new List<SQLCmdDef>();
            TmdbStreamingProvider justWatchProvider = new()
            {
                ProviderId = 0,
                ProviderName = "JustWatch Store",
                LogoPath = "/aaFIYn2NGPCo0rpLiWokbvNqNJd.png",
                DisplayPriority = -1,
                ProviderType = string.Empty
            };
            sqlCmds.AddRange( justWatchProvider.GetAddProviderCmds() );

            using( var timer = new AutoTimer( $"    Analyze Streaming Providers - Getting Commands", Logger() ) )
            {
                using SemaphoreSlim gatekeeper = HelperFuncs.CreateSemaphoreSlim( true );
                TmdbInterface reader = await TmdbInterface.CreateAsync( _embyInterfaces, CancellationToken );

                IEnumerable<Task<(int index, BaseItem item, List<SQLCmdDef?>? cmds)>> pool = allItems.Select( ( baseItem, index ) => Task.Run( async () =>
                {
                    await gatekeeper.WaitAsync();
                    try
                    {
                        (int index, BaseItem item, List<SQLCmdDef?>? cmds) retVal = (index, baseItem, new List<SQLCmdDef?>());
                        if( baseItem == null )
                            return retVal;

                        if( existing.ContainsKey( baseItem.Id.ToString() ) )
                            return retVal;

                        existing.TryAdd( baseItem.Id.ToString(), true );

                        string name = Utilities.MediaInfo.GetDescName( baseItem ).descName;
                        retVal.cmds.AddRange( await FindAllStreamingProviders( reader, baseItem ) );
                        CancellationToken?.ThrowIfCancellationRequested();
                        int currProgress = Interlocked.Increment( ref completedCount );

                        SetProgress( 80.0 * ( 1.0 * currProgress ) / count );

                        Logger().Debug( $"    Processed Video ({currProgress} of {count}) - {name}" );
                        return retVal;
                    }
                    finally
                    {
                        gatekeeper.Release();
                    }
                } ) );

                (int index, BaseItem item, List<SQLCmdDef?>? cmds)[] unsortedResults = await Task.WhenAll( pool );
                sqlCmds = unsortedResults
                            .Where( item => item.cmds != null )
                            .OrderBy( item => item.index )
                            .SelectMany( item => item.cmds! )
                            .Where( subItem => subItem != null )
                            .Select( subItem => subItem! )
                            .ToList();
            }

            using( var timer = new AutoTimer( $"    Analyze Streaming Providers - Executing Commands", Logger() ) )
            {
                SetProgress( 80 );
                await ExecuteCommands( sqlCmds );
                SetProgress( 100 );
                Logger().Debug( $"Finished Media Analysis" );
            }
        }

        private async Task<List<SQLCmdDef?>?> FindAllStreamingProviders( TmdbInterface reader, BaseItem baseItem )
        {
            List<SQLCmdDef?> retVal = [];

            string tmdbId = baseItem.GetProviderId( MetadataProviders.Tmdb );
            if( string.IsNullOrEmpty( tmdbId ) )
                return [];

            AddStreamingProvidersInfo? streamingProviders = null;
            try
            {
                streamingProviders = await reader.GetStreamingProvidersAsync( Logger(), baseItem ).ConfigureAwait( false );
                if( streamingProviders == null )
                    return [];

                Dictionary<string, HashSet<int>> allProviders = streamingProviders?.allProviders ?? [];
                retVal.AddRange( streamingProviders?.addProviderCmds ?? [] );

                string allProvidersJSON = JsonSerializer.Serialize( allProviders );
                string sql = string.Empty;
                if( baseItem is Movie )
                {
                    sql = "UPDATE Media SET StreamingProviders=@StreamingProviders WHERE ItemId=@ItemId";
                }
                else if( baseItem is Episode )
                {
                    sql = "UPDATE Media SET StreamingProviders=@StreamingProviders WHERE ItemId=@ItemId";
                }
                else if( baseItem is Series )
                {
                    sql = "UPDATE Series SET StreamingProviders=@StreamingProviders WHERE ItemId=@ItemId";
                }

                List<(string name, object? value)> parameters =
                    [
                        ( "@StreamingProviders", allProvidersJSON ),
                        ( "@ItemId", baseItem.Id.ToString()),
                    ];

                retVal.Add( new SQLCmdDef( sql, parameters ) );
            }
            catch( Exception ex )
            {
                Logger().Error( $"Problem connecting to TMDB - {baseItem.Name} - {tmdbId}" );
                Logger().Error( ex.ToString() );
            }

            if( streamingProviders == null )
            {
                Logger().Warn( $"Could not find Streaming Providers for {baseItem.Name} - {tmdbId}" );
                return [];
            }


            return retVal;
        }

        public async Task<List<StreamingProvider>> GetStreamingProviders( List<int> ids, bool forceIncludeJustWatch, bool locked )
        {
            CheckIsValid( ECheckLevel.eReport );

            if( forceIncludeJustWatch )
            {
                ids.Insert( 0, 0 );
            }

            ids = ids.Distinct().ToList();

            var cmd = SQLCmdDef.CreateSQLCmdDef( "SELECT ProviderId, Name, LogoPath, DisplayPriority, UrlTemplate FROM StreamingProviders WHERE ProviderId IN (<KEYLIST>) ORDER BY DisplayPriority ASC", "<KEYLIST>", ids.ConvertAll( x => (object)x ) );
            List<StreamingProvider> retVal = [];
            await ExecuteCommand( cmd, statement =>
            {
                IResultSet row = statement.Current;
                int curr = 0;
                int id = row.GetInt( curr++ );
                string name = row.GetString( curr++ );
                string logoPath = row.GetString( curr++ );
                int displayPriority = row.GetInt( curr++ );
                string urlTemplate = row.GetString( curr++ );

                var currProvider = new StreamingProvider()
                {
                    ProviderId = id,
                    ProviderName = name,
                    LogoPath = logoPath,
                    DisplayPriority = displayPriority,
                    ProviderType = string.Empty,
                    UrlTemplate = urlTemplate
                };
                retVal.Add( currProvider );
                return Task.FromResult( true );
            }, locked: locked );

            retVal = retVal.OrderBy( provider => provider.DisplayPriority ).ToList();
            return retVal;
        }
    }
}
