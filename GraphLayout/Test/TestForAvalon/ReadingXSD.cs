using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using System.Xml.Schema;
using Microsoft.Msagl.ControlForWpfObsolete;
using Microsoft.Msagl.Drawing;
using Color = Microsoft.Msagl.Drawing.Color;

//using Microsoft.AsmL.Tools.Algos.GraphLayout;

namespace TestForAvalon {
    partial class Window1 {
        XmlSchemaSet set = new XmlSchemaSet();
        bool stripTypes = true;

        public bool ShowTypeGraph {
            set { stripTypes = value; }
        }

        Graph Open(string file) {
            if (string.Compare(Path.GetExtension(file), ".xsd", true) == 0) {
                return OpenXsd(new[] {file});
            }
            MessageBox.Show("expecting xsd file");
            return null;
        }

        Graph OpenXsd(string[] files) {
            try {
                set = new XmlSchemaSet();
                set.ValidationEventHandler += SchemaSetValidationEventHandler;
                set.CompilationSettings.EnableUpaCheck = false;
                foreach (string file in files) {
                    var settings = new XmlReaderSettings {DtdProcessing = DtdProcessing.Ignore};
                    XmlReader r = XmlReader.Create(file, settings);
                    set.Add(null, r);
                    r.Close();
                }
                set.Compile();
                return ShowXsdGraph();
            }
            catch (Exception ex) {
                toolStripStatusLabel.Text = ex.Message;
                return null;
            }
        }

        void SchemaSetValidationEventHandler(object sender, ValidationEventArgs e) {
            toolStripStatusLabel.Text = e.Message;
            Debug.WriteLine(e.Message);
        }

        public Graph ShowXsdGraph() {
            diagram.Clear();
            if (stripTypes) {
                return GetTypeGraph();
            }
            return GetImportGraph();
        }

        Graph GetImportGraph() {
            var g = new Graph("Imports", "id");
            var nodes = new Hashtable();
            var edges = new Hashtable();
            foreach (XmlSchema s in set.Schemas()) {
                WalkImports(nodes, edges, s, s.SourceUri ?? s.TargetNamespace, g);
            }
            g.Attr.LayerDirection = direction;

            return g;
        }

        static Node WalkImports(Hashtable nodes, Hashtable edges, XmlSchema s, string uri, Graph g) {
            if (nodes.ContainsKey(uri)) {
                return (Node) nodes[uri]; // already visited
            }
            Node b1 = AddSchemaBox(nodes, uri, g);
            if (s != null) {
                foreach (var o in s.Includes) {
                    XmlSchema si = null;
                    var include = o as XmlSchemaInclude;
                    var baseUri = new Uri(o.SourceUri);
                    Uri suri = null;
                    var color = new Color(0, 0, 128); //Colors.Navy;
                    if (include != null) {
                        si = include.Schema;
                        suri = new Uri(baseUri, include.SchemaLocation);
                        color = new Color(0, 128, 0); //Colors.Green;
                    } else {
                        var import = o as XmlSchemaImport;
                        if (import != null) {
                            si = import.Schema;
                            suri = new Uri(baseUri, import.SchemaLocation);
                        }
                    }
                    Node b2 = WalkImports(nodes, edges, si, suri.AbsoluteUri, g);
                    if (b2 != b1) {
                        AddEdge(edges, g, b1, b2, color);
                    }
                }
            }
            return b1;
        }

// ReSharper disable UnusedMethodReturnValue.Local
        static Edge AddEdge(Hashtable edges,
// ReSharper restore UnusedMethodReturnValue.Local
                     Graph g, Node from,
                     Node to, Color color) {
            var toEdges = edges[from] as Hashtable;
            if (toEdges == null) {
                toEdges = new Hashtable();
                edges[from] = toEdges;
            }
            var e = toEdges[to] as Edge;
            if (e == null) {
                e = g.AddEdge(from.Attr.Id, to.Attr.Id);
                e.Attr.Color = color;
                toEdges[to] = e;
                e.Attr.Id = from.Attr.Id + " -> " + to.Attr.Id;
            }
            return e;
        }

        public static string GetFileName(string uri) {
            var u = new Uri(uri);
            string[] parts = u.Segments;
            return parts[parts.Length - 1];
        }

        static Node AddSchemaBox(Hashtable table, string uri, Graph g) {
            Node b1;
            if (table.ContainsKey(uri)) {
                b1 = (Node) table[uri];
            } else {
                // Make sure labels are unique.
                string baseLabel = GetFileName(uri);
                string label = baseLabel;
                int count = 0;
                Node found = g.FindNode(baseLabel);
                while (found != null) {
                    count++;
                    label = baseLabel + "(" + count + ")";
                    found = g.FindNode(label);
                }
                b1 = g.AddNode(uri);
                SetNodeColors(b1);
                b1.Attr.Shape = Shape.Box;
                b1.LabelText = label;
                b1.Attr.XRadius = b1.Attr.YRadius = 2; // rounded box.
                b1.Attr.LabelMargin = 5;
                table[uri] = b1;
            }
            return b1;
        }

        static string GetId(XmlSchemaType t) {
            string id = "{" + t.QualifiedName.Namespace + "}" + t.QualifiedName.Name;
            return id;
        }

        Graph GetTypeGraph() {
            var g = new Graph("Types", "id");
            var table = new Hashtable();
            foreach (XmlSchemaType st in set.GlobalTypes.Values) {
                if (st != null) {
                    if (st.QualifiedName == null || st.QualifiedName.Namespace != "http://www.w3.org/2001/XMLSchema") {
                        table[st] = null;
                    }
                }
            }
            foreach (XmlSchemaType ct in new ArrayList(table.Keys)) {
                XmlSchemaType baseType = ct.BaseXmlSchemaType;
                if (baseType != null && baseType.QualifiedName.Namespace != "http://www.w3.org/2001/XMLSchema") {
                    var b1 = table[ct] as Node;
                    if (b1 == null) {
                        b1 = g.AddNode(GetId(ct));
                        b1.LabelText = GetLabel(ct);
                        b1.Attr.Shape = Shape.Box;
                        b1.Attr.XRadius = b1.Attr.YRadius = 2; // rounded box.
                        b1.Attr.LabelMargin = 5;
                        SetNodeColors(b1);
                        table[ct] = b1;
                    }
                    Node b2 = g.AddNode(GetId(baseType));
                    b2.LabelText = GetLabel(baseType);
                    b2.Attr.Shape = Shape.Box;
                    b2.Attr.XRadius = b2.Attr.YRadius = 2; // rounded box.
                    SetNodeColors(b2);
                    b2.Attr.LabelMargin = 5;

                    g.AddEdge(b1.Attr.Id, b2.Attr.Id);
                }
            }
            g.Attr.LayerDirection = direction;

            return g;
        }

        static void SetNodeColors(Node b1) {
            b1.Attr.Color = Color.Black;
            if (b1.Label != null)
                b1.Label.FontColor = Color.Black;
        }

        static string GetLabel(XmlSchemaType type) {
            string label = type.Name;
            if (!string.IsNullOrEmpty(label))
                return label;

            if (type.QualifiedName != null) {
                foreach (XmlQualifiedName prefix in type.Namespaces.ToArray()) {
                    if (prefix.Namespace == type.QualifiedName.Namespace) {
                        if (!string.IsNullOrEmpty(prefix.Name)) {
                            return prefix.Name + ":" + type.Name;
                        }
                    }
                }
                return type.QualifiedName.Name;
            }
            return "";
        }

        

        
    }
}