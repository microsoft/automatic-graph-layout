

$sourceNugetExe = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
$targetNugetExe = $PSScriptRoot + "\nuget.exe"

Write-Host $targetNugetExe

if (!(test-path $targetNugetExe))
{
    Write-Host coudl not find nuget locally. Downloading...
    Invoke-WebRequest $sourceNugetExe -OutFile $targetNugetExe
}

Push-Location $PSScriptRoot 
&$targetNugetExe pack Microsoft.Msagl.nuspec
&$targetNugetExe pack Microsoft.Msagl.Drawing.nuspec
&$targetNugetExe pack Microsoft.Msagl.GraphViewerGDI.nuspec
&$targetNugetExe pack Microsoft.Msagl.WpfGraphControl.nuspec
Pop-Location