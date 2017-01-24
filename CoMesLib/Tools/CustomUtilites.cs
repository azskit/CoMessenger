using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CorporateMessengerLibrary.Tools
{
    public static class CustomUtilities
    {
        public static string FormatException(Exception exceptionToFormat)
        {
            var exError = new StringBuilder();
            if (exceptionToFormat == null)
            {
                throw new ArgumentNullException("exceptionToFormat");
            }
            while (exceptionToFormat != null)
            {
                exError.AppendLine(exceptionToFormat.Message);
                exError.AppendLine(exceptionToFormat.StackTrace);
                exceptionToFormat = exceptionToFormat.InnerException;
            }
            return exError.ToString();
        }
    }
}
