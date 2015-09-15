using Chalmers.ILL.Models;
using Chalmers.ILL.Models.PartialPage;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.UmbracoApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemReturnSurfaceController : SurfaceController
    {
        public static int EVENT_TYPE { get { return 14; } }

        IOrderItemManager _orderItemManager;
        IUmbracoWrapper _umbraco;

        public OrderItemReturnSurfaceController(IOrderItemManager orderItemManager, IUmbracoWrapper umbraco)
        {
            _orderItemManager = orderItemManager;
            _umbraco = umbraco;
        }

        /// <summary>
        /// Render the Partial View for returning a book to its library
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderReturnAction(int nodeId)
        {
            var pageModel = new ChalmersILLActionReturnModel(_orderItemManager.GetOrderItem(nodeId));

            _umbraco.PopulateModelWithAvailableValues(pageModel);

            // The return format depends on the client's Accept-header
            return PartialView("Chalmers.ILL.Action.Return", pageModel);
        }

        [HttpPost]
        public ActionResult ReturnItem(int nodeId)
        {
            var json = new ResultResponse();

            try
            {
                var eventId = _orderItemManager.GenerateEventId(EVENT_TYPE);
                _orderItemManager.SetStatus(nodeId, "10:Återsänd", eventId);

                json.Success = true;
                json.Message = "Returnering genomförd.";
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Misslyckades med att returnera: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }
    }
}
