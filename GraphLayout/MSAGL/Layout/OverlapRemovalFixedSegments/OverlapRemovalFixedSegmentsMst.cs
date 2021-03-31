using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.GraphAlgorithms;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Layout.ProximityOverlapRemoval.MinimumSpanningTree;
using Microsoft.Msagl.Routing;
using Microsoft.Msagl.Routing.ConstrainedDelaunayTriangulation;
using System;
using System.Collections.Generic;
using System.Linq;
using SymmetricSegment = Microsoft.Msagl.Core.DataStructures.SymmetricTuple<Microsoft.Msagl.Core.Geometry.Point>;
namespace Microsoft.Msagl.Layout.OverlapRemovalFixedSegments
{

    public enum SiteType { CenterBoxMoveable, CenterBoxFixed, PointOnSegment }

    public class OverlapRemovalFixedSegmentsMst
    {
        public int time = 0;

        Rectangle[] moveableRectangles;
        Rectangle[] fixedRectangles;
        SymmetricSegment[] fixedSegments;

        Point[] oldPositionsMoveable;

        String[] nodeLabelsMoveable;
        String[] nodeLabelsFixed;


        Cdt cdt;

        Dictionary<Point, TreeNode> pointToTreeNode = new Dictionary<Point, TreeNode>();

        RTree<Segment, Point> _segmentTree = new RTree<Segment, Point>();

        RTree<TreeNode, Point> _moveableRectanglesTree = new RTree<TreeNode, Point>();

        RTree<TreeNode, Point> _fixedRectanglesTree = new RTree<TreeNode, Point>();

        RTree<TreeNode, Point> _rectNodesRtree = new RTree<TreeNode, Point>();

        Dictionary<TreeNode, List<TreeNode>> movedCriticalNodes = new Dictionary<TreeNode, List<TreeNode>>();

        Dictionary<TreeNode, List<TreeNode>> oldOverlapsBoxes = new Dictionary<TreeNode, List<TreeNode>>();

        Dictionary<TreeNode, List<Segment>> oldOverlapsSegments = new Dictionary<TreeNode, List<Segment>>();

        List<List<TreeNode>> _subtrees = new List<List<TreeNode>>();
 
        List<TreeNode> _roots = new List<TreeNode>();

         List<SymmetricSegment> treeSegments; 

        const int Precision = 5;
         const double PrecisionDelta = 1e-5;

         double EdgeContractionFactor = 0.1;
         double EdgeExpansionFactor = 1.1;
         double BoxSegmentOverlapShiftFactor = 1.1;

        public class Segment
        {
            public int segmentId;
             Point point1;
             Point point2;

            public Segment(Point point1, Point point2)
            {
                this.point1 = point1;
                this.point2 = point2;
            }
            public Point p1 { get { return point1; } }
            public Point p2 { get { return point2; } }
        }

        public enum SiteType
        {
            RectFixed,
            RectMoveable,
            AdditionalPointBoxSegmentOverlap
        };

        public class TreeNode
        {
            public static int numNodes = 0;

            public Point shiftToRoot = new Point(0, 0);
            public Set<TreeNode> neighbors = new Set<TreeNode>();
            public int visited = 0;
            public TreeNode parent = null;
            public bool isFixed = false;
            public Rectangle rect;
            public int id;
            public int rectId;
            public string label;

            public Segment segment;

            public Point sitePoint;

            public SiteType type;

            public TreeNode(){
                id = numNodes++;
            }

            public override string ToString()
            {
                return (label != null ? label.ToString() : "null"); // +", " + sitePoint.ToString() + ", " + rect.ToString();
            }
        }


        Point Round(Point p) {
            return ApproximateComparer.Round(p, Precision);
        }

        public void SetNodeLabels(String[] labelsMovealbe, String[] labelsFixed) {
            nodeLabelsMoveable = labelsMovealbe;
            nodeLabelsFixed = labelsFixed;
        }

