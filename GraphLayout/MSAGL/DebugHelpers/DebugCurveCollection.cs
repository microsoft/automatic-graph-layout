using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;

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
        public DebugCurveCollection(IEnumerable<DebugCurve> debugCurves){
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
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            try
            {
                string jsonString = JsonSerializer.Serialize(new DebugCurveCollection(debugCurves), options);
                File.WriteAllText(fileName, jsonString);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        ///<summary>
        ///</summary>
        ///<param name="fileName"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public static IEnumerable<DebugCurve> ReadFromFile(string fileName) {
            try
            {
                string jsonString = File.ReadAllText(fileName);
                var debugCurveCollection = JsonSerializer.Deserialize<DebugCurveCollection>(jsonString);
                return new List<DebugCurve>(debugCurveCollection.DebugCurvesArray);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
                return new List<DebugCurve>();
            }
        }
    }
}
