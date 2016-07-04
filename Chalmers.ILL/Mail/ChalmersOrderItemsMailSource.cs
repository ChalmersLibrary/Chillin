using Chalmers.ILL.Controllers.SurfaceControllers;
using Chalmers.ILL.OrderItems;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Core.Logging;
using Newtonsoft.Json;
using Examine;
using System.IO;
using HtmlAgilityPack;
using System.Text;
using Chalmers.ILL.Extensions;
using System.Text.RegularExpressions;
using Chalmers.ILL.Patron;
using Microsoft.Exchange.WebServices.Data;
using Chalmers.ILL.Utilities;
using Chalmers.ILL.SignalR;
using System.Threading;
using Umbraco.Core.Services;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Chalmers.ILL.UmbracoApi;
using Chalmers.ILL.Models.Mail;
using Chalmers.ILL.MediaItems;

namespace Chalmers.ILL.Mail
{
    public class ChalmersOrderItemsMailSource : ISource
    {
        public static int CREATE_ORDER_FROM_MAIL_DATA_EVENT_TYPE { get { return 20; } }
        public static int UPDATE_ORDER_FROM_MAIL_DATA_PATRON_EVENT_TYPE { get { return 21; } }
        public static int UPDATE_ORDER_FROM_MAIL_DATA_NOT_PATRON_EVENT_TYPE { get { return 22; } }

        IExchangeMailWebApi _exchangeMailWebApi;
        IOrderItemManager _orderItemManager;
        INotifier _notifier;
        IMediaItemManager _mediaItemManager;
        IPatronDataProvider _patronDataProvider;
        IOrderItemSearcher _orderItemSearcher;

        private SourcePollingResult _result;
        public SourcePollingResult Result
        {
            get
            {
                return _result;
            }
        }

        public ChalmersOrderItemsMailSource(IExchangeMailWebApi exchangeMailWebApi, IOrderItemManager orderItemManager,
            INotifier notifier, IMediaItemManager mediaItemManager, IPatronDataProvider patronDataProvider, IOrderItemSearcher orderItemSearcher)
        {
            _exchangeMailWebApi = exchangeMailWebApi;
            _orderItemManager = orderItemManager;
            _notifier = notifier;
            _mediaItemManager = mediaItemManager;
            _patronDataProvider = patronDataProvider;
            _orderItemSearcher = orderItemSearcher;
        }

        public SourcePollingResult Poll()
        {
            _result = new SourcePollingResult("Huvudpostlåda");

            // List of read mails
            List<MailQueueModel> list = null;

            // Connect to Exchange Service
            try
            {
                _exchangeMailWebApi.ConnectToExchangeService(ConfigurationManager.AppSettings["chalmersIllExhangeLogin"], ConfigurationManager.AppSettings["chalmersIllExhangePass"]);
            }
            catch (Exception e)
            {
                throw new SourcePollingException("Error connecting to Exchange Service.", e);
            }

            // Get a list of mails from Inbox folder
            try
            {
                list = _exchangeMailWebApi.ReadMailQueue();
            }
            catch (Exception e)
            {
                throw new SourcePollingException("Error reading mail from Exchange.", e);
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
                            var orderIdPattern = new Regex("#+(cthb-.{8}-[0-9]+)");
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
                            LogHelper.Error<SystemSurfaceController>("Failed to process one E-mail, tagging it with ERROR.", e);
                            _result.Errors++;
                            _result.Messages.Add(item.ParseErrorMessage);
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new SourcePollingException("Error when post processing E-mails.", e);
                }

                // Get SierraInfo to the list
                try
                {
                    int indexet = 0;

                    foreach (MailQueueModel item in list)
                    {
                        // New order received from someone
                        if (item.Type == MailQueueType.NEW)
                        {
                            list[indexet].SierraPatronInfo = _patronDataProvider.GetPatronInfoFromLibraryCardNumberOrPersonnummer(item.PatronCardNo, item.PatronCardNo);
                        }

                        indexet++;
                    }
                }
                catch (Exception e)
                {
                    throw new SourcePollingException("Error connecting to Sierra.", e);
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

                            var eventId = _orderItemManager.GenerateEventId(CREATE_ORDER_FROM_MAIL_DATA_EVENT_TYPE);
                            _orderItemManager.AddSierraDataToLog(orderItemNodeId, item.SierraPatronInfo, eventId);

                            // Archive the mail message to correct folder
                            if (ConfigurationManager.AppSettings["chalmersILLArchiveProcessedMails"] == "true")
                            {
                                FolderId archiveFolderId = _exchangeMailWebApi.ArchiveMailMessage(item.Id);
                                list[index].ArchiveFolderId = archiveFolderId;
                            }

                            // Update item with some useful data
                            list[index].OrderItemNodeId = orderItemNodeId;
                            list[index].StatusResult = "Created new OrderItem node.";

                            _result.NewOrders++;
                        }
                        catch (Exception e)
                        {
                            list[index].OrderItemNodeId = -1;
                            list[index].StatusResult = "Error creating new OrderItem node: " + e.Message;
                            LogHelper.Error<SystemSurfaceController>("Error creating new OrderItem node", e);
                            _result.Errors++;
                            _result.Messages.Add(list[index].StatusResult);
                        }

                    }

