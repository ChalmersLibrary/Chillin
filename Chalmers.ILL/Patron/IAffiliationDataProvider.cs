using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.Patron
{
    public interface IAffiliationDataProvider
    {
        string GetAffiliationFromPersonNumber(string pnum);
    }
}
