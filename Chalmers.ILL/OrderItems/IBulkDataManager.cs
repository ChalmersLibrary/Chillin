using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.OrderItems
{
    public interface IBulkDataManager
    {
        List<SimplifiedOrderItem> GetChillinDataForSierraPatron(int recordId, string lang);
    }

    public class SimplifiedOrderItem
    {
        public SimplifiedOrderItem()
        {
            Type = "";
            Reference = "";
            Status = "";
        }

        public string Type { get; set; }
        public string Reference { get; set; }
        public string Status { get; set; }
    }
}
