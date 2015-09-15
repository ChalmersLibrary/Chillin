using Chalmers.ILL.Mail;
using Chalmers.ILL.Models;
using Chalmers.ILL.Models.Mail;
using Chalmers.ILL.Models.PartialPage;
using Chalmers.ILL.OrderItems;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemProviderReturnDateSurfaceController : SurfaceController
    {
        public static int EVENT_TYPE { get { return 12; } }

        IOrderItemManager _orderItemManager;

        public OrderItemProviderReturnDateSurfaceController(IOrderItemManager orderItemManager)
        {
            _orderItemManager = orderItemManager;
        }

        /// <summary>
        /// Render the Partial View for changing the return date from the provider.
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderProviderReturnDateAction(int nodeId)
        {
            var pageModel = new ChalmersILLActionProviderReturnDateModel(_orderItemManager.GetOrderItem(nodeId));

            // The return format depends on the client's Accept-header
            return PartialView("Chalmers.ILL.Action.ProviderReturnDate", pageModel);
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult ChangeReturnDate(string packJson)
        {
            var json = new ResultResponse();

            try
            {
                var pack = JsonConvert.DeserializeObject<ChangeReturnDatePackage>(packJson);

                var orderItem = _orderItemManager.GetOrderItem(pack.nodeId);

                var eventId = _orderItemManager.GenerateEventId(EVENT_TYPE);

                if (pack.logMsg != "")
                {
                    _orderItemManager.AddLogItem(pack.nodeId, "LOG", pack.logMsg, eventId, false, false);
                }

                if (orderItem.LastDeliveryStatus != -1)
                {
                    _orderItemManager.SetStatus(pack.nodeId, orderItem.LastDeliveryStatus, eventId, false, false);
                }
                _orderItemManager.SetProviderDueDate(pack.nodeId, pack.providerDueDate, eventId);

                json.Success = true;
                json.Message = "Återlämningsdatum från utlånande bibliotek ändrat.";
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Misslyckades med att ändra återlämningsdatum från utlånande bibliotek: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        public class ChangeReturnDatePackage
        {
            public int nodeId;
            public string logMsg;
            public DateTime providerDueDate;
        }
    }
}
