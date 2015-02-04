using Chalmers.ILL.Models;
using Examine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Statistics
{
    public class DefaultStatCalc : IStatisticsCalculator
    {
        public int CalculateDataPointValue(ISearchResults dataBag, string calculationTypeStr)
        {
            int res = 0;

            if (calculationTypeStr == "COUNT")
            {
                res = dataBag.Count();
            }
            else if (calculationTypeStr == "AVERAGE_ORDER_LENGTH")
            {
                res = (int)Math.Ceiling(dataBag.Select(CalculateTotalTurnaroundTime).DefaultIfEmpty().Average());
            }
            else if (calculationTypeStr == "MEDIAN_ORDER_LENGTH")
            {
                var timeDifferences = dataBag.Select(CalculateTotalTurnaroundTime).OrderBy(x => x);
                var count = timeDifferences.Count();
                if (count > 1)
                {
                    res = (int)Math.Ceiling((double)(timeDifferences.ElementAt(count / 2) + timeDifferences.ElementAt((count - 1) / 2)) / 2);
                }
                else if (count == 1)
                {
                    res = (int)timeDifferences.First();
                }
                else
                {
                    res = 0;
                }
            }
            else
            {
                throw new Exception("Unknown calculation type.");
            }

            return res;
        }

        #region private

        private int CalculateTotalTurnaroundTime(SearchResult item)
        {
            var log = JsonConvert.DeserializeObject<List<LogItem>>(item.Fields["Log"]);

            var startTime = DateTime.ParseExact(item.Fields["createDate"], "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None);
            var endTime = startTime;

            log.OrderByDescending(x => x.CreateDate);
            foreach (var logItem in log)
            {
                // Find the latest status change and declare that to be the latest
                if (logItem.Type == "STATUS")
                {
                    endTime = logItem.CreateDate;
                }
            }

            var test = (endTime - startTime).TotalMinutes;
            var test2 = (int)test;
            return (int)(endTime - startTime).TotalMinutes;
        }

        #endregion
    }
}