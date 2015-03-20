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
    public class OrderItemClaimSurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;
        ITemplateService _templateService;

        public OrderItemClaimSurfaceController(IOrderItemManager orderItemManager, ITemplateService templateService)
        {
            _orderItemManager = orderItemManager;
            _templateService = templateService;
        }

        [HttpGet]
        public ActionResult RenderClaimAction(int nodeId)
        {
            var pageModel = new ChalmersILLActionClaimModel(_orderItemManager.GetOrderItem(nodeId));

            pageModel.ClaimBookMailTemplate = _templateService.GetTemplateData("ClaimBookMailTemplate", pageModel.OrderItem);

            return PartialView("Chalmers.ILL.Action.Claim", pageModel);
        }
    }
}
