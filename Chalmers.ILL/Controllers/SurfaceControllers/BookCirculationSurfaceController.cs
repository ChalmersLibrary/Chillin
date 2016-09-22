using Chalmers.ILL.Models;
using Chalmers.ILL.OrderItems;
using System;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class BookCirculationSurfaceController : SurfaceController
    {
        public static int BOOK_RETURNED_FROM_BORROWER_EVENT_TYPE { get { return 23; } }

        IOrderItemManager _orderItemManager;

        public BookCirculationSurfaceController(IOrderItemManager orderItemManager)
        {
            _orderItemManager = orderItemManager;
        }

        [HttpPost]
        public ActionResult Returned(int nodeId)
        {
            var json = new ResultResponse();

            try
            {
                var eventId = _orderItemManager.GenerateEventId(BOOK_RETURNED_FROM_BORROWER_EVENT_TYPE);
                _orderItemManager.SetStatus(nodeId, "13:Transport", eventId);

                json.Success = true;
                json.Message = "Lyckades med att återlämna bok till filial.";
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Misslyckades med att returnera från filial: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }
    }
}