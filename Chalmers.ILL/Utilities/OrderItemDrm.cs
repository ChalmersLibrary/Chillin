using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using umbraco.cms.businesslogic.datatype;
using Chalmers.ILL.Extensions;

namespace Chalmers.ILL.Utilities
{
    public class OrderItemDrm
    {
        public static bool SetDrmWarning(int orderNodeId, bool status, bool doReindex = true, bool doSignal = true)
        {
            var cs = new Umbraco.Core.Services.ContentService();

            try
            {
                var content = cs.GetById(orderNodeId);
                content.SetValue("drmWarning", status);
                cs.SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);
                Logging.WriteLogItemInternal(orderNodeId, "DRM", "Kan innehålla drm-material!", doReindex, doSignal);
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}