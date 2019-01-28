using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Chalmers.ILL.Mail
{
    public static class MailBodyFixer
    {
        public static string RemoveHtmlAroundLinks(string htmlText)
        {
            var res = "";
            if (!String.IsNullOrEmpty(htmlText))
            {
                res = Regex.Replace(htmlText, @"<a[^>]*href=""([^""]*)""[^>]*>([^<]*)</a>", "$2 ($1)");
            }
            return res;
        }
    }
}