﻿using Chalmers.ILL.Mail;
using Chalmers.ILL.Models.Mail;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Templates;
using Chalmers.ILL.UmbracoApi;
using System;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class OrderItemReceivedAtBranchSurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;
        IUmbracoWrapper _umbraco;
        ITemplateService _templateService;
        IMailService _mailService;

        public static int ARTICLE_RECEIVED_AT_BRANCH_EVENT_TYPE { get { return 29; } }

        public OrderItemReceivedAtBranchSurfaceController(IOrderItemManager orderItemManager, IUmbracoWrapper umbraco,
            ITemplateService templateService, IMailService mailService)
        {
            _orderItemManager = orderItemManager;
            _umbraco = umbraco;
            _templateService = templateService;
            _mailService = mailService;
        }

        /// <summary>
        /// If order item in transport, register it as received, and render page with information.
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderResponse(int nodeId)
        {
            var pageModel = new Models.Page.ReceivedAtBranchResultModel(_orderItemManager.GetOrderItem(nodeId));
            _umbraco.PopulateModelWithAvailableValues(pageModel);

            if (pageModel.OrderItem != null)
            {
                if (pageModel.OrderItem.Type == "Artikel" && pageModel.OrderItem.Status.Contains("Transport"))
                {
                    try
                    {
                        var orderItem = pageModel.OrderItem;
                        var eventId = _orderItemManager.GenerateEventId(ARTICLE_RECEIVED_AT_BRANCH_EVENT_TYPE);
                        _orderItemManager.AddLogItem(orderItem.NodeId, "LOG", "Transport genomförd - QR-kod scannad.", eventId, false, false);
                        var mailMessage = _templateService.GetTemplateData("ArticleAvailableInInfodiskMailTemplate", orderItem);
                        var mail = new OutgoingMailModel(orderItem.OrderId, orderItem.PatronName, orderItem.PatronEmail);
                        mail.message = mailMessage;
                        _mailService.SendMail(mail);
                        _orderItemManager.AddLogItem(orderItem.NodeId, "MAIL_NOTE", "Skickat automatiskt leveransmail till " + orderItem.PatronEmail, eventId, false, false);
                        _orderItemManager.AddLogItem(orderItem.NodeId, "MAIL", mailMessage, eventId, false, false);
                        _orderItemManager.SetStatus(orderItem.NodeId, "05:Levererad", eventId, true, true);

                        pageModel.IsSuccess = true;
                        pageModel.Message = "Arrival of article has been registered and email has been sent to patron.";
                    }
                    catch (Exception e)
                    {
                        pageModel.IsSuccess = false;
                        pageModel.Message = "Something went wrong when trying to register the item as received at branch.";
                    }
                }
                else
                {
                    pageModel.IsSuccess = false;
                    pageModel.Message = "Nothing to do with item.";
                }
            }
            else
            {
                pageModel.IsSuccess = false;
                pageModel.Message = "Nothing to do with item.";
            }

            return View("ReceivedAtBranchResult", pageModel);
        }
    }
}
