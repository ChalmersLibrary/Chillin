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
                ret = date.ToString("HH:mm", CultureInfo.CurrentCulture);
            }
            else if ((DateTime.Now - date).TotalDays < 7) // Inom de senaste sju dagarna
            {
                ret = new System.Globalization.CultureInfo("sv-SE").DateTimeFormat.GetDayName(date.DayOfWeek) + " " + date.ToString("HH:mm", CultureInfo.CurrentCulture);
            }

            return ret;
        }
    }
}