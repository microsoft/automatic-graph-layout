/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Msagl.DebugHelpers;
using Microsoft.Msagl.GraphViewerGdi;
using System.Text.Json;
namespace DebugCurveViewer {
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                System.Diagnostics.Debug.WriteLine("no file name was given");
                return;
            }
            var debugCurves = GetDebugCurves(args[0]);
            if (debugCurves != null)
            {
                var f = new Form1();
                DisplayGeometryGraph.ShowDebugCurvesEnumerationOnForm(debugCurves, f);
                f.Dispose();
            }
        }

        static DebugCurve[] GetDebugCurves(string fileName)
        {
            try
            {
                var jsonString = File.ReadAllText(fileName);
                var debugCurveCollection = JsonSerializer.Deserialize<DebugCurveCollection>(jsonString);
                if (null == debugCurveCollection)
                {
                    System.Diagnostics.Debug.WriteLine("cannot read debugcurves from " + fileName);
                    return null;
                }
                return debugCurveCollection.DebugCurvesArray;
            }
            catch (System.IO.FileNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
            return null;
        }
    }
}
