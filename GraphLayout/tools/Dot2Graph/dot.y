%using Microsoft.Msagl.Core.DataStructures;
%using Microsoft.Msagl.Drawing;
%using System.Collections;
%using System.Linq;
%using Microsoft.Msagl.Core.Geometry.Curves;
%using Microsoft.Msagl.Core.Layout;
%namespace Dot2Graph


%{
    bool strict;
    Graph graph = new Graph("");
	Subgraph currentSubgraph;	
%}

%start graph

%union {
    public   string sVal;
    public   ArrayList aVal;
    internal AttributeValuePair avPair;
    internal Parser.Cell<string> sList;
	internal Parser.Cell<Parser.Cell<string>> sLists;
}

%token DIGRAPH GRAPH ARROW SUBGRAPH NODE EDGE
%token <sVal> ID

%left '{'

%%

graph     :  graphType graphName '{' stmt_list '}'
          ;



graphType : DIGRAPH { strict = false; this.graph.Directed = true; }
          | GRAPH   { strict = false; this.graph.Directed = false; }
		  | ID DIGRAPH { strict = $1 != null && "strict" == $1.ToLower(); this.graph.Directed = true; }
          | ID GRAPH   { strict = $1 != null && "strict" == $1.ToLower(); this.graph.Directed = false; }          
          ;
 
graphName : ID { this.graph.Attr.Id = $1; } 
          |    { }
          ;

stmt_list : { $$.sList = new Cell<string>(); }
          | stmt stmt_list { $$.sList = Append($1.sList, $2.sList); }
          | stmt ';' stmt_list { $$.sList = Append($1.sList, $3.sList); }
          ;

stmt      : ID '=' ID { MkEqStmt($1, $3); $$.sList = new Cell<string>(); } 
          | node_stmt { $$.sList = $1.sList; }
          | edge_stmt { $$.sList = $1.sList; }
          | attr_stmt { $$.sList = new Cell<string>(); }
          | subgraph  { $$.sList = $1.sList; }
          ;

node_stmt : node_id opt_attr_list { $$.sList = MkNodeStmt($1.sVal, $2.aVal); }
          ;

edge_stmt : endpoint edgeRHS opt_attr_list { $$.sList = MkEdgeStmt($1.sList, $2.sLists, $3.aVal);} 
          ;

endpoint  : node_id  { $$.sList = MkSingleton($1.sVal); }
          | subgraph { $$.sList = $1.sList; }
		  ;

edgeRHS   : ARROW endpoint  { $$.sLists = MkSingleton($2.sList); }
          | edgeRHS ARROW endpoint  { $$ = $1; $$.sList = new Cell<string>($$.sList, null, $3.sList); }
          ;

subgraph  : '{'  stmt_list '}' { $$.sList = $2.sList; }
          | SUBGRAPH  '{' stmt_list '}' { $$.sList = $3.sList;}
          | SUBGRAPH  id  {CreateNewCurrentSubgraph($2.sVal); } '{' stmt_list '}' { $$.sList = new Cell<string>(); 
		                        PopCurrentSubgraph(); }
          | SUBGRAPH  { $$.sList = new Cell<string>(); }
          ;

attr_stmt : GRAPH attr_list { MkGraphAttrStmt($2.aVal); }
          | NODE attr_list { MkNodeAttrStmt($2.aVal); }
          | EDGE attr_list { MkEdgeAttrStmt($2.aVal); }
          ;

opt_attr_list : { $$.aVal = new ArrayList(); }
          | attr_list { $$ = $1; }
          ;

attr_list : '[' ']' { $$.aVal = new ArrayList(); }
          | '[' a_list ']' { $$ = $2; }
          ;

a_list    : avPair            { $$.aVal = new ArrayList(); $$.aVal.Add($1.avPair); }
          | avPair a_list     { $2.aVal.Add($1.avPair); $$ = $2; }
          | avPair ',' a_list { $3.aVal.Add($1.avPair); $$ = $3; }
          ;

avPair    : id '=' id { $$.avPair = MkAvPair($1.sVal, $3.sVal); }
          | id { $$.avPair = MkAvPair($1.sVal, ""); }
          ;

node_id   : id opt_port { $$.sVal = $1.sVal; /* ignore port */ }
          ;

opt_port  : {}
          | port {}
          ;

port      : port_location {}
          | port_angle {}
          | port_angle port_location {}
          | port_location port_angle {}
          ;

port_location : ':' id {}
          | ':' '(' id ',' id ')' {}
          ;

id        : ID { $$.sVal = $1; }
          ;

