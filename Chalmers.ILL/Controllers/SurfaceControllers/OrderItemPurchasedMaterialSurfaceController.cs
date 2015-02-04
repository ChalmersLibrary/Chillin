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
    public class OrderItemPurchasedMaterialSurfaceController : SurfaceController
    {
        /// <summary>
        /// Set purchased material property for OrderItem
        /// </summary>
        /// <param name="orderNodeId">OrderItem Node Id</param>
        /// <param name="typeId">Purchased material property DataType Id</param>
        /// <returns>MVC ActionResult with JSON</returns>
        [HttpGet]
        public ActionResult SetOrderItemPurchasedMaterial(int orderNodeId, int purchasedMaterialId)
        {
            var json = new ResultResponse();

            try
            {
                // Use internal method to set type property and log the result
                OrderItemPurchasedMaterial.SetOrderItemPurchasedMaterialInternal(orderNodeId, purchasedMaterialId);

                // Construct JSON response for client (ie jQuery/getJSON)
                json.Success = true;
                json.Message = "Changed purchased material to " + purchasedMaterialId;
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