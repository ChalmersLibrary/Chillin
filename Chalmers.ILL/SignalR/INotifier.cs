using Chalmers.ILL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;

namespace Chalmers.ILL.SignalR
{
    public interface INotifier
    {
        void ReportNewOrderItemUpdate(IContent d);
        void ReportNewOrderItemUpdate(OrderItemModel orderItem);
        void UpdateOrderItemUpdate(int nodeId, string editedBy, string editedByMemberName, bool significant = false, bool isPending = false, bool updateFromMail = false);
    }
}
