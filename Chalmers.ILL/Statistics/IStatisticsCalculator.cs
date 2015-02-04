using Examine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.Statistics
{
    public interface IStatisticsCalculator
    {
        int CalculateDataPointValue(ISearchResults dataBag, string calculationTypeStr);
    }
}