        public void SaveOldPositionsMoveable() {
            oldPositionsMoveable = new Point[moveableRectangles.Length];

            for(int i=0; i<moveableRectangles.Length; i++)
            {
                oldPositionsMoveable[i] = moveableRectangles[i].Center;
            }
        }

        public Point[] GetTranslations() {            
            var nodes = _moveableRectanglesTree.GetAllLeaves();
            Point[] translation = new Point[_moveableRectanglesTree.Count];

            foreach (var n in nodes)
            {
                translation[n.rectId] = n.rect.Center - oldPositionsMoveable[n.rectId];
            }
            return translation;
        }

        public OverlapRemovalFixedSegmentsMst(Rectangle[] moveableRectangles, Rectangle[] fixedRectangles, SymmetricSegment[] fixedSegments)
        {
            TreeNode.numNodes = 0;
            this.moveableRectangles = moveableRectangles;
            this.fixedRectangles = fixedRectangles;
            this.fixedSegments = fixedSegments;

            SaveOldPositionsMoveable();
        }

        public void Init()
        {
            InitCdt();
            InitTree();
        }

        public void InitCdt() {

            InitSegmentTree();

            for (int i = 0; i < fixedRectangles.Length; i++) {
                Point p = Round( fixedRectangles[i].Center );
                var node = new TreeNode { isFixed = true, rectId = i, rect = fixedRectangles[i], sitePoint = p, type = SiteType.RectFixed };
                if (nodeLabelsFixed != null)
                {
                    node.label = nodeLabelsFixed[i];
                }
                pointToTreeNode[p] = node;
                _rectNodesRtree.Add(node.rect, node);
                _fixedRectanglesTree.Add(fixedRectangles[i], node);
            }

            for (int i = 0; i < moveableRectangles.Length; i++)
            {
                Point p = Round(moveableRectangles[i].Center);
                var node = new TreeNode { isFixed = false, rectId = i, rect = moveableRectangles[i], sitePoint = p, type = SiteType.RectMoveable };
                if (nodeLabelsMoveable != null)
                {
                    node.label = nodeLabelsMoveable[i];
                }

                pointToTreeNode[p] = node;
                _rectNodesRtree.Add(node.rect, node);
                _moveableRectanglesTree.Add(moveableRectangles[i], node);
            }

            var sites = pointToTreeNode.Keys.ToList();

            //AddSitesForBoxSegmentOverlaps(sites);            

            cdt = new Cdt(sites, null, null);
            cdt.Run();
        }

         void AddNodesForBoxSegmentOverlaps(out List<Point> sites, out List<SymmetricSegment> edges)
        {
            sites = new List<Point>();
            edges = new List<SymmetricSegment>();

            foreach (var rect in moveableRectangles)
            {
                List<Segment> segments = GetAllSegmentsIntersecting(rect);
                if (!segments.Any()) continue;

                var seg = segments.First();
                //foreach (var seg in segments)
                //{
                    Point p;
                    Point pClosestOnSeg = RectSegIntersection.ClosestPointOnSegment(seg.p1, seg.p2, rect.Center);
                    
                    // if too close
                    if ((pClosestOnSeg - rect.Center).Length < 10*PrecisionDelta)
                    {
                        Point d = (seg.p2 - seg.p2).Rotate90Ccw();
                        Point delta = 0.5*GetShiftUntilNoLongerOverlapRectSeg(rect, seg, d);
                        p = Round(rect.Center + delta);
                    }
                    else
                    {
                        p = Round(0.5 * (rect.Center + pClosestOnSeg));
                    }
                                        
                    TreeNode node = new TreeNode { isFixed = false, sitePoint = p, type = SiteType.AdditionalPointBoxSegmentOverlap, segment = seg, rect = rect, label = "SegOvlp "};
                    if (!pointToTreeNode.ContainsKey(p)) {
                        sites.Add(p);
                        pointToTreeNode[p] = node;
                        edges.Add(new SymmetricSegment(p, Round(rect.Center)) );
                    }

                    TreeNode box = pointToTreeNode[Round(rect.Center)];
                    if(box.type == SiteType.RectMoveable) node.label += box.label;
                //}
            }
        }

