using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Msagl.Core.Geometry.Curves;
#if TEST_MSAGL

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
            System.Diagnostics.Debug.WriteLine("\nDeserializing LinkedList from file: {0} ..\n", fileName);

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