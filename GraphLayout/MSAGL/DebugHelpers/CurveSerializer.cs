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
#if SILVERLIGHT
#else 
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
#if DEBUGGLEE

namespace Microsoft.Msagl.Splines {
    /// <summary>
    /// For debugging purposees only
    /// </summary>
    internal class CurveSerializer {
        CurveSerializer() { }
        internal static void Serialize(string fileName, ICurve i) {

            Stream file = File.Open(fileName, FileMode.Create);

            // Create a formatter object based on command line arguments
            IFormatter formatter = new BinaryFormatter();

            // Serialize the object graph to stream
            formatter.Serialize(file, i);

            // All done
            file.Close();
        }

        internal static ICurve Deserialize(string fileName) {
            // Verify that the input file exists
            if (!File.Exists(fileName))
                return null;

            // Open the requested file to a stream object
            Console.WriteLine("\nDeserializing LinkedList from file: {0} ..\n", fileName);

            Stream file = File.Open(fileName, FileMode.Open);

            IFormatter formatter = new BinaryFormatter();

            try {
                ICurve i = formatter.Deserialize(file) as ICurve;
                return i;
            } catch (Exception) {
                throw;
            } finally {
                file.Close();
            }

        }

    }
}
#endif

#endif