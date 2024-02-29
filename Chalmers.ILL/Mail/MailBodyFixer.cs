using System;
using System.Text.RegularExpressions;

namespace Chalmers.ILL.Mail
{
    public static class MailBodyFixer
    {
        public static string RemoveHtmlAroundLinks(string htmlText)
        {
            var res = "";
            if (!String.IsNullOrEmpty(htmlText))
            {
                res = Regex.Replace(htmlText, @"<a[^>]*href=""([^""]*)""[^>]*>\1</a>", "$1");
                res = Regex.Replace(res, @"<a[^>]*href=""([^""]*)""[^>]*>([^<]*)</a>", "$2 ( $1 )");
            }
            return res;
        }
    }
}