         List<Segment> GetAllSegmentsIntersecting(Rectangle rect)
        {
            var segmentsIntersecting = new List<Segment>();
            var boxIntersecting = _segmentTree.GetAllIntersecting(rect);
            foreach(var seg in boxIntersecting)
            {
                if(RectSegIntersection.Intersect(rect, seg.p1, seg.p2))
                    segmentsIntersecting.Add(seg);
            }
            return segmentsIntersecting;
        }

        public void InitSegmentTree() {
            for (int i = 0; i < fixedSegments.Length; i++) {
                Segment seg = new Segment(fixedSegments[i].A, fixedSegments[i].B);
                var bbox = new Rectangle(seg.p1, seg.p2);
                _segmentTree.Add(bbox, seg);
            }
        }

        public List<SymmetricSegment> GetMstFromCdt() {
            Func<CdtEdge, double> weights = GetWeightOfCdtEdgeDefault;//GetWeightOfCdtEdgeDefault;// GetWeightOfCdtEdgePenalizeFixed; //GetWeightOfCdtEdgePenalizeSegmentCrossings ;
            var mstEdges = MstOnDelaunayTriangulation.GetMstOnCdt(cdt, weights);
            return (from e in mstEdges select new SymmetricSegment(e.upperSite.Point, e.lowerSite.Point)).ToList();
        }

        public void BuildForestFromCdtEdges(List<SymmetricSegment> edges) {
            foreach (var edge in edges) {
                Point p1 = edge.A;
                Point p2 = edge.B;
                AddTreeEdge(p1, p2);
            }
        }

         void AddTreeEdge(Point p1, Point p2)
        {
            var n1 = pointToTreeNode[p1];
            var n2 = pointToTreeNode[p2];
            n1.neighbors.Insert(n2);
            n2.neighbors.Insert(n1);
        }

         bool RectsOverlap(TreeNode n1, TreeNode n2) {
            return !Rectangle.Intersect(n1.rect, n2.rect).IsEmpty;
        }

        public double GetWeightOfCdtEdgeDefault(CdtEdge e) {
            Point point1 = Round(e.upperSite.Point);
            Point point2 = Round(e.lowerSite.Point);
            TreeNode n1 = pointToTreeNode[point1];
            TreeNode n2 = pointToTreeNode[point2];

            if (n1.type == SiteType.AdditionalPointBoxSegmentOverlap ||
                n2.type == SiteType.AdditionalPointBoxSegmentOverlap)
                return -Math.Max(n1.rect.Diagonal, n2.rect.Diagonal); // todo: better values? should be very small

            Rectangle box1 = n1.rect;
            Rectangle box2 = n2.rect;

            double t;

            if (!Rectangle.Intersect(box1, box2).IsEmpty)
                return GetWeightOverlappingRectangles(box1, box2, out t);

            return GetDistance(box1, box2);
        }

        public double GetWeightOfCdtEdgePenalizeFixed(CdtEdge e)
        {
            Point point1 = Round(e.upperSite.Point);
            Point point2 = Round(e.lowerSite.Point);
            TreeNode n1 = pointToTreeNode[point1];
            TreeNode n2 = pointToTreeNode[point2];

            Rectangle box1 = n1.rect;
            Rectangle box2 = n2.rect;

            bool overlap = Rectangle.Intersect(box1, box2).IsEmpty;
            double t;

            if (!Rectangle.Intersect(box1, box2).IsEmpty)
                return GetWeightOverlappingRectangles(box1, box2, out t);

            double factor = (n1.isFixed || n2.isFixed ? 10 : 1);

            return factor * GetDistance(box1, box2);
        }

