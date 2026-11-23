using MediaBrowser.Controller.Entities;
using ServiceStack;
using SQLitePCL.pretty;
using Statistics2026.Utilities;

namespace Statistics2026.Data
{
    public sealed partial class Statistics2026DB
    {
        public async Task AddAllUsersTaskImplAsync()
        {
            CheckIsValid( ECheckLevel.eUpdate );

            Logger().Debug( $"Starting User Analysis" );
            SetProgress( 0 );
            List<User> userList = EmbyHelperFuncs.GetAllUsers( _embyInterfaces );
            if( userList == null )
                return;

            double count = userList.Count;
            int completedCount = 0;

            SetProgress( 0 );
            var sqlCmds = new List<SQLCmdDef>();
            await DeleteAllInvalid( userList.ConvertAll( item => (BaseItem)item ), "Users", "UserId" );

            using( var timer = new AutoTimer( $"    Analyze Users - Getting Commands", Logger() ) )
            {
                IEnumerable<Task<(int index, User item, List<SQLCmdDef?> cmds)>> pool = userList.Select( ( user, index ) => Task.Run( async () =>
                {
                    (int index, User item, List<SQLCmdDef?> cmds) retVal = (index: index, item: user, cmds: new List<SQLCmdDef?>());
                    if( user == null )
                        return retVal;

                    retVal.cmds.Add( AddUser( user ) );
                    CancellationToken?.ThrowIfCancellationRequested();
                    int currProgress = Interlocked.Increment( ref completedCount );
                    SetProgress( 80.0 * ( 1.0 * currProgress ) / count );
                    Logger().Debug( $"    Processed User ({currProgress} of {count}) - {user.Name}" );
                    return retVal;
                } ) );

                CancellationToken?.ThrowIfCancellationRequested();
                (int index, User item, List<SQLCmdDef?> cmds)[] unsortedResults = await Task.WhenAll( pool );
                sqlCmds = unsortedResults
                            .Where( item => item.cmds != null )
                            .OrderBy( item => item.index )
                            .SelectMany( item => item.cmds! )
                            .Where( cmd => cmd != null )
                            .Select( cmd => cmd! )
                            .ToList();

                CancellationToken?.ThrowIfCancellationRequested();
            }

            using( var timer = new AutoTimer( $"    Analyze Users - Executing Commands", Logger() ) )
            {
                SetProgress( 80 );
                await ExecuteCommands( sqlCmds );
                SetProgress( 100 );
            }

            Logger().Debug( $"Finished User Analysis" );
        }

        private (long watched, long watchable) AnalyzeOverallTime( User? user, List<User>? userList )
        {
            CheckIsValid( ECheckLevel.eEmbyInterfaceOnly );

            if( user == null && userList == null )
                throw new ArgumentException( "Either user or allUsers must be provided." );

            (List<Video>? allVideosForUser, List<Video>? allVideos) = EmbyHelperFuncs.GetAllEpisodesAndMovies( user, LibraryManager(), true );

            long watchable = 0;
            long watched = 0;
            if( user == null && userList != null ) // use the list of users
            {
                watched = allVideos.Where( video => userList.Any( u => UserDataManager().GetUserData( u, video ).Played ) ).Sum( item => item.RunTimeTicks ?? 0 );
                watchable = allVideos.Sum( item => item.RunTimeTicks ?? 0 );
            }
            else
            {
                watched = allVideosForUser.Where( video => UserDataManager().GetUserData( user, video ).Played ).Sum( item => item.RunTimeTicks ?? 0 );
                watchable = allVideosForUser.Sum( item => item.RunTimeTicks ?? 0 );
            }

            return (watched, watchable);
        }

        private SQLCmdDef? AddUser( User user )
        {
            CheckIsValid( ECheckLevel.eUpdate );

            if( user == null )
                return null;

            if( user.Id == null )
            {
                Logger().Error( $"AddUser {user.Name}: is missing Id" );
            }

            if( user.Name == null )
            {
                Logger().Error( $"AddUser {user.Id}: is missing Name" );
                return null;
            }

            bool isAdmin = user.Policy.IsAdministrator;
            string sql =
                "INSERT INTO Users " +
                "(" +
                    "  UserId" +
                    ", UserName" +
                    ", ConnectUserId" +
                    ", IsAdministrator" +
                    ", MediaTableName" +
                ")" +
                " VALUES " +
                "(" +
                "  @UserId" +
                ", @UserName" +
                ", @ConnectUserId" +
                ", @IsAdministrator" +
                ", @MediaTableName" +
                ") " +
                " ON CONFLICT(UserId) " +
                " DO UPDATE " +
                " SET " +
                "  UserName=@UserName" +
                " ,ConnectUserId=@ConnectUserId" +
                " ,IsAdministrator=@IsAdministrator" +
                " ,MediaTableName=@MediaTableName"
                ;
            return new SQLCmdDef( sql,
            [
                ( "@UserId", user.Id.ToString()),
                ( "@UserName", user.Name),
                ( "@ConnectUserId", user.ConnectUserId),
                ( "@IsAdministrator", isAdmin),
                ( "@MediaTableName", TableDefs().getUserTableName( user ) )
            ] );
        }

