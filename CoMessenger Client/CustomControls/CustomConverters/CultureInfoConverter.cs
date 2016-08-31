using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;

namespace COMessengerClient.CustomControls.CustomConverters
{
    public class CultureInfoConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            CultureInfo cultureInfo = (CultureInfo)value;

            return String.Format(CultureInfo.InvariantCulture, "{0} ({1})", cultureInfo.NativeName, cultureInfo.EnglishName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion   
    }
}
