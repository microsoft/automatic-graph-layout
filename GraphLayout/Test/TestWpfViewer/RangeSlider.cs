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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Msagl.Layout.LargeGraphLayout;
using Microsoft.Msagl.WpfGraphControl;
namespace TestWpfViewer {
    public class RangeSlider {
        DispatcherTimer repeatTimer;
        public EventHandler RangeChanged;

        void SetVisualX(UIElement t, double x) {
            Canvas.SetLeft(t, x);
        }
        double GetVisualX(UIElement t) {
            return Canvas.GetLeft(t);
        }
        public double Low {
            get { return FromCanvasToRange(GetVisualX(lowThumb)+lowThumb.Width); }
            set {
                var x = FromRangeToCanvas(value);
                Debug.Assert(x>=minButton.Width+lowThumb.Width && x<=canvas.Width-maxButton.Width-highThumb.Width);
                var del = x - (GetVisualX(lowThumb) - lowThumb.Width);
                SetVisualX(lowThumb, x-lowThumb.Width); 
                SetVisualX(highThumb,x);
                AdjustMediumThumb();
                if (High < Low)
                    High = Low;
            }
        }

        double FromRangeToCanvas(double x) {
            if (Maximum == Minimum)
                return 0;
            if (x < Minimum)
                x = Minimum;
            else if (x > Maximum)
                x = Maximum;
            var k = (canvas.Width - WidthOfEverythingWithoutMiddleThumb())/(Maximum - Minimum);
            var shift = minButton.Width + lowThumb.Width - k*Minimum;
            return k*x + shift;
        }

        double WidthOfEverythingWithoutMiddleThumb() {
            return 4*bWidth;
        }

        double FromCanvasToRange(double x) {
            var range = canvas.Width - WidthOfEverythingWithoutMiddleThumb();
            var domain = Maximum - Minimum;
            var k = domain/range;
            var shift = Minimum - k*2*bWidth;
            return k*x + shift;
        }

        public double High {
            get { return FromCanvasToRange(GetVisualX(highThumb)); }
            set { 
                var currentHighThX = GetVisualX(highThumb);
                var x = FromRangeToCanvas(value);
                if (x > canvas.Width - maxButton.Width - highThumb.Width) return;
                SetVisualX(highThumb,x);
                AdjustMediumThumb();
                if (Low > High)
                    Low = High;
            }
        }

        public double Minimum { get; set; }
        public double Maximum { get; set; }
        Canvas canvas = new Canvas();
        Border minButton,  maxButton;
        Thumb lowThumb, mediumThumb, highThumb;
        double bWidth;
        static double dpiX;
        double tickStep;

        public FrameworkElement Visual {
            get { return canvas; }
        }



        public RangeSlider(double barWidth) {
            var barHeight = DpiXStatic*0.13;
            SetVisuals(barWidth, barHeight);
            SetTimer();
            SetEvents();
            Maximum = 100;
//            
           
            tickStep = 0.01*DpiXStatic; //so we are moving 1/100 of an inch per tick
        }

        void SetTimer() {
            repeatTimer = new DispatcherTimer();
            repeatTimer.Interval = TimeSpan.FromMilliseconds(10);         
        }

        

        void SetEvents() {          
            minButton.MouseLeftButtonDown += MinButtonMouseLeftButtonDown;
            minButton.MouseLeftButtonUp += MinButtonMouseLeftButtonUp;

            maxButton.MouseLeftButtonDown += MaxButtonMouseLeftButtonDown;
            maxButton.MouseLeftButtonUp += MaxButtonMouseLeftButtonUp;

            lowThumb.DragDelta += LowThumbDragDelta;
            highThumb.DragDelta += HighThumbDragDelta;
            mediumThumb.DragDelta += MediumThumbDragDelta;
        }

        void MoveVisualX(FrameworkElement element, double del) {
            SetVisualX(element, GetVisualX(element)+del);
        }

        void MediumThumbDragDelta(object sender, DragDeltaEventArgs e) {
            var del = e.HorizontalChange;
            var x = GetVisualX(highThumb);
            var range = new Interval(minButton.Width + lowThumb.Width, canvas.Width - maxButton.Width - highThumb.Width);
            var nx = range.GetInRange(x + del);
            del = nx - x;
            if (del == 0) return;
            MoveVisualX(lowThumb, del);
            MoveVisualX(mediumThumb, del);
            MoveVisualX(highThumb, del);
            RaiseEvent();
        }

        void RaiseEvent() {
            if (RangeChanged != null)
                RangeChanged(this, null);
        }

        void HighThumbDragDelta(object sender, DragDeltaEventArgs e) {
            var del = e.HorizontalChange;
            del = PositionHighThumb(del);
            if (del == 0) return;
            var x = GetVisualX(highThumb)-lowThumb.Width;
            if (x < GetVisualX(lowThumb))
                SetVisualX(lowThumb, x);
            AdjustMediumThumb();
            RaiseEvent();
            e.Handled = true;
        }

        void LowThumbDragDelta(object sender, DragDeltaEventArgs e) {
            var del = e.HorizontalChange;
            del = PositionLowThumb(del);
            if (del == 0) return;
            var x = GetVisualX(lowThumb) + lowThumb.Width;
            Debug.Assert(x<=canvas.Width-maxButton.Width-highThumb.Width);
            if(x>GetVisualX(highThumb))
                SetVisualX(highThumb,x);
            AdjustMediumThumb();
            e.Handled = true;
            RaiseEvent();
        }

