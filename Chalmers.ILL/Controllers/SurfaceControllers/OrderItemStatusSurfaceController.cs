using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Web.Mvc;
using umbraco.cms.businesslogic.member;
using umbraco.cms.businesslogic.datatype;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using Chalmers.ILL.Models;
using Chalmers.ILL.Utilities;
using Chalmers.ILL.OrderItems;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{

    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemStatusSurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;

        public OrderItemStatusSurfaceController(IOrderItemManager orderItemManager)
        {
            _orderItemManager = orderItemManager;
        }

        /// <summary>
        /// Set status property for OrderItem
        /// </summary>
        /// <param name="orderNodeId">OrderItem Node Id</param>
        /// <param name="statusId">Status property DataType Id</param>
        /// <returns>MVC ActionResult with JSON</returns>
        [HttpGet]
        public ActionResult SetOrderItemStatus(int orderNodeId, int statusId, int cancellationReasonId = -1, int purchasedMaterialId = -1)
        {
            var json = new ResultResponse();

            try 
	        {
                if (cancellationReasonId != -1)
                {
                    _orderItemManager.SetCancellationReason(orderNodeId, cancellationReasonId, false, false);
                }

                if (purchasedMaterialId != -1)
                {
                    _orderItemManager.SetPurchasedMaterial(orderNodeId, purchasedMaterialId, false, false);
                }

                // Use internal method to set status property and log the result
                _orderItemManager.SetStatus(orderNodeId, statusId);

                // Construct JSON response for client (ie jQuery/getJSON)
                json.Success = true;
                json.Message = "Changed status to " + statusId;
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