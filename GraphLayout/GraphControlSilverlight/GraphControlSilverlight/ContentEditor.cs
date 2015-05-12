using System;
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
