param($installPath, $toolsPath, $package, $project) 
  cd $toolsPath; 
  cd ..;
  if (Test-Path "libz3.dll") { move libz3.dll lib }