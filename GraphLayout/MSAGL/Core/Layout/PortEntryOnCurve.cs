using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
#if TEST_MSAGL
using System.Diagnostics;
#endif

namespace Microsoft.Msagl.Core.Layout {
  ///<summary>
  ///</summary>
  public class PortEntryOnCurve : IPortEntry {
    Rectangle[] allowedRects;
    /// <summary>
    /// returns an enumeration of the rectangles that are allowed for routing
    /// </summary>
    public IEnumerable<Rectangle> AllowedRectangles {

      get {
        if (this.allowedRects == null) {
          this.allowedRects = this.Spans.Select(span => this.TrimEntryCurve(span).BoundingBox).ToArray();
        }
        return this.allowedRects;
      }
    }

    ICurve TrimEntryCurve(Tuple<double, double> span) {
      var start = span.Item1;
      var end = span.Item2;
      if (start < end) {
        return this.EntryCurve.Trim(start, end);
      }

      // For the classes that implement it, wrap the Trim.
      if ((this.EntryCurve is Polyline) || (this.EntryCurve is Curve) || (this.EntryCurve is RoundedRect)) {
        return this.EntryCurve.TrimWithWrap(start, end);
      }

      // Ellipse does not (yet) support TrimWithWrap but for our purposes we can deal with it as a Curve.
      if (this.EntryCurve is Ellipse) {
        var c = new Curve();
        c.AddSegment(this.EntryCurve.Trim(start, this.EntryCurve.ParEnd));
        c.AddSegment(this.EntryCurve.Trim(this.EntryCurve.ParStart, end));
        return c;
      }

      // For the remaining implementations of ICurve, this is what Trim does if start is greater than end, unless it throws.
      return this.EntryCurve.Trim(end, start);
    }

    /// <summary>
    /// paremeter spans
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
    public IEnumerable<Tuple<double, double>> Spans { get; private set; }
#if TEST_MSAGL
        readonly bool curveIsClosed;
#endif
    ///<summary>
    ///</summary>
    ///<param name="entryCurve"></param>
    ///<param name="parameterSpans"></param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
    public PortEntryOnCurve(ICurve entryCurve, IEnumerable<Tuple<double, double>> parameterSpans) {
      EntryCurve = entryCurve;
#if TEST_MSAGL
            var polyline = entryCurve as Polyline;
            curveIsClosed = (polyline != null) ? polyline.Closed : ApproximateComparer.Close(EntryCurve.Start, EntryCurve.End);
#endif
      Spans = parameterSpans;
#if TEST_MSAGL
            TestSpans(entryCurve, parameterSpans, curveIsClosed);
#endif
    }

#if TEST_MSAGL
        static void TestSpans(ICurve entryCurve, IEnumerable<Tuple<double, double>> parameterSpans, bool curveClosed) {
            foreach (var parameterSpan in parameterSpans)
                Debug.Assert(InParamDomain(entryCurve, parameterSpan, curveClosed));
        }

        static bool InParamDomain(ICurve entryCurve, Tuple<double, double> parameterSpan, bool curveClosed) {
            return InParamDomain(entryCurve, parameterSpan.Item1) && InParamDomain(entryCurve, parameterSpan.Item2) &&
                   (parameterSpan.Item1 <= parameterSpan.Item2 || curveClosed);
        }

        static bool InParamDomain(ICurve entryCurve,  double parameter) {
            return entryCurve.ParStart <= parameter && entryCurve.ParEnd >= parameter;
        }
#endif
    internal ICurve EntryCurve { get; set; }

    /// <summary>
    /// returns the points uniformly distributed over the entries
    /// </summary>
    /// <returns></returns>      
    public IEnumerable<Point> GetEntryPoints() {
      return Spans.SelectMany(SpanPoints);
    }

    IEnumerable<Point> SpanPoints(Tuple<double, double> span) {
      if (span.Item1 == EntryCurve.ParStart && span.Item2 == EntryCurve.ParEnd ||
          span.Item2 == EntryCurve.ParStart && span.Item1 == EntryCurve.ParEnd)
        return new[] { MiddlePoint(EntryCurve) };
      var poly = EntryCurve as Polyline;
      if (poly != null)
        return SpanPointsFromPolyline(poly, span);

      // If the EntryCurve is of a type that may contain multiple segments, get their midpoints.
      if ((this.EntryCurve is Polyline) || (this.EntryCurve is Curve) || (this.EntryCurve is RoundedRect)) {
        var trimmedCurve = this.TrimEntryCurve(span) as Curve;
        if (trimmedCurve != null) {
          return SpanPointsFromCurveSegments(trimmedCurve);
        }
      }

      // Otherwise we just take the midpoint of the span.  Ellipse could be made smart enough here to
      // get a midpoint for each axis it faces.
      return new[] { EntryCurve[MiddleOfSpan(span)] };
    }

    static Point MiddlePoint(ICurve c) {
      return c[(c.ParStart + c.ParEnd) / 2];
    }

    double MiddleOfSpan(Tuple<double, double> span) {
      if (span.Item1 < span.Item2) return 0.5 * (span.Item1 + span.Item2);
      var halfLen = GetSpanLength(span) / 2;
      var t = span.Item1 + halfLen;
      return t <= EntryCurve.ParEnd ? t : t - EntryCurve.ParStart;
    }

    static IEnumerable<Point> SpanPointsFromCurveSegments(Curve curve) {
      return curve.Segments.Select(MiddlePoint);
    }

    static IEnumerable<Point> SpanPointsFromPolyline(Polyline poly, Tuple<double, double> span) {
      var trimmedPoly = (Polyline)poly.TrimWithWrap(span.Item1, span.Item2);
      for (var p = trimmedPoly.StartPoint; p.Next != null; p = p.Next)
        yield return (p.Point + p.Next.Point) / 2;
    }

    double GetSpanLength(Tuple<double, double> span) {
      return span.Item1 < span.Item2 ? span.Item2 - span.Item1 : EntryCurve.ParEnd - span.Item1 + span.Item2;
    }
  }
}