        public double GetWeightOfCdtEdgePenalizeSegmentCrossings(CdtEdge e)
        {
            Point point1 = Round(e.upperSite.Point);
            Point point2 = Round(e.lowerSite.Point);
            TreeNode n1 = pointToTreeNode[point1];
            TreeNode n2 = pointToTreeNode[point2];

            Rectangle box1 = n1.rect;
            Rectangle box2 = n2.rect;

            bool overlap = Rectangle.Intersect(box1, box2).IsEmpty;
            double t;

            if (!Rectangle.Intersect(box1, box2).IsEmpty)
                return GetWeightOverlappingRectangles(box1, box2, out t);

            double factor = ( IsCrossedBySegment(new SymmetricSegment(point1, point2)) ? 3 : 1 );

            return factor * GetDistance(box1, box2);
        }

        public double GetWeightOverlappingRectangles(Rectangle box1, Rectangle box2, out double t) {

            Point point1 = box1.Center;
            Point point2 = box2.Center;

            double dist = (point1 - point2).Length;
            double dx = Math.Abs(point1.X - point2.X);
            double dy = Math.Abs(point1.Y - point2.Y);

            double wx = (box1.Width / 2 + box2.Width / 2);
            double wy = (box1.Height / 2 + box2.Height / 2);

            const double machineAcc = 1.0e-16;

            //double t;
            if (dx < machineAcc * wx)
            {
                t = wy / dy;
            }
            else if (dy < machineAcc * wy)
            {
                t = wx / dx;
            }
            else
            {
                t = Math.Min(wx / dx, wy / dy);
            }

            //if (t > 1) t = Math.Max(t, 1.001); // must be done, otherwise the convergence is very slow
            ////            tmax = Math.Max(tmax, t);
            ////            tmin = Math.Min(tmin, t);
            //t = Math.Min(expandMax, t);
            //t = Math.Max(expandMin, t);
            //tRes = t;
            //return t * dist;
            return - (t-1)*dist;
        }

         double getT(Rectangle box1, Rectangle box2) {
            Point point1 = box1.Center;
            Point point2 = box2.Center;

            double dist = (point1 - point2).Length;
            double dx = Math.Abs(point1.X - point2.X);
            double dy = Math.Abs(point1.Y - point2.Y);

            double wx = (box1.Width / 2 + box2.Width / 2);
            double wy = (box1.Height / 2 + box2.Height / 2);

            const double machineAcc = 1.0e-16;

            double t;
            if (dx < machineAcc * wx)
            {
                t = wy / dy;
            }
            else if (dy < machineAcc * wy)
            {
                t = wx / dx;
            }
            else
            {
                t = Math.Min(wx / dx, wy / dy);
            }
            return t;
        }

        /// <summary>
        /// vector to shift box2 along the line between the centers until overlap
        /// </summary>
        /// <param name="box1"></param>
        /// <param name="box2"></param>
        /// <returns></returns>
        public Point GetShiftUntilOverlap(Rectangle box1, Rectangle box2) {
            Point point1 = box1.Center;
            Point point2 = box2.Center;

            double t = getT(box1, box2);

            return (1 - t) * (point1 - point2);
        }

        /// <summary>
        /// vector to shift box2 along the line between the centers until no longer overlap
        /// </summary>
        /// <param name="box1"></param>
        /// <param name="box2"></param>
        /// <returns></returns>
        public Point GetShiftUntilNoLongerOverlap(Rectangle box1, Rectangle box2)
        {
            Point point1 = box1.Center;
            Point point2 = box2.Center;

            double t = getT(box1, box2);

            return (t - 1) * (point2 - point1);
        }

        public double GetDistance(Rectangle rect1, Rectangle rect2) {
            if (!Rectangle.Intersect(rect1, rect2).IsEmpty) return 0;

            Rectangle leftmost = rect1.Left <= rect2.Left ? rect1 : rect2;
            Rectangle notLeftmost = rect1.Left <= rect2.Left ? rect2 : rect1;

            Rectangle botommost = rect1.Bottom <= rect2.Bottom ? rect1 : rect2;
            Rectangle notBotommost = rect1.Bottom <= rect2.Bottom ? rect2 : rect1;

            double dx = notLeftmost.Left - leftmost.Right;
            double dy = notBotommost.Bottom - botommost.Top;

            if(rect1.IntersectsOnX(rect2)) return dy;
            if (rect1.IntersectsOnY(rect2)) return dx;

            return Math.Sqrt(dx * dx + dy * dy);
        }

