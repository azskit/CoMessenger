using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;

namespace COMessengerClient.CustomControls.CustomConverters
{
    public class ServerPortConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            string server = (string)values[0];
            string port = (string)values[1];

            if (String.IsNullOrEmpty(port))
                return server;

            return string.Format(CultureInfo.InvariantCulture, "{0}:{1}", server, port);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string ServerPort = (string)value;

            if (!ServerPort.Contains(':'))
                return null;
            else
                return ServerPort.Split(':');
        }
    }
}
