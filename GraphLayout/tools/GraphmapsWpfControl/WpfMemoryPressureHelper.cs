using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace Microsoft.Msagl.GraphmapsWpfControl {
    // This class is here just to provide a workaround for the bug in WPF memory management described here:
    // https://connect.microsoft.com/VisualStudio/feedback/details/687605/gc-is-forced-when-working-with-small-writeablebitmap
    // Call the ResetTimers method after every creation of a WriteableBitmap.
    internal static class WpfMemoryPressureHelper {
        const string TypeName_MSInternalMemoryPressure = @"MS.Internal.MemoryPressure";
        const string FieldName_LockObj = @"lockObj";
        const string FieldName_CollectionTimer = @"_collectionTimer";
        const string FieldName_AllocationTimer = @"_allocationTimer";

    
        static object lockObj;
        static Stopwatch allocationTimer;
        static Stopwatch collectionTimer;

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline",
            Justification = "Need error handling around initialisation")]
        static WpfMemoryPressureHelper() {
            try {
                Type memPressureType = typeof (BitmapImage).Assembly.GetType(TypeName_MSInternalMemoryPressure);
                if (memPressureType == null) {
                    System.Diagnostics.Debug.WriteLine("Could not find type: {0}", TypeName_MSInternalMemoryPressure);

                    return;
                }

                FieldInfo lockObjField = memPressureType.GetField(FieldName_LockObj,
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (lockObjField == null) {
                    System.Diagnostics.Debug.WriteLine("Could not find field: {0}", FieldName_LockObj);

                    return;
                }

                FieldInfo collectionTimerField = memPressureType.GetField(FieldName_CollectionTimer,
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (collectionTimerField == null) {
                    System.Diagnostics.Debug.WriteLine("Could not find field: {0}", FieldName_CollectionTimer);

                    return;
                }

                FieldInfo allocationTimerField = memPressureType.GetField(FieldName_AllocationTimer,
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (lockObjField == null) {
                    System.Diagnostics.Debug.WriteLine("Could not find field: {0}", FieldName_AllocationTimer);

                    return;
                }

                lockObj = lockObjField.GetValue(null);
                collectionTimer = (Stopwatch) collectionTimerField.GetValue(null);
                allocationTimer = (Stopwatch) allocationTimerField.GetValue(null);
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine("Failed to initialize {0}", ex);
            }
        }

        /// <summary>
        /// Reset the timers used to control WPF memory management.
        /// </summary>
        /// <remarks>
        /// Call this method after every time a new WriteableBitmap is created to prevent problems when 
        /// GC takes longer than 850ms.
        /// </remarks>
        public static void ResetTimers() {
            if (lockObj == null ||
                collectionTimer == null ||
                allocationTimer == null) {
                // Reflection code failed. New version of .Net?
                return;
            }

            lock (lockObj) {
                long timeDelta = collectionTimer.ElapsedMilliseconds - allocationTimer.ElapsedMilliseconds;

                if (Math.Abs(timeDelta) < 25) {
                    collectionTimer.Restart();
                }

                allocationTimer.Restart();
            }
        }
    }
}