using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using ServiceStack;
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
            var videoList = EmbyHelperFuncs.GetAllEpisodesAndMovies( null, LibraryManager() );
            SetProgress( 100 );

            if( videoList == null )
            {
                return;
            }

            double count = videoList.Count;
            var completedCount = 0;

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
                using var gatekeeper = HelperFuncs.CreateSemaphoreSlim();
                var reader = new TmdbInterface( _embyInterfaces, CancellationToken );

                var pool = videoList.Select( ( video, index ) => Task.Run( async () =>
                {
                    await gatekeeper.WaitAsync();
                    try
                    {
                        (int index, Video item, List<SQLCmdDef?>? cmds) retVal = (index, video, new List<SQLCmdDef?>());
                        if( video == null )
                            return retVal;

                        if( existing.ContainsKey( video.Id.ToString() ) )
                            return retVal;

                        existing.TryAdd( video.Id.ToString(), true );

                        var name = Utilities.MediaInfo.GetDescName( video ).descName;
                        retVal.cmds.AddRange( await FindAllMediaProviders( reader, video ) );
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

                var unsortedResults = await Task.WhenAll( pool );
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
                ExecuteCommands( sqlCmds );
                SetProgress( 100 );
                Logger().Debug( $"Finished Media Analysis" );
            }
        }

        private async Task<List<SQLCmdDef?>?> FindAllMediaProviders( TmdbInterface reader, Video video )
        {
            List<SQLCmdDef?> retVal = [];

            var tmdbId = video.GetProviderId( MetadataProviders.Tmdb );
            if( string.IsNullOrEmpty( tmdbId ) )
                return [];

            AddStreamingProvidersInfo? streamingProviders = null;
            try
            {
                streamingProviders = await reader.GetStreamingProvidersAsync( Logger(), video ).ConfigureAwait( false );
                if( streamingProviders == null )
                    return [];

                var allProviders = streamingProviders?.allProviders ?? [];
                retVal.AddRange( streamingProviders?.addProviderCmds ?? [] );

                if( video is Movie )
                {
                    var sql = "UPDATE Media SET StreamingProviders=@StreamingProviders WHERE ItemId=@ItemId";
                    List<(string name, object? value)> parameters =
                        [
                            ( "@StreamingProviders", JsonSerializer.Serialize( allProviders )),
                            ( "@ItemId", video.Id.ToString()),
                        ];

                    retVal.Add( new SQLCmdDef( sql, parameters ) );
                }
                else if( video is Episode )
                {
                    var sql = "UPDATE Media SET StreamingProviders=@StreamingProviders WHERE ItemId=@ItemId";
                    List<(string name, object? value)> parameters =
                        [
                            ( "@StreamingProviders", JsonSerializer.Serialize( allProviders )),
                            ( "@ItemId", video.Id.ToString()),
                        ];

                    retVal.Add( new SQLCmdDef( sql, parameters ) );

                    var series = ( video as Episode )!.Series;
                    if( series != null )
                    {
                        sql = "UPDATE Series SET StreamingProviders=@StreamingProviders WHERE ItemId=@ItemId";
                        parameters =
                            [
                                ( "@StreamingProviders", JsonSerializer.Serialize( allProviders )),
                               ( "@ItemId", series.Id.ToString()),
                            ];

                        retVal.Add( new SQLCmdDef( sql, parameters ) );
                    }
                }
            }
            catch( Exception ex )
            {
                Logger().Error( $"Problem connecting to TMDB - {video.Name} - {tmdbId}" );
                Logger().Error( ex.ToString() );
            }

            if( streamingProviders == null )
            {
                Logger().Warn( $"Could not find Streaming Providers for {video.Name} - {tmdbId}" );
                return [];
            }


            return retVal;
        }

        public List<StreamingProvider> GetStreamingProviders( List<int> ids, bool forceIncludeJustWatch )
        {
            CheckIsValid( ECheckLevel.eReport );

            if( forceIncludeJustWatch )
            {
                ids.Insert( 0, 0 );
            }

            ids = ids.Distinct().ToList();

            var cmd = SQLCmdDef.CreateSQLCmdDef( "SELECT ProviderId, Name, LogoPath, DisplayPriority, UrlTemplate FROM StreamingProviders WHERE ProviderId IN (<KEYLIST>) ORDER BY DisplayPriority ASC", "<KEYLIST>", ids.ConvertAll( x => (object)x ) );
            List<StreamingProvider> retVal = [];
            ExecuteCommand( cmd, statement =>
            {
                var row = statement.Current;
                int curr = 0;
                var id = row.GetInt( curr++ );
                var name = row.GetString( curr++ );
                var logoPath = row.GetString( curr++ );
                var displayPriority = row.GetInt( curr++ );
                var urlTemplate = row.GetString( curr++ );

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
                return true;
            } );

            retVal = retVal.OrderBy( provider => provider.DisplayPriority ).ToList();
            return retVal;
        }
    }
}
