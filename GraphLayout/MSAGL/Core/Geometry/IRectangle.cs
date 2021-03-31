namespace Microsoft.Msagl.Core.Geometry {
    public interface IRectangle<P> {
        void Add(P point);
        void Add(IRectangle<P> rectangle);
        bool Contains(P point);
        bool Contains(IRectangle<P> rect);
        IRectangle<P> Intersection(IRectangle<P> rectangle);
        bool Intersects(IRectangle<P> rectangle);
        IRectangle<P> Unite(IRectangle<P> b);
        double Area { get; }
    }
}