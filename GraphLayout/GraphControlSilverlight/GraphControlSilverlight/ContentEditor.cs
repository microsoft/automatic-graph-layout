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
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    public class ContentEditorProvider
    {
        public DGraph Owner { get; private set; }

        public ContentEditorProvider(DGraph owner)
        {
            Owner = owner;
        }

        public virtual FrameworkElement GetNewGUIInstance(DObject obj)
        {
            DTextLabel editingLabel = obj is IHavingDLabel ? (obj as IHavingDLabel).Label as DTextLabel : obj as DTextLabel;
            if (editingLabel == null)
                return null;
            var tb = new TextBox() { MinWidth = 50.0, Text = editingLabel.Text, SelectionStart = 0, SelectionLength = editingLabel.Text.Length, AcceptsReturn = true };
            tb.KeyDown += (sender, args) =>
            {
                if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.None)
                {
                    if (args.Key == System.Windows.Input.Key.Enter)
                        Owner.LabelEditor.Close(true);
                    else if (args.Key == System.Windows.Input.Key.Escape)
                        Owner.LabelEditor.Close(false);
                }
            };
            return tb;
        }

        public virtual void FocusGUI(FrameworkElement guiElement, DObject obj)
        {
            (guiElement as TextBox).Focus();
        }

        public virtual void UpdateLabel(FrameworkElement guiElement, DObject obj)
        {
            DTextLabel editingLabel = obj is IHavingDLabel ? (obj as IHavingDLabel).Label as DTextLabel : obj as DTextLabel;
            editingLabel.Text = (guiElement as TextBox).Text;
        }
    }
}