                    // Reply from user to existing order
                    else if (item.Type == MailQueueType.REPLY)
                    {
                        try
                        {
                            var eventId = _orderItemManager.GenerateEventId(UPDATE_ORDER_FROM_MAIL_DATA_PATRON_EVENT_TYPE);
                            // Set the OrderItem Status so it appears in lists
                            try
                            {
                                _orderItemManager.SetStatus(item.OrderItemNodeId, "02:Åtgärda", eventId, false, false);
                            }
                            catch (Exception es)
                            {
                                throw new Exception("Exception during SetOrderItemStatusInternal: " + es.Message);
                            }

                            // Set new FollowUpDate for the OrderItem
                            try
                            {
                                _orderItemManager.SetFollowUpDateWithoutLogging(item.OrderItemNodeId, DateTime.Now, false, false);
                            }
                            catch (Exception ef)
                            {
                                throw new Exception("Exception during SetFollowUpDate: " + ef.Message);
                            }

                            // Write LogItem with the mail received and metadata
                            try
                            {
                                _orderItemManager.AddLogItem(item.OrderItemNodeId, "MAIL", getTextFromHtml(item.MessageBody), eventId, false, false);
                                _orderItemManager.AddLogItem(item.OrderItemNodeId, "MAIL_NOTE", "Svar från " + item.Sender + " [" + item.From + "]", eventId);
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

                            _result.UpdatedOrders++;
                        }
                        catch (Exception e)
                        {
                            list[index].StatusResult = "Error following up reply on OrderItem: " + e.Message;
                            LogHelper.Error<SystemSurfaceController>("Error following up reply on OrderItem", e);
                            _result.Errors++;
                            _result.Messages.Add(list[index].StatusResult);
                        }
                    }

                    else if (item.Type == MailQueueType.DELIVERY)
                    {
                        try
                        {
                            var eventId = _orderItemManager.GenerateEventId(UPDATE_ORDER_FROM_MAIL_DATA_NOT_PATRON_EVENT_TYPE);
                            // Set the OrderItem Status so it appears in lists
                            try
                            {
                                _orderItemManager.SetStatus(item.OrderItemNodeId, "09:Mottagen", eventId, false, false);
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
                                    _orderItemManager.SetDrmWarningWithoutLogging(item.OrderItemNodeId, true, false, false);
                                }
                            }
                            catch (Exception es)
                            {
                                throw new Exception("Exception during toggling of DrmWarning" + es.Message);
                            }

                            // Set new FollowUpDate for the OrderItem
                            try
                            {
                                _orderItemManager.SetFollowUpDateWithoutLogging(item.OrderItemNodeId, DateTime.Now, false, false);
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
                                    foreach (var attachment in item.Attachments)
                                    {
                                        var savedMediaItem = _mediaItemManager.CreateMediaItem(attachment.Title, item.OrderItemNodeId, item.OrderId, attachment.Data);

                                        // Dispose stream that is no longer needed. Handle this in some better way?
                                        savedMediaItem.Data.Dispose();

                                        _orderItemManager.AddExistingMediaItemAsAnAttachment(item.OrderItemNodeId, savedMediaItem.Id, attachment.Title, savedMediaItem.Url, eventId, false, false);
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
                                _orderItemManager.AddLogItem(item.OrderItemNodeId, "MAIL", getTextFromHtml(item.MessageBody), eventId, false, false);
                                _orderItemManager.AddLogItem(item.OrderItemNodeId, "MAIL_NOTE", "Leverans från " + item.Sender + " [" + item.From + "]", eventId);
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

                            _result.UpdatedOrders++;
                        }
                        catch (Exception e)
                        {
                            list[index].StatusResult = "Error following up delivery on OrderItem: " + e.Message;
                            LogHelper.Error<SystemSurfaceController>("Error following up delivery on OrderItem", e);
                            _result.Errors++;
                            _result.Messages.Add(list[index].StatusResult);
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
                                        LogHelper.Error<SystemSurfaceController>("Failed to forward message to " + address + ".", innerInnerExc);
                                    }
                                }
                            }
                            catch (Exception innerExc)
                            {
                                LogHelper.Error<SystemSurfaceController>("Failed to forward message to bug fixers.", innerExc);
                            }

                            // Forward failed mail to manual handling.
                            _exchangeMailWebApi.ForwardMailMessage(item.Id, ConfigurationManager.AppSettings["chalmersILLForwardingAddress"]);
                            list[index].StatusResult = "This message has been forwarded to " + ConfigurationManager.AppSettings["chalmersILLForwardingAddress"];
                            _result.Errors++;
                            _result.Messages.Add(list[index].StatusResult);
                        }
                        catch (Exception e)
                        {
                            list[index].StatusResult = "Error forwarding mail: " + e.Message;
                            LogHelper.Error<SystemSurfaceController>("Error forwarding mail", e);
                            _result.Errors++;
                            _result.Messages.Add(list[index].StatusResult);
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
                            LogHelper.Error<SystemSurfaceController>("Error forwarding mail", e);
                            _result.Errors++;
                            _result.Messages.Add(list[index].StatusResult);
                        }
                    }

                    item.Attachments = null;

                    index++;
                }
            }

            return Result;
        }

        private string getBoundOrder(MailQueueModel m)
        {
            string ret = null;

            // Search for our items
            var results = _orderItemSearcher.Search(@"nodeTypeAlias:ChalmersILLOrderItem AND 
                (Status:01\:Ny OR Status:02\:Åtgärda OR Status:03\:Beställd OR Status:04\:Väntar OR Status:09\:Mottagen)");
            string providerOrderId;
            string orderId;
            foreach (var result in results)
            {
                providerOrderId = result.ProviderOrderId;
                orderId = result.OrderId;

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