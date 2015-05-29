namespace TestGraphmaps {
    internal class GraphmlEdge {
        readonly GraphmlNode _source;
        readonly GraphmlNode _target;

        internal GraphmlEdge(GraphmlNode source, GraphmlNode target) {
            _source = source;
            _target = target;
        }


        public GraphmlNode Source {
            get { return _source; }
        }

        public GraphmlNode Target {
            get { return _target; }
        }
    }
}