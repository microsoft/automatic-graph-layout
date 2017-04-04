using System.Windows;
using System.Windows.Input;
using Microsoft.Msagl.Drawing;

namespace Microsoft.Msagl.GraphmapsWpfControl {
    internal class GvMouseEventArgs : MsaglMouseEventArgs {
        MouseEventArgs args;
        Point position;

        internal GvMouseEventArgs(MouseEventArgs argsPar, GraphmapsViewer graphScrollerP) {
            args = argsPar;
            position = args.GetPosition((IInputElement) graphScrollerP.GraphCanvas.Parent);
        }

        public override bool LeftButtonIsPressed {
            get { return args.LeftButton == MouseButtonState.Pressed; }
        }


        public override bool MiddleButtonIsPressed {
            get { return args.MiddleButton == MouseButtonState.Pressed; }
        }

        public override bool RightButtonIsPressed {
            get { return args.RightButton == MouseButtonState.Pressed; }
        }


        public override bool Handled {
            get { return args.Handled; }
            set { args.Handled = value; }
        }

        public override int X {
            get { return (int) position.X; }
        }

        public override int Y {
            get { return (int) position.Y; }
        }

        /// <summary>
        ///     number of clicks
        /// </summary>
        public override int Clicks {
            get {
                var e = args as MouseButtonEventArgs;
                return e != null ? e.ClickCount : 0;
            }
        }
    }
}