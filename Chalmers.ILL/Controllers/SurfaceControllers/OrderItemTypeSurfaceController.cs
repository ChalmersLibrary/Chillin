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
    public class OrderItemTypeSurfaceController : SurfaceController
    {
        public static int EVENT_TYPE { get { return 1; } }

        IOrderItemManager _orderItemManager;

        public OrderItemTypeSurfaceController(IOrderItemManager orderItemManager)
        {
            _orderItemManager = orderItemManager;
        }

        /// <summary>
        /// Set type property for OrderItem
        /// </summary>
        /// <param name="orderNodeId">OrderItem Node Id</param>
        /// <param name="typeId">Type property DataType Id</param>
        /// <returns>MVC ActionResult with JSON</returns>
        [HttpGet]
        public ActionResult SetOrderItemType(int orderNodeId, int typeId)
        {
            var json = new ResultResponse();

            try
            {
                var eventId = _orderItemManager.GenerateEventId(EVENT_TYPE);

                // Use internal method to set type property and log the result
                _orderItemManager.SetType(orderNodeId, typeId, eventId);

                // Construct JSON response for client (ie jQuery/getJSON)
                json.Success = true;
                json.Message = "Changed type to " + typeId;
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