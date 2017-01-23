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

    public class UserNameValidationRule : ValidationRule
    {
        private static readonly Regex UserNameValidator = new Regex
            (
                @"^(" +
            //<user>
                @"([A-zА-я0-9_-]+)" +
                "|" +

            //<user>@<domain>.<domain>
                @"([A-zА-я0-9_-]+)@([A-zА-я0-9_\.-]*)" +
                "|" +

            //<domain>\<user>
                @"([A-zА-я0-9_\.-]+)\\([A-zА-я0-9_-]*)" +
                ")$"
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
