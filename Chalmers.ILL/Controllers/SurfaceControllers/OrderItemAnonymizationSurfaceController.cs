using Chalmers.ILL.Models;
using Chalmers.ILL.OrderItems;
using Microsoft.Exchange.WebServices.Data;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemAnonymizationSurfaceController : SurfaceController
    {
        public static int MANUAL_ANONYMIZATION_EVENT_TYPE { get { return 31; } }

        IOrderItemManager _orderItemManager;

        public OrderItemAnonymizationSurfaceController(IOrderItemManager orderItemManager)
        {
            _orderItemManager = orderItemManager;
        }

        [HttpGet]
        public ActionResult RenderAnonymizeAction(int nodeId)
        {
            // Get a new OrderItem populated with values for this node
            var orderItem = _orderItemManager.GetOrderItem(nodeId);
            
            // The return format depends on the client's Accept-header
            return PartialView("Chalmers.ILL.Action.Anonymize", orderItem);
        }

        [HttpPost]
        public ActionResult Anonymize(int nodeId, string reference, string logsSerialized)
        {
            var json = new ResultResponse();

            try
            {
                var logs = JsonConvert.DeserializeObject<List<LogItem>>(logsSerialized);

                var eventId = _orderItemManager.GenerateEventId(MANUAL_ANONYMIZATION_EVENT_TYPE);
                _orderItemManager.SilentAnonymization(nodeId, reference, logs, eventId, false, false);
                _orderItemManager.AddLogItem(nodeId, "ANONYMISERING", "Manuell anonymisering av order.", eventId);

                // Construct JSON response for client (ie jQuery/getJSON)
                json.Success = true;
                json.Message = "Anonymized.";
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
