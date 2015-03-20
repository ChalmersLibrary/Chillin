using Chalmers.ILL.Models.PartialPage;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemPatronReturnDateSurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;
        ITemplateService _templateService;

        public OrderItemPatronReturnDateSurfaceController(IOrderItemManager orderItemManager, ITemplateService templateService)
        {
            _orderItemManager = orderItemManager;
            _templateService = templateService;
        }

        /// <summary>
        /// Render the Partial View for changing the return date against the patron.
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderPatronReturnDateAction(int nodeId)
        {
            var pageModel = new ChalmersILLActionPatronReturnDateModel(_orderItemManager.GetOrderItem(nodeId));

            pageModel.ReturnDateChangedMailTemplate = _templateService.GetTemplateData("ReturnDataChangeMailTemplate", pageModel.OrderItem);

            // The return format depends on the client's Accept-header
            return PartialView("Chalmers.ILL.Action.PatronReturnDate", pageModel);
        }
    }
}