port_angle : '@' id { $$.sVal = $2.sVal; }
          ;

%%   

    internal class Cell<T> {
		public Cell<T> left, right;
		public T value;
		public Cell() 
		{
		}
		public Cell(Cell<T> l, T v, Cell<T> r) 
		{
			left = l;
			right = r;
			value = v;
		}
		public T[] ToArray() {
			List<T> values = new List<T>();
			Walk(values);
			return values.ToArray();
		}
		private void Walk(List<T> values) {
			if (left != null) {
				left.Walk(values);
			}
			if (value != null) {
				values.Add(value);
			}
			if (right != null) {
				right.Walk(values);
			}

		}
	}

     void CreateNewCurrentSubgraph(string subgraphId) {
        var sg = new Subgraph(subgraphId);
        if (currentSubgraph == null)
            graph.RootSubgraph.AddSubgraph(sg);
        else
            currentSubgraph.AddSubgraph(sg);
        currentSubgraph = sg;
    }

    void PopCurrentSubgraph(){
	    currentSubgraph = currentSubgraph.ParentSubgraph;
        if (currentSubgraph == graph.RootSubgraph)
            currentSubgraph = null;
    }

    Cell<T> MkSingleton<T>(T s) { 
		return new Cell<T>(null, s, null);
	}

	Cell<T> Append<T>(Cell<T> x, Cell<T> y) {
		return new Cell<T>(x,default(T),y);
	}
   
    void MkEqStmt(string l, string r) {
        var couple = new Tuple<Microsoft.Msagl.Drawing.Label,GraphAttr>(new Microsoft.Msagl.Drawing.Label(), this.graph.Attr);
        var avPair = AttributeValuePair.CreateFromsStrings(l, r);
        AttributeValuePair.AddAttributeValuePair(couple, avPair);
    } 

    Cell<string> MkNodeStmt(string name, ArrayList attrs) {
        var node = AddNode(name);
        node.Attr.Shape = Shape.Ellipse;
        
        var couple = new Tuple<Microsoft.Msagl.Drawing.Label,NodeAttr>(new Microsoft.Msagl.Drawing.Label(node.Id), node.Attr);
        Microsoft.Msagl.Core.Layout.Node geomNode;
        couple = AttributeValuePair.AddNodeAttrs(couple, attrs, out geomNode);
        node.Label = couple.Item1;
        node.Label.Owner = node;
        node.Attr = couple.Item2;
        if (geomNode != null) {
            node.GeometryNode = geomNode;
            geomNode.UserData = node;
        }
        return MkSingleton(name);
    }

	Microsoft.Msagl.Drawing.Node AddNode(string name) {
        var node = graph.AddNode(name);
        if(currentSubgraph!=null)
            currentSubgraph.AddNode(node);
        return node;
    }

    void MkGraphAttrStmt(ArrayList attrs) {
        var couple = new Tuple<Microsoft.Msagl.Drawing.Label,GraphAttr>(graph.Label, graph.Attr);
        couple = AttributeValuePair.AddGraphAttrs(couple, attrs);
        graph.Label = couple.Item1;
        graph.Attr = couple.Item2;
    }

    void MkNodeAttrStmt(ArrayList al) {}
    void MkEdgeAttrStmt(ArrayList al) {}

   void MkEdgeStmt(string src, string dst, ArrayList attrs) { 
        if (src == null || src == "") src = " ";
        if (dst == null || dst == "") dst = " ";
        var edge = graph.AddEdge(src, dst);
        if (currentSubgraph != null) {
            currentSubgraph.AddNode(graph.FindNode(src));
            currentSubgraph.AddNode(graph.FindNode(dst));
        }
        AttributeValuePair.AddEdgeAttrs(attrs, edge);
    }

    void MkEdgeStmt(string src, Cell<string> dst, ArrayList attrs) {
        foreach(var d in dst.ToArray()) {
			MkEdgeStmt(src, d, attrs);
        }
    } 

    void MkEdgeStmt(Cell<string> src, Cell<string> dst, ArrayList attrs) {
        foreach(var s in src.ToArray()) {
          foreach(var d in dst.ToArray()) {
             MkEdgeStmt(s, d, attrs);
          }
        }
    } 

	Cell<string> MkEdgeStmt(Cell<string> src, Cell<Cell<string>> edges, ArrayList attrs) {
		Cell<string> result = src;
		foreach(var dst in edges.ToArray()) {
			MkEdgeStmt(src, dst, attrs);
			src = dst;
			result = new Cell<string>(result, null, dst);
		}
		return result;
	}

    AttributeValuePair MkAvPair(string src, string dst) {
       return AttributeValuePair.CreateFromsStrings(src, dst);
    }

    public Parser() : base(null) { }

    public static Graph Parse(System.IO.Stream reader, out int line, out int col, out string msg) {
        Parser parser = new Parser();    
		Scanner scanner = new Scanner(reader);                
        parser.Scanner = scanner;
		line = 0;
		col = 0;
		msg = "syntax error";
        try {
         if (parser.Parse()) {
              TryCreateGeometryGraph(parser.graph);
            return parser.graph;
          }
        }
        catch (Exception e) {
            parser.Scanner.yyerror(e.Message); 
        }
		line = scanner.Line;
		col  = scanner.Col;
		msg  = scanner.Message;
        return null;
    }
	  static void TryCreateGeometryGraph(Graph graph1) {
        if (!AllGeometryInPlace(graph1))
            return;
        CreateGeometryGraph(graph1);
    }

    static void CreateGeometryGraph(Graph graph1) {
        var geomGraph = graph1.GeometryGraph = new GeometryGraph();
        foreach (var n in graph1.Nodes)
            geomGraph.Nodes.Add(n.GeometryNode);

        foreach (var de in graph1.Edges) {
            var ge = de.GeometryEdge;
            ge.Source = de.SourceNode.GeometryNode;
            ge.Target = de.TargetNode.GeometryNode;
            geomGraph.Edges.Add(de.GeometryEdge);
            if (de.Label != null)
                ge.Label = de.Label.GeometryLabel;
        }
        geomGraph.UpdateBoundingBox();
       // geomGraph.Transform(GetTransformForDotSquashedGeometry());
            
    }

    static PlaneTransformation GetTransformForDotSquashedGeometry() {
        return new PlaneTransformation(72, 0, 0, 0, 72, 0);
    }

    static bool AllGeometryInPlace(Graph graph1) {
        return graph1.Nodes.All(n => n.GeometryNode != null) && graph1.Edges.All(e => e.GeometryEdge != null);
    }

	public static Graph Parse(string file, out int line, out int col, out string msg) {
		System.IO.Stream reader = null;
        reader = new System.IO.FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.Read);
        return Parser.Parse(reader, out line, out col, out msg);  
	}