        public Rectangle GetInitialBoundingBox() {
            Rectangle bbox = new Rectangle();
            bbox.SetToEmpty();
            foreach (var rect in fixedRectangles) {
                bbox.Add(rect);
            }
            foreach (var rect in moveableRectangles)
            {
                bbox.Add(rect);
            }
            return bbox;
        }

        public Rectangle GetBoundingBox(IEnumerable<TreeNode> nodes)
        {
            Rectangle bbox = new Rectangle();
            bbox.SetToEmpty();
            foreach (var node in nodes)
            {
                bbox.Add(node.rect);
            }
            return bbox;
        }

        public Point GetClosestSiteTo(Point p) {
            double dist = Double.MaxValue;
            Point closest = pointToTreeNode.Keys.First();
            foreach (var s in pointToTreeNode.Keys)
            {
                double dist1 = (p - s).LengthSquared;
                if (dist1 < dist) {
                    dist = dist1;
                    closest = s;
                }
            }
            return closest;
        }

        public Point GetClosestSiteTo(List<TreeNode> nodes, Point p)
        {
            double dist = Double.MaxValue;
            Point closest = pointToTreeNode.Keys.First();
            foreach (var node in nodes)
            {
                Point s = node.sitePoint;
                double dist1 = (p - s).LengthSquared;
                if (dist1 < dist)
                {
                    dist = dist1;
                    closest = s;
                }
            }
            return closest;
        }

        public Point GetClosestSiteToPreferFixed(List<TreeNode> nodes, Point p)
        {
            double dist = Double.MaxValue;
            Point closest = pointToTreeNode.Keys.First();
            foreach (var node in nodes)
            {
                Point s = node.sitePoint;
                double dist1 = (p - s).LengthSquared;

                if (node.isFixed)
                    dist1 /= 4;                

                if (dist1 < dist)
                {
                    dist = dist1;
                    closest = s;
                }
            }
            return closest;
        }

        public TreeNode GetRandom(List<TreeNode> nodes)
        {
            Random rand = new Random(1);
            int i = rand.Next(0, nodes.Count);
            return nodes[i];
        }

         void OrientTreeEdges(TreeNode root) {
            List<TreeNode> L = new List<TreeNode>();
            time++;
            root.visited = time;
            root.parent = null;
            L.Add(root);

            while (L.Any())
            {
                TreeNode p = L[L.Count() - 1];
                L.RemoveAt(L.Count() - 1);
                foreach (var neighb in p.neighbors) {
                    if (neighb.visited < time) {
                        neighb.parent = p;
                        neighb.visited = time;
                        L.Add(neighb);
                    }
                }
            }
        }

        public void PrecomputeMovedCriticalNodes(TreeNode n)
        {
            if (movedCriticalNodes.ContainsKey(n))
                return; // never happens

            movedCriticalNodes[n] = new List<TreeNode>();
                            
            foreach(var neighb in n.neighbors){
                if (neighb != n.parent)
                {
                    PrecomputeMovedCriticalNodes(neighb);
                    if (!n.isFixed)
                    {
                        movedCriticalNodes[n].AddRange(movedCriticalNodes[neighb]);
                    }
                }
            }
            //int numOverlaps = _nodesRtree.GetAllIntersecting(n.rect).Count();
            //if(!n.isFixed && numOverlaps > 1)
            if (!n.isFixed && (oldOverlapsBoxes.ContainsKey(n) ))
            {
                movedCriticalNodes[n].Add(n);
            }
        }

