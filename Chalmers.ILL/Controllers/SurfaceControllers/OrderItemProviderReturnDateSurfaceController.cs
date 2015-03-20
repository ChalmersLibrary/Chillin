using Chalmers.ILL.Models.PartialPage;
using Chalmers.ILL.OrderItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemProviderReturnDateSurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;

        public OrderItemProviderReturnDateSurfaceController(IOrderItemManager orderItemManager)
        {
            _orderItemManager = orderItemManager;
        }

        /// <summary>
        /// Render the Partial View for changing the return date from the provider.
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderProviderReturnDateAction(int nodeId)
        {
            var pageModel = new ChalmersILLActionProviderReturnDateModel(_orderItemManager.GetOrderItem(nodeId));

            // The return format depends on the client's Accept-header
            return PartialView("Chalmers.ILL.Action.ProviderReturnDate", pageModel);
        }
    }
}
