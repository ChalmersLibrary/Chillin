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

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemCancellationReasonSurfaceController : SurfaceController
    {
        /// <summary>
        /// Set cancellation reason property for OrderItem
        /// </summary>
        /// <param name="orderNodeId">OrderItem Node Id</param>
        /// <param name="typeId">Cancellation reason property DataType Id</param>
        /// <returns>MVC ActionResult with JSON</returns>
        [HttpGet]
        public ActionResult SetOrderItemCancellationReason(int orderNodeId, int cancellationReasonId)
        {
            var json = new ResultResponse();

            try
            {
                // Use internal method to set type property and log the result
                OrderItemCancellationReason.SetOrderItemCancellationReasonInternal(orderNodeId, cancellationReasonId);

                // Construct JSON response for client (ie jQuery/getJSON)
                json.Success = true;
                json.Message = "Changed cancellation reason to " + cancellationReasonId;
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