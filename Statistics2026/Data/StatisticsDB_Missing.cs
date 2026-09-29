using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using Statistics2026.Api;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        public void AnalyzeMissingMoviesTaskImpl( CancellationToken cancellationToken, IProgress<double> progress )
        {
            CheckIsValid( ECheckType.eUpdate );

            _embyInterfaces!._logger?.Debug( $"AnalyzeMissingMovies - Starting Analysis" );
            progress.Report( 0 );
            var collections = _dbHelper.GetLibraryItems<BoxSet>();
            progress.Report( 100 );

            double count = collections.Count();
            var curr = 0.0;

            progress.Report( 0 );
            var sqlCmds = new List<SQLCmdDef>();

            foreach( var collection in collections )
            {
                progress.Report( 80.0 * ( ++curr ) / count );
                sqlCmds.AddRange( AnalyzeMissingMovies( collection, cancellationToken, progress ) );
                cancellationToken.ThrowIfCancellationRequested();
                _embyInterfaces!._logger?.Debug( $"AnalyzeMissingMovies -     Processed Collection ({curr} of {count}) - {collection.Name} items processed" );
            }

            cancellationToken.ThrowIfCancellationRequested();

            progress.Report( 80 );
            _dbHelper.ExecuteCommands( sqlCmds );
            progress.Report( 100 );
            _embyInterfaces!._logger?.Debug( $"AnalyzeMissingMovies - Finished Analysis" );
        }

        private List< SQLCmdDef> AnalyzeMissingMovies( BoxSet collection, CancellationToken cancellationToken, IProgress<double> progress )
        {
            return [];
        }

        public void AnalyzeMissingEpisodesTaskImpl( CancellationToken cancellationToken, IProgress<double> progress )
        {
            CheckIsValid( ECheckType.eUpdate );

            _embyInterfaces!._logger?.Debug( $"AnalyzeMissingEpisodes - Starting Analysis" );
            progress.Report( 0 );
            var allSeries = _dbHelper.GetLibraryItems<Series>().Cast<Series>().ToList();
            progress.Report( 100 );

            double count = allSeries.Count();
            var curr = 0.0;

            progress.Report( 0 );
            var sqlCmds = new List<SQLCmdDef>();

            foreach( var series in allSeries )
            {
                progress.Report( 80.0 * ( ++curr ) / count );
                sqlCmds.AddRange( AnalyzeMissingEpisodes( series, cancellationToken, progress ) );
                cancellationToken.ThrowIfCancellationRequested();
                _embyInterfaces!._logger?.Debug( $"AnalyzeMissingMovies -     Processed Collection ({curr} of {count}) - {series.Name} items processed" );
            }

            cancellationToken.ThrowIfCancellationRequested();

            progress.Report( 80 );
            _dbHelper.ExecuteCommands( sqlCmds );
            progress.Report( 100 );
            _embyInterfaces!._logger?.Debug( $"AnalyzeMissingEpisodes - Finished Analysis" );
        }


        private List<SQLCmdDef> AnalyzeMissingEpisodes( Series series, CancellationToken cancellationToken, IProgress<double> progress )
        {
            return [];
        }
    }
}
