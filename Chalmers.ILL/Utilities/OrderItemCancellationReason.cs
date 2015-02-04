using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Chalmers.ILL.Extensions;

namespace Chalmers.ILL.Utilities
{
    public class OrderItemCancellationReason
    {
        public static bool SetOrderItemCancellationReasonInternal(int orderNodeId, int cancellationReasonId, bool doReindex = true, bool doSignal = true)
        {
            var cs = new Umbraco.Core.Services.ContentService();

            try
            {
                var content = cs.GetById(orderNodeId);

                // Try and parse out the int of the Umbraco property, if it exists
                int currentCancellationReason = Helpers.GetPropertyValueAsInteger(content.GetValue("cancellationReason"));

                // Only make a change if the new value differs from the current
                if (currentCancellationReason != cancellationReasonId)
                {
                    content.SetValue("cancellationReason", cancellationReasonId);
                    cs.SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);
                    Logging.WriteLogItemInternal(orderNodeId, "ANNULLERINGSORSAK", "Annulleringsorsak ändrad till " + umbraco.library.GetPreValueAsString(cancellationReasonId), doReindex, doSignal);
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