# Codec Info plugin for Emby Server
![alt text]( https://github.com/towel42-com/Statistics2.0/blob/492065e733cee0eb2570f959ae8835671528fdc6/Statistics2.0/thumb.png)

This is a plugin for Emby server. If you do not already have Emby server installed please go to emby.media and download the server. Otherwise this plugin will be very useless for you.

## Build

When you build the project it will copy the dll to the Emby server plugin folder (%appdata%/Emby-Server/Plugins). Just restart Emby after you build and you should see the plugin installed. If not, check the copy command or copy the dll yourself.

## Installation

### Windows
Copy the dll file into the "%appdata%/Emby-Server/Plugins" folder, restart your Emby server and the new plugin should be visible.

### Unix
Copy the dll file into the "/var/lib/emby-server/plugins" folder, set emby as the Group:Owner, restart your Emby server and the new plugin should be visible.
