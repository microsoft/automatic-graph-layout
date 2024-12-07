.\AcquireNuGetExe.ps1

Push-Location $PSScriptRoot 
nuget pack Microsoft.Msagl.2024.nuspec
nuget pack Microsoft.Msagl.GraphViewerGDI.nuspec
nuget pack Microsoft.Msagl.WpfGraphControl.nuspec
Pop-Location