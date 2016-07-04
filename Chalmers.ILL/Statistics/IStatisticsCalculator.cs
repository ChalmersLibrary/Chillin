using Chalmers.ILL.Models;
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
        int CalculateDataPointValue(IEnumerable<OrderItemModel> dataBag, string calculationTypeStr);
    }
}
