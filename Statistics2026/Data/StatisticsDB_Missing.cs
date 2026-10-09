using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using ServiceStack;
using Statistics2026.Api;
using Statistics2026.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Statistics2026.Data
{
    public sealed partial class StatisticsDB
    {
        private const string kSQLAddToMissing =
            "INSERT INTO Missing" +
            "(" +
                "  Key" +
                ", TmdbId" +
                ", Title" +
                ", OriginalTitle" +
                ", ReleaseDate" +
                ", Overview" +
                ", PosterPath" +
                ", ParentId" +
                ", IsEpisode" +
                ", SeasonNum" +
                ", EpisodeNum" +
            ")" +
            " VALUES " +
            "(" +
                "  @Key" +
                ", @TmdbId" +
                ", @Title" +
                ", @OriginalTitle" +
                ", @ReleaseDate" +
                ", @Overview" +
                ", @PosterPath" +
                ", @ParentId" +
                ", @IsEpisode" +
                ", @SeasonNum" +
                ", @EpisodeNum" +
            ")" +
            " ON CONFLICT(Key) " +
            " DO UPDATE " +
            " SET " +
                "  TmdbId=@TmdbId" +
                ", Title=@Title" +
                ", OriginalTitle=@OriginalTitle" +
                ", ReleaseDate=@ReleaseDate" +
                ", Overview=@Overview" +
                ", PosterPath=@PosterPath" +
                ", ParentId=@ParentId" +
                ", IsEpisode=@IsEpisode" +
                ", SeasonNum=@SeasonNum" +
                ", EpisodeNum=@EpisodeNum"
                ;

        public async Task AnalyzeMissingMoviesTaskImpl( CancellationToken cancellationToken, IProgress<double> progress )
        {
            CheckIsValid( ECheckType.eUpdate );

            _embyInterfaces!._logger?.Debug( $"Starting Analysis" );
            progress.Report( 0 );
            var collections = _dbHelper.GetLibraryItems<BoxSet>().ToList();
            collections.Sort( ( a, b ) => StringComparer.OrdinalIgnoreCase.Compare( a.SortName, b.SortName ) );

            progress.Report( 100 );

            var reader = new TmdbCollectionReader( _embyInterfaces, cancellationToken );

            double count = collections.Count();
            var curr = 0.0;

            progress.Report( 0 );
            var sqlCmds = new List<SQLCmdDef>
            {
                new( "DELETE FROM Missing WHERE NOT IsEpisode" )
            };

            foreach( var collection in collections )
            {
                progress.Report( 80.0 * ( ++curr ) / count );
                var cmds = await AnalyzeMissingMoviesInCollection( reader, collection, cancellationToken, progress ).ConfigureAwait( false );
                sqlCmds.AddRange( cmds );
                cancellationToken.ThrowIfCancellationRequested();
                _embyInterfaces!._logger?.Debug( $"    Processed Collection ({curr} of {count}) - {collection.Name} items processed" );
            }

            cancellationToken.ThrowIfCancellationRequested();

            progress.Report( 80 );
            _dbHelper.ExecuteCommands( sqlCmds );
            progress.Report( 100 );
            _embyInterfaces!._logger?.Debug( $"Finished Analysis" );
        }

        private bool InvalidDate( DateTime? dt, bool ignoreConfig = false )
        {
            if( dt == null )
                return true;

            var numDaysToAdd = ignoreConfig ? 0 : Plugin.Instance!.Configuration.numDaysFutureForMissing;
            var maxDate = DateTime.Today.Date.AddDays( numDaysToAdd );
            if( dt.Value.Date > maxDate )
                return true;

            if( dt == DateTime.MinValue )
                return true;

            return false;
        }

        private async Task<List<SQLCmdDef>> AnalyzeMissingMoviesInCollection( TmdbCollectionReader reader, BoxSet collection, CancellationToken cancellationToken, IProgress<double> progress )
        {
            _embyInterfaces!._logger?.Info( $"    Analyzing Collection '{collection.Name}' - checking for missing movies" );

            var collectionTmbdId = collection.GetProviderId( MetadataProviders.Tmdb );
            if( string.IsNullOrEmpty( collectionTmbdId ) )
                return [];

            var tmdbCollection = await reader.GetRemoteCollectionMembersAsyncViaCustom( collectionTmbdId, cancellationToken ).ConfigureAwait( false );
            if( tmdbCollection == null )
            {
                _embyInterfaces._logger!.Warn( $"Could not find TMDB collection for {collection.Name} - {collectionTmbdId}" );
                return [];
            }

            List<SQLCmdDef> retVal = [];
            foreach( var tmdbMovie in tmdbCollection.Movies )
            {
                if( InvalidDate( tmdbMovie.ReleaseDate() ) )
                    continue;

                var movieTmdbId = tmdbMovie.Id;
                _embyInterfaces!._logger?.Debug( $"        Checking for movie {tmdbMovie.Title} for {collection.Name} on server" );
                var embyMovie = _dbHelper.GetMovieByTmdbId( _embyInterfaces._libraryManager, movieTmdbId );

                if( embyMovie == null )
                {
                    _embyInterfaces!._logger?.Info( $"            {tmdbMovie.Title} is missing" );
                    var key = $"{tmdbCollection.Id}-{tmdbMovie.Id}";
                    retVal.Add( new SQLCmdDef( kSQLAddToMissing,
                    [
                        ("@Key", key ),
                        ("@TmdbId", tmdbMovie.Id ),
                        ("@Title", tmdbMovie.Title ),
                        ("@OriginalTitle", tmdbMovie.OriginalTitle ),
                        ("@ReleaseDate", _dbHelper.ToDateTimeParamValue( tmdbMovie.ReleaseDate() ) ),
                        ("@Overview", tmdbMovie.Overview ),
                        ("@PosterPath", tmdbMovie.PosterPath ),
                        ("@ParentId", collection.Id.ToString() ),
                        ("@IsEpisode", false )
                    ] ) );
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            return retVal;
        }

        public async Task AnalyzeMissingEpisodesTaskImpl( CancellationToken cancellationToken, IProgress<double> progress )
        {
            CheckIsValid( ECheckType.eUpdate );

            _embyInterfaces!._logger?.Debug( $"Starting Analysis" );
            progress.Report( 0 );
            var allSeries = _dbHelper.GetLibraryItems<Series>().Cast<Series>().ToList();
            allSeries.Sort( ( a, b ) => StringComparer.OrdinalIgnoreCase.Compare( a.SortName, b.SortName ) );
            progress.Report( 100 );

            var reader = new TmdbCollectionReader( _embyInterfaces, cancellationToken );

            double count = allSeries.Count();
            var curr = 0.0;

            progress.Report( 0 );
            var sqlCmds = new List<SQLCmdDef>
            {
                new( "DELETE FROM Missing WHERE IsEpisode" )
            };

            foreach( var series in allSeries )
            {
                progress.Report( 80.0 * ( ++curr ) / count );
                var cmds = await AnalyzeMissingEpisodes( reader, series, cancellationToken, progress );
                sqlCmds.AddRange( cmds );
                cancellationToken.ThrowIfCancellationRequested();
                _embyInterfaces!._logger?.Debug( $"    Processed Collection ({curr} of {count}) - {series.Name} items processed" );
            }

            cancellationToken.ThrowIfCancellationRequested();

            progress.Report( 80 );
            _dbHelper.ExecuteCommands( sqlCmds );
            progress.Report( 100 );
            _embyInterfaces!._logger?.Debug( $"Finished Analysis" );
        }

        private async Task<List<SQLCmdDef>> AnalyzeMissingEpisodes( TmdbCollectionReader reader, Series series, CancellationToken cancellationToken, IProgress<double> progress )
        {
            _embyInterfaces!._logger?.Info( $"     Analyzing Series '{series.Name}' - checking for missing episodes" );

            var seriesTmbdId = series.GetProviderId( MetadataProviders.Tmdb );
            if( string.IsNullOrEmpty( seriesTmbdId ) )
                return [];

            var tmdbSeries = await reader.GetRemoteSeriesAsync( seriesTmbdId, cancellationToken ).ConfigureAwait( false );
            if( tmdbSeries == null )
            {
                _embyInterfaces._logger!.Warn( $"Could not find TMDB series for {series.Name} - {seriesTmbdId}" );
                return [];
            }

            List<SQLCmdDef> retVal = [];
            foreach( var tmdbSeason in tmdbSeries.Seasons )
            {
                if( tmdbSeason.SeasonNumber == 0 && !Plugin.Instance!.Configuration.reportOnMissingSpecials )
                    continue;

                var embySeason = _dbHelper.GetSeasonFromSeries( series, tmdbSeason.SeasonNumber );

                foreach( var tmdbEpisode in tmdbSeason.Episodes )
                {
                    if( InvalidDate( tmdbEpisode.AirDate() ) )
                        continue;

                    var episodeTmdbId = tmdbEpisode.Id;
                    var episodeIdent = $"S{tmdbSeason.SeasonNumber:D2}E{tmdbEpisode.EpisodeNumber:D2}";
                    _embyInterfaces!._logger?.Debug( $"         Checking for episode {episodeIdent} for {series.Name} on server" );
                    var embyEpisode = ( embySeason != null ) ? _dbHelper.GetEpisodeFromTmdbId( _embyInterfaces._libraryManager, embySeason, episodeTmdbId, tmdbEpisode.EpisodeNumber ) : null;

                    if( embyEpisode == null )
                    {
                        _embyInterfaces!._logger?.Info( $"             {series.Name} - {episodeIdent} is missing" );

                        var key = $"{tmdbSeries.Id}-{tmdbSeason.Id}-{tmdbEpisode.Id}";
                        //return $"{primaryName} - S{season:D2}E{episode:D2} - {secondaryName}";

                        var title = $"{tmdbSeries.Name} - S{tmdbSeason.SeasonNumber:D2}E{tmdbEpisode.EpisodeNumber:D2} - {tmdbEpisode.Name}";
                        retVal.Add( new SQLCmdDef( kSQLAddToMissing,
                        [
                            ("@Key", key ),
                            ("@TmdbId", tmdbEpisode.Id ),
                            ("@Title", title ),
                            ("@OriginalTitle", string.Empty ),
                            ("@ReleaseDate", _dbHelper.ToDateTimeParamValue( tmdbEpisode.AirDate() ) ),
                            ("@Overview", tmdbEpisode.Overview ),
                            ("@PosterPath", tmdbEpisode.StillPath ),
                            ("@ParentId", series.Id.ToString() ),
                            ("@IsEpisode", true ),
                            ("@SeasonNum", tmdbSeason.SeasonNumber ),
                            ("@EpisodeNum", tmdbEpisode.EpisodeNumber )
                        ] ) );
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            return retVal;
        }

        private StatCard TotalMissingMedia( bool episodes )
        {
            CheckIsValid( ECheckType.eReport );

            var whereClause = episodes ? "IsEpisode" : "NOT IsEpisode";

            var sql = $"SELECT COUNT( * ) FROM Missing WHERE {whereClause}";
            var value = GetSingleValueFromSQL( sql );
            var numMissing = value.ToInt64();

            sql = $"SELECT COUNT( DISTINCT ParentId ) FROM Missing WHERE {whereClause}";
            value = GetSingleValueFromSQL( sql );
            var numCollections = value.ToInt64();

            var title = episodes ? Constants.MissingEpisodes : Constants.MissingMovies;
            var subTitle = episodes ? Constants.MissingEpisodesSubTitle : Constants.MissingMoviesSubTitle;
            var help = episodes ? Constants.MissingEpisodesHelp : Constants.MissingMoviesHelp;

            var retVal = new TextBasedStatCard( title, help, EStatCardStyle.eCompact );

            retVal.AddLine( numMissing.ToString(), true );
            retVal.AddLine( subTitle, true );
            retVal.AddLine( numCollections.ToString(), true );
            return retVal;
        }

        private List<MediaItemResponse> getMediaListResponseMissing( EWhichMediaList whichMedia )
        {
            var episodes = ( whichMedia & EWhichMediaList.eEpisodes ) != 0;

            var sql = "SELECT " +
                "  Missing.Title AS ListDisplayName" +
                ", Missing.ReleaseDate"
                ;
            if( episodes )
            {
                sql += ", Series.Name";
            }
            else
            {
                sql += ", ''";
            }

            sql +=
                ", Missing.PosterPath" +
                ", Missing.SeasonNum" +
                ", Missing.EpisodeNum" +
                ", Missing.ParentId" +
                " FROM " +
                "   Missing ";

            if( episodes )
                sql += " LEFT JOIN Series ON Series.ItemId=Missing.ParentId ";

            if( episodes )
                sql += " WHERE Missing.IsEpisode ";
            else
                sql += " WHERE NOT Missing.IsEpisode ";

            sql += " ORDER BY ListDisplayName ASC";

            if( episodes )
                sql += ", Missing.SeasonNum ASC, Missing.EpisodeNum ASC ";

            var retVal = new List<MediaItemResponse>();
            _dbHelper.ExecuteCommand( new SQLCmdDef( sql ), statement =>
            {
                var row = statement.Current;
                var premiereDate = DBHelper.ReadDateTime( row.GetString( 1 ) )?.Date ?? DateTime.MinValue;
                var premiereYear = premiereDate == DateTime.MinValue ? 0 : premiereDate.Year;

                var curr = new MediaItemResponse()
                {
                    DisplayName = new SortByText( CleanSortName( row.GetString( 0 ) ), row.GetString( 0 ) ),
                    PremiereDate = premiereDate.ToShortDateString() ?? string.Empty,
                    ResolutionDetail = string.Empty,
                    Codec = string.Empty,
                    DolbyVisionProfile = string.Empty,
                };
                var col = 2;
                curr.ParentName = new SortByText( row.GetString( col++ ), string.Empty );
                var posterPath = row.GetString( col++ );
                var seasonNum = row.GetInt( col++ );
                var episodeNum = row.GetInt( col++ );
                var parentId = row.GetString( col++ );

                if( !episodes )
                {
                    var collections = GetCollectionName( parentId );
                    curr.ParentName = collections ?? new();
                }

                var futureDate = InvalidDate( premiereDate, true );
                if( posterPath != null && !posterPath.StartsWith( "/" ) )
                {
                    posterPath = "/" + posterPath;
                    curr.ItemUrl = "https://image.tmdb.org/t/p/w185" + posterPath;
                }

                if( !futureDate && curr.ItemUrl != null && curr.ItemUrl != string.Empty )
                {
                    curr.DisplayName.Text = $"<a is=\"emby-linkbutton\" href=\"{curr.ItemUrl}\"><img loading=\"lazy\" src=\"{curr.ItemUrl}\" height=\"105px\"/>{curr.DisplayName.SortBy}</a>";
                }

                var searchKey = curr.DisplayName.SortBy;
                var subKey = string.Empty;
                if( episodes )
                {
                    searchKey = curr.ParentName.SortBy;
                    if( seasonNum != 0 )
                        subKey += $"S{seasonNum:D2}";
                    if( episodeNum != 0 )
                        subKey += $"E{episodeNum:D2}";
                }
                else
                {
                    if( premiereYear != 0 )
                    {
                        subKey = $"{premiereYear}";
                    }
                }

                if( futureDate )
                {
                    curr.SearchLocation = new SortByText( WebUtility.HtmlEncode( "<FUTURE RELEASE>" ) );
                    if( premiereDate != null )
                    {
                        var daysTo = ( premiereDate - DateTime.Today ).TotalDays;
                        if( daysTo == 0 && premiereDate.Date != DateTime.Today.Date )
                            daysTo = 1;
                        var msg = $"Available in {daysTo} day";
                        if( daysTo != 1 )
                            msg += "s";
                        curr.SearchLocation = new SortByText( msg );
                    }
                }
                else if( !string.IsNullOrEmpty( Plugin.Instance!.Configuration.searchLocation ) )
                {
                    var searchUrl = Plugin.Instance!.Configuration.searchLocation;
                    if( !searchUrl.EndsWith( "?q=" ) )
                        searchUrl += "?q=";
                    searchUrl += searchKey;

                    if( !subKey.IsEmpty() )
                        searchUrl += $" {subKey}";

                    var sortBy = searchKey;
                    if( !subKey.IsEmpty() )
                        sortBy += $" - {subKey}";

                    curr.SearchLocation.SortBy = sortBy;

                    var displayText = $"Click to Search for '{sortBy}'";
                    var searchLocation = $"<a is=\"emby-linkbutton\" href=\"{searchUrl}\" target=\"_blank\" rel=\"noopener noreferrer\" title=\"Search for {sortBy}\">{displayText}</a>";
                    curr.SearchLocation.Text = searchLocation;
                }

                retVal.Add( curr );
                return true;
            } );
            return retVal;
        }

        public StatCard TotalMissingMovies()
        {
            return TotalMissingMedia( false );
        }

        public StatCard TotalMissingEpisodes()
        {
            return TotalMissingMedia( true );
        }
    }
}
