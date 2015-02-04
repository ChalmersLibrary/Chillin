using Chalmers.ILL.Models;
using Examine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Statistics
{
    public class DefaultStatMngr : IStatisticsManager
    {
        ISearcher _searcher;
        IStatisticsCalculator _statCalc;

        public DefaultStatMngr(ISearcher searcher, IStatisticsCalculator statCalc)
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
                    var searchCriteria = _searcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
                    sVar.Values.Add(_statCalc.CalculateDataPointValue(_searcher.Search(searchCriteria.RawQuery(dataPointQuery)), sVar.CalculationType));
                }
            }
        }
    }
}