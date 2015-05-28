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

                if (pack.logMsg != "")
                {
                    _orderItemManager.AddLogItem(pack.nodeId, "LOG", pack.logMsg, false, false);
                }

                _orderItemManager.SetProviderDueDate(pack.nodeId, pack.providerDueDate);

                json.Success = true;
                json.Message = "Återlämningsdatum mot låntagare ändrat.";
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Misslyckades med att ändra återlämningsdatum mot låntagare: " + e.Message;
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
