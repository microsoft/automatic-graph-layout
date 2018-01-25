using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.DebugHelpers.Persistence;

namespace Microsoft.Msagl.DebugHelpers {
    internal class CurveStream {
        string data;
        CurveStreamElement[] curveStreamElements;
        int offset;

        internal CurveStream(string curveData) {
            data=curveData;
            Init();
        }

        void Init() {
            data=data.Trim();
            var blocks = data.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            curveStreamElements = RefineBlocks(blocks).ToArray();

        }

        static IEnumerable<CurveStreamElement> RefineBlocks(string[] blocks) {
            foreach (string block in blocks) {
                var ch = block[0];
                if (Char.IsLetter(ch)) {
                    yield return new CharStreamElement(ch);
                    if (block.Length > 1) {
                        double res;
                        if (Double.TryParse(block.Substring(1), out res))
                            yield return new DoubleStreamElement(res);
                        else yield return null;
                    }
                } else {
                    double res;
                    if (Double.TryParse(block, out res))
                        yield return new DoubleStreamElement(res);
                    else yield return null;
                }
            }
        }

        internal CurveStreamElement GetNextCurveStreamElement() {
            if (offset >= curveStreamElements.Length)
                return null;
            return curveStreamElements[offset++];
        }

        internal CurveStreamElement PickNextCurveStreamElement() {
            if (offset >= curveStreamElements.Length)
                return null;
            return curveStreamElements[offset];
        }
    }
}