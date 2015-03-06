using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models
{
    public class OrderItemSeedModel
    {
        public string PatronName { get; set; }
        public string PatronEmail { get; set; }
        public string PatronCardNumber { get; set; }
        public string DeliveryLibrarySigel { get; set; }
        public string Message { get; set; }
        public SierraModel SierraPatronInfo { get; set; }
    }
}