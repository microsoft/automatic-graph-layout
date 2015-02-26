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