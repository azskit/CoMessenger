using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;

namespace COMessengerClient.CustomControls.CustomConverters
{
    public class MathConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Double first = (Double)value;

            String operand = parameter as String;

            char operat = operand[0];

            double second = Double.Parse(operand.Substring(1));

            switch (operat)
            {
                case '+':
                    return first + second;
                case '-':
                    return first - second;
                case '*':
                    return first * second;
                case '/':
                    return first / second;
                default:
                    throw new InvalidOperationException("Converter parameter must be set to one of [+-*/]number");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion   
    }
}
