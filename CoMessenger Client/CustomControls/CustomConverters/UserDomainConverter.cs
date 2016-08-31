using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;

namespace COMessengerClient.CustomControls.CustomConverters
{
    public class UserDomainConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            if (!(values is string[]))
                return "";

            string login = (string)values[0];
            string domain = (string)values[1];

            //Коль домена нет, то суем логин
            if (String.IsNullOrEmpty(domain))
                return login;
            else
                return string.Format(CultureInfo.InvariantCulture, "{0}\\{1}", domain, login);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string DomainUser = (string)value;

            string[] retValues = new string[2];

            if (DomainUser.Contains("\\"))
            {
                retValues = DomainUser.Split('\\');
                return retValues.Reverse().ToArray<object>();
            }
            else if (DomainUser.Contains("@"))
            {
                return DomainUser.Split('@');
            }
            else
            {
                retValues[0] = DomainUser;
                retValues[1] = String.Empty;
                return retValues;
            }
        }
    }
}
