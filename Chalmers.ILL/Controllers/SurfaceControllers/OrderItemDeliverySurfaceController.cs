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
using Microsoft.Exchange.WebServices.Data;
using System.Configuration;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{

    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemDeliverySurfaceController : SurfaceController
    {
        /// <summary>
        /// Render the Partial View for sending mail to user from within the system
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderDeliveryAction(int nodeId)
        {
            // Get a new OrderItem populated with values for this node
            var orderItem = OrderItem.GetOrderItem(nodeId);

            // The return format depends on the client's Accept-header
            // return PartialView("Chalmers.ILL.Action.Mail", orderItem);
            return PartialView("Chalmers.ILL.Action.Delivery", orderItem);
        }

        /// <summary>
        /// Send mail to user from within the system, possibly change status and log means of delivery
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <param name="newStatus">Means of delivery</param>
        /// <returns>JSON result</returns>
        [HttpGet]
        public ActionResult SetDelivery(int nodeId, string logEntry, string delivery)
        {
            var json = new ResultResponse();

            try
            {
                // Connect to Umbraco ContentService
                var contentService = UmbracoContext.Application.Services.ContentService;

                // Find OrderItem
                var contentNode = contentService.GetById(nodeId);

                // Log this action
                Logging.WriteLogItemInternal(nodeId, "LEVERERAD", "Skickad med " + delivery, false, false);

                if (logEntry != "")
                {
                    Logging.WriteLogItemInternal(nodeId, "LOG", logEntry, false, false);
                }

                // Set status = Levererad
                try
                {
                    OrderItemStatus.SetOrderItemStatusInternal(nodeId, Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionGuid"], "05:Levererad"), false, false);
                }
                catch (Exception)
                {
                    throw;
                }

                // Save
                contentService.SaveWithoutEventsAndWithSynchronousReindexing(contentNode);

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