        public void PrecomputeMovedCriticalNodesBoxesSegments(TreeNode n)
        {
            if (movedCriticalNodes.ContainsKey(n))
                return; // never happens

            movedCriticalNodes[n] = new List<TreeNode>();

            foreach (var neighb in n.neighbors)
            {
                if (neighb != n.parent)
                {
                    PrecomputeMovedCriticalNodesBoxesSegments(neighb);
                    if (!n.isFixed)
                    {
                        movedCriticalNodes[n].AddRange(movedCriticalNodes[neighb]);
                    }
                }
            }
            if (n.type == SiteType.RectMoveable && (oldOverlapsBoxes.ContainsKey(n) || oldOverlapsSegments.ContainsKey(n)))
            {
                movedCriticalNodes[n].Add(n);
            }
        }

        public void MoveSubtree(TreeNode n, Point delta) {
            if (n.isFixed) return;
            foreach (var neighb in n.neighbors)
            {
                if (neighb != n.parent)
                {
                    MoveSubtree(neighb, delta);
                }
            }
            n.rect = translate(n.rect, delta);            
        }

        public void PrecomputeMovedCriticalNodesRoot(TreeNode root)
        {
            foreach (var n in root.neighbors)
            {
                PrecomputeMovedCriticalNodes(n);
            }
        }

        public void PrecomputeMovedCriticalNodesBoxesSegmentsRoot(TreeNode root)
        {
            foreach (var n in root.neighbors)
            {
                PrecomputeMovedCriticalNodesBoxesSegments(n);
            }
        }

        public void PrecomputeMovedCriticalNodesBoxesSegmentsAllRoots()
        {
            foreach (var root in _roots)
            {
                PrecomputeMovedCriticalNodesBoxesSegmentsRoot(root);
            }
        }

        public void PrecomputeMovedCriticalNodesAllRoots()
        {
            foreach (var root in _roots)
            {
                PrecomputeMovedCriticalNodesRoot(root);
            }
        }

        public void GetDfsOrder(TreeNode node, List<TreeNode> nodes) {
            foreach (var neighb in node.neighbors) {
                if (neighb != node.parent) {
                    nodes.Add(neighb);
                    GetDfsOrder(neighb, nodes);
                }
            }
        }

         void SaveCurrentOverlapsBoxes() {
            // assume all Rtrees are updated

            oldOverlapsBoxes = new Dictionary<TreeNode, List<TreeNode>>();
            foreach (var v in _moveableRectanglesTree.GetAllLeaves())
            {
                var vOverlaps = _rectNodesRtree.GetAllIntersecting(v.rect);
                if (vOverlaps.Count() <= 1)
                    continue;
                var vOverlapsList = new List<TreeNode>();
                foreach (var u in vOverlaps)
                {
                    if (u != v)
                        vOverlapsList.Add(u);
                }
                if (vOverlapsList.Any()) oldOverlapsBoxes.Add(v, vOverlapsList);
            }        
        }

         void SaveCurrentOverlapsSegments() {
            oldOverlapsSegments = new Dictionary<TreeNode, List<Segment>>();
            foreach (var v in _moveableRectanglesTree.GetAllLeaves())
            {
                var vOverlaps = _segmentTree.GetAllIntersecting(v.rect);
                if (!vOverlaps.Any())
                    continue;
                var vOverlapsList = new List<Segment>();
                foreach (var s in vOverlaps)
                {
                    if(RectSegIntersection.Intersect(v.rect, s.p1, s.p2))
                        vOverlapsList.Add(s);
                }
                if (vOverlapsList.Any()) oldOverlapsSegments.Add(v, vOverlapsList);
            }  
        }

        public void SaveCurrentOverlapsBoxesSegments() {
            SaveCurrentOverlapsBoxes();
            SaveCurrentOverlapsSegments();
        }

        public void DoOneIterationBoxesSegmentsAllRoots()
        {
            SaveCurrentOverlapsBoxesSegments();
            PrecomputeMovedCriticalNodesBoxesSegmentsAllRoots();

            foreach (var root in _roots)
            {
                DoOneIterationBoxesSegments(root);
            }
        }

