using System.Windows;
using Microsoft.Msagl.GraphViewerGdi;

namespace TestForAvalon {
    /// <summary>
    /// Interaction logic for MyApp.xaml
    /// </summary>

    public partial class MyApp {

        static string[] args;

        public static string[] Args {
            get { return args; }
            set { args = value; }
        }

        protected override void OnStartup(StartupEventArgs e) {
#if DEBUG
            DisplayGeometryGraph.SetShowFunctions();
#endif
            args = e.Args;
            base.OnStartup(e);
        }
    }
}