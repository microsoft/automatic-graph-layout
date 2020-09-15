GL = $(DEVDIR)\GraphLayout

DIST = $(DEVDIR)\msaglExternalDistribution

msbuild="c:\Windows\Microsoft.NET\Framework\v3.5\msbuild.exe"

alldebug:  gleedebug


release:  buildrelease 

buildrelease:
	cd $(DEVDIR)\GraphLayout
	$(msbuild) GraphLayout.sln /p:Configuration=Release /l:$(GL)\utilities\msbuildlogger\bin\Debug\msbuildlogger.dll /noconlog


logger:
	cd $(GL)\utilities\msbuildlogger
	devenv msbuildlogger.csproj /build

glee:
	cd $(DEVDIR)\GraphLayout
	$(msbuild) GraphLayout.sln /p:Configuration=Debug /l:$(GL)\utilities\msbuildlogger\bin\Debug\msbuildlogger.dll /noconlog

all: logger utils gleeall 


release: logger buildrelease
	cd $(DEVDIR)\

utils:
	cd $(GL)\Utilities\SyncFromFileSystem 
	$(msbuild) SyncFromFileSystem.sln  /p:Configuration=Debug /l:$(GL)\utilities\msbuildlogger\bin\Debug\msbuildlogger.dll /noconlog


gleedebug:
	cd $(DEVDIR)\GraphLayout
	$(msbuild) GraphLayout.sln /p:Configuration=Debug /l:$(GL)\utilities\msbuildlogger\bin\Debug\msbuildlogger.dll /noconlog

distribution: external toolbox

external: release buildSamples extRelCopy externalMsi 


extRelCopy: 
	-rmdir /S /Q $(DIST)\bin $(DIST)
	-robocopy $(GL)\testforgdi\bin\release $(DIST)\bin *msagl*pdb  *msagl*dll 
	-robocopy $(GL)\Samples $(DIST)\Samples *csproj *sln *cs *resx readme.txt /S
	-robocopy $(GL) $(DIST) Micro*.rtf redist.txt
#	copy /Y  $(GL)\ReadmeExternal.doc $(DIST)\README.doc
#	copy /Y  "$(GL)\GLEE SHARED SOURCE LICENSE (2006-8-22).rtf" $(DIST)


#need it only to be sure that the samples build
buildSamples: 
	$(msbuild) $(GL)\samples\Samples.sln /p:Configuration=Debug
	$(msbuild) $(GL)\samples\Samples.sln /p:Configuration=Release


externalMsi:
    "C:\Program Files\Caphyon\Advanced Installer 5.2.2\AdvancedInstaller.exe" /build $(GL)\msagl.aip -force


toolbox: buildrelease copybins

wzzip="c:\Program Files\WinZip\WZZIP.EXE"



copybins:
	del /F /Q  bin\TestForGDI.zip bin\TestForAvalon.zip
	$(wzzip) -rp bin\TestForGDI.zip  TestForGDI/bin/Release/
	$(wzzip) -rp  bin\TestForAvalon.zip TestForAvalon/bin/Release/


publishToVibe: 	release checkoutDLLsInVibeLibs copyDLLsToVibeLibs submitDLLsIntoVibeLibs


checkoutDLLsInVibeLibs:
	cd $(VIBEDEVDIR)\DET\DET\libs
	sd edit *msagl*

copyDLLsToVibeLibs:
	robocopy $(DEVDIR)\GraphLayout\TestForAvalon\bin\release $(VIBEDEVDIR)\DET\DET\libs *.Msagl.*dll *.Msagl.*pdb /LEV:0

submitDLLsIntoVibeLibs:
	cd $(VIBEDEVDIR)\DET\DET\libs
	sd submit ...


