﻿using Chalmers.ILL.Mail;
using Chalmers.ILL.Models;
using Chalmers.ILL.Models.Mail;
using Chalmers.ILL.Models.PartialPage;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Services;
using Chalmers.ILL.Templates;
using Chalmers.ILL.UmbracoApi;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemReceiveBookSurfaceController : SurfaceController
    {
        public static int EVENT_TYPE { get { return 10; } }
        public static int BOOK_RECEIVED_AT_BRANCH_EVENT_TYPE { get { return 25; } }

        private readonly IUmbracoWrapper _umbraco;
        private readonly IOrderItemManager _orderItemManager;
        private readonly ITemplateService _templateService;
        private readonly IMailService _mailService;
        private readonly IFolioService _folioService;

        public OrderItemReceiveBookSurfaceController(
            IUmbracoWrapper umbraco, 
            IOrderItemManager orderItemManager, 
            ITemplateService templateService, 
            IMailService mailService,
            IFolioService folioService)
        {
            _orderItemManager = orderItemManager;
            _templateService = templateService;
            _mailService = mailService;
            _umbraco = umbraco;
            _folioService = folioService;
        }

        [HttpGet]
        public ActionResult RenderReceiveBookAction(int nodeId)
        {
            var pageModel = new ChalmersILLActionReceiveBookModel(_orderItemManager.GetOrderItem(nodeId));
            _umbraco.PopulateModelWithAvailableValues(pageModel);
            pageModel.BookAvailableMailTemplate = _templateService.GetTemplateData("BookAvailableMailTemplate");
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

                //FOLIO
                var instance = new InstanceBasic
                {
                    Title = pack.Title,
                    Source = "External",
                    StatusId = ConfigurationManager.AppSettings["statusId"].ToString(),
                    DiscoverySuppress = true,
                    InstanceTypeId = ConfigurationManager.AppSettings["instanceTypeId"].ToString(),
                    Identifiers = new Identifier[] 
                    { 
                        new Identifier 
                        { 
                            Value = pack.OrderId, 
                            IdentifierTypeId = ConfigurationManager.AppSettings["identifierTypeId"].ToString()
                        } 
                    }
                };

                 _folioService.InitFolio(instance, pack.bookId, pack.PickUpServicePoint, pack.readOnlyAtLibrary);

                //---

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

                // Overwrite the message with message from template service so that we get the new values injected.
                if (pack.readOnlyAtLibrary)
                {
                    _orderItemManager.SetReadOnlyAtLibrary(pack.orderNodeId, true, eventId, false, false);
                }
                else
                {
                    _orderItemManager.SetReadOnlyAtLibrary(pack.orderNodeId, false, eventId, false, false);
                }

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


        /// <summary>
        /// Set that the order item is received at branch and ready for the patron to fetch.
        /// </summary>
        /// <param name="orderNodeId">OrderItem Node Id</param>
        /// <param name="bookId">Delivery Library Book Id</param>
        /// <param name="dueDate">Delivery Library Due Date</param>
        /// <param name="providerInformation">Information about the provider</param>
        /// <returns>MVC ActionResult with JSON</returns>
        [HttpPost, ValidateInput(false)]
        public ActionResult SetOrderItemDeliveryReceivedAtBranch(int nodeId)
        {
            var json = new ResultResponse();

            try
            {
                DeliveryReceivedPackage pack = new DeliveryReceivedPackage();
                pack.orderNodeId = nodeId;

                var orderItem = _orderItemManager.GetOrderItem(pack.orderNodeId);

                pack.readOnlyAtLibrary = orderItem.ReadOnlyAtLibrary;

                var eventId = _orderItemManager.GenerateEventId(BOOK_RECEIVED_AT_BRANCH_EVENT_TYPE);

                if (pack.readOnlyAtLibrary)
                {
                    _orderItemManager.AddLogItem(pack.orderNodeId, "LEVERERAD", "Leveranstyp: Ej hemlån.", eventId, false, false);
                }
                else
                {
                    _orderItemManager.AddLogItem(pack.orderNodeId, "LEVERERAD", "Leveranstyp: Avhämtning i infodisk.", eventId, false, false);
                }
                _orderItemManager.SetStatus(pack.orderNodeId, "14:Infodisk", eventId, false, false);

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
            public string logMsg { get; set; }
            public bool readOnlyAtLibrary { get; set; }
            public string Title { get; set; }
            public string OrderId { get; set; }
            public string PickUpServicePoint { get; set; }
            public OutgoingMailPackageModel mailData { get; set; }
        }
    }
}