        public void DoOneIterationBoxesSegments(TreeNode root)
        {
            //SaveCurrentOverlapsBoxesSegments(); // do once 

            List<TreeNode> dfsOrderedNodes = new List<TreeNode>();
            GetDfsOrder(root, dfsOrderedNodes); //doesn't contain root

            foreach (var v in dfsOrderedNodes)
            {
                var vMovesCritical = movedCriticalNodes[v];
                if (!vMovesCritical.Any()) continue;

                Point delta;

                if (v.type == SiteType.AdditionalPointBoxSegmentOverlap)
                {
                    HandleCaseAdditionalPointOvlp(v, out delta);
                }
                else// if (v.type == SiteType.RectMoveable)
                {
                    HandleCaseRectMoveable(v, out delta);
                }

                double a1 = GetAreaOldOverlapsBoxesSegments(vMovesCritical);

                // shift and compare
                translateAllRects(vMovesCritical, delta);
                double a2 = GetAreaOldOverlapsBoxesSegments(vMovesCritical);
                translateAllRects(vMovesCritical, -delta);

                ////test: small chance of extanding edge instead of contracting
                //if (contractingEdge && a1 <= a2)
                //{
                //    Random rnd = new Random(1);
                //    int r = rnd.Next(0, 100);
                //    if (r < 50)
                //    {
                //        delta = -delta;
                //        translateAllRects(vMovesCritical, delta);
                //        a2 = GetAreaOldOverlapsBoxesSegments(vMovesCritical);
                //        translateAllRects(vMovesCritical, -delta);
                //    }
                //}
                /////////////////////////////////

                if (a1 <= a2) continue;

                MoveSubtree(v, delta);
            }
        }

         void HandleCaseRectMoveable(TreeNode v, out Point delta)
        {
            if (v.parent.type == SiteType.RectFixed || v.parent.type == SiteType.RectMoveable)
            {
                if (RectsOverlap(v, v.parent))
                {
                    delta = EdgeExpansionFactor*GetShiftUntilNoLongerOverlap(v.parent.rect, v.rect);
                }
                else
                {
                    delta = EdgeContractionFactor*GetShiftUntilOverlap(v.parent.rect, v.rect);
                }
            }
            else //if (v.parent.type == SiteType.AdditionalPointBoxSegmentOverlap)
            {
                Point d = v.sitePoint - v.parent.sitePoint;
                delta = BoxSegmentOverlapShiftFactor*GetShiftUntilNoLongerOverlapRectSeg(v.rect, v.parent.segment, d);
            }
        }

         Point GetShiftUntilNoLongerOverlapRectSeg(Rectangle rect, Segment segment, Point moveDir)
        {
            return RectSegIntersection.GetOrthShiftUntilNoLongerOverlapRectSeg(rect, segment.p1, segment.p2, moveDir);
        }

         void HandleCaseAdditionalPointOvlp(TreeNode v, out Point delta)
        {
            // v should be assigned the corresponding rectangle
            if (RectsOverlap(v, v.parent))
            {
                delta = EdgeExpansionFactor * GetShiftUntilNoLongerOverlap(v.parent.rect, v.rect);
            }
            else
            {
                delta = EdgeContractionFactor * GetShiftUntilOverlap(v.parent.rect, v.rect);
            }
        }

         double GetAreaOldOverlapsBoxesSegments(List<TreeNode> vMovesCritical)
        {
            double a = 0;
            foreach (var u in vMovesCritical)
            {
                if (oldOverlapsBoxes.ContainsKey(u))
                {
                    var uOverlaps = oldOverlapsBoxes[u];
                    foreach (var w in uOverlaps)
                    {
                        var rect = Rectangle.Intersect(u.rect, w.rect);
                        if (!rect.IsEmpty) a += rect.Area;
                    }
                }
                if (oldOverlapsSegments.ContainsKey(u)) {
                    var uOverlaps = oldOverlapsSegments[u];
                    foreach (var seg in uOverlaps)
                    {
                        a += RectSegIntersection.GetOverlapAmount(u.rect, seg.p1, seg.p2);
                    }
                }
            }
            return a;
        }

