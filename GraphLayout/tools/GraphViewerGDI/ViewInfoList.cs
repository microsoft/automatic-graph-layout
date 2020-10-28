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
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Msagl.GraphViewerGdi
{
  internal class ViewInfosList
  {


    ViewInfoHolder current = new ViewInfoHolder();

    internal ViewInfo CurrentView { get { return current.viewInfo; } }

    internal bool AddNewViewInfo(ViewInfo viewInfo)
    {

      if (current.viewInfo == null || current.viewInfo != viewInfo)
      {

        //                    Log.W(viewInfo);

        ViewInfoHolder n = new ViewInfoHolder(viewInfo);
        current.next = n;
        n.prev = current;
        current = n;
        return true;
      }
      return false;

    }

    internal bool BackwardAvailable
    {
      get { return current.prev != null && current.prev.viewInfo != null; }
    }

    internal void Forward()
    {
      if (ForwardAvailable)
        current = current.next;
    }
    internal void Backward()
    {
      if (BackwardAvailable)
        current = current.prev;
    }

    internal bool ForwardAvailable
    {
      get { return current.next != null && current.next.viewInfo != null; }
    }

    internal void Clear() {
      current = new ViewInfoHolder();
    }
  }
}
