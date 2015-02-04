using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Chalmers.ILL.Extensions;

namespace Chalmers.ILL.Utilities
{
    public class OrderItemPurchasedMaterial
    {
        public static bool SetOrderItemPurchasedMaterialInternal(int orderNodeId, int purchasedMaterialId, bool doReindex = true, bool doSignal = true)
        {
            var cs = new Umbraco.Core.Services.ContentService();

            try
            {
                var content = cs.GetById(orderNodeId);

                // Try and parse out the int of the Umbraco property, if it exists
                int currentPurchasedMaterial = Helpers.GetPropertyValueAsInteger(content.GetValue("purchasedMaterial"));

                // Only make a change if the new value differs from the current
                if (currentPurchasedMaterial != purchasedMaterialId)
                {
                    content.SetValue("purchasedMaterial", purchasedMaterialId);
                    cs.SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);
                    Logging.WriteLogItemInternal(orderNodeId, "MATERIALINKÖP", "Inköpt material ändrat till " + umbraco.library.GetPreValueAsString(purchasedMaterialId), doReindex, doSignal);
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