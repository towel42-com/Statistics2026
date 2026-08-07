param (
    [Parameter(Mandatory = $true)]
    [string]$TargetPath,

    [Parameter(Mandatory = $false)]
    [string]$EmbyRoot,
    
    [Parameter(Mandatory = $false)]
    [switch]$NoColor,

    [Parameter(Mandatory = $false)]
    [switch]$NoLaunch
)

function Write-CustomLog {
   param (
        [Parameter(Mandatory = $true, Position=0)]
        [string]$Message,
        
        [Parameter(Mandatory = $false)]
        [System.ConsoleColor]$ForegroundColor = (Get-Host).UI.RawUI.ForegroundColor,

        [Parameter(Mandatory = $false)]
        [switch]$Force
    )

    if ( $Force -or $Verbose) {
        if (-not (Get-Variable -Name 'NoColor' -ErrorAction SilentlyContinue) -or !$NoColor) {
            Write-Host $Message -ForegroundColor $ForegroundColor
        }
        else {
            Write-Host $Message
        }
    }
}


function Stop-RunningProcess {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [string]$ProcessName
    )


    process {
        # Check for running instances of the specified process name
        $TargetProcess = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue

        if ($TargetProcess) {
            Write-CustomLog "    Found running instance(s) of '$ProcessName'. Terminating..." -ForegroundColor Yellow
            
            # Forcefully terminate the process
            $TargetProcess | Stop-Process -Force
            
            Write-CustomLog "Successfully terminated '$ProcessName'." -ForegroundColor Green
        } else {
            Write-CustomLog "Process '$ProcessName' is not currently running." -ForegroundColor Cyan
        }
    }
}

if (-not $TargetPath) {
    Write-CustomLog -Force 'Please set the ${TargetPath} variable' -ForegroundColor Red
    exit 1
}

if (-not $EmbyRoot) {
    Write-CustomLog -Force 'Please set the ${EmbyRoot} variable' -ForegroundColor Red
    exit 1
}

if (-not (Test-Path -Path ${TargetPath} -PathType Leaf)) {
    Write-CustomLog -Force "The file '${TargetPath} does not exist." -ForegroundColor Red
    exit 1
}

if (-not (Test-Path -Path ${EmbyRoot} -PathType Container)) {
    Write-CustomLog -Force "The directory '${EmbyRoot}' does not exist." -ForegroundColor Red
    exit 1
}

Write-CustomLog "               Plugin: $TargetPath"
Write-CustomLog "Emby Server Directory: $EmbyRoot"
Write-CustomLog "===================================================="

if (-not (Get-Variable -Name 'NoLaunch' -ErrorAction SilentlyContinue) -or !$NoLaunch) {
    Write-CustomLog "Killing existing EmbyServer and embytray executables"
    Stop-RunningProcess -ProcessName "EmbyServer"
    Stop-RunningProcess -ProcessName "embytray"
}

Write-CustomLog "Plugin: ${TargetPath}"
Write-CustomLog -Force "Copying '${TargetPath}' to '${EmbyRoot}\programdata\plugins\' directory"
Copy-Item -Path "${TargetPath}" -Destination "${EmbyRoot}\programdata\plugins\"

if (-not (Get-Variable -Name 'NoLaunch' -ErrorAction SilentlyContinue) -or !$NoLaunch) {
    Write-CustomLog -Force "Launching Emby Server '${EmbyRoot}\system\EmbyServer.exe'"
    cd "${EmbyRoot}\system"
    Start-Process -FilePath "${EmbyRoot}\system\EmbyServer.exe"
}

