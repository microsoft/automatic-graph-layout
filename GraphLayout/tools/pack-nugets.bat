@echo off
rem Check if the first argument exists
if "%~1"=="" (
    echo Error: No version number provided.
    echo Usage: %~n0  9.9.9
    exit /B 1
)
rem Define the list of .nuspec files
set nuspec_files=.\MSAGL\AutomaticGraphLayout.nuspec .\Drawing\AutomaticGraphLayout.Drawing.nuspec .\tools\GraphViewerGDI\GraphViewerGDI.nuspec .\tools\WpfGraphControl\WpfGraphControl.nuspec

IF NOT EXIST nuget.exe (
    echo Downloading nuget.exe...
    powershell -Command "(New-Object Net.WebClient).DownloadFile('https://dist.nuget.org/win-x86-commandline/latest/nuget.exe', 'nuget.exe')"
)

set BuildVersion="%~1"
echo BuildVersion is %BuildVersion%

rem Loop through each .nuspec file and generate the NuGet package
for %%f in (%nuspec_files%) do (
    echo Processing %%f

    rem Backup the original .nuspec file
    copy "%%f" "%%f.bak" >nul

    rem Replace the placeholder in the .nuspec file
    powershell -Command "(Get-Content '%%f') -replace 'BuildVersion', '%BuildVersion%' | Set-Content '%%f'"

    echo Packing %%f...
    nuget.exe pack "%%f"
    if errorlevel 1 (
        echo Failed to pack %%f
        rem Restore the original .nuspec file from the backup
        move /Y "%%f.bak" "%%f" >nul
        del nuget.exe
        exit /B 1
    )

    rem Restore the original .nuspec file from the backup
    move /Y "%%f.bak" "%%f" >nul    
)
del nuget.exe
echo All NuGet packages have been successfully generated.
