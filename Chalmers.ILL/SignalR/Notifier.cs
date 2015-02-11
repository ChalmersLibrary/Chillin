using System;
using System.Linq;
using System.Collections;
using Microsoft.AspNet.SignalR;
using Umbraco.Core.Models;
using umbraco.cms.businesslogic.member;
using Chalmers.ILL.Utilities;
using Chalmers.ILL.Controllers.SurfaceControllers;
using umbraco.cms.businesslogic.datatype;
using System.Configuration;

namespace Chalmers.ILL.SignalR
{
    public class Notifier
    {
        /*
         * This notifier is a simple helper, to send notifications to a SignalR hub
         * It takes a document as a parameter and then passes that data on to signalR
         * Which then sends it to all connected browsers.
         * Finally, in the browser, a javascript method is executed based on the data from the server
         */
        public static void ReportNewOrderItemUpdate(IContent d)
        {
            // get the NotificationHub
            var context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();

            // Parse out the integer of status and type
            int OrderStatusId = 0;
            int OrderTypeId = 0;
            DateTime followUpDate = DateTime.MaxValue;

            if (d.GetValue("status") != null)
            {
                Int32.TryParse(d.GetValue("status").ToString(), out OrderStatusId);
            }

            if (d.GetValue("type") != null)
            {
                Int32.TryParse(d.GetValue("type").ToString(), out OrderTypeId);
            }

            if (d.GetValue("followUpDate") != null)
            {
                followUpDate = Convert.ToDateTime(d.GetValue("followUpDate").ToString());
            }

            // Extract the real chillin order status id from the umbraco id.
            int chillinOrderStatusId = 0;
            var ds = new Umbraco.Core.Services.DataTypeService();
            PreValue iter;
            foreach (DictionaryEntry pv in Helpers.GetPreValues(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionName"]))
            {
                iter = ((PreValue)pv.Value);
                if (iter.Id == OrderStatusId)
                {
                    chillinOrderStatusId = Convert.ToInt32(iter.Value.Split(':').First());
                    break;
                }
            }

            // create a notication object to send to the clients
            var n = new OrderItemNotification
                        {
                            NodeId = d.Id,
                            EditedBy = OrderItem.GetOrderItem(d.Id).EditedBy,
                            EditedByMemberName = OrderItem.GetOrderItem(d.Id).EditedByMemberName,
                            SignificantUpdate = true,
                            IsPending = chillinOrderStatusId == 1 || chillinOrderStatusId == 2 || chillinOrderStatusId == 9 || (chillinOrderStatusId > 2 && chillinOrderStatusId < 5 && DateTime.Now > followUpDate),
                            UpdateFromMail = false
                        };

            // this calls the javascript method updateStream(message) in all connected browsers
            context.Clients.All.updateStream(n);
        }

        public static void UpdateOrderItemUpdate(int nodeId, string editedBy, string editedByMemberName, bool significant = false, bool isPending = false, bool updateFromMail = false)
        {
            // get the NotificationHub
            var context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();

            var n = new OrderItemNotification
            {
                NodeId = nodeId,
                EditedBy = editedBy,
                EditedByMemberName = editedByMemberName,
                SignificantUpdate = significant,
                IsPending = isPending,
                UpdateFromMail = updateFromMail
            };

            // this calls the javascript method updateStream(message) in all connected browsers
            context.Clients.All.updateStream(n);
        }
    }

    // Significant update indicates that this is something else than a lock/unlock event.
    // IsPending indicates that the item should exist in the list on the order page.
    public class OrderItemNotification
    {
        public int NodeId { get; set; }
        public string EditedBy { get; set; }
        public string EditedByMemberName { get; set; }
        public bool SignificantUpdate { get; set; }
        public bool IsPending { get; set; }
        public bool UpdateFromMail { get; set; }
    }
}