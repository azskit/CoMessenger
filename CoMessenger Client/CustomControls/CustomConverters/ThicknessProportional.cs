﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace COMessengerClient.CustomControls.CustomConverters
{
    public class ThicknessProportional : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            if (targetType != typeof(Thickness))
                throw new InvalidOperationException("The target must be a Thickness");

            double scaleFactor;

            if (!Double.TryParse(parameter as string, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out scaleFactor))
                throw new InvalidOperationException("Converter parameter must be double");

            if (!(value is double))
                throw new InvalidOperationException("Source value must be double");

            double sourceValue = (double)value;

            double calculatedRadios = sourceValue * scaleFactor;

            return new Thickness(calculatedRadios);
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion   
    }
}
