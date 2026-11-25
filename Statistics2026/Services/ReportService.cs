using Emby.Media.Common.Extensions;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Tasks;
using Statistics2026.Data;
using Statistics2026.Utilities;
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Statistics2026.Data.Statistics2026DB;

namespace Statistics2026.Services
{
    [Route( "/Statistics2026/get_movie/{WhichStatistic}", "GET", Summary = "Get the movie statistic in from the database" )]
    [Authenticated( Roles = "admin" )]
    public class GetMovie : IReturn<object>
    {
        public StatGen.EStatisticType whichStatistic { get; set; }
    }

    [Route( "/Statistics2026/get_series/{WhichStatistic}", "GET", Summary = "Get the series statistic information from the database" )]
    [Authenticated( Roles = "admin" )]
    public class GetSeries : IReturn<object>
    {
        public StatGen.EStatisticType whichStatistic { get; set; }
    }

    [Route( "/Statistics2026/get_episode/{WhichStatistic}", "GET", Summary = "Get the episode statistic information from the database" )]
    [Authenticated( Roles = "admin" )]
    public class GetEpisode : IReturn<object>
    {
        public StatGen.EStatisticType whichStatistic { get; set; }
    }

    public class ReportService : BaseService
    {
        public ReportService(
                   IFileSystem fileSystem,
                   ILibraryManager libraryManager,
                   ILogManager logManager,
                   IServerApplicationPaths serverApplicationPaths,
                   IUserDataManager userDataManager,
                   IUserManager userManager,
                   IApplicationHost appHost,
                   IJsonSerializer jsonSerializer,
                   IProviderManager providerManager,
                   IServerConfigurationManager configManager,
                   ITaskManager taskManager,
                   ISessionManager sessionManager,
                   IHttpClient httpClient,
                   IAuthenticationRepository authenticationRepository,
                   INotificationManager notificationManager
                ) : base(
                       fileSystem,
                       libraryManager,
                       logManager,
                       serverApplicationPaths,
                       userDataManager,
                       userManager,
                       appHost,
                       jsonSerializer,
                       providerManager,
                       configManager,
                       taskManager,
                       sessionManager,
                       httpClient,
                       authenticationRepository,
                       notificationManager
                    )
        {
        }

        public Task<object> Get( GetMovie request )
        {
            return HelperFuncs.HandleRequestAsync( request.GetType().Name, _embyInterfaces, async (timer,db) =>
            {
                StatGen.EStatisticType whichStatistic = request.whichStatistic;
                timer.Text += $" - {whichStatistic}";

                StatCard groupData = await db.StatisticForAsync( null, whichStatistic, StatGen.EVideoType.Movie ).ConfigureAwait( false );
                if( groupData == null )
                    return new StatCardResponse();

                return groupData.createStat();
            } );
        }

        public Task<object> Get( GetSeries request )
        {
            return HelperFuncs.HandleRequestAsync( request.GetType().Name, _embyInterfaces, async (timer,db) =>
            {
                StatGen.EStatisticType whichStatistic = request.whichStatistic;
                timer.Text += $" - {whichStatistic}";

                StatCard groupData = await db.StatisticForAsync( null, whichStatistic, StatGen.EVideoType.Series ).ConfigureAwait( false );
                if( groupData == null )
                    return new StatCardResponse();

                return groupData.createStat();
            } );
        }

        public Task<object> Get( GetEpisode request )
        {
            return HelperFuncs.HandleRequestAsync( request.GetType().Name, _embyInterfaces, async (timer,db) =>
            {
                StatGen.EStatisticType whichStatistic = request.whichStatistic;
                timer.Text += $" - {whichStatistic}";

                StatCard groupData = await db.StatisticForAsync( null, whichStatistic, StatGen.EVideoType.Episode ).ConfigureAwait( false );
                if( groupData == null )
                    return new StatCardResponse();

                return groupData.createStat();
            } );
        }
    }
}
