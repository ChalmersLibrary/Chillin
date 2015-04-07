using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using umbraco.cms.businesslogic.member;
using Chalmers.ILL.Models;
using Chalmers.ILL.Utilities;
using Chalmers.ILL.Extensions;
using System.Configuration;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Models.PartialPage;
using Examine;
using Chalmers.ILL.Providers;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{

    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemProviderSurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;
        IProviderService _providerService;

        public OrderItemProviderSurfaceController(IOrderItemManager orderItemManager, IProviderService providerService)
        {
            _orderItemManager = orderItemManager;
            _providerService = providerService;
        }

        [HttpGet]
        public ActionResult RenderProviderAction(int nodeId)
        {
            // Get a new OrderItem populated with values for this node
            var pageModel = new ChalmersILLActionProviderModel(_orderItemManager.GetOrderItem(nodeId));

            pageModel.Providers = _providerService.FetchAndCreateListOfUsedProviders();
            var deliveryTimeInHours = _providerService.GetSuggestedDeliveryTimeInHoursForProvider(pageModel.OrderItem.ProviderName);
            pageModel.EstimatedDeliveryCurrentProvider = DateTime.Now.AddHours(deliveryTimeInHours);

            // The return format depends on the client's Accept-header
            return PartialView("Chalmers.ILL.Action.Provider", pageModel);
        }

        [HttpGet]
        public ActionResult SetProvider(int nodeId, string providerName, string providerOrderId, string providerInformation, string newFollowUpDate)
        {
            var json = new ResultResponse();

            try
            {
                _orderItemManager.SetFollowUpDate(nodeId, Convert.ToDateTime(newFollowUpDate), false, false);
                _orderItemManager.SetProviderName(nodeId, providerName, false, false);
                _orderItemManager.SetProviderOrderId(nodeId, providerOrderId, false, false);
                _orderItemManager.SetProviderInformation(nodeId, providerInformation, false, false);
                _orderItemManager.SetStatus(nodeId, Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionName"], "03:Beställd"));

                json.Success = true;
                json.Message = "Sparade data för beställning.";
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Error: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }
    }
}