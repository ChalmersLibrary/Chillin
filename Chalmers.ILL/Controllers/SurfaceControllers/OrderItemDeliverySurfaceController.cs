﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using umbraco.cms.businesslogic.member;
using Chalmers.ILL.Models;
using Chalmers.ILL.Utilities;
using Chalmers.ILL.Extensions;
using Microsoft.Exchange.WebServices.Data;
using System.Configuration;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Logging;
using Chalmers.ILL.UmbracoApi;
using Chalmers.ILL.Models.PartialPage;
using Chalmers.ILL.Templates;
using Chalmers.ILL.Mail;
using Chalmers.ILL.Models.Mail;
using Newtonsoft.Json;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{

    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemDeliverySurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;
        IInternalDbLogger _internalDbLogger;
        IUmbracoWrapper _umbraco;
        ITemplateService _templateService;
        IMailService _mailService;

        public OrderItemDeliverySurfaceController(IOrderItemManager orderItemManager, IInternalDbLogger internalDbLogger,
            IUmbracoWrapper umbraco, ITemplateService templateService, IMailService mailService)
        {
            _orderItemManager = orderItemManager;
            _internalDbLogger = internalDbLogger;
            _umbraco = umbraco;
            _templateService = templateService;
            _mailService = mailService;
        }

        /// <summary>
        /// Render the Partial View for sending mail to user from within the system
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderDeliveryAction(int nodeId)
        {
            var pageModel = new ChalmersILLActionDeliveryModel(_orderItemManager.GetOrderItem(nodeId));

            _umbraco.PopulateModelWithAvailableValues(pageModel);

            pageModel.ArticleDeliveryByMailTemplate = _templateService.GetTemplateData("ArticleDeliveryByMailTemplate", pageModel.OrderItem);
            pageModel.BookAvailableMailTemplate = _templateService.GetTemplateData("BookAvailableMailTemplate", pageModel.OrderItem);
            pageModel.BookSlipTemplate = _templateService.GetTemplateData("BookSlipTemplate", pageModel.OrderItem);

            // The return format depends on the client's Accept-header
            return PartialView("Chalmers.ILL.Action.Delivery", pageModel);
        }

        /// <summary>
        /// Send mail to user from within the system, possibly change status and log means of delivery
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <param name="newStatus">Means of delivery</param>
        /// <returns>JSON result</returns>
        [HttpPost]
        public ActionResult SetDelivery(int nodeId, string logEntry, string delivery)
        {
            var json = new ResultResponse();

            try
            {
                // Connect to Umbraco ContentService
                var contentService = UmbracoContext.Application.Services.ContentService;

                // Find OrderItem
                var contentNode = contentService.GetById(nodeId);

                // Log this action
                _internalDbLogger.WriteLogItemInternal(nodeId, "LEVERERAD", "Skickad med " + delivery, false, false);

                if (logEntry != "")
                {
                    _internalDbLogger.WriteLogItemInternal(nodeId, "LOG", logEntry, false, false);
                }

                // Set status = Levererad
                try
                {
                    _orderItemManager.SetOrderItemStatusInternal(nodeId, Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionName"], "05:Levererad"), false, false);
                }
                catch (Exception)
                {
                    throw;
                }

                // Save
                _orderItemManager.SaveWithoutEventsAndWithSynchronousReindexing(contentNode);

                // Construct JSON response for client (ie jQuery/getJSON)
                json.Success = true;
                json.Message = "Saved provider data.";
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Error: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Set that the order item is received and ready for the patron to fetch.
        /// </summary>
        /// <param name="orderNodeId">OrderItem Node Id</param>
        /// <param name="bookId">Delivery Library Book Id</param>
        /// <param name="dueDate">Delivery Library Due Date</param>
        /// <param name="providerInformation">Information about the provider</param>
        /// <returns>MVC ActionResult with JSON</returns>
        [HttpPost]
        public ActionResult SetOrderItemDeliveryReceived(string packJson)
        {
            var json = new ResultResponse();

            try
            {
                DeliveryReceivedPackage pack = JsonConvert.DeserializeObject<DeliveryReceivedPackage>(packJson);

                var orderItem = _orderItemManager.GetOrderItem(pack.orderNodeId);

                // Use internal method to set status property and log the result
                _orderItemManager.SetOrderItemDeliveryReceivedInternal(pack.orderNodeId, pack.bookId, pack.dueDate, pack.providerInformation, false, false);

                _internalDbLogger.WriteLogItemInternal(pack.orderNodeId, "LOG", pack.logMsg, false, false);

                _mailService.SendMail(new OutgoingMailModel(orderItem.OrderId, pack.mailData));
                _internalDbLogger.WriteLogItemInternal(pack.orderNodeId, "MAIL_NOTE", "Skickat mail till " + pack.mailData.recipientEmail, false, false);
                _internalDbLogger.WriteLogItemInternal(pack.orderNodeId, "MAIL", pack.mailData.message, true, true);

                // Construct JSON response for client (ie jQuery/getJSON)
                json.Success = true;
                json.Message = "Changed delivery item information to bookid:" + pack.bookId + " due date:+" + pack.dueDate + " provider information:" + pack.providerInformation;
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
        }
    }

}