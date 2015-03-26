using System;
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
        IUmbracoWrapper _umbraco;
        ITemplateService _templateService;
        IMailService _mailService;

        public OrderItemDeliverySurfaceController(IOrderItemManager orderItemManager, IUmbracoWrapper umbraco, 
            ITemplateService templateService, IMailService mailService)
        {
            _orderItemManager = orderItemManager;
            _umbraco = umbraco;
            _templateService = templateService;
            _mailService = mailService;
        }

        /// <summary>
        /// Render the Partial View for the action of delivering an item.
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderDeliveryAction(int nodeId)
        {
            var pageModel = new ChalmersILLActionDeliveryModel(_orderItemManager.GetOrderItem(nodeId));
            _umbraco.PopulateModelWithAvailableValues(pageModel);
            return PartialView("Chalmers.ILL.Action.Delivery", pageModel);
        }

        /// <summary>
        /// Render the Partial View for the article by e-mail delivery type.
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderArticleByEmailDeliveryType(int nodeId)
        {
            var pageModel = new Models.PartialPage.DeliveryType.ArticleByEmail(_orderItemManager.GetOrderItem(nodeId));
            _umbraco.PopulateModelWithAvailableValues(pageModel);
            pageModel.ArticleDeliveryByMailTemplate = _templateService.GetTemplateData("ArticleDeliveryByMailTemplate", pageModel.OrderItem);
            pageModel.DrmWarning = pageModel.OrderItem.DrmWarning == "1" ? true : false;
            return PartialView("DeliveryType/ArticleByEmail", pageModel);
        }

        /// <summary>
        /// Render the Partial View for the article by mail or internal mail delivery type.
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderArticleByMailOrInternalMailDeliveryType(int nodeId)
        {
            var pageModel = new Models.PartialPage.DeliveryType.ArticleByMailOrInternalMail(_orderItemManager.GetOrderItem(nodeId));
            _umbraco.PopulateModelWithAvailableValues(pageModel);
            pageModel.DrmWarning = pageModel.OrderItem.DrmWarning == "1" ? true : false;
            return PartialView("DeliveryType/ArticleByMailOrInternalMail", pageModel);
        }

        /// <summary>
        /// Render the Partial View for the book instant loan delivery type.
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderBookInstantLoanDeliveryType(int nodeId)
        {
            var pageModel = new Models.PartialPage.DeliveryType.BookInstantLoan(_orderItemManager.GetOrderItem(nodeId));
            _umbraco.PopulateModelWithAvailableValues(pageModel);
            pageModel.BookAvailableMailTemplate = _templateService.GetTemplateData("BookAvailableMailTemplate", pageModel.OrderItem);
            return PartialView("DeliveryType/BookInstantLoan", pageModel);
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
                _orderItemManager.AddLogItem(nodeId, "LEVERERAD", "Skickad med " + delivery, false, false);

                if (logEntry != "")
                {
                    _orderItemManager.AddLogItem(nodeId, "LOG", logEntry, false, false);
                }

                // Set status = Levererad
                try
                {
                    _orderItemManager.SetStatus(nodeId, Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionName"], "05:Levererad"), false, false);
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

                _orderItemManager.SetDueDate(pack.orderNodeId, pack.dueDate, false, false);
                _orderItemManager.SetProviderDueDate(pack.orderNodeId, pack.dueDate, false, false);
                _orderItemManager.SetBookId(pack.orderNodeId, pack.bookId, false, false);
                _orderItemManager.SetProviderInformation(pack.orderNodeId, pack.providerInformation, false, false);

                _orderItemManager.SetStatus(pack.orderNodeId, Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionName"], "11:Infodisk"), false, false);
                _orderItemManager.AddLogItem(pack.orderNodeId, "LOG", pack.logMsg, false, false);

                _mailService.SendMail(new OutgoingMailModel(orderItem.OrderId, pack.mailData));
                _orderItemManager.AddLogItem(pack.orderNodeId, "MAIL_NOTE", "Skickat mail till " + pack.mailData.recipientEmail, false, false);
                _orderItemManager.AddLogItem(pack.orderNodeId, "MAIL", pack.mailData.message);

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
        }
    }

}