using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace COMessengerClient.CustomControls.CustomValidationRules
{

    public class ServerPortValidationRule : ValidationRule
    {
        private static readonly Regex UserNameValidator = new Regex
            (
            //<server>\<port>
                @"^([A-zА-я0-9_\.-]+):(\d+)$"
            );

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {

            string username = (string)value;

            ValidationResult result = new ValidationResult(true, null);

            if (String.IsNullOrEmpty(username))
                return result;

            if (!UserNameValidator.IsMatch(username))
            {
                result = new ValidationResult(false, App.ThisApp.Locally.LocaleStrings["Wrong login format"]);
            }

            return result;
        }
    }
}
