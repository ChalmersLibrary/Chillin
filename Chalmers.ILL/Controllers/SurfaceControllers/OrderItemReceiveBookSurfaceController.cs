using Chalmers.ILL.Mail;
using Chalmers.ILL.Models;
using Chalmers.ILL.Models.Mail;
using Chalmers.ILL.Models.PartialPage;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Templates;
using Chalmers.ILL.UmbracoApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemReceiveBookSurfaceController : SurfaceController
    {
        public static int EVENT_TYPE { get { return 10; } }

        IUmbracoWrapper _umbraco;
        IOrderItemManager _orderItemManager;
        ITemplateService _templateService;
        IMailService _mailService;

        public OrderItemReceiveBookSurfaceController(IUmbracoWrapper umbraco, IOrderItemManager orderItemManager, ITemplateService templateService, 
            IMailService mailService)
        {
            _orderItemManager = orderItemManager;
            _templateService = templateService;
            _mailService = mailService;
            _umbraco = umbraco;
        }

        [HttpGet]
        public ActionResult RenderReceiveBookAction(int nodeId)
        {
            var pageModel = new ChalmersILLActionReceiveBookModel(_orderItemManager.GetOrderItem(nodeId));
            _umbraco.PopulateModelWithAvailableValues(pageModel);
            pageModel.BookAvailableMailTemplate = _templateService.GetTemplateData("BookAvailableMailTemplate", pageModel.OrderItem);
            return PartialView("Chalmers.ILL.Action.ReceiveBook", pageModel);
        }

        /// <summary>
        /// Set that the order item is received and ready for the patron to fetch.
        /// </summary>
        /// <param name="orderNodeId">OrderItem Node Id</param>
        /// <param name="bookId">Delivery Library Book Id</param>
        /// <param name="dueDate">Delivery Library Due Date</param>
        /// <param name="providerInformation">Information about the provider</param>
        /// <returns>MVC ActionResult with JSON</returns>
        [HttpPost, ValidateInput(false)]
        public ActionResult SetOrderItemDeliveryReceived(string packJson)
        {
            var json = new ResultResponse();

            try
            {
                DeliveryReceivedPackage pack = JsonConvert.DeserializeObject<DeliveryReceivedPackage>(packJson);

                var orderItem = _orderItemManager.GetOrderItem(pack.orderNodeId);

                var eventId = _orderItemManager.GenerateEventId(EVENT_TYPE);

                if (pack.readOnlyAtLibrary)
                {
                    _orderItemManager.AddLogItem(pack.orderNodeId, "LEVERERAD", "Leveranstyp: Ej hemlån.", eventId, false, false);
                }
                else
                {
                    _orderItemManager.AddLogItem(pack.orderNodeId, "LEVERERAD", "Leveranstyp: Avhämtning i infodisk.", eventId, false, false);
                }
                _orderItemManager.SetDueDate(pack.orderNodeId, pack.dueDate, eventId, false, false);
                _orderItemManager.SetProviderDueDate(pack.orderNodeId, pack.dueDate, eventId, false, false);
                _orderItemManager.SetBookId(pack.orderNodeId, pack.bookId, eventId, false, false);
                _orderItemManager.SetProviderInformation(pack.orderNodeId, pack.providerInformation, eventId, false, false);
                _orderItemManager.SetStatus(pack.orderNodeId, "14:Infodisk", eventId, false, false);
                _orderItemManager.AddLogItem(pack.orderNodeId, "LOG", pack.logMsg, eventId, false, false);

                // We save everything here first so that we get the new values injected into the message by the template service.
                _orderItemManager.SetPatronEmail(pack.orderNodeId, pack.mailData.recipientEmail, eventId);

                // Overwrite the message with message from template service so that we get the new values injected.
                if (pack.readOnlyAtLibrary)
                {
                    pack.mailData.message = _templateService.GetTemplateData("BookAvailableForReadingAtLibraryMailTemplate", _orderItemManager.GetOrderItem(pack.orderNodeId));
                }
                else
                {
                    pack.mailData.message = _templateService.GetTemplateData("BookAvailableMailTemplate", _orderItemManager.GetOrderItem(pack.orderNodeId));
                }

                _mailService.SendMail(new OutgoingMailModel(orderItem.OrderId, pack.mailData));
                _orderItemManager.AddLogItem(pack.orderNodeId, "MAIL_NOTE", "Skickat mail till " + pack.mailData.recipientEmail, eventId, false, false);
                _orderItemManager.AddLogItem(pack.orderNodeId, "MAIL", pack.mailData.message, eventId);

                json.Success = true;
                json.Message = "Leverans till infodisk genomförd.";
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Error: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        public class DeliveryReceivedPackage
        {
            public int orderNodeId { get; set; }
            public string bookId { get; set; }
            public DateTime dueDate { get; set; }
            public string providerInformation { get; set; }
            public OutgoingMailPackageModel mailData { get; set; }
            public string logMsg { get; set; }
            public bool readOnlyAtLibrary { get; set; }
        }
    }
}