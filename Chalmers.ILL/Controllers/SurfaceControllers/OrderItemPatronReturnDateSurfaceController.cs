using Chalmers.ILL.Mail;
using Chalmers.ILL.Models;
using Chalmers.ILL.Models.Mail;
using Chalmers.ILL.Models.PartialPage;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Templates;
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
    public class OrderItemPatronReturnDateSurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;
        ITemplateService _templateService;
        IMailService _mailService;

        public OrderItemPatronReturnDateSurfaceController(IOrderItemManager orderItemManager, ITemplateService templateService, IMailService mailService)
        {
            _orderItemManager = orderItemManager;
            _templateService = templateService;
            _mailService = mailService;
        }

        /// <summary>
        /// Render the Partial View for changing the return date against the patron.
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderPatronReturnDateAction(int nodeId)
        {
            var pageModel = new ChalmersILLActionPatronReturnDateModel(_orderItemManager.GetOrderItem(nodeId));

            pageModel.ReturnDateChangedMailTemplate = _templateService.GetTemplateData("ReturnDateChangedMailTemplate", pageModel.OrderItem);

            // The return format depends on the client's Accept-header
            return PartialView("Chalmers.ILL.Action.PatronReturnDate", pageModel);
        }

        [HttpPost]
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

                _orderItemManager.SetDueDate(pack.nodeId, pack.dueDate, false, false);
                _orderItemManager.SetProviderDueDate(pack.nodeId, pack.dueDate, false, false);

                // We save everything here first so that we get the new values injected into the message by the template service.
                _orderItemManager.SetPatronEmail(pack.nodeId, pack.mail.recipientEmail);

                // Overwrite the message with message from template service so that we get the new values injected.
                pack.mail.message = _templateService.GetTemplateData("ReturnDateChangedMailTemplate", _orderItemManager.GetOrderItem(pack.nodeId));

                _mailService.SendMail(pack.mail);
                _orderItemManager.AddLogItem(pack.nodeId, "MAIL_NOTE", "Skickat mail till " + pack.mail.recipientEmail, false, false);
                _orderItemManager.AddLogItem(pack.nodeId, "MAIL", pack.mail.message);

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
            public DateTime dueDate;
            public OutgoingMailModel mail;
        }
    }
}
