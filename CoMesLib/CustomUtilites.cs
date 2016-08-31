using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CorporateMessengerLibrary
{
    public static class CustomUtilites
    {
        public static string formatException(Exception e)
        {
            var exError = new StringBuilder();
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }
            while (e != null)
            {
                exError.AppendLine(e.Message);
                exError.AppendLine(e.StackTrace);
                e = e.InnerException;
            }
            return exError.ToString();
        }
    }
}
