# Codec Info plugin for Emby Server
![alt text](https://raw.githubusercontent.com/towel42-com/Statistics2026/trunk/Statistics2026/thumb.png)

This is a plugin for Emby server. If you do not already have Emby server installed please go to emby.media and download the server. Otherwise this plugin will be very useless for you.

The development environment assumes you are running the server and developing your plugin on on a Windows Device.  See the Unix section below installation on Unix/OSX.

## Setup
1. The solution relies on the external file called [InstallAndLaunch.bat](https://raw.githubusercontent.com/towel42-com/EmbyPluginUtils/trunk/InstallAndLaunch.bat).
It is part of the [Towel42/EmbyPluginUtils](https://github.com/towel42-com/EmbyPluginUtils) repositiory.  

    This file gets used in two locations
    
        * As a post build event call
        * As a launch script for debugging

    In the Statistics2026.csproj file, you will find a root level property group defining the variable InstallAndLaunchPath.
    
    After you download the file, update the csproj with the correct full path to the batch file you downloaded.
    
1. Next, In the Statistics2026.csproj file, in the PostBuild target section, you will find a series of Property groups defining EmbyServerRoot.

    I tend to keep 2 development servers, one is a copy of my full production server used for scale testing.  The other is a small subset.
    I do not develop on my host server, so all my installations are zip file installations.  

    If you are developing on an installed server, using Default should work fine.
    
    In the csproj file, you will find a Target with the Name "DetermineEmbyRoot".  Update the values for your installations.
    
    In Statistics2026/Properties you will find 'launchSettings.json'. Here is where you setup your debugging profiles.  Their names should match the conditionals
    setup in the csproj file.
   
If you set InstallAndLaunchPath and EmbyServerRoot correctly, no further work should be necessary to install and test the plugin.

## Installation
Whenever you build, the dll is installed to the correct location inside your Emby Server.  However, if the server is currently running, it will not be used until the server is restarted

## Debugging
When you launch a debug session, the InstallAndLaunch batch file, will shut down and locally running EmbyServers and their child processes.  Note, this may close your 
browsers, as they may have been launched as a child process.

After shutdown, the plugin will be copied to the correct location, and the Server will be launched.

The batch file will stay open for 5 seconds after the launch.

In order to debug the C# plugin, you must attach to the process.

SHIFT-ALT-P is the short cut for "Reattach to process".  If no previous session has been run, it will prompt you with a list of executables.  Select EmbyServer.exe.

Note, Your shortcut may be different. If so, under Debug you should find a "Attach to Process" menu item.


### Unix
You need to copy the generated DLL into "/var/lib/emby-server/plugins" folder, set emby as the Group:Owner, restart your Emby server and the new plugin should be visible.