/*

port : ':' ID [ ':' compass_pt ] 
     | ':' compass_pt 
subgraph : [ subgraph [ ID ] ] '{' stmt_list '}' 
compass_pt : (n | ne | e | se | s | sw | w | nw | c | _) 

The keywords node, edge, graph, digraph, subgraph, and strict are case-independent. 
Note also that the allowed compass point values are not keywords,
so these strings can be used elsewhere as ordinary identifiers and, conversely, 
the parser will actually accept any identifier. 

An ID is one of the following: 

Any string of alphabetic ([a-zA-Z\200-\377]) characters, underscores ('_') or digits ([0-9]), not beginning with a digit; 
a numeral [-]?(.[0-9]+ | [0-9]+(.[0-9]*)? ); 
any double-quoted string ("...") possibly containing escaped quotes (\")1; 
an HTML string (<...>). 
An ID is just a string; the lack of quote characters in the first two forms is just for simplicity. There is no semantic difference between abc_2 and "abc_2", or between 2.34 and "2.34". Obviously, to use a keyword as an ID, it must be quoted. Note that, in HTML strings, angle brackets must occur in matched pairs, and unescaped newlines are allowed. In addition, the content must be legal XML, so that the special XML escape sequences for ", &, <, and > may be necessary in order to embed these characters in attribute values or raw text. 
Both quoted strings and HTML strings are scanned as a unit, so any embedded comments will be treated as part of the strings. 

An edgeop is -> in directed graphs and -- in undirected graphs. 

An a_list clause of the form ID is equivalent to ID=true. 

The language supports C++-style comments: and //. In addition, a line beginning with a '#' character is considered a line output from a C preprocessor (e.g., # 34 to indicate line 34 ) and discarded. 

Semicolons aid readability but are not required except in the rare case that a named subgraph with no body immediately preceeds an anonymous subgraph, since the precedence rules cause this sequence to be parsed as a subgraph with a heading and a body. Also, any amount of whitespace may be inserted between terminals. 

As another aid for readability, dot allows single logical lines to span multiple physical lines using the standard C convention of a backslash immediately preceding a newline character. In addition, double-quoted strings can be concatenated using a '+' operator. As HTML strings can contain newline characters, they do not support the concatenation operator. 

*/
