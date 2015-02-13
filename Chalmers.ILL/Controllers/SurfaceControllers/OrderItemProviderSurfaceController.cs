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
using Chalmers.ILL.Logging;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{

    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemProviderSurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;
        IInternalDbLogger _internalDbLogger;

        public OrderItemProviderSurfaceController(IOrderItemManager orderItemManager, IInternalDbLogger internalDbLogger)
        {
            _orderItemManager = orderItemManager;
            _internalDbLogger = internalDbLogger;
        }

        [HttpGet]
        public ActionResult RenderProviderAction(int nodeId)
        {
            // Get a new OrderItem populated with values for this node
            var orderItem = _orderItemManager.GetOrderItem(nodeId);

            // The return format depends on the client's Accept-header
            return PartialView("Chalmers.ILL.Action.Provider", orderItem);
        }

        [HttpGet]
        public ActionResult SetProvider(int nodeId, string providerName, string providerOrderId, string newFollowUpDate)
        {
            var json = new ResultResponse();

            try
            {
                // Connect to Umbraco ContentService
                var contentService = UmbracoContext.Application.Services.ContentService;

                // Find OrderItem
                var contentNode = contentService.GetById(nodeId);

                // Read current values on provider
                var currentProviderName = contentNode.GetValue("providerName") != null ? contentNode.GetValue("providerName").ToString() : "";
                var currentProviderOrderId = contentNode.GetValue("providerOrderId") != null ? contentNode.GetValue("providerOrderId").ToString() : "";

                // Set Provider properties
                contentNode.SetValue("providerName", providerName);
                contentNode.SetValue("providerOrderId", providerOrderId);

                // Log this action
                if (currentProviderName != providerName)
                {
                    _internalDbLogger.WriteLogItemInternal(nodeId, "ORDER", "Beställd från " + providerName, false, false);
                }

                if (currentProviderOrderId != providerOrderId)
                {
                    _internalDbLogger.WriteLogItemInternal(nodeId, "ORDER", "Beställningsnr: " + providerOrderId, false, false);
                }

                // Set status = Beställd
                try
                {
                    _orderItemManager.SetOrderItemStatusInternal(nodeId, Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionName"], "03:Beställd"), false, false);
                }
                catch (Exception)
                {
                    throw;
                }

                // Set FollowUpDate property if it differs from current
                var currentFollowUpDate = _orderItemManager.GetOrderItem(nodeId).FollowUpDate;

                if (!String.IsNullOrEmpty(newFollowUpDate))
                {
                    DateTime parsedNewFollowUpDate = Convert.ToDateTime(newFollowUpDate);
                    if (currentFollowUpDate != parsedNewFollowUpDate)
                    {
                        _orderItemManager.SetFollowUpDate(nodeId, parsedNewFollowUpDate, false, false);
                        _internalDbLogger.WriteLogItemInternal(nodeId, "DATE", "Följs upp senast " + newFollowUpDate, false, false);
                    }
                }

                // Save
                _orderItemManager.SaveWithoutEventsAndWithSynchronousReindexing(contentNode);

                // Construct JSON response for client (ie jQuery/getJSON)
                json.Success = true;
                json.Message = "Saved provider data.";
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