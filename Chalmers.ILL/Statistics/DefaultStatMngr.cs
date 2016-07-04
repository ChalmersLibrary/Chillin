using Chalmers.ILL.Models;
using Chalmers.ILL.OrderItems;
using System.Collections.Generic;

namespace Chalmers.ILL.Statistics
{
    public class DefaultStatMngr : IStatisticsManager
    {
        IOrderItemSearcher _searcher;
        IStatisticsCalculator _statCalc;

        public DefaultStatMngr(IOrderItemSearcher searcher, IStatisticsCalculator statCalc)
        {
            _searcher = searcher;
            _statCalc = statCalc;
        }

        public void CalculateAllData(StatisticsRequest sReq)
        {
            foreach (var sVar in sReq.StatisticsData)
            {
                sVar.Values = new List<int>();
                foreach (var dataPointQuery in sVar.LuceneQueries)
                {
                    sVar.Values.Add(_statCalc.CalculateDataPointValue(_searcher.Search(dataPointQuery), sVar.CalculationType));
                }
            }
        }
    }
}