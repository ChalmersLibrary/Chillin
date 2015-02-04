using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using umbraco.cms.businesslogic.datatype;
using Chalmers.ILL.Extensions;

namespace Chalmers.ILL.Utilities
{
    public class OrderItemDeliveryLibrary
    {
        /// <summary>
        /// Internal method to set delivery library property on an OrderItem using deliveryLibraryId
        /// </summary>
        /// <param name="orderNodeId">OrderItem Node Id</param>
        /// <param name="statusId">Delivery library to set using deliveryLibraryId</param>
        /// <returns>True if everything went ok</returns>
        public static bool SetOrderItemDeliveryLibraryInternal(int orderNodeId, int deliveryLibraryId, bool doReindex = true, bool doSignal = true)
        {
            var cs = new Umbraco.Core.Services.ContentService();

            try
            {
                var content = cs.GetById(orderNodeId);

                // Try and parse out the int of the Umbraco property, if it exists
                int currentDeliveryLibrary = Helpers.GetPropertyValueAsInteger(content.GetValue("deliveryLibrary"));

                // Only make a change if the new value differs from the current
                if (currentDeliveryLibrary != deliveryLibraryId)
                {
                    content.SetValue("deliveryLibrary", deliveryLibraryId);
                    cs.SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);
                    Logging.WriteLogItemInternal(orderNodeId, "BIBLIOTEK", "Bibliotek ändrat från " + (currentDeliveryLibrary != -1 ? umbraco.library.GetPreValueAsString(currentDeliveryLibrary).Split(':').Last() : "Odefinierad") + " till " + umbraco.library.GetPreValueAsString(deliveryLibraryId).Split(':').Last(), doReindex, doSignal);
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