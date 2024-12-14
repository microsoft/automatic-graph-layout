#!/bin/bash

dotnet nuget push /c/dev/agl/GraphLayout/Msagl.1.2.1.nupkg --api-key my_api_key --source https://api.nuget.org/v3/index.json
dotnet nuget push /c/dev/agl/GraphLayout/Msagl.Drawing.1.2.1.nupkg --api-key my_api_key --source https://api.nuget.org/v3/index.json
dotnet nuget push /c/dev/agl/GraphLayout/Msagl.GraphViewerGDI.1.2.1.nupkg --api-key my_api_key --source https://api.nuget.org/v3/index.json
dotnet nuget push /c/dev/agl/GraphLayout/Msagl.WpfGraphControl.1.2.1.nupkg --api-key my_api_key --source https://api.nuget.org/v3/index.json
