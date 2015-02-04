using Chalmers.ILL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.Statistics
{
    public interface IStatisticsManager
    {
        void CalculateAllData(StatisticsRequest sReq);
    }
}
