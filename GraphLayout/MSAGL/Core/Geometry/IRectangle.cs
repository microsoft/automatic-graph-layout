namespace Microsoft.Msagl.Core.Geometry {
    public interface IRectangle<P> {
        void Add(P point);
       
        bool Contains(P point);
        bool Contains(IRectangle<P> rect);
        IRectangle<P> Intersection(IRectangle<P> rectangle);
        bool Intersects(IRectangle<P> rectangle);
        void Add(IRectangle<P> b);
        double Area { get; }

        bool Contains(P p, double radius);
        IRectangle<P> Unite(IRectangle<P> rectangle);
    }
}