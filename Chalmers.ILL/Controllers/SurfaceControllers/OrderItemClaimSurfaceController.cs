using Chalmers.ILL.Mail;
using Chalmers.ILL.Models;
using Chalmers.ILL.Models.Mail;
using Chalmers.ILL.Models.PartialPage;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Templates;
using Chalmers.ILL.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemClaimSurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;
        ITemplateService _templateService;
        IMailService _mailService;

        public OrderItemClaimSurfaceController(IOrderItemManager orderItemManager, ITemplateService templateService, IMailService mailService)
        {
            _orderItemManager = orderItemManager;
            _templateService = templateService;
            _mailService = mailService;
        }

        [HttpGet]
        public ActionResult RenderClaimAction(int nodeId)
        {
            var pageModel = new ChalmersILLActionClaimModel(_orderItemManager.GetOrderItem(nodeId));

            pageModel.ClaimBookMailTemplate = _templateService.GetTemplateData("ClaimBookMailTemplate", pageModel.OrderItem);
            pageModel.ClaimDueDate = DateTime.Now > pageModel.OrderItem.DueDate ? pageModel.OrderItem.DueDate : DateTime.Now;

            return PartialView("Chalmers.ILL.Action.Claim", pageModel);
        }

        [HttpPost]
        public ActionResult ClaimItem(string packJson)
        {
            var json = new ResultResponse();

            try
            {
                var pack = JsonConvert.DeserializeObject<ClaimItemPackage>(packJson);

                _orderItemManager.SetDueDate(pack.nodeId, pack.dueDate, false, false);
                _orderItemManager.SetProviderDueDate(pack.nodeId, pack.dueDate, false, false);
                _orderItemManager.SetStatus(pack.nodeId, Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionName"], "12:Krävd"), false, false);
                
                // We save everything here first so that we get the new values injected into the message by the template service.
                _orderItemManager.SetPatronEmail(pack.nodeId, pack.mail.recipientEmail);

                // Overwrite the message with message from template service so that we get the new values injected.
                pack.mail.message = _templateService.GetTemplateData("ClaimBookMailTemplate", _orderItemManager.GetOrderItem(pack.nodeId));

                _mailService.SendMail(pack.mail);
                _orderItemManager.AddLogItem(pack.nodeId, "MAIL_NOTE", "Skickat mail till " + pack.mail.recipientEmail, false, false);
                _orderItemManager.AddLogItem(pack.nodeId, "MAIL", pack.mail.message);

                json.Success = true;
                json.Message = "Krav genomfört.";
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Misslyckades med att kräva: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        public class ClaimItemPackage
        {
            public int nodeId;
            public DateTime dueDate;
            public OutgoingMailModel mail;
        }
    }
}
