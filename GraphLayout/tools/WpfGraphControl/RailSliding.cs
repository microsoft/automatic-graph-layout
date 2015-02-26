/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
using System.Collections;
using System.Windows.Input;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Layout.LargeGraphLayout;

namespace Microsoft.Msagl.WpfGraphControl {
    internal class RailSliding {
        internal Rail _rail;
        internal Point _mouseDownPositionOnGraph;
        internal System.Windows.Point _mouseDownPositionOnScreen;
        internal bool _fromStartToEnd; //direction of sliding
        double _parameter;
        internal double _speed;
        ICurve _curve;
        internal double _length;
        bool _stuck;

        internal double Parameter {
            get { return _parameter; }
            set {_parameter = value;}
        }

        public ICurve Curve {
            get { return _curve; }
            set {_curve = value;}
        }

        internal Point GetPointOnRail(double par) {
           
            if (par > _curve.ParEnd)
                par = _curve.ParEnd;
            else if (par < _curve.ParStart)
                par = _curve.ParStart;
            return _curve[par];
        }

        internal Point Derivative() {
            return _curve.Derivative(_parameter);
        }

        public double GetNextPlusParOnSlidingRailWithStepLength(double stepLength) {
            var upper = _curve.ParEnd;
            var lower = _parameter;
            var ret = (upper + lower)/2;
            int n = 5;
            double len = _curve.LengthPartial(_parameter, ret);
            while (n >= 0 && Math.Abs(len - stepLength) > 0.01) {
                n--;
                if (len > stepLength) {
                    upper = ret;
                }
                else {
                    lower = ret;
                }
                ret = (upper + lower)/2;
                len = _curve.LengthPartial(_parameter, ret);
            }
            return ret;
        }

        public double GetNextMinusParOnSlidingRailWithStepLength(double stepLength) {
            var lower = _curve.ParStart;
            var upper = _parameter;
            var ret = (upper + lower)/2;
            int n = 5;
            double len = _curve.LengthPartial(ret, _parameter);
            while (n >= 0 && Math.Abs(len - stepLength) > 0.01) {
                n--;
                if (len > stepLength)
                    lower = ret;
                else
                    upper = ret;
                ret = (upper + lower)/2;
                len = _curve.LengthPartial(ret, _parameter);
            }
            return ret;
        }

        public bool IsStuck(System.Windows.Point mouseOnScreen) {
            if (_rail == null)
                return false;
            if (!(Math.Abs(_curve.ParStart - _parameter) < 0.01 || Math.Abs(_curve.ParEnd - _parameter) < 0.01))
                return false;

            var mouseDelta=new Point( mouseOnScreen.X - _mouseDownPositionOnScreen.X, -  mouseOnScreen.Y + _mouseDownPositionOnScreen.Y);

            var tangent = _curve.Derivative(Parameter);

            var product = mouseDelta*tangent;
            return product > 0 && Math.Abs(_curve.ParEnd - _parameter) < 0.01 ||
                   product < 0 && Math.Abs(_curve.ParStart - _parameter) < 0.01;

        }

        internal Point GetStuckPoint() {
            if (Math.Abs(_curve.ParEnd - _parameter) < 0.01)
                return _curve.End;
            return _curve.Start;
        }

        internal Point GetStuckPointDerivative() {
            if (Math.Abs(_curve.ParEnd - _parameter) < 0.01)
                return _curve.Derivative(_curve.ParEnd);
            return _curve.Derivative(_curve.ParStart);
        }

        internal Point GetCurrentPointOnRail() {
            return _curve[_parameter];
        }
    }
}