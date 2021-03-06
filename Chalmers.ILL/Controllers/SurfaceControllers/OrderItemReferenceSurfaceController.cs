﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using umbraco.cms.businesslogic.member;
using Chalmers.ILL.Models;
using Chalmers.ILL.Utilities;
using Chalmers.ILL.Extensions;
using Chalmers.ILL.OrderItems;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{

    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemReferenceSurfaceController : SurfaceController
    {
        public static int EVENT_TYPE { get { return 4; } }

        IOrderItemManager _orderItemManager;

        public OrderItemReferenceSurfaceController(IOrderItemManager orderItemManager)
        {
            _orderItemManager = orderItemManager;
        }

        [HttpGet]
        public ActionResult RenderReferenceAction(int nodeId)
        {
            // Get a new OrderItem populated with values for this node
            var orderItem = _orderItemManager.GetOrderItem(nodeId);

            // The return format depends on the client's Accept-header
            return PartialView("Chalmers.ILL.Action.Reference", orderItem);
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult SetReference(int nodeId, string reference)
        {
            var json = new ResultResponse();

            try
            {
                // Find OrderItem
                var orderItem = _orderItemManager.GetOrderItem(nodeId);

                _orderItemManager.SetReference(nodeId, reference, _orderItemManager.GenerateEventId(EVENT_TYPE), false, false);

                // Construct JSON response for client (ie jQuery/getJSON)
                json.Success = true;
                json.Message = "Saved reference.";
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