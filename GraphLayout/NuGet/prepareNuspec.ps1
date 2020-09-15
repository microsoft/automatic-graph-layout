param 
([string]$buildVersion)
Push-Location $PSScriptRoot 
$dependency = '$(BuildVersion)'
$file = '../tools/WpfGraphControl/WpfGraphControl.nuspec'
(Get-Content $file) | foreach {$_.replace($dependency, $buildVersion)} > $file

$file = '../tools/GraphViewerGDI/GraphViewerGDI.nuspec'
(Get-Content $file) | foreach {$_.replace($dependency, $buildVersion)} > $file

Pop-Location