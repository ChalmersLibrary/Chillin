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
    public class OrderItemDeliveryLibrarySurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;

        public OrderItemDeliveryLibrarySurfaceController(IOrderItemManager orderItemManager)
        {
            _orderItemManager = orderItemManager;
        }

        /// <summary>
        /// Set delivery library property for OrderItem
        /// </summary>
        /// <param name="orderNodeId">OrderItem Node Id</param>
        /// <param name="statusId">Delivery library DataType Id</param>
        /// <returns>MVC ActionResult with JSON</returns>
        [HttpGet]
        public ActionResult SetOrderItemDeliveryLibrary(int orderNodeId, int deliveryLibraryId)
        {
            var json = new ResultResponse();

            try
            {
                // Use internal method to set status property and log the result
                _orderItemManager.SetOrderItemDeliveryLibraryInternal(orderNodeId, deliveryLibraryId);

                // Construct JSON response for client (ie jQuery/getJSON)
                json.Success = true;
                json.Message = "Changed delivery library to " + deliveryLibraryId;
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Error: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Set delivery library information for OrderItem
        /// </summary>
        /// <param name="orderNodeId">OrderItem Node Id</param>
        /// <param name="bookId">Delivery Library Book Id</param>
        /// <param name="dueDate">Delivery Library Due Date</param>
        /// <param name="providerInformation">Information about the provider</param>
        /// <returns>MVC ActionResult with JSON</returns>
        [HttpGet]
        public ActionResult SetOrderItemDeliveryReceived(int orderNodeId, string bookId, DateTime dueDate, string providerInformation)
        {
            var json = new ResultResponse();

            try
            {
                // Use internal method to set status property and log the result
                _orderItemManager.SetOrderItemDeliveryReceivedInternal(orderNodeId, bookId, dueDate, providerInformation);

                // Construct JSON response for client (ie jQuery/getJSON)
                json.Success = true;
                json.Message = "Changed delivery item information to bookid:" + bookId + " due date:+" + dueDate + " provider information:" + providerInformation;
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