         Rectangle translate(Rectangle rect, Point delta) {
            return new Rectangle(rect.LeftBottom + delta, rect.RightTop + delta);
        }

         void translateAllRects(List<TreeNode> nodes, Point delta)
        {
            foreach (var v in nodes) {
                v.rect = translate(v.rect, delta);
            }
        }


        public List<SymmetricSegment> GetEdgesWithoutSegmentCrossings(List<SymmetricSegment> edges)
        {
            List<SymmetricSegment> edgesLeft = new List<SymmetricSegment>();
            foreach (var edge in edges)
            {
                if (!IsCrossedBySegment(edge))
                    edgesLeft.Add(edge);
            }
            return edgesLeft;
        }

        public void InitConnectedComponents(List<SymmetricSegment> edges)
        {
            var treeNodes = new TreeNode[pointToTreeNode.Count];
            foreach (var node in pointToTreeNode.Values)
            {
                treeNodes[node.id] = node;
            }

            var intEdges = new List<SimpleIntEdge>();
            foreach (var edge in edges)
            {
                int sourceId = pointToTreeNode[edge.A].id;
                int targetId = pointToTreeNode[edge.B].id;
                intEdges.Add(new SimpleIntEdge { Source = sourceId, Target = targetId });                              
            }
            var components = ConnectedComponentCalculator<SimpleIntEdge>.GetComponents(new BasicGraphOnEdges<SimpleIntEdge>(intEdges, pointToTreeNode.Count));

            foreach (var component in components)
            {
                List<TreeNode> nodeList = new List<TreeNode>();
                foreach (var nodeId in component)
                {
                    nodeList.Add(treeNodes[nodeId]);
                }
                _subtrees.Add(nodeList);
            }

            //ChooseRoots();
            ChooseRootsRandom();

            BuildForestFromCdtEdges(edges);

            foreach (var root in _roots)
            {
                OrientTreeEdges(root);
            }
        }

        public void ChooseRoots()
        {
            foreach (var subtree in _subtrees)
            {
                var bbox = GetBoundingBox(subtree);
                var p = GetClosestSiteToPreferFixed(subtree, bbox.Center);
                _roots.Add(pointToTreeNode[p]);
            }
        }

        public void ChooseRootsRandom()
        {
            foreach (var subtree in _subtrees)
            {
                bool rootChosen = false;
                foreach (var n in subtree)
                {
                    if (n.type == SiteType.AdditionalPointBoxSegmentOverlap)
                    {
                        _roots.Add(n);
                        rootChosen = true;
                        break;
                    }
                }
                if (!rootChosen)
                {
                    var n = GetRandom(subtree);
                    _roots.Add(n);
                }
            }
        }

         bool IsCrossedBySegment(SymmetricSegment edge)
        {
            var intersectingSegments = _segmentTree.GetAllIntersecting(new Rectangle(edge.A, edge.B));
            bool hasIntersection = false;
            foreach (var seg in intersectingSegments)
            {
                if (RectSegIntersection.SegmentsIntersect(edge.A, edge.B, seg.p1, seg.p2))
                {
                    hasIntersection = true;
                    break;
                }
            }
            return hasIntersection;
        }

        public void InitTree()
        {
            var mstEdges = GetMstFromCdt();

            List<SymmetricSegment> treeEdges = GetEdgesWithoutSegmentCrossings(mstEdges);

            List<Point> addSites;
            List<SymmetricSegment> addEdges;

            AddNodesForBoxSegmentOverlaps(out addSites, out addEdges);

            treeEdges.AddRange(addEdges);

            InitConnectedComponents(treeEdges);

            treeSegments = treeEdges;
        }

        public List<SymmetricSegment> GetTreeEdges()
        {
            return treeSegments;
        }
    }
}
