using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using umbraco.cms.businesslogic.datatype;
using Chalmers.ILL.Extensions;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;

namespace Chalmers.ILL.Utilities
{
    public class OrderItemStatus
    {

        /// <summary>
        /// Internal method to set status property on an OrderItem using statusId
        /// </summary>
        /// <param name="orderNodeId">OrderItem Node Id</param>
        /// <param name="statusId">Status to set using statusID</param>
        /// <returns>True if everything went ok</returns>
        public static bool SetOrderItemStatusInternal(int orderNodeId, int statusId, bool doReindex = true, bool doSignal = true)
        {
            try {
                // Connect to the content service
                var cs = new Umbraco.Core.Services.ContentService();

                // Get node for the order item
                var content = cs.GetById(orderNodeId);

                // Try and parse out the int of the Umbraco property, if it exists
                int currentStatus = Helpers.GetPropertyValueAsInteger(content.GetValue("status"));

                // Only make a change if the new value differs from the current
                if (currentStatus != statusId)
                {
                    content.SetValue("status", statusId);
                    cs.SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);
                    Logging.WriteLogItemInternal(orderNodeId, "STATUS", "Status ändrad från " + (currentStatus != -1 ? umbraco.library.GetPreValueAsString(currentStatus).Split(':').Last() : "Odefinierad") + " till " + umbraco.library.GetPreValueAsString(statusId).Split(':').Last(), doReindex, doSignal);
                }

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}