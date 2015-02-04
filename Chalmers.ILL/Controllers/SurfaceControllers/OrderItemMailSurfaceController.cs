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
using System.Globalization;
using System.Text.RegularExpressions;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{

    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemMailSurfaceController : SurfaceController
    {
        /// <summary>
        /// Render the Partial View for sending mail to user from within the system
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderMailAction(int nodeId)
        {
            // Get a new OrderItem populated with values for this node
            var orderItem = OrderItem.GetOrderItem(nodeId);

            // The return format depends on the client's Accept-header
            return PartialView("Chalmers.ILL.Action.Mail", orderItem);
        }

        /// <summary>
        /// Send mail to user from within the system, possibly change status and PatronEmail
        /// </summary>
        /// <param name="m">The data for the outgoing mail.</param>
        /// <returns>JSON result</returns>
        [HttpPost, ValidateInput(false)]
        public ActionResult SendMail(OutgoingMailModel m)
        {
            var json = new ResultResponse();

            try
            {
                // Connect to Umbraco ContentService
                var contentService = UmbracoContext.Application.Services.ContentService;

                // Find OrderItem
                var contentNode = contentService.GetById(m.nodeId);

                // Read current values that can be affected
                var currentPatronEmail = OrderItem.GetOrderItem(m.nodeId).PatronEmail;
                var currentStatus = OrderItem.GetOrderItem(m.nodeId).StatusPrevalue;

                // Send mail to recipient
                try
                {
                    var attachments = new Dictionary<string, byte[]>();
                    if (m.attachments != null)
                    {
                        var ms = UmbracoContext.Application.Services.MediaService;
                        var originalFilenamePattern = new Regex(@"^cthb-[a-zA-Z0-9]+-[0-9]+-(.*\.(?:pdf|tif|tiff))", RegexOptions.IgnoreCase);
                        foreach (var mediaId in m.attachments)
                        {
                            var c = ms.GetById(mediaId);
                            if (c != null)
                            {
                                var data = System.IO.File.ReadAllBytes(Server.MapPath(c.GetValue("file").ToString()));
                                var match = originalFilenamePattern.Match(c.Name);
                                if (match.Groups.Count > 1)
                                {
                                    attachments.Add(match.Groups[1].Value, data);
                                }
                                else
                                {
                                    throw new Exception("Failed to extract file name for attachment " + c.Name + ". Can only send pdf, tif and tiff files.");
                                }
                            }
                            else
                            {
                                throw new Exception("Failed to fetch media item for id " + mediaId + ".");
                            }
                        }
                    }
                    string body = m.message + ConfigurationManager.AppSettings["chalmersILLMailSignature"].Replace("\\n", "\n");
                    ExchangeService service = ExchangeMailWebApi.ConnectToExchangeService(ConfigurationManager.AppSettings["chalmersIllExhangeLogin"], ConfigurationManager.AppSettings["chalmersIllExhangePass"]);
                    ExchangeMailWebApi.SendMailMessage(service, OrderItem.GetOrderItem(m.nodeId).OrderId, body, ConfigurationManager.AppSettings["chalmersILLMailSubject"], m.recipientName, m.recipientEmail, attachments);
                    Logging.WriteLogItemInternal(m.nodeId, "MAIL_NOTE", "Skickat mail till " + m.recipientEmail, false, false);
                    Logging.WriteLogItemInternal(m.nodeId, "MAIL", m.message, false, false);
                }
                catch (Exception)
                {
                    throw;
                }

                // Set PatronEmail property if it differs from recipientEmail
                if (currentPatronEmail != m.recipientEmail)
                {
                    contentNode.SetValue("patronEmail", m.recipientEmail);
                    Logging.WriteLogItemInternal(m.nodeId, "MAIL_NOTE", "PatronEmail ändrad till " + m.recipientEmail, false, false);
                }

                // Set FollowUpDate property if it differs from current
                DateTime currentFollowUpDate = OrderItem.GetOrderItem(m.nodeId).FollowUpDate;

                if (!String.IsNullOrEmpty(m.newFollowUpDate))
	            {
                    DateTime parsedNewFollowUpDate = Convert.ToDateTime(m.newFollowUpDate);
                    if (currentFollowUpDate != parsedNewFollowUpDate)
                    {
                        OrderItem.SetFollowUpDate(m.nodeId, parsedNewFollowUpDate, false, false);
                        Logging.WriteLogItemInternal(m.nodeId, "DATE", "Följs upp senast " + m.newFollowUpDate, false, false);
                    }
	            }

                // Set status property if it differs from newStatus
                if (currentStatus != m.newStatus)
                {
                    OrderItemStatus.SetOrderItemStatusInternal(m.nodeId, Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionGuid"], m.newStatus), false, false);
                }

                contentService.SaveWithoutEventsAndWithSynchronousReindexing(contentNode);

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
                ExchangeService service = ExchangeMailWebApi.ConnectToExchangeService(ConfigurationManager.AppSettings["chalmersIllExhangeLogin"], ConfigurationManager.AppSettings["chalmersIllExhangePass"]);
                ExchangeMailWebApi.SendPlainMailMessage(service, body, "New request from TEST #new", ConfigurationManager.AppSettings["chalmersIllSenderAddress"]);

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