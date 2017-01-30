using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace COMessengerClient.CustomControls.CustomConverters
{
    public class ProportionalDoubleConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Double))
                throw new InvalidOperationException("The target must be a Double");

            Double scaleFactor;

            if (!Double.TryParse(parameter as string, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out scaleFactor))
                throw new InvalidOperationException("Converter parameter must be double");

            if (!(value is double))
                throw new InvalidOperationException("Source value must be double");

            Double sourceValue = (double)value;

            return sourceValue * scaleFactor;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion   
    }
}
