using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Services;
using Statistics2026.Utilities;

namespace Statistics2026.Notifications
{
    public class NotificationSender
    {
        private readonly INotificationManager _notificationManager;

        private NotificationSender( INotificationManager notificationManager )
        {
            _notificationManager = notificationManager;
        }

        private async Task SendEpisodesAvailableNotification( List<string> episodesAvailable )
        {
            var notificationRequest = new Emby.Notifications.NotificationRequest
            {
                EventId = "Statistics20206.EpisodeNowAvailable",
                
                Title = "New Episodes will be available today",
                Description = string.Join( "\n", episodesAvailable ),

                Severity = LogSeverity.Info,
                Date = DateTime.Now
            };

            _notificationManager.SendNotification( notificationRequest );
        }

        public static void SendEpisodesAvailableNotification( List<string> episodesAvailable, EmbyInterfaces embyInterfaces )
        {
            var notificationSender = new NotificationSender( embyInterfaces.NotificationManager() );
            _ = notificationSender.SendEpisodesAvailableNotification( episodesAvailable );
        }
    }
}
