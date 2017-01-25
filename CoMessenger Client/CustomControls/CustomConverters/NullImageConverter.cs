using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace COMessengerClient.CustomControls.CustomConverters
{
    public class NullImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;
            //return new DrawingImage(new GeometryDrawing(new SolidColorBrush(Colors.Red), null, App.ThisApp.Resources["MaleSingle"] as Geometry));
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
