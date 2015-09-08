using Chalmers.ILL.Mail;
using Chalmers.ILL.Models.PartialPage;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Templates;
using Chalmers.ILL.UmbracoApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemReceiveBookSurfaceController : SurfaceController
    {
        IUmbracoWrapper _umbraco;
        IOrderItemManager _orderItemManager;
        ITemplateService _templateService;
        IMailService _mailService;

        public OrderItemReceiveBookSurfaceController(IUmbracoWrapper umbraco, IOrderItemManager orderItemManager, ITemplateService templateService, 
            IMailService mailService)
        {
            _orderItemManager = orderItemManager;
            _templateService = templateService;
            _mailService = mailService;
            _umbraco = umbraco;
        }

        [HttpGet]
        public ActionResult RenderReceiveBookAction(int nodeId)
        {
            var pageModel = new ChalmersILLActionReceiveBookModel(_orderItemManager.GetOrderItem(nodeId));
            _umbraco.PopulateModelWithAvailableValues(pageModel);
            pageModel.BookAvailableMailTemplate = _templateService.GetTemplateData("BookAvailableMailTemplate", pageModel.OrderItem);
            return PartialView("Chalmers.ILL.Action.ReceiveBook", pageModel);
        }
    }
}