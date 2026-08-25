using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaBrowser.Model.IO;
using Statistics2026.Data;


using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;

using MediaBrowser.Model.Entities;

using MediaBrowser.Model.Logging;
using System.Net.Mime;

namespace Statistics2026.Data
{
    public class UserInfo
    {
        public UserInfo() { }
        public UserInfo(User user)
        {
            UserId = user.Id.ToString();
            UserName = user.Name;
            ConnectUserId = user.ConnectUserId;
        }

        public string UserId { get; set; } = String.Empty;
        public string UserName { get; set; } = String.Empty;
        public string ConnectUserId { get; set; } = String.Empty;
    }
}
