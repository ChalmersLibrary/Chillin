using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Chalmers.ILL.Extensions;
using System.Configuration;
using Umbraco.Core.Models;

namespace Chalmers.ILL.Utilities
{
    public class OrderItemType
    {
        public static bool SetOrderItemTypeInternal(int orderNodeId, int typeId, bool doReindex = true, bool doSignal = true)
        {
            try {
                // Connect to the content service
                var cs = new Umbraco.Core.Services.ContentService();

                // Get node for the order item
                var content = cs.GetById(orderNodeId);

                // Try and parse out the int of the Umbraco property, if it exists
                int currentType = Helpers.GetPropertyValueAsInteger(content.GetValue("type"));

                // Only make a change if the new value differs from the current
                if (currentType != typeId)
                {
                    content.SetValue("type", typeId);
                    if (typeId == Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderTypeDataTypeDefinitionName"], "Artikel"))
                    {
                        content.SetValue("deliveryLibrary", Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Huvudbiblioteket"));
                    }
                    cs.SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);
                    Logging.WriteLogItemInternal(orderNodeId, "TYP", "Typ ändrad till " + umbraco.library.GetPreValueAsString(typeId), doReindex, doSignal);
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