        void AdjustMediumThumb() {
            mediumThumb.Width = Math.Max(0, GetVisualX(highThumb) - GetVisualX(lowThumb) - lowThumb.Width);
            SetVisualX(mediumThumb, GetVisualX(lowThumb)+lowThumb.Width);
        }

        double PositionLowThumb(double del) {
           var range= new Interval(minButton.Width, canvas.Width - maxButton.Width - highThumb.Width-lowThumb.Width);
           var x = GetVisualX(lowThumb);
            var nx = range.GetInRange(x + del);
            del = nx - x;
            if (del == 0) return 0;
            MoveVisualX(lowThumb,del);
            return del;
        }

        double PositionHighThumb(double del) {
            var range = new Interval(minButton.Width+lowThumb.Width, canvas.Width - maxButton.Width - highThumb.Width);
            var x = GetVisualX(highThumb);
            var nx = range.GetInRange(x + del);
            del = nx - x;
            if (del == 0) return 0;
            MoveVisualX(highThumb, del);
            return del;
        }

        void MaxButtonMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            repeatTimer.Tick -= MaxButtonTickWhileHoldingMouseDown;
            repeatTimer.Stop();
        }

        void MaxButtonMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            repeatTimer.Tick += MaxButtonTickWhileHoldingMouseDown;
            repeatTimer.Start();
        }

        void MaxButtonTickWhileHoldingMouseDown(object sender, EventArgs e) {
            //try to move highThumb one pixel to the right
            var left = Math.Min(GetVisualX(highThumb) + tickStep, canvas.Width - maxButton.Width - highThumb.Width);
            var del = left - GetVisualX(highThumb);
            if (del>0) {
                MoveVisualX(lowThumb, del);
                MoveVisualX(highThumb, del);
                AdjustMediumThumb();
                RaiseEvent();
            }
        }


        void MinButtonMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            repeatTimer.Tick -= MinButtonTickWhileHoldingMouseDown;
            repeatTimer.Stop();
        }

        

        void MinButtonMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            repeatTimer.Tick += MinButtonTickWhileHoldingMouseDown;
            repeatTimer.Start();
        }

        void MinButtonTickWhileHoldingMouseDown(object sender, EventArgs e) {
            //try to move lowThumb one pixel to the left
            var left = Math.Max(GetVisualX(lowThumb)-tickStep, minButton.Width);
            var del = GetVisualX(lowThumb)-left;
            if (del>0) {
                MoveVisualX(lowThumb, -del);
                MoveVisualX(highThumb, -del);
                AdjustMediumThumb();
                RaiseEvent();
            }
        }

        void SetVisuals(double barWidth, double barHeight) {
            canvas.Width = barWidth;
            canvas.Height = barHeight;
            //canvas.Background = Brushes.YellowGreen;
            bWidth = Math.Min(barWidth/20, 12);
            minButton = new Border {Width = bWidth, Height = barHeight, Background = Brushes.LightGray};
            AddLeftArrowToMinButton();
            // minButton is already correctly positioned
            lowThumb = new Thumb {Width = bWidth, Height = barHeight};
            SetVisualX(lowThumb, minButton.Width);

            mediumThumb = new Thumb {Width = 0,
                                        Height = barHeight*2/5,
                                        BorderBrush = Brushes.Blue,
                                        BorderThickness = new Thickness(barHeight/5)
                                    };
            Canvas.SetTop(mediumThumb, barHeight/2 - mediumThumb.Height/2);
            SetVisualX(mediumThumb, 2*bWidth);
            
            highThumb = new Thumb {Width = bWidth, Height = barHeight};
            SetVisualX(highThumb, 2*bWidth); 


            maxButton = new Border {Width = bWidth, Height = barHeight, Background = Brushes.LightGray};
            AddRightArrowToMaxButton();
            SetVisualX(maxButton, canvas.Width - bWidth);
            


            canvas.Children.Add(minButton);
            canvas.Children.Add(lowThumb);
            canvas.Children.Add(mediumThumb);
            canvas.Children.Add(highThumb);
            canvas.Children.Add(maxButton);
            canvas.UpdateLayout();
        
        }

        void AddRightArrowToMaxButton() {
            var rightArrow = new Polygon {Fill = Brushes.Black, Stretch = Stretch.Uniform, Margin = new Thickness(bWidth/5)};
            rightArrow.Points.Add(new Point(0, 0));
            rightArrow.Points.Add(new Point(0, 1));
            rightArrow.Points.Add(new Point(0.8, 0.5));
            
            maxButton.Child = rightArrow;
        }

        void AddLeftArrowToMinButton() {
            var leftArrow = new Polygon {Margin = new Thickness(bWidth/5), Fill = Brushes.Black, Stretch = Stretch.Uniform};
            leftArrow.Points.Add(new Point(1, 0));
            leftArrow.Points.Add(new Point(1, 1));
            leftArrow.Points.Add(new Point(0.2, 0.5));
            
            minButton.Child = leftArrow;
        }

        internal static double DpiXStatic {
            get {
                if (dpiX == 0)
                    GetDpi();
                return dpiX;
            }
        }

        static void GetDpi() {
            int hdcSrc = NativeMethods.GetWindowDC(NativeMethods.GetDesktopWindow());
            //LOGPIXELSX = 88,
            //LOGPIXELSY = 90,
            dpiX = NativeMethods.GetDeviceCaps(hdcSrc, 88);
            NativeMethods.ReleaseDC(NativeMethods.GetDesktopWindow(), hdcSrc);
        }

    }


}

