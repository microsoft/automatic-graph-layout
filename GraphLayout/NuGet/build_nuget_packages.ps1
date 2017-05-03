

$sourceNugetExe = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$targetNugetExe = $PSScriptRoot + "\nuget.exe"

Write-Host $targetNugetExe

if (!(test-path $targetNugetExe))
{
    Write-Host coudl not find nuget locally. DOwnloading...
    Invoke-WebRequest $sourceNugetExe -OutFile $targetNugetExe
}

Push-Location $PSScriptRoot 
&$targetNugetExe pack Microsoft.Automatic.Graph.Layout.nuspec
&$targetNugetExe pack Microsoft.Automatic.Graph.Drawing.nuspec
Pop-Location