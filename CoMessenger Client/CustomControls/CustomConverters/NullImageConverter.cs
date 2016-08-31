using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Globalization;
using System.Windows.Data;

namespace COMessengerClient.CustomControls.CustomConverters
{
    public class NullImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return DependencyProperty.UnsetValue;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
