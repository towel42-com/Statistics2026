using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaBrowser.Model.IO;
using Statistics20.Data;


using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;

using MediaBrowser.Model.Entities;

using MediaBrowser.Model.Logging;
using System.Net.Mime;

namespace Statistics20.Data
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

        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ConnectUserId { get; set; }
    }
}
