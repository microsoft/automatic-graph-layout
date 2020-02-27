DEVDIR=..\..\..
bin=..\..\bin

EngineSource= $(DEVDIR)\GraphLayout\MSAGL\bin\$(CONFIG)\Microsoft.Msagl.dll
EngineTarget=$(bin)\Microsoft.Msagl.dll
EngineSourcePdb= $(DEVDIR)\GraphLayout\MSAGL\bin\$(CONFIG)\Microsoft.Msagl.Pdb
EngineTargetPdb=$(bin)\Microsoft.Msagl.Pdb
EngineSourceXml= $(DEVDIR)\GraphLayout\MSAGL\bin\$(CONFIG)\Microsoft.Msagl.xml
EngineTargetXml=$(bin)\Microsoft.Msagl.xml
ParallExtSource= $(DEVDIR)\GraphLayout\MSAGL\ParallelExtensions_3_5.dll
ParallExtTarget= $(bin)\ParallelExtensions_3_5.dll

DrawingSource= $(DEVDIR)\GraphLayout\Drawing\bin\$(CONFIG)\Microsoft.Msagl.Drawing.dll
DrawingTarget= $(bin)\Microsoft.Msagl.Drawing.dll
DrawingSourcePdb= $(DEVDIR)\GraphLayout\Drawing\bin\$(CONFIG)\Microsoft.Msagl.Drawing.Pdb
DrawingTargetPdb= $(bin)\Microsoft.Msagl.Drawing.Pdb
DrawingSourceXml= $(DEVDIR)\GraphLayout\Drawing\bin\$(CONFIG)\Microsoft.Msagl.Drawing.xml
DrawingTargetXml= $(bin)\Microsoft.Msagl.Drawing.xml

ViewerSource=$(DEVDIR)\GraphLayout\GraphViewerGdi\bin\$(CONFIG)\Microsoft.Msagl.GraphViewerGDI.dll
ViewerTarget=$(bin)\Microsoft.Msagl.GraphViewerGDI.dll
ViewerSourcePdb=$(DEVDIR)\GraphLayout\GraphViewerGdi\bin\$(CONFIG)\Microsoft.Msagl.GraphViewerGDI.Pdb
ViewerTargetPdb=$(bin)\Microsoft.Msagl.GraphViewerGDI.Pdb
ViewerSourceXml=$(DEVDIR)\GraphLayout\GraphViewerGdi\bin\$(CONFIG)\Microsoft.Msagl.GraphViewerGDI.xml
ViewerTargetXml=$(bin)\Microsoft.Msagl.GraphViewerGDI.xml

all: mkdir copy

mkdir: 
	-mkdir $(bin)
	echo $(CONFIG)

clean: 
	del /s /q $(bin)



copy: $(EngineTarget) $(SLEngineTarget) $(DrawingTarget) $(ViewerTarget) $(ParallExtTarget)

$(EngineTarget):  $(EngineSource)
	copy  $(EngineSource)  $(EngineTarget)
	copy  $(EngineSourcePdb)  $(EngineTargetPdb)
	copy  $(EngineSourceXml)  $(EngineTargetXml)
	
$(SLEngineTarget):  $(SLEngineSource)
	copy  $(SLEngineSource)  $(SLEngineTarget)
	copy  $(SLEngineSourcePdb)  $(SLEngineTargetPdb)
	copy  $(SLEngineSourceXml)  $(SLEngineTargetXml)
	
$(DrawingTarget):  $(DrawingSource)
	copy  $(DrawingSource)  $(DrawingTarget)
	copy  $(DrawingSourcePdb)  $(DrawingTargetPdb)
	copy  $(DrawingSourceXml)  $(DrawingTargetXml)

$(ViewerTarget):  $(ViewerSource)
	copy  $(ViewerSource) $(ViewerTarget)
	copy  $(ViewerSourcePdb) $(ViewerTargetPdb)
	copy  $(ViewerSourceXml) $(ViewerTargetXml)
	
$(ParallExtTarget): $(ParallExtSource)
	copy $(ParallExtSource) $(ParallExtTarget)

