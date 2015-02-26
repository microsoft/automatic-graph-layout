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

namespace Logger
{
  public class MyLogger:Microsoft.Build.Utilities.Logger
  {
    public override void Initialize(Microsoft.Build.Framework.IEventSource eventSource)
    {
      eventSource.ErrorRaised += new Microsoft.Build.Framework.BuildErrorEventHandler(eventSource_ErrorRaised);
      eventSource.WarningRaised += new Microsoft.Build.Framework.BuildWarningEventHandler(eventSource_WarningRaised);
      eventSource.BuildFinished += new Microsoft.Build.Framework.BuildFinishedEventHandler(eventSource_BuildFinished);
    }

    void eventSource_BuildFinished(object sender, Microsoft.Build.Framework.BuildFinishedEventArgs e)
    {
        Console.WriteLine("{0}",e.Message);
    }

    void eventSource_WarningRaised(object sender, Microsoft.Build.Framework.BuildWarningEventArgs e)
    {
      string dir = System.IO.Directory.GetCurrentDirectory();

      string line = String.Format("{0}({1},{2}): warning {3}: {4} ",
        System.IO.Path.Combine(dir, e.File), e.LineNumber, e.ColumnNumber, e.Code, e.Message);
      Console.WriteLine(line);
    }

    void eventSource_ErrorRaised(object sender, Microsoft.Build.Framework.BuildErrorEventArgs e)
    {
      string dir=System.IO.Directory.GetCurrentDirectory();
      
      string line = String.Format("{0}({1},{2}): error {3}: {4} ",System.IO.Path.Combine(dir, e.File) , e.LineNumber, e.ColumnNumber,e.Code,e.Message);
      Console.WriteLine(line);
    }
    
   }
}
