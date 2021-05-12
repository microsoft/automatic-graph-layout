using System;

namespace SvgLayerSample {
    class Program : Examples {
        static void Main(string[] args) {
            var svg = Examples.Example01();
            Console.WriteLine(svg);
            TextCopy.ClipboardService.SetText(svg);
        }
    }
}
