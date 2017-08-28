param (
    [switch]$Update = $false
)

Set-StrictMode -Version 2
$ErrorActionPreference = "Stop"


$sourceNugetExe = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$destination_path = Join-Path $env:LOCALAPPDATA "NuGetCommandLine"

$targetNugetExe = "$destination_path\nuget.exe"


if ( (!(test-path $targetNugetExe)) -or ( $Update ) )
{
    
    Write-Host "Downloading install NuGet to $destination_path"

    if (!(test-path $destination_path))
    {
        New-Item -Path $destination_path -ItemType Directory
    }


    if (test-path $targetNugetExe)
    {
        Remove-Item $targetNugetExe
    }

    Invoke-WebRequest $sourceNugetExe -OutFile $targetNugetExe
}


Set-Alias nuget $targetNugetExe -Scope Global -Verbose