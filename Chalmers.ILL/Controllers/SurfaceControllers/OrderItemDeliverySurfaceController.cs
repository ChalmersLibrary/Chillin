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
        /// Render the Partial View for the article in infodisk delivery type.
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderArticleInInfodiskDeliveryType(int nodeId)
        {
            var pageModel = new Models.PartialPage.DeliveryType.ArticleInInfodisk(_orderItemManager.GetOrderItem(nodeId));
            _umbraco.PopulateModelWithAvailableValues(pageModel);
            pageModel.DrmWarning = pageModel.OrderItem.DrmWarning == "1" ? true : false;
            pageModel.ArticleDeliveryLibrary = GetArticleDeliveryLibrary(pageModel.OrderItem.SierraInfo.home_library);
            return PartialView("DeliveryType/ArticleInInfodisk", pageModel);
        }

        /// <summary>
        /// Render the Partial View for the article in transit delivery type.
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderArticleInTransitDeliveryType(int nodeId)
        {
            var pageModel = new Models.PartialPage.DeliveryType.ArticleInTransit(_orderItemManager.GetOrderItem(nodeId));
            _umbraco.PopulateModelWithAvailableValues(pageModel);
            pageModel.DrmWarning = pageModel.OrderItem.DrmWarning == "1" ? true : false;
            pageModel.ArticleDeliveryLibrary = GetArticleDeliveryLibrary(pageModel.OrderItem.SierraInfo.home_library);
            return PartialView("DeliveryType/ArticleInTransit", pageModel);
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
            pageModel.BookSlipTemplate = _templateService.GetTemplateData("BookSlipTemplate", pageModel.OrderItem);
            return PartialView("DeliveryType/BookInstantLoan", pageModel);
        }

        /// <summary>
        /// Render the Partial View for the book read at library delivery type.
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderBookReadAtLibraryDeliveryType(int nodeId)
        {
            var pageModel = new Models.PartialPage.DeliveryType.BookReadAtLibrary(_orderItemManager.GetOrderItem(nodeId));
            _umbraco.PopulateModelWithAvailableValues(pageModel);
            pageModel.BookAvailableForReadingAtLibraryMailTemplate = _templateService.GetTemplateData("BookAvailableForReadingAtLibraryMailTemplate", pageModel.OrderItem);
            return PartialView("DeliveryType/BookReadAtLibrary", pageModel);
        }

        /// <summary>
        /// Log message, set delivered status and log delivery type.
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <param name="logEntry">Log message</param>
        /// <param name="delivery">Type of delivery</param>
        /// <returns>JSON result</returns>
        [HttpPost]
        public ActionResult SetDelivery(int nodeId, string logEntry, string delivery)
        {
            var json = new ResultResponse();

            try
            {
                _orderItemManager.AddLogItem(nodeId, "LEVERERAD", "Leveranstyp: " + delivery, false, false);
                _orderItemManager.AddLogItem(nodeId, "LOG", logEntry, false, false);
                _orderItemManager.SetStatus(nodeId, "05:Levererad");

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
        /// Log message, set delivered status, log delivery type and send mail to user.
        /// </summary>
        /// <param name="packJson">The serialized object of type DeliveryByMailPackage.</param>
        /// <returns>JSON result</returns>
        [HttpPost]
        public ActionResult DeliverByMail(string packJson)
        {
            var res = new ResultResponse();

            try
            {
                var pack = JsonConvert.DeserializeObject<DeliverByMailPackage>(packJson);

                _orderItemManager.AddLogItem(pack.nodeId, "LEVERERAD", "Leveranstyp: Direktleverans via e-post.", false, false);
                _orderItemManager.AddLogItem(pack.nodeId, "LOG", pack.logEntry, false, false);
                _orderItemManager.SetStatus(pack.nodeId, "05:Levererad", false, false);

                // We save everything here first so that we get the new values injected into the message by the template service.
                _orderItemManager.SetPatronEmail(pack.nodeId, pack.mail.recipientEmail);

                // Overwrite the message with message from template service so that we get the new values injected.
                pack.mail.message = _templateService.GetTemplateData("ArticleDeliveryByMailTemplate", _orderItemManager.GetOrderItem(pack.nodeId));

                _mailService.SendMail(pack.mail);
                _orderItemManager.AddLogItem(pack.nodeId, "MAIL_NOTE", "Skickat mail till " + pack.mail.recipientEmail, false, false);
                _orderItemManager.AddLogItem(pack.nodeId, "MAIL", pack.mail.message);

                res.Success = true;
                res.Message = "Lyckades leverera via mail.";
            }
            catch (Exception e)
            {
                res.Success = false;
                res.Message = "Fel vid leveransförsök via mail: " + e.Message;
            }

            return Json(res, JsonRequestBehavior.AllowGet);
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

                if (pack.readOnlyAtLibrary)
                {
                    _orderItemManager.AddLogItem(pack.orderNodeId, "LEVERERAD", "Leveranstyp: Ej hemlån.", false, false);
                }
                else
                {
                    _orderItemManager.AddLogItem(pack.orderNodeId, "LEVERERAD", "Leveranstyp: Avhämtning i infodisk.", false, false);
                }
                _orderItemManager.SetDueDate(pack.orderNodeId, pack.dueDate, false, false);
                _orderItemManager.SetProviderDueDate(pack.orderNodeId, pack.dueDate, false, false);
                _orderItemManager.SetBookId(pack.orderNodeId, pack.bookId, false, false);
                _orderItemManager.SetProviderInformation(pack.orderNodeId, pack.providerInformation, false, false);

                _orderItemManager.SetStatus(pack.orderNodeId, "11:Utlånad", false, false);
                _orderItemManager.AddLogItem(pack.orderNodeId, "LOG", pack.logMsg, false, false);

                // We save everything here first so that we get the new values injected into the message by the template service.
                _orderItemManager.SetPatronEmail(pack.orderNodeId, pack.mailData.recipientEmail);

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

        public class DeliverByMailPackage
        {
            public int nodeId { get; set; }
            public string logEntry { get; set; }
            public OutgoingMailModel mail { get; set; }
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

        private string GetArticleDeliveryLibrary(string sierraHomeLibrary)
        {
            var res = "Kunde ej avgöra lämpligt leveransbibliotek.";
            if (sierraHomeLibrary != null && sierraHomeLibrary.Contains("hbib"))
            {
                res = "Huvudbiblioteket";
            }
            else if (sierraHomeLibrary != null && sierraHomeLibrary.Contains("lbib"))
            {
                res = "Lindholmenbiblioteket";
            }
            else if (sierraHomeLibrary != null && sierraHomeLibrary.Contains("abib"))
            {
                res = "Arkitekturbiblioteket";
            }
            return res;
        }
    }
}