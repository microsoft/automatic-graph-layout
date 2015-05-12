using System;
using System.Net;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace Microsoft.Msagl.GraphControlSilverlight
{
    public class VisibilityConverter : IValueConverter
    {
        public Visibility VisibilityForFalse { get; set; }
        public Visibility VisibilityForTrue { get; set; }

        public VisibilityConverter()
        {
            VisibilityForFalse = Visibility.Collapsed;
            VisibilityForTrue = Visibility.Visible;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? VisibilityForTrue : VisibilityForFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullVisibilityConverter : IValueConverter
    {
        public Visibility VisibilityIfNull { get; set; }
        public Visibility VisibilityIfNotNull { get; set; }

        public NullVisibilityConverter()
        {
            VisibilityIfNull = Visibility.Collapsed;
            VisibilityIfNotNull = Visibility.Visible;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? VisibilityIfNull : VisibilityIfNotNull;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ZoomConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)((double)value * 100.0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double val;
            Double.TryParse(value.ToString(), out val);
            return val / 100.0;
        }
    }
}