        private async Task<long> NumUsers( bool hasConnectUserId, bool excludeAdmin )
        {
            CheckIsValid( ECheckLevel.eReport );

            string sql = "SELECT COUNT(UserName) FROM Users ";

            List<string> conditions = [];

            if( hasConnectUserId )
                conditions.Add( "ConnectUserId <> '' AND ConnectUserId IS NOT NULL" );

            if( excludeAdmin )
                conditions.Add( "NOT IsAdministrator" );

            sql += HelperFuncs.JoinWhereClauses( conditions );

            return ( await GetSingleValueFromSQL( sql ) ).ToInt64();
        }

        private Task<long> NumUsers()
        {
            CheckIsValid( ECheckLevel.eReport );

            return NumUsers( Statistics2026.Plugin.Instance!.Configuration.userReportSettings.hasConnectUserID, Statistics2026.Plugin.Instance!.Configuration.userReportSettings.excludeAdmin );
        }

        public async Task<StatCard> UserCount()
        {
            long numUsers = await NumUsers();
            return ValueGroupForSingleValue( Constants.TotalUsers, null, numUsers );
        }

        public async Task<StatCard> MostActiveUsers()
        {
            CheckIsValid( ECheckLevel.eReport );

            List<string> tables = await TableDefs().allUserMediaTables();

            string sqlBase =
                $"SELECT " +
                $"  Users.UserId" +
                $", Users.UserName" +
                $", SUM(<TABLE_NAME>.TotalTicksPlayed)" +
                $" FROM <TABLE_NAME>" +
                $" LEFT JOIN Users ON <TABLE_NAME>.UserId=Users.UserId"
                ;
            List<string> conditions = [];

            if( Statistics2026.Plugin.Instance!.Configuration.userReportSettings.hasConnectUserID )
                conditions.Add( "Users.ConnectUserId <> '' AND Users.ConnectUserId IS NOT NULL" );

            if( Statistics2026.Plugin.Instance!.Configuration.userReportSettings.excludeAdmin )
                conditions.Add( "NOT IsAdministrator" );

            sqlBase += HelperFuncs.JoinWhereClauses( conditions );

            var playTimeMap = new SortedDictionary<long, List<(string userId, string userName, long ticksPlayed)>>();
            foreach( string tableName in tables )
            {
                string sql = sqlBase;
                sql = sql.Replace( "<TABLE_NAME>", tableName );

                await ExecuteCommand( new SQLCmdDef( sql ), statement =>
                {
                    IResultSet row = statement.Current;

                    int col = 0;
                    string userId = row.GetString( col++ );
                    string userName = row.GetString( col++ );
                    if( userId == null || userName == null )
                        return Task.FromResult( true );
                    long ticksPlayed = row.GetInt64( col++ );
                    if( !playTimeMap.TryGetValue( ticksPlayed, out List<(string userId, string userName, long ticksPlayed)>? currItem ) )
                    {
                        playTimeMap[ ticksPlayed ] = [];
                    }

                    playTimeMap[ ticksPlayed ].Add( (userId, userName, ticksPlayed) );
                    return Task.FromResult( true );
                } );
            }

            int numUsers = Plugin.Instance!.Configuration.userReportSettings.numMostActiveUsers;
            string help = Constants.HelpMostActiveUsers;
            help = help.Replace( "<numUsers>", numUsers.ToString() );
            var groupData = new TableBasedStatCard( Constants.MostActiveUsers, help, [ "Days", "Hours", "Minutes" ] )
            {
                SubTitle = "By Play Time"
            };

            int cnt = 0;
            foreach( KeyValuePair<long, List<(string userId, string userName, long ticksPlayed)>> currKVP in playTimeMap.Reverse() )
            {
                foreach( (string userId, string userName, long ticksPlayed) curr in currKVP.Value )
                {
                    string userName = curr.userName;
                    string userId = curr.userId;
                    long ticks = curr.ticksPlayed;
                    var runtime = new RunTime( ticks );
                    groupData.addRow( userName, [ runtime.Days, runtime.Hours, runtime.Minutes ] );
                }

                if( cnt >= numUsers )
                    break;
                cnt++;
            }

            return groupData;
        }
    }
}