using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Api;
using Statistics2026.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        public async Task AddAllMediaProvidersTaskImplAsync()
        {
            CheckIsValid( ECheckLevel.eUpdate );

            Logger().Debug( $"Starting Video Analysis" );

            Progress?.Report( 0 );
            var videoList = EmbyHelperFuncs.GetAllEpisodesAndMovies( null, LibraryManager() );
            Progress?.Report( 100 );

            if( videoList == null )
            {
                return;
            }

            double count = videoList.Count;
            var completedCount = 0;

            TmdbStreamingProvider.ClearCache();

            Progress?.Report( 0 );
            var existing = new ConcurrentDictionary<string, bool>();
            var sqlCmds = new List<SQLCmdDef>();

            using( var timer = new AutoTimer( $"    Analyze Streaming Providers - Getting Commands", Logger() ) )
            {
                using var gatekeeper = HelperFuncs.CreateSemaphoreSlim();
                var reader = new TmdbInterface( _embyInterfaces, CancellationToken );

                var pool = videoList.Select( ( video, index ) => Task.Run( async () =>
                {
                    await gatekeeper.WaitAsync();
                    try
                    {
                        var retVal = (index: index, video: video, cmds: (List<SQLCmdDef>?)null);
                        if( video == null )
                            return retVal;

                        if( existing.ContainsKey( video.Id.ToString() ) )
                            return retVal;

                        existing.TryAdd( video.Id.ToString(), true );

                        var name = Utilities.MediaInfo.GetDescName( video ).descName;
                        retVal.cmds = await FindAllMediaProviders( reader, video );
                        CancellationToken?.ThrowIfCancellationRequested();
                        int currProgress = Interlocked.Increment( ref completedCount );

                        Progress?.Report( 80.0 * ( 1.0 * currProgress ) / count );

                        Logger().Debug( $"    Processed Video ({currProgress} of {count}) - {name}" );
                        return retVal;
                    }
                    finally
                    {
                        gatekeeper.Release();
                    }
                } ) );

                var unsortedResults = await Task.WhenAll( pool );
                var results = unsortedResults.OrderBy( item => item.index ).ToList();
                foreach( var result in results )
                {
                    if( result.cmds != null )
                    {
                        sqlCmds.AddRange( result.cmds );
                    }
                }
            }

            using( var timer = new AutoTimer( $"    Analyze Streaming Providers - Executing Commands", Logger() ) )
            {
                Progress?.Report( 80 );
                ExecuteCommands( sqlCmds );
                Progress?.Report( 100 );
                Logger().Debug( $"Finished Media Analysis" );
            }
        }

        private async Task<List<SQLCmdDef>> FindAllMediaProviders( TmdbInterface reader, Video video )
        {
            List<SQLCmdDef> retVal = [];

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

        private List<StreamingProvider>? GetStreamingProviders( List<int> ids )
        {
            CheckIsValid( ECheckLevel.eReport );

            var cmd = SQLCmdDef.CreateSQLCmdDef( "SELECT ProviderId, Name, LogoPath, DisplayPriority FROM StreamingProviders WHERE ProviderId IN (<KEYLIST>) ORDER BY DisplayPriority ASC", "<KEYLIST>", ids.ConvertAll( x => (object)x ) );
            List<StreamingProvider>? retVal = null;
            ExecuteCommand( cmd, statement =>
            {
                var row = statement.Current;
                int curr = 0;
                var id = row.GetInt( curr++ );
                var name = row.GetString( curr++ );
                var logoPath = row.GetString( curr++ );
                var displayPriority = row.GetInt( curr++ );

                var currProvider = new StreamingProvider()
                {
                    ProviderId = id,
                    ProviderName = name,
                    LogoPath = logoPath,
                    DisplayPriority = displayPriority,
                    ProviderType = string.Empty
                };
                if( retVal == null )
                    retVal = [];
                retVal.Add( currProvider );
                return true;
            } );

            return retVal;
        }
    }
}
