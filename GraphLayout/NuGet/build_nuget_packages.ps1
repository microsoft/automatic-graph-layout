.\AcquireNuGetExe.ps1

Push-Location $PSScriptRoot 
nuget pack Microsoft.Msagl.nuspec
nuget pack Microsoft.Msagl.Drawing.nuspec
nuget pack Microsoft.Msagl.GraphViewerGDI.nuspec
nuget pack Microsoft.Msagl.WpfGraphControl.nuspec
Pop-Location