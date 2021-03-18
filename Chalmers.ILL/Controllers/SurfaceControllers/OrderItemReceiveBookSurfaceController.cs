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
                    Title = "Chillin kalle kula vilda äventyr",
                    Source = "External",
                    StatusId = "daf2681c-25af-4202-a3fa-e58fdf806183",
                    DiscoverySuppress = true,
                    InstanceTypeId = "30fffe0e-e985-4144-b2e2-1e8179bdb41f",
                    Identifiers = new Identifier[] { new Identifier { Value = , IdentifierTypeId = "2e8b3b6c-0e7d-4e48-bca2-b0b23b376af5" } }
                };

                 _folioService.InitFolio(instance, pack.bookId); //Lägga till servicePointPickUp

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

                // We save everything here first so that we get the new values injected into the message by the template service.
                _orderItemManager.SetPatronEmail(pack.orderNodeId, pack.mailData.recipientEmail, eventId);

                // Overwrite the message with message from template service so that we get the new values injected.
                if (pack.readOnlyAtLibrary)
                {
                    pack.mailData.message = _templateService.ReplaceMoustaches("BookAvailableForReadingAtLibraryMailTemplate", 
                        pack.mailData.message,  _orderItemManager.GetOrderItem(pack.orderNodeId));
                    _orderItemManager.SetReadOnlyAtLibrary(pack.orderNodeId, true, eventId, false, false);
                }
                else
                {
                    pack.mailData.message = _templateService.ReplaceMoustaches("BookAvailableMailTemplate", 
                        pack.mailData.message, _orderItemManager.GetOrderItem(pack.orderNodeId));
                    _orderItemManager.SetReadOnlyAtLibrary(pack.orderNodeId, false, eventId, false, false);
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

        /// <summary>
        /// Set that the order item is received and ready for transport.
        /// </summary>
        /// <param name="orderNodeId">OrderItem Node Id</param>
        /// <param name="bookId">Delivery Library Book Id</param>
        /// <param name="dueDate">Delivery Library Due Date</param>
        /// <param name="providerInformation">Information about the provider</param>
        /// <returns>MVC ActionResult with JSON</returns>
        [HttpPost, ValidateInput(false)]
        public ActionResult SetOrderItemDeliveryReceivedForTransport(string packJson)
        {
            var json = new ResultResponse();

            try
            {
                DeliveryReceivedPackage pack = JsonConvert.DeserializeObject<DeliveryReceivedPackage>(packJson);

                var orderItem = _orderItemManager.GetOrderItem(pack.orderNodeId);

                var eventId = _orderItemManager.GenerateEventId(EVENT_TYPE);

                if (pack.readOnlyAtLibrary)
                {
                    _orderItemManager.AddLogItem(pack.orderNodeId, "TRANSPORT", "Transporttyp: Ej hemlån.", eventId, false, false);
                    _orderItemManager.SetReadOnlyAtLibrary(pack.orderNodeId, true, eventId, false, false);
                }
                else
                {
                    _orderItemManager.AddLogItem(pack.orderNodeId, "TRANSPORT", "Transporttyp: Avhämtning i infodisk.", eventId, false, false);
                    _orderItemManager.SetReadOnlyAtLibrary(pack.orderNodeId, false, eventId, false, false);
                }
                _orderItemManager.SetDueDate(pack.orderNodeId, pack.dueDate, eventId, false, false);
                _orderItemManager.SetProviderDueDate(pack.orderNodeId, pack.dueDate, eventId, false, false);
                _orderItemManager.SetBookId(pack.orderNodeId, pack.bookId, eventId, false, false);
                _orderItemManager.SetProviderInformation(pack.orderNodeId, pack.providerInformation, eventId, false, false);
                _orderItemManager.SetStatus(pack.orderNodeId, "13:Transport", eventId, false, false);
                _orderItemManager.AddLogItem(pack.orderNodeId, "LOG", pack.logMsg, eventId, true, true);
                
                json.Success = true;
                json.Message = "Transport till filial påbörjad.";
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
                pack.mailData = new OutgoingMailPackageModel();
                pack.orderNodeId = nodeId;

                var orderItem = _orderItemManager.GetOrderItem(pack.orderNodeId);

                pack.readOnlyAtLibrary = orderItem.ReadOnlyAtLibrary;
                pack.mailData.recipientEmail = orderItem.PatronEmail;

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
            public OutgoingMailPackageModel mailData { get; set; }
            public string logMsg { get; set; }
            public bool readOnlyAtLibrary { get; set; }
        }
    }
}