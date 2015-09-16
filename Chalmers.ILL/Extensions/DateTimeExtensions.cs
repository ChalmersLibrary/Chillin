using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToPrettyString(this DateTime date)
        {
            var ret = date.ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture);

            if ((DateTime.Now - date).TotalMinutes < 60) // Inom en timme
            {
                ret = Math.Floor((DateTime.Now - date).TotalMinutes) + " minuter";
            }
            else if (DateTime.Now.Date == date.Date) // Samma dag
            {
                ret = Math.Floor((DateTime.Now - date).TotalHours) + " timmar";
            }
            else if (DateTime.Now.AddDays(-1).Date == date.Date) // I går
            {
                ret = "i går";
            }
            else if ((DateTime.Now - date).TotalDays < 30) // Inom de senaste trettio dagarna
            {
                ret = Math.Floor((DateTime.Now - date).TotalDays) + " dagar";
            }
            else if ((DateTime.Now - date).TotalDays < 365) // Inom det senaste året
            {
                ret = Math.Floor((DateTime.Now - date).TotalDays / 7) + " veckor";
            }
            else // Gammal
            {
                ret = Math.Floor((DateTime.Now - date).TotalDays / 365) + " år";
            }

            return ret;
        }
    }
}