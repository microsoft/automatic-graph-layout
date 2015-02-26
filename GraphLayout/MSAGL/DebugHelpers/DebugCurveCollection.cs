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
#if TEST_MSAGL && ! SILVERLIGHT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Microsoft.Msagl.DebugHelpers{
    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix"), Serializable]
    public class DebugCurveCollection{
        /// <summary>
        /// 
        /// </summary>
        /// <param name="debugCurves"></param>
        DebugCurveCollection(IEnumerable<DebugCurve> debugCurves){
            DebugCurvesArray = debugCurves.ToArray();
        }
        /// <summary>
        /// 
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public DebugCurve[] DebugCurvesArray;

        ///<summary>
        ///</summary>
        ///<param name="debugCurves"></param>
        ///<param name="fileName"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public static void WriteToFile(IEnumerable<DebugCurve> debugCurves, string fileName) {
            Stream stream = File.Open(fileName, FileMode.Create);
            var bformatter = new BinaryFormatter();
            try {
                bformatter.Serialize(stream, new DebugCurveCollection(debugCurves));
            }
            catch (SerializationException e) {
                Console.WriteLine(e.ToString());
            }
            stream.Close();
        }

        ///<summary>
        ///</summary>
        ///<param name="fileName"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public static IEnumerable<DebugCurve> ReadFromFile(string fileName) {
            Stream stream = File.Open(fileName, FileMode.Open);
            var bformatter = new BinaryFormatter();
            DebugCurveCollection dc = null;
            try {
                dc = bformatter.Deserialize(stream) as DebugCurveCollection;
            }
            catch (SerializationException e) {
                Console.WriteLine(e.ToString());
            }
            stream.Close();
            return new List<DebugCurve>(dc.DebugCurvesArray);
        }
    }
}
#endif