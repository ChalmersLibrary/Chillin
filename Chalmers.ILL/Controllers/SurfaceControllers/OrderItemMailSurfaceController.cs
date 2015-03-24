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
using System.Globalization;
using System.Text.RegularExpressions;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Mail;
using Chalmers.ILL.Models.Mail;
using Chalmers.ILL.Models.PartialPage;
using Chalmers.ILL.UmbracoApi;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemMailSurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;
        IExchangeMailWebApi _exchangeMailWebApi;
        IUmbracoWrapper _dataTypes;
        IMailService _mailService;

        public OrderItemMailSurfaceController(IOrderItemManager orderItemManager, IExchangeMailWebApi exchangeMailWebApi, 
            IUmbracoWrapper dataTypes, IMailService mailService)
        {
            _orderItemManager = orderItemManager;
            _exchangeMailWebApi = exchangeMailWebApi;
            _dataTypes = dataTypes;
            _mailService = mailService;
        }

        /// <summary>
        /// Render the Partial View for sending mail to user from within the system
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderMailAction(int nodeId)
        {
            var model = new ChalmersILLActionMailModel(_orderItemManager.GetOrderItem(nodeId));

            _dataTypes.PopulateModelWithAvailableValues(model);

            // The return format depends on the client's Accept-header
            return PartialView("Chalmers.ILL.Action.Mail", model);
        }

        /// <summary>
        /// Send mail to user from within the system, possibly change status and PatronEmail
        /// </summary>
        /// <param name="m">The data for the outgoing mail.</param>
        /// <returns>JSON result</returns>
        [HttpPost, ValidateInput(false)]
        public ActionResult SendMail(OutgoingMailPackageModel m)
        {
            var json = new ResultResponse();

            try
            {
                // Connect to Umbraco ContentService
                var contentService = UmbracoContext.Application.Services.ContentService;

                // Find OrderItem
                var contentNode = contentService.GetById(m.nodeId);

                // Read current values that can be affected
                var orderItem = _orderItemManager.GetOrderItem(m.nodeId);
                var currentPatronEmail = orderItem.PatronEmail;
                var currentStatus = orderItem.StatusPrevalue;

                // Send mail to recipient
                try
                {
                    _mailService.SendMail(new OutgoingMailModel(orderItem.OrderId, m));

                    _orderItemManager.WriteLogItemInternal(m.nodeId, "MAIL_NOTE", "Skickat mail till " + m.recipientEmail, false, false);
                    _orderItemManager.WriteLogItemInternal(m.nodeId, "MAIL", m.message, false, false);
                }
                catch (Exception)
                {
                    throw;
                }

                // Set PatronEmail property if it differs from recipientEmail
                if (currentPatronEmail != m.recipientEmail)
                {
                    contentNode.SetValue("patronEmail", m.recipientEmail);
                    _orderItemManager.WriteLogItemInternal(m.nodeId, "MAIL_NOTE", "PatronEmail ändrad till " + m.recipientEmail, false, false);
                }

                // Set FollowUpDate property if it differs from current
                DateTime currentFollowUpDate = orderItem.FollowUpDate;

                if (!String.IsNullOrEmpty(m.newFollowUpDate))
	            {
                    DateTime parsedNewFollowUpDate = Convert.ToDateTime(m.newFollowUpDate);
                    if (currentFollowUpDate != parsedNewFollowUpDate)
                    {
                        _orderItemManager.SetFollowUpDate(m.nodeId, parsedNewFollowUpDate, false, false);
                        _orderItemManager.WriteLogItemInternal(m.nodeId, "DATE", "Följs upp senast " + m.newFollowUpDate, false, false);
                    }
	            }

                // Set status property if it differs from newStatus and if it is not -1 (no change)
                if (orderItem.Status != m.newStatusId && orderItem.Status != -1)
                {
                    _orderItemManager.SetOrderItemStatusInternal(m.nodeId, m.newStatusId, false, false);
                }

                // Update cancellation reason if we have a value that is not -1 (no change)
                if (orderItem.CancellationReason != m.newCancellationReasonId && m.newCancellationReasonId != -1)
                {
                    _orderItemManager.SetOrderItemCancellationReasonInternal(m.nodeId, m.newCancellationReasonId, false, false);
                }

                // Update purchased material if we have a value that is not -1 (no change)
                if (orderItem.PurchasedMaterial != m.newPurchasedMaterialId && m.newPurchasedMaterialId != -1)
                {
                    _orderItemManager.SetOrderItemPurchasedMaterialInternal(m.nodeId, m.newPurchasedMaterialId, false, false);
                }

                _orderItemManager.SaveWithoutEventsAndWithSynchronousReindexing(contentNode);

                // Construct JSON response for client (ie jQuery/getJSON)
                json.Success = true;
                json.Message = "Sent mail and eventually changed some properties.";
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Error: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Send mail to create a new order.
        /// </summary>
        /// <remarks>Mainly used for testing purposes.</remarks>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <param name="recipientEmail">User Email</param>
        /// <param name="recipientName">User Name</param>
        /// <param name="message">Mail message</param>
        /// <param name="newStatus">Change OrderItem status</param>
        /// <returns>JSON result</returns>
        [HttpPost, ValidateInput(false)]
        public ActionResult SendMailForNewOrder(string message, string name, string mail, string libraryCardNumber)
        {
            var json = new ResultResponse();

            try
            {
                string body = "<div id='chalmers.ill.orderitem'>\n" +
                        "<div id='OriginalOrder'>" + message + "</div>\n" +
                        "<div id='PatronName'>" + name + "</div>\n" +
                        "<div id='PatronEmail'>" + mail + "</div>\n" +
                        "<div id='PatronCardNo'>" + libraryCardNumber + "</div>\n" +
                        "<div id='Purchase'>False</div>\n" +
                    "</div>\n";
                _exchangeMailWebApi.ConnectToExchangeService(ConfigurationManager.AppSettings["chalmersIllExhangeLogin"], ConfigurationManager.AppSettings["chalmersIllExhangePass"]);
                _exchangeMailWebApi.SendPlainMailMessage(body, "New request from TEST #new", ConfigurationManager.AppSettings["chalmersIllSenderAddress"]);

                json.Success = true;
                json.Message = "Sent mail for new order.";
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