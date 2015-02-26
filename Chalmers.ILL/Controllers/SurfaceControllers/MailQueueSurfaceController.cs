using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using Chalmers.ILL.Models;
using Chalmers.ILL.Utilities;
using umbraco.cms.businesslogic.member;
using System.Configuration;
using Microsoft.Exchange.WebServices.Data;
using Npgsql;
using System.Globalization;
using Examine;
using Examine.SearchCriteria;
using UmbracoExamine;
using Chalmers.ILL.SignalR;
using Chalmers.ILL.Extensions;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Media;
using umbraco.cms.businesslogic.media;
using System.IO;
using Umbraco.Core.Services;
using System.Threading;
using Umbraco.Core.Events;
using Newtonsoft.Json;
using HtmlAgilityPack;
using System.Text;
using System.Text.RegularExpressions;
using Chalmers.ILL.Patron;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Logging;
using Chalmers.ILL.Mail;
using Chalmers.ILL.UmbracoApi;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class MailQueueSurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;
        INotifier _notifier;
        IInternalDbLogger _internalDbLogger;
        IExchangeMailWebApi _exchangeMailWebApi;
        IUmbracoWrapper _dataTypes;

        public MailQueueSurfaceController(IOrderItemManager orderItemManager, INotifier notifier, 
            IInternalDbLogger internalDbLogger, IExchangeMailWebApi exchangeMailWebApi, IUmbracoWrapper dataTypes)
        {
            _orderItemManager = orderItemManager;
            _notifier = notifier;
            _internalDbLogger = internalDbLogger;
            _exchangeMailWebApi = exchangeMailWebApi;
            _dataTypes = dataTypes;
        }

        /// <summary>
        /// Basic method for reading from mail queue in Exchange
        /// </summary>
        /// <param name="username">Exchange username</param>
        /// <param name="password">Exchange password</param>
        /// <returns>Json</returns>
        [HttpPost]
        public ActionResult ReadFromQueue(string username, string password)
        {
            convertOrdersWithExpiredFollowUpDateAndCertainStatusToNewStatus();

            signalExpiredFollowUpDates();

            // Possible result JSON when something goes wrong
            var json = new ResultResponse();

            // List of read mails
            List<MailQueueModel> list = null;

            // Connect to Exchange Service
            try
            {
                _exchangeMailWebApi.ConnectToExchangeService(ConfigurationManager.AppSettings["chalmersIllExhangeLogin"], ConfigurationManager.AppSettings["chalmersIllExhangePass"]);
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Error connecting to Exchange Service: " + e.Message;
                LogHelper.Error<MailQueueSurfaceController>("Error connecting to Exchange Service", e);
                return Json(json, JsonRequestBehavior.DenyGet);
            }

            // Get a list of mails from Inbox folder
            try
            {
                list = _exchangeMailWebApi.ReadMailQueue();
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Error reading mail from Exchange: " + e.Message;
                LogHelper.Error<MailQueueSurfaceController>("Error reading mail from Exchange", e);
                return Json(json, JsonRequestBehavior.DenyGet);
            }

            // Post processing of e-mails
            if (list.Count > 0)
            {
                try
                {
                    string deliveryOrderId;
                    foreach (MailQueueModel item in list)
                    {
                        try
                        {
                            var orderIdPattern = new Regex("[#+](cthb-.{8}-[0-9]+)");
                            deliveryOrderId = getBoundOrder(item);
                            // Bind the type of messages we have (NEW or REPLY)
                            if ((item.Subject != null && item.Subject.Contains("#new")) || item.To.Contains("+new"))
                            {
                                item.Type = MailQueueType.NEW;
                            }
                            else if ((item.Subject != null && item.Subject.Contains("#cthb-")))
                            {
                                item.OrderId = orderIdPattern.Match(item.Subject).Groups[1].Value;
                                item.OrderItemNodeId = Convert.ToInt32(item.OrderId.Split('-').Last());
                                item.Type = MailQueueType.REPLY;
                            }
                            else if (item.To.Contains("+cthb-"))
                            {
                                item.OrderId = orderIdPattern.Match(item.To).Groups[1].Value;
                                item.OrderItemNodeId = Convert.ToInt32(item.OrderId.Split('-').Last());
                                item.Type = MailQueueType.REPLY;
                            }
                            else if (deliveryOrderId != null)
                            {
                                item.OrderId = deliveryOrderId;
                                item.OrderItemNodeId = Convert.ToInt32(deliveryOrderId.Split('-').Last());
                                item.Type = MailQueueType.DELIVERY;
                            }
                            else
                            {
                                item.Type = MailQueueType.UNKNOWN;
                            }
                        }
                        catch (Exception e)
                        {
                            item.Type = MailQueueType.ERROR;
                            item.ParseErrorMessage = "Chillin failed to process E-mail. Reason: " + e.Message;
                            LogHelper.Error<MailQueueSurfaceController>("Failed to process one E-mail, tagging it with ERROR.", e);
                        }
                    }
                }
                catch (Exception e)
                {
                    json.Success = false;
                    json.Message = "Error when post processing E-mails: " + e.Message;
                    LogHelper.Error<MailQueueSurfaceController>("Error when post processing E-mails.", e);
                    return Json(json, JsonRequestBehavior.DenyGet);
                }

                // Get SierraInfo to the list
                try
                {
                    using (Sierra s = new Sierra())
                    {
                        s.Connect(ConfigurationManager.AppSettings["sierraConnectionString"]);
                        int indexet = 0;

                        foreach (MailQueueModel item in list)
                        {
                            // New order received from someone
                            if (item.Type == MailQueueType.NEW)
                            {
                                list[indexet].SierraPatronInfo = s.GetPatronInfoFromLibraryCardNumberOrPersonnummer(item.PatronCardNo, item.PatronCardNo);
                            }

                            indexet++;
                        }
                    }
                }
                catch (Exception e)
                {
                    json.Success = false;
                    json.Message = "Error connecting to Sierra: " + e.Message;
                    LogHelper.Error<MailQueueSurfaceController>("Error connecting to Sierra", e);
                    return Json(json, JsonRequestBehavior.DenyGet);
                }


                // Continues if we have a list of mail messages...
                int index = 0;

                // For each fetched mail to process
                foreach (MailQueueModel item in list)
                {
                    // New order received from someone
                    if (item.Type == MailQueueType.NEW)
                    {
                        try
                        {
                            // Write a new OrderItem
                            int orderItemNodeId = _orderItemManager.CreateOrderItemInDbFromMailQueueModel(item, false, false);

                            _internalDbLogger.WriteSierraDataToLog(orderItemNodeId, item.SierraPatronInfo);

                            // Archive the mail message to correct folder
                            if (ConfigurationManager.AppSettings["chalmersILLArchiveProcessedMails"] == "true")
                            {
                                FolderId archiveFolderId = _exchangeMailWebApi.ArchiveMailMessage(item.Id);
                                list[index].ArchiveFolderId = archiveFolderId;
                            }

                            // Update item with some useful data
                            list[index].OrderItemNodeId = orderItemNodeId;
                            list[index].StatusResult = "Created new OrderItem node.";
                        }
                        catch (Exception e)
                        {
                            list[index].OrderItemNodeId = -1;
                            list[index].StatusResult = "Error creating new OrderItem node: " + e.Message;
                            LogHelper.Error<MailQueueSurfaceController>("Error creating new OrderItem node", e);
                        }

                    }

                    // Reply from user to existing order
                    else if (item.Type == MailQueueType.REPLY)
                    {
                        try
                        {
                            // Set the OrderItem Status so it appears in lists
                            try
                            {
                                _orderItemManager.SetOrderItemStatusInternal(item.OrderItemNodeId, Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionName"], "02:Åtgärda"), false, false);
                            }
                            catch (Exception es)
                            {
                                throw new Exception("Exception during SetOrderItemStatusInternal: " + es.Message);
                            }

                            // Set new FollowUpDate for the OrderItem
                            try
                            {
                                _orderItemManager.SetFollowUpDate(item.OrderItemNodeId, DateTime.Now, false, false);
                            }
                            catch (Exception ef)
                            {
                                throw new Exception("Exception during SetFollowUpDate: " + ef.Message);
                            }

                            // Write LogItem with the mail received and metadata
                            try
                            {
                                _internalDbLogger.WriteLogItemInternal(item.OrderItemNodeId, "MAIL", getTextFromHtml(item.MessageBody), false, false);
                                _internalDbLogger.WriteLogItemInternal(item.OrderItemNodeId, "MAIL_NOTE", "Svar från " + item.Sender + " [" + item.From + "]");
                            }
                            catch (Exception el)
                            {
                                throw new Exception("Exception during WriteLogItemInternal: " + el.Message);
                            }

                            // Archive the mail message to correct folder
                            try
                            {
                                if (ConfigurationManager.AppSettings["chalmersILLArchiveProcessedMails"] == "true")
                                {
                                    FolderId archiveFolderId = _exchangeMailWebApi.ArchiveMailMessage(item.Id);
                                    list[index].ArchiveFolderId = archiveFolderId;
                                }
                            }
                            catch (Exception ea)
                            {
                                throw new Exception("Exception during Archiving: " + ea.Message);
                            }

                            // Update item with some useful data
                            list[index].StatusResult = "Wrote LogItem type MAIL for this OrderId.";

                            _notifier.UpdateOrderItemUpdate(item.OrderItemNodeId, "-1", "", true, true, true);
                        }
                        catch (Exception e)
                        {
                            list[index].StatusResult = "Error following up reply on OrderItem: " + e.Message;
                            LogHelper.Error<MailQueueSurfaceController>("Error following up reply on OrderItem", e);
                        }
                    }

                    else if (item.Type == MailQueueType.DELIVERY)
                    {
                        try
                        {
                            // Set the OrderItem Status so it appears in lists
                            try
                            {
                                _orderItemManager.SetOrderItemStatusInternal(item.OrderItemNodeId, Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionName"], "09:Mottagen"), false, false);
                            }
                            catch (Exception es)
                            {
                                throw new Exception("Exception during SetOrderItemStatusInternal: " + es.Message);
                            }

                            // Check if the incoming mail mentions drm content.
                            try
                            {
                                if (item.MessageBody.ToLower().Contains("drm"))
                                {
                                    _orderItemManager.SetDrmWarning(item.OrderItemNodeId, true, false, false);
                                }
                            }
                            catch (Exception es)
                            {
                                throw new Exception("Exception during toggling of DrmWarning" + es.Message);
                            }

                            // Set new FollowUpDate for the OrderItem
                            try
                            {
                                _orderItemManager.SetFollowUpDate(item.OrderItemNodeId, DateTime.Now, false, false);
                            }
                            catch (Exception ef)
                            {
                                throw new Exception("Exception during SetFollowUpDate: " + ef.Message);
                            }

                            // Pick out attachments and save them in Umbracos media repository.
                            try
                            {
                                if (item.Attachments.Count > 0)
                                {
                                    var ms = Services.MediaService;
                                    var mainFolder = ms.GetChildren(-1).First(m => m.Name == ConfigurationManager.AppSettings["umbracoOrderItemAttachmentsMediaFolderName"]);
                                    using (Semaphore semLock = new Semaphore(0, 1))
                                    {
                                        TypedEventHandler<IMediaService, SaveEventArgs<IMedia>> handler = (sender, e) => { semLock.Release(e.SavedEntities.Count()); };
                                        try
                                        {
                                            MediaService.Saved += handler;

                                            foreach (var attachment in item.Attachments)
                                            {
                                                var m = ms.CreateMedia(item.OrderId + "-" + attachment.Title, mainFolder, "OrderItemAttachment");
                                                m.SetValue("file", attachment.Title, attachment.Data);
                                                m.SetValue("orderItemNodeId", item.OrderItemNodeId);

                                                // Save the media and wait until it is finished so that we can retrieve the link to the item.
                                                ms.Save(m);
                                                semLock.WaitOne();

                                                // cleanup, memory stream not needed any longer
                                                attachment.Data.Dispose();

                                                _orderItemManager.AddOrderItemAttachment(item.OrderItemNodeId, m.Id, attachment.Title, m.GetValue("file").ToString(), false, false);
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            throw;
                                        }
                                        finally
                                        {
                                            MediaService.Saved -= handler;
                                        }
                                    }
                                }
                            }
                            catch (Exception el)
                            {
                                throw new Exception("Failed to extract attachments from delivery mail: " + el.Message);
                            }

                            // Write LogItem with the mail received and metadata
                            try
                            {
                                _internalDbLogger.WriteLogItemInternal(item.OrderItemNodeId, "MAIL", getTextFromHtml(item.MessageBody), false, false);
                                _internalDbLogger.WriteLogItemInternal(item.OrderItemNodeId, "MAIL_NOTE", "Leverans från " + item.Sender + " [" + item.From + "]");
                            }
                            catch (Exception el)
                            {
                                throw new Exception("Exception during WriteLogItemInternal: " + el.Message);
                            }

                            // Archive the mail message to correct folder
                            try
                            {
                                if (ConfigurationManager.AppSettings["chalmersILLArchiveProcessedMails"] == "true")
                                {
                                    FolderId archiveFolderId = _exchangeMailWebApi.ArchiveMailMessage(item.Id);
                                    list[index].ArchiveFolderId = archiveFolderId;
                                }
                            }
                            catch (Exception ea)
                            {
                                throw new Exception("Exception during Archiving: " + ea.Message);
                            }

                            // Update item with some useful data
                            list[index].StatusResult = "Wrote LogItem type MAIL for this OrderId.";

                            _notifier.UpdateOrderItemUpdate(item.OrderItemNodeId, "-1", "", true, true, true);
                        }
                        catch (Exception e)
                        {
                            list[index].StatusResult = "Error following up delivery on OrderItem: " + e.Message;
                            LogHelper.Error<MailQueueSurfaceController>("Error following up delivery on OrderItem", e);
                        }
                    }
                    else if (item.Type == MailQueueType.ERROR)
                    {
                        try
                        {
                            // Forward failed mail to bug fixers.
                            try
                            {
                                foreach (var addressWithPotentialWs in ConfigurationManager.AppSettings["bugFixersMailingList"].Split(','))
                                {
                                    var address = addressWithPotentialWs.Trim();
                                    try
                                    {
                                        _exchangeMailWebApi.ForwardMailMessage(item.Id, address, item.ParseErrorMessage, false);
                                    }
                                    catch (Exception innerInnerExc)
                                    {
                                        LogHelper.Error<MailQueueSurfaceController>("Failed to forward message to " + address + ".", innerInnerExc);
                                    }
                                }
                            }
                            catch (Exception innerExc)
                            {
                                LogHelper.Error<MailQueueSurfaceController>("Failed to forward message to bug fixers.", innerExc);
                            }

                            // Forward failed mail to manual handling.
                            _exchangeMailWebApi.ForwardMailMessage(item.Id, ConfigurationManager.AppSettings["chalmersILLForwardingAddress"]);
                            list[index].StatusResult = "This message has been forwarded to " + ConfigurationManager.AppSettings["chalmersILLForwardingAddress"];
                        }
                        catch (Exception e)
                        {
                            list[index].StatusResult = "Error forwarding mail: " + e.Message;
                            LogHelper.Error<MailQueueSurfaceController>("Error forwarding mail", e);
                        }
                    }
                    else // UNKNOWN, forward to mailbox configured in web.config
                    {
                        try
                        {
                            _exchangeMailWebApi.ForwardMailMessage(item.Id, ConfigurationManager.AppSettings["chalmersILLForwardingAddress"]);
                            list[index].StatusResult = "This message has been forwarded to " + ConfigurationManager.AppSettings["chalmersILLForwardingAddress"];
                        }
                        catch (Exception e)
                        {
                            list[index].StatusResult = "Error forwarding mail: " + e.Message;
                            LogHelper.Error<MailQueueSurfaceController>("Error forwarding mail", e);
                        }
                    }

                    item.Attachments = null;

                    index++;
                }
            }

            // Return the list of mails as Json to client
            if (list.Count != 0)
            {
                return Json(list, JsonRequestBehavior.DenyGet);
            }
            else
            {
                json.Success = true;
                json.Message = "No new messages in queue.";
                return Json(json, JsonRequestBehavior.DenyGet);
            }
        }    
    
        private void signalExpiredFollowUpDates()
        {
            try
            {
                // Connect to an Examine Search Provider
                var searcher = ExamineManager.Instance.SearchProviderCollection["ChalmersILLOrderItemsSearcher"];

                // Specify Search Criteria
                var searchCriteria = searcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);

                // Specify the query
                var query = searchCriteria.RawQuery(@"nodeTypeAlias:ChalmersILLOrderItem AND 
                    Status:03\:Beställd AND 
                    FollowUpDate:[" + DateTime.Now.AddMinutes(-60).ToString("yyyyMMddHHmmssfff") + " TO " + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "]");

                // Search for our items and signal the ones that have expired recently.
                var results = searcher.Search(query);
                foreach (var item in results)
                {
                    // -1 means that we haven't checked edited by properly and should disregard it
                    var memberId = -1;
                    _notifier.UpdateOrderItemUpdate(item.Id, memberId.ToString(), "", true, true);
                }
            }
            catch (Exception e)
            {
                LogHelper.Error<MailQueueSurfaceController>("Failed to signal expired follow up dates.", e);
            }
        }

        private void convertOrdersWithExpiredFollowUpDateAndCertainStatusToNewStatus()
        {
            // Connect to an Examine Search Provider
            var searcher = ExamineManager.Instance.SearchProviderCollection["ChalmersILLOrderItemsSearcher"];

            // Specify Search Criteria
            var searchCriteria = searcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);

            // Specify the query
            var query = searchCriteria.RawQuery(@"nodeTypeAlias:ChalmersILLOrderItem AND 
                Status:04\:Väntar AND 
                FollowUpDate:[197501010000000 TO " + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "]");

            // -1 means that we haven't checked edited by properly and should disregard it
            var memberId = -1;

            // Search for our items and signal the ones that have expired recently.
            var ids = searcher.Search(query).Select(x => x.Id).ToList();
            foreach (var id in ids)
            {
                _internalDbLogger.WriteLogItemInternal(id, "LOG", "Automatisk statusändring på grund av att uppföljningsdatum löpt ut.", false, false);

                _orderItemManager.SetOrderItemStatusInternal(id,
                    _dataTypes.GetAvailableStatuses().First(x => x.Value.Contains("Åtgärda")).Id,
                    true, true);

                _notifier.UpdateOrderItemUpdate(id, memberId.ToString(), "", true, true);
            }
        }

        private string getBoundOrder(MailQueueModel m)
        {
            string ret = null;

            // Connect to an Examine Search Provider
            var searcher = ExamineManager.Instance.SearchProviderCollection["ChalmersILLOrderItemsSearcher"];

            // Specify Search Criteria
            var searchCriteria = searcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);

            // Specify the query
            var query = searchCriteria.RawQuery(@"nodeTypeAlias:ChalmersILLOrderItem AND 
                (Status:01\:Ny OR Status:02\:Åtgärda OR Status:03\:Beställd OR Status:04\:Väntar OR Status:09\:Mottagen)");

            // Search for our items
            var results = searcher.Search(query);
            string providerOrderId;
            string orderId;
            foreach (var result in results)
            {
                providerOrderId = result.Fields.GetValueString("ProviderOrderId");
                orderId = result.Fields.GetValueString("OrderId");

                if (!String.IsNullOrEmpty(providerOrderId) && !String.IsNullOrEmpty(orderId))
                {
                    var attachmentIndicatingBinding = false;
                    foreach (var att in m.Attachments)
                    {
                        if (att.Title.EndsWith(".txt"))
                        {
                            att.Data.Position = 0;
                            StreamReader sr = new StreamReader(att.Data);
                            string dataStr = sr.ReadToEnd();
                            if (att.Title.Contains(providerOrderId) || dataStr.Contains(providerOrderId) || att.Title.Contains(orderId) || dataStr.Contains(orderId))
                            {
                                attachmentIndicatingBinding = true;
                                break;
                            }
                        }
                    }

                    if ((m.Subject != null && m.Subject.Contains(providerOrderId)) ||
                        (m.MessageBody != null && m.MessageBody.Contains(providerOrderId)) ||
                        (m.Subject != null && m.Subject.Contains(orderId)) ||
                        (m.MessageBody != null && m.MessageBody.Contains(orderId)) ||
                        attachmentIndicatingBinding)
                    {
                        ret = orderId;
                        break;
                    }
                }
            }

            return ret;
        }

        private string getTextFromHtml(string html)
        {
            string ret = "";
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            StringBuilder sb = new StringBuilder();
            if (doc != null && doc.DocumentNode != null)
            {
                var textNodes = doc.DocumentNode.SelectNodes("//text()");
                if (textNodes != null)
                {
                    foreach (HtmlTextNode node in textNodes)
                    {
                        sb.AppendLine(HtmlEntity.DeEntitize(node.Text));
                    }
                    ret = sb.ToString();
                }
            }
            return ret;
        }
    }
}