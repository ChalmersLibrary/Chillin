using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Web;
using Chalmers.ILL.Models;
using Chalmers.ILL.Models.Mail;
using Umbraco.Core.Models;
using Chalmers.ILL.Database;
using Newtonsoft.Json;
using umbraco.cms.businesslogic.member;
using Chalmers.ILL.Utilities;
using System.Text.RegularExpressions;
using Chalmers.ILL.UmbracoApi;
using System.Configuration;
using Umbraco.Core.Logging;
using Chalmers.ILL.SignalR;

namespace Chalmers.ILL.OrderItems
{
    public class EntityFrameworkOrderItemManager : IOrderItemManager
    {
        private INotifier _notifier;
        private OrderItemsDbContext _dbContext;
        private IUmbracoWrapper _umbraco;
        private Random _rand;

        public EntityFrameworkOrderItemManager(IUmbracoWrapper umbraco)
        {
            _umbraco = umbraco;
            _rand = new Random();
        }

        public void SetNotifier(INotifier notifier)
        {
            _notifier = notifier;
        }

        public void AddExistingMediaItemAsAnAttachment(int orderNodeId, int mediaNodeId, string title, string link, string eventId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems
                .Where(x => x.NodeId == orderNodeId)
                .Include(x => x.AttachmentList)
                .FirstOrDefault();
            if (orderItem != null)
            {
                if (!String.IsNullOrEmpty(title) && !String.IsNullOrEmpty(link))
                {
                    if (orderItem.AttachmentList == null)
                    {
                        orderItem.AttachmentList = new List<OrderAttachment>();
                    }

                    var att = new OrderAttachment();
                    att.Title = title;
                    att.Link = link;
                    att.MediaItemNodeId = mediaNodeId;
                    orderItem.AttachmentList.Add(att);

                    AddLogItem(orderNodeId, "ATTACHMENT", "Nytt dokument bundet till ordern.", eventId, false, false);

                    MaybeSaveToDatabase(doReindex);
                }
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to add existing media item as an attachment.");
            }
        }

        public void AddLogItem(int OrderItemNodeId, string Type, string Message, string eventId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems
                .Where(x => x.NodeId == OrderItemNodeId)
                .Include(x => x.LogItemsList)
                .FirstOrDefault();
            if (orderItem != null)
            {
                if (!String.IsNullOrEmpty(Message))
                {
                    if (orderItem.LogItemsList == null)
                    {
                        orderItem.LogItemsList = new List<LogItem>();
                    }

                    LogItem newLog = new LogItem
                    {
                        MemberName = GetCurrentUserOrSystem(),
                        Type = Type,
                        Message = Message,
                        CreateDate = DateTime.Now,
                        EventId = eventId
                    };

                    orderItem.LogItemsList.Add(newLog);

                    MaybeSaveToDatabase(doReindex);
                }
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to add log item.");
            }
        }

        public void AddSierraDataToLog(int orderItemNodeId, SierraModel sm, string eventId, bool doReindex = true, bool doSignal = true)
        {
            if (!string.IsNullOrEmpty(sm.id))
            {
                string logtext = "Firstname: " + sm.first_name + " Lastname: " + sm.last_name + "\n" +
                                    "Barcode: " + sm.barcode + " Email: " + sm.email + " Ptyp: " + sm.ptype + "\n";
                AddLogItem(orderItemNodeId, "SIERRA", logtext, eventId, doReindex, doSignal);
            }
            else
            {
                AddLogItem(orderItemNodeId, "SIERRA", "Låntagaren hittades inte.", eventId, doReindex, doSignal);
            }
        }

        public int CreateOrderItemInDbFromMailQueueModel(MailQueueModel model, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();

            // Temporary OrderId with MD5 Hash
            var orderId = "cthb-" + Helpers.CalculateMD5Hash(DateTime.Now.Ticks.ToString());
            var contentName = orderId;

            var newOrderItem = new OrderItemModel();

            // Set properties
            var originalOrder = UrlDecodeAndEscapeAllLinks(model.OriginalOrder);
            newOrderItem.OriginalOrder = originalOrder;
            newOrderItem.Reference = originalOrder;
            newOrderItem.PatronName = model.PatronName;
            newOrderItem.PatronEmail = model.PatronEmail;
            newOrderItem.PatronCardNo = model.PatronCardNo;
            newOrderItem.FollowUpDate = DateTime.Now;
            newOrderItem.EditedBy = "";
            newOrderItem.Status = _umbraco.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionName"], "01:Ny");
            newOrderItem.SierraInfo = model.SierraPatronInfo;
            newOrderItem.LogItemsList = new List<LogItem>();
            newOrderItem.AttachmentList = new List<OrderAttachment>();
            newOrderItem.DueDate = DateTime.Now;
            newOrderItem.ProviderDueDate = DateTime.Now;
            newOrderItem.DeliveryDate = new DateTime(1970, 1, 1);
            newOrderItem.BookId = "";
            newOrderItem.ProviderInformation = "";

            if (!String.IsNullOrEmpty(model.SierraPatronInfo.home_library))
            {
                if (model.SierraPatronInfo.home_library.ToLower() == "abib")
                {
                    newOrderItem.DeliveryLibrary = _umbraco.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Arkitekturbiblioteket");
                }
                else if (model.SierraPatronInfo.home_library.ToLower() == "lbib")
                {
                    newOrderItem.DeliveryLibrary = _umbraco.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Lindholmenbiblioteket");
                }
                else
                {
                    newOrderItem.DeliveryLibrary = _umbraco.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Huvudbiblioteket");
                }
            }

            // Set Type directly if "IsPurchaseRequest" is true
            if (model.IsPurchaseRequest)
            {
                newOrderItem.Type = _umbraco.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderTypeDataTypeDefinitionName"], "Inköpsförslag");
            }

            // Save the OrderItem to get an Id
            SaveToDatabase(false);

            // Shorten the OrderId and include the NodeId
            newOrderItem.OrderId = orderId.Substring(0, 13) + "-" + newOrderItem.NodeId.ToString();

            // Save
            MaybeSaveToDatabase(doReindex);

            return newOrderItem.NodeId;
        }

        public int CreateOrderItemInDbFromOrderItemSeedModel(OrderItemSeedModel model, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();

            // Temporary OrderId with MD5 Hash
            var orderId = "cthb-" + Helpers.CalculateMD5Hash(DateTime.Now.Ticks.ToString());
            var contentName = orderId;

            var newOrderItem = new OrderItemModel();

            // Set properties
            newOrderItem.OriginalOrder = model.Message;
            newOrderItem.Reference = model.MessagePrefix + model.Message;
            newOrderItem.PatronName = model.PatronName;
            newOrderItem.PatronEmail = model.PatronEmail;
            newOrderItem.PatronCardNo = model.PatronCardNumber;
            newOrderItem.FollowUpDate = DateTime.Now;
            newOrderItem.EditedBy = "";
            newOrderItem.Status = _umbraco.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionName"], "01:Ny");
            newOrderItem.SierraInfo = model.SierraPatronInfo;
            newOrderItem.LogItemsList = new List<LogItem>();
            newOrderItem.AttachmentList = new List<OrderAttachment>();
            newOrderItem.DueDate = DateTime.Now;
            newOrderItem.ProviderDueDate = DateTime.Now;
            newOrderItem.DeliveryDate = new DateTime(1970, 1, 1);
            newOrderItem.BookId = "";
            newOrderItem.ProviderInformation = "";

            if (model.DeliveryLibrarySigel == "Z")
            {
                newOrderItem.DeliveryLibrary = _umbraco.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Huvudbiblioteket");
            }
            else if (model.DeliveryLibrarySigel == "ZL")
            {
                newOrderItem.DeliveryLibrary = _umbraco.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Lindholmenbiblioteket");
            }
            else if (model.DeliveryLibrarySigel == "ZA")
            {
                newOrderItem.DeliveryLibrary = _umbraco.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Arkitekturbiblioteket");
            }
            else if (!String.IsNullOrEmpty(model.SierraPatronInfo.home_library))
            {
                if (model.SierraPatronInfo.home_library.ToLower() == "abib")
                {
                    newOrderItem.DeliveryLibrary = _umbraco.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Arkitekturbiblioteket");
                }
                else if (model.SierraPatronInfo.home_library.ToLower() == "lbib")
                {
                    newOrderItem.DeliveryLibrary = _umbraco.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Lindholmenbiblioteket");
                }
                else
                {
                    newOrderItem.DeliveryLibrary = _umbraco.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Huvudbiblioteket");
                }
            }

            // Save the OrderItem to get an Id
            SaveToDatabase(false);

            // Shorten the OrderId and include the NodeId
            newOrderItem.OrderId = orderId.Substring(0, 13) + "-" + newOrderItem.NodeId.ToString();

            // Save
            MaybeSaveToDatabase(doReindex);

            return newOrderItem.NodeId;
        }

        public string GenerateEventId(int type)
        {
            return "event-" + _rand.Next(0, 65535).ToString("X4") + "-" + type.ToString("D2");
        }

        public List<LogItem> GetLogItems(int nodeId)
        {
            var res = new List<LogItem>();
            EnsureDatabaseContext();
            try
            {
                var orderItem = _dbContext.OrderItems
                    .Where(x => x.NodeId == nodeId)
                    .Include(x => x.LogItemsList)
                    .FirstOrDefault();
                if (orderItem != null)
                {
                    res = orderItem.LogItemsList;
                }
                else
                {
                    throw new OrderItemNotFoundException("Failed to find order item when trying to fetch log items.");
                }
                return res;
            }
            finally
            {
                DisposeDatabaseContext();
            }
        }

        public OrderItemModel GetOrderItem(int nodeId)
        {
            OrderItemModel res = null;
            EnsureDatabaseContext();
            try
            {
                try
                {
                    var orderItems = _dbContext.OrderItems
                        .Where(x => x.NodeId == nodeId)
                        .Include(x => x.AttachmentList)
                        .Include(x => x.LogItemsList)
                        .Include(x => x.SierraInfo)
                        .Include(x => x.SierraInfo.adress)
                        .ToList();

                    if (orderItems.Count() == 0)
                    {
                        LogHelper.Warn<OrderItemManager>("GetOrderItem: Couldn't find any node with the ID " + nodeId + " while querying with Examine.");
                    }
                    else if (orderItems.Count() > 1)
                    {
                        // should never happen
                        LogHelper.Warn<OrderItemManager>("GetOrderItem: Found more than one node with the ID " + nodeId + " while querying with Examine.");
                    }
                    else
                    {
                        res = orderItems.Single();
                    }
                }
                catch (Exception e)
                {
                    LogHelper.Error<OrderItemManager>("Failed to query node.", e);
                }

                res.FollowUpDateIsDue = res.FollowUpDate <= DateTime.Now ? true : false;

                // Status (id, whole prevalue "xx:yyyy" and just string "yyyy")
                res.StatusString = res.Status != -1 ? umbraco.library.GetPreValueAsString(res.Status).Split(':').Last() : "";
                res.StatusPrevalue = res.Status != -1 ? umbraco.library.GetPreValueAsString(res.Status) : "";

                // Previous status (id, whole prevalue "xx:yyyy" and just string "yyyy")
                res.PreviousStatusString = res.PreviousStatus != -1 ? umbraco.library.GetPreValueAsString(res.PreviousStatus).Split(':').Last() : "";
                res.PreviousStatusPrevalue = res.PreviousStatus != -1 ? umbraco.library.GetPreValueAsString(res.PreviousStatus) : "";

                // Last delivery status (id, whole prevalue "xx:yyyy" and just string "yyyy")
                res.LastDeliveryStatusString = res.LastDeliveryStatus != -1 ? umbraco.library.GetPreValueAsString(res.LastDeliveryStatus).Split(':').Last() : "";
                res.LastDeliveryStatusPrevalue = res.LastDeliveryStatus != -1 ? umbraco.library.GetPreValueAsString(res.LastDeliveryStatus) : "";

                // Type (id and prevalue)
                res.TypePrevalue = res.Type != -1 ? umbraco.library.GetPreValueAsString(res.Type) : "";

                // Delivery Library (id and prevalue)
                res.DeliveryLibraryPrevalue = res.DeliveryLibrary != -1 ? umbraco.library.GetPreValueAsString(res.DeliveryLibrary) : "";

                // Cancellation reason (id and prevalue)
                res.CancellationReasonPrevalue = res.CancellationReason != -1 ? umbraco.library.GetPreValueAsString(res.CancellationReason) : "";

                // Purchased material (id and prevalue)
                res.PurchasedMaterialPrevalue = res.PurchasedMaterial != -1 ? umbraco.library.GetPreValueAsString(res.PurchasedMaterial) : "";

                // NOTE: These values are no longer stored in the actual node, only added in OrderItemSurfaceController after checking relations.
                res.EditedBy = "";
                res.EditedByMemberName = "";
                res.EditedByCurrentMember = false;

                // Include the Content Version Count in Umbraco db
                // FIXME: Always set to zero to avoid ContentService calls. Never used. Should be removed from model?
                res.ContentVersionsCount = 0;

                res.DeliveryLibrarySameAsHomeLibrary = IsDeliveryLibrarySameAsHomeLibrary(res);

                return res;
            }
            finally
            {
                DisposeDatabaseContext();
            }
        }

        public void RemoveConnectionToMediaItem(int orderNodeId, int mediaNodeId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems
                .Where(x => x.NodeId == orderNodeId)
                .Include(x => x.AttachmentList)
                .FirstOrDefault();
            if (orderItem != null)
            {
                orderItem.AttachmentList.RemoveAll(i => i.MediaItemNodeId == mediaNodeId);
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to remove connection to media item.");
            }
        }

        public void SaveWithoutEventsAndWithSynchronousReindexing(int nodeId, bool doReindex = true, bool doSignal = true)
        {
            MaybeSaveToDatabase(doReindex);
        }

        public void SaveWithoutEventsAndWithSynchronousReindexing(IContent content, bool doReindex = true, bool doSignal = true)
        {
            MaybeSaveToDatabase(doReindex);
        }

        public void SetBookId(int nodeId, string bookId, string eventId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(nodeId);
            if (orderItem != null)
            {
                if (orderItem.BookId != bookId)
                {
                    orderItem.BookId = bookId;
                    AddLogItem(nodeId, "BOKINFO", "Bok-ID ändrat till " + bookId + ".", eventId, false, false);
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set book ID.");
            }
        }

        public void SetCancellationReason(int orderNodeId, int cancellationReasonId, string eventId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(orderNodeId);
            if (orderItem != null)
            {
                if (orderItem.CancellationReason != cancellationReasonId)
                {
                    orderItem.CancellationReason = cancellationReasonId;
                    AddLogItem(orderNodeId, "ANNULLERINGSORSAK", "Annulleringsorsak ändrad till " + umbraco.library.GetPreValueAsString(cancellationReasonId), eventId, false, false);
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set cancellation reason.");
            }
        }

        public void SetDeliveryLibrary(int orderNodeId, string deliveryLibraryPrevalue, string eventId, bool doReindex = true, bool doSignal = true)
        {
            var deliveryLibraryId = _umbraco.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], deliveryLibraryPrevalue);
            SetDeliveryLibrary(orderNodeId, deliveryLibraryId, eventId, doReindex, doSignal);
        }

        public void SetDeliveryLibrary(int orderNodeId, int deliveryLibraryId, string eventId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(orderNodeId);
            if (orderItem != null)
            {
                var currentDeliveryLibrary = orderItem.DeliveryLibrary;
                if (currentDeliveryLibrary != deliveryLibraryId)
                {
                    orderItem.DeliveryLibrary = deliveryLibraryId;
                    AddLogItem(orderNodeId, "BIBLIOTEK", "Bibliotek ändrat från " + (currentDeliveryLibrary != -1 ? umbraco.library.GetPreValueAsString(currentDeliveryLibrary).Split(':').Last() : "Odefinierad") + " till " + umbraco.library.GetPreValueAsString(deliveryLibraryId).Split(':').Last(), eventId, false, false);
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set delivery library.");
            }
        }

        public void SetDrmWarning(int orderNodeId, bool status, string eventId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(orderNodeId);
            if (orderItem != null)
            {
                if (orderItem.DrmWarning != (status ? "1" : "0"))
                {
                    orderItem.DrmWarning = (status ? "1" : "0");
                    AddLogItem(orderNodeId, "DRM", "Kan innehålla drm-material!", eventId, false, false);
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set DRM warning.");
            }
        }

        public void SetDrmWarningWithoutLogging(int orderNodeId, bool status, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(orderNodeId);
            if (orderItem != null)
            {
                if (orderItem.DrmWarning != (status ? "1" : "0"))
                {
                    orderItem.DrmWarning = (status ? "1" : "0");
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set DRM warning without logging.");
            }
        }

        public void SetDueDate(int nodeId, DateTime date, string eventId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(nodeId);
            if (orderItem != null)
            {
                if (orderItem.DueDate != date)
                {
                    orderItem.DueDate = date;
                    AddLogItem(nodeId, "DATE", "Återlämnas av låntagare senast " + date.ToString("yyyy-MM-dd HH:mm"), eventId, false, false);
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set due date.");
            }
        }

        public void SetFollowUpDate(int nodeId, DateTime date, string eventId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(nodeId);
            if (orderItem != null)
            {
                if (orderItem.FollowUpDate != date)
                {
                    orderItem.FollowUpDate = date;
                    AddLogItem(nodeId, "DATE", "Följs upp senast " + date.ToString("yyyy-MM-dd HH:mm"), eventId, false, false);
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set follow up date.");
            }
        }

        public void SetFollowUpDateWithoutLogging(int nodeId, DateTime date, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(nodeId);
            if (orderItem != null)
            {
                if (orderItem.FollowUpDate != date)
                {
                    orderItem.FollowUpDate = date;
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set follow up date without logging.");
            }
        }

        public void SetPatronData(int nodeId, string sierraInfo, int sierraPatronRecordId, int pType, string homeLibrary, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems
                .Where(x => x.NodeId == nodeId)
                .Include(x => x.SierraInfo)
                .Include(X => X.SierraInfo.adress)
                .FirstOrDefault();
            if (orderItem != null)
            {
                var newSierraInfo = JsonConvert.DeserializeObject<SierraModel>(sierraInfo);
                orderItem.SierraInfo.adress = newSierraInfo.adress;
                orderItem.SierraInfo.barcode = newSierraInfo.barcode;
                orderItem.SierraInfo.email = newSierraInfo.email;
                orderItem.SierraInfo.first_name = newSierraInfo.first_name;
                orderItem.SierraInfo.home_library = newSierraInfo.home_library;
                orderItem.SierraInfo.id = newSierraInfo.id;
                orderItem.SierraInfo.last_name = newSierraInfo.last_name;
                orderItem.SierraInfo.mblock = newSierraInfo.mblock;
                orderItem.SierraInfo.ptype = newSierraInfo.ptype;
                orderItem.SierraInfo.record_id = newSierraInfo.record_id;
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set patron data.");
            }
        }

        public void SetPatronEmail(int nodeId, string email, string eventId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(nodeId);
            if (orderItem != null)
            {
                if (orderItem.PatronEmail != email)
                {
                    orderItem.PatronEmail = email;
                    AddLogItem(nodeId, "MAIL_NOTE", "E-post mot låntagare ändrad till " + email, eventId, false, false);
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set patron email.");
            }
        }

        public void SetProviderDueDate(int nodeId, DateTime date, string eventId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(nodeId);
            if (orderItem != null)
            {
                if (orderItem.ProviderDueDate != date)
                {
                    orderItem.ProviderDueDate = date;
                    AddLogItem(nodeId, "DATE", "Återlämnas till leverantör senast " + date.ToString("yyyy-MM-dd HH:mm"), eventId, false, false);
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set provider due date.");
            }
        }

        public void SetProviderInformation(int nodeId, string providerInformation, string eventId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(nodeId);
            if (orderItem != null)
            {
                if (orderItem.ProviderInformation != providerInformation)
                {
                    orderItem.ProviderInformation = providerInformation;
                    AddLogItem(nodeId, "LEVERANTÖR", "Leverantörsinformation ändrad till \"" + providerInformation + "\".", eventId, false, false);
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set provider information.");
            }
        }

        public void SetProviderName(int nodeId, string providerName, string eventId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(nodeId);
            if (orderItem != null)
            {
                if (orderItem.ProviderName != providerName)
                {
                    orderItem.ProviderName = providerName;
                    AddLogItem(nodeId, "ORDER", "Beställd från " + providerName, eventId, false, false);
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set provider name.");
            }
        }

        public void SetProviderNameWithoutLogging(int nodeId, string providerName, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(nodeId);
            if (orderItem != null)
            {
                if (orderItem.ProviderName != providerName)
                {
                    orderItem.ProviderName = providerName;
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set provider name without logging.");
            }
        }

        public void SetProviderOrderId(int nodeId, string providerOrderId, string eventId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(nodeId);
            if (orderItem != null)
            {
                if (orderItem.ProviderOrderId != providerOrderId)
                {
                    orderItem.ProviderOrderId = providerOrderId;
                    AddLogItem(nodeId, "ORDER", "Beställningsnr ändrat till " + providerOrderId, eventId, false, false);
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set provider order ID.");
            }
        }

        public void SetPurchasedMaterial(int orderNodeId, int purchasedMaterialId, string eventId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(orderNodeId);
            if (orderItem != null)
            {
                if (orderItem.PurchasedMaterial != purchasedMaterialId)
                {
                    orderItem.PurchasedMaterial = purchasedMaterialId;
                    AddLogItem(orderNodeId, "MATERIALINKÖP", "Inköpt material ändrat till " + umbraco.library.GetPreValueAsString(purchasedMaterialId), eventId, false, false);
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set purchased material.");
            }
        }

        public void SetReference(int nodeId, string reference, string eventId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(nodeId);
            if (orderItem != null)
            {
                if (orderItem.Reference != reference)
                {
                    orderItem.Reference = reference;
                    AddLogItem(nodeId, "REF", "Referens ändrad", eventId);
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set reference.");
            }
        }

        public void SetStatus(int orderNodeId, string statusPrevalue, string eventId, bool doReindex = true, bool doSignal = true)
        {
            var statusId = _umbraco.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionName"], statusPrevalue);
            SetStatus(orderNodeId, statusId, eventId, doReindex, doSignal);
        }

        public void SetStatus(int orderNodeId, int statusId, string eventId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(orderNodeId);
            if (orderItem != null)
            {
                if (orderItem.Status != statusId)
                {
                    var currentStatus = orderItem.Status;
                    orderItem.PreviousStatus = orderItem.Status;
                    orderItem.Status = statusId;
                    OnStatusChanged(orderItem, statusId);
                    AddLogItem(orderNodeId, "STATUS", "Status ändrad från " + (currentStatus != -1 ? umbraco.library.GetPreValueAsString(currentStatus).Split(':').Last() : "Odefinierad") + " till " + umbraco.library.GetPreValueAsString(statusId).Split(':').Last(), eventId, false, false);
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set status.");
            }
        }

        public void SetType(int orderNodeId, int typeId, string eventId, bool doReindex = true, bool doSignal = true)
        {
            EnsureDatabaseContext();
            var orderItem = _dbContext.OrderItems.Find(orderNodeId);
            if (orderItem != null)
            {
                if (orderItem.Type != typeId)
                {
                    orderItem.Type = typeId;
                    OnTypeChanged(orderItem, typeId);
                    AddLogItem(orderNodeId, "TYP", "Typ ändrad till " + umbraco.library.GetPreValueAsString(typeId), eventId, false, false);
                }
                MaybeSaveToDatabase(doReindex);
            }
            else
            {
                throw new OrderItemNotFoundException("Failed to find order item when trying to set type.");
            }
        }

        #region Private methods

        private void OnStatusChanged(OrderItemModel orderItem, int newStatusId)
        {
            UpdateLastDeliveryStatusWhenProper(orderItem, newStatusId);
            UpdateDeliveryDateWhenProper(orderItem, newStatusId);
        }

        private void OnTypeChanged(OrderItemModel orderItem, int newTypeId)
        {
            SetDeliveryLibraryIfNewTypeIsArtikel(orderItem, newTypeId);
        }

        private void UpdateLastDeliveryStatusWhenProper(OrderItemModel orderItem, int newStatusId)
        {
            var statusStr = umbraco.library.GetPreValueAsString(newStatusId).Split(':').Last();
            if (statusStr.Contains("Levererad") || statusStr.Contains("Utlånad") || statusStr.Contains("Transport") || statusStr.Contains("Infodisk"))
            {
                orderItem.LastDeliveryStatus = newStatusId;
            }
        }

        private void UpdateDeliveryDateWhenProper(OrderItemModel orderItem, int newStatusId)
        {
            var deliveryDateStr = orderItem.DeliveryDate == null ? "" : orderItem.DeliveryDate.ToString();
            var deliveryDate = deliveryDateStr == "" ? new DateTime(1970, 1, 1) : Convert.ToDateTime(deliveryDateStr);
            var statusStr = umbraco.library.GetPreValueAsString(newStatusId).Split(':').Last();
            if (deliveryDate.Year == 1970 && (statusStr.Contains("Levererad") || statusStr.Contains("Utlånad") || statusStr.Contains("Transport") ||
                    statusStr.Contains("Infodisk")))
            {
                orderItem.DeliveryDate = DateTime.Now;
            }
        }

        private void SetDeliveryLibraryIfNewTypeIsArtikel(OrderItemModel orderItem, int newTypeId)
        {
            if (newTypeId == _umbraco.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderTypeDataTypeDefinitionName"], "Artikel"))
            {
                orderItem.DeliveryLibrary = _umbraco.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Huvudbiblioteket");
            }
        }

        private bool IsDeliveryLibrarySameAsHomeLibrary(OrderItemModel orderItem)
        {
            return orderItem.SierraInfo.home_library == null ||
                (orderItem.DeliveryLibraryPrevalue == "Huvudbiblioteket" && orderItem.SierraInfo.home_library.Contains("hbib")) ||
                (orderItem.DeliveryLibraryPrevalue == "Lindholmenbiblioteket" && orderItem.SierraInfo.home_library.Contains("lbib")) ||
                (orderItem.DeliveryLibraryPrevalue == "Arkitekturbiblioteket" && orderItem.SierraInfo.home_library.Contains("abib"));
        }

        private void EnsureDatabaseContext()
        {
            if (_dbContext == null)
            {
                _dbContext = new OrderItemsDbContext();
                _dbContext.Configuration.LazyLoadingEnabled = false;
                _dbContext.Configuration.ProxyCreationEnabled = false;
            }
        }

        private void DisposeDatabaseContext()
        {
            if (_dbContext != null)
            {
                _dbContext.Dispose();
                _dbContext = null;
            }
        }

        private void MaybeSaveToDatabase(bool shouldSave)
        {
            if (shouldSave)
            {
                SaveToDatabase();
            }
        }

        private void SaveToDatabase(bool disposeDbContext = true)
        {
            if (_dbContext != null)
            {
                _dbContext.SaveChanges();
                DisposeDatabaseContext();
            }
            else
            {
                throw new Exception("Database context was null when trying to save changes to order items.");
            }
        }

        private List<LogItem> GetLogItemsReverse(int nodeId)
        {
            var contentNode = _dbContext.OrderItems.Find(nodeId);

            string oldLogItems;
            var logItems = new List<LogItem>();

            if (!String.IsNullOrEmpty(contentNode.Log))
            {
                oldLogItems = contentNode.Log;
                logItems = JsonConvert.DeserializeObject<List<LogItem>>(oldLogItems);
            }
            return logItems;
        }

        private string GetCurrentUserOrSystem()
        {
            var res = "System";
            if (Member.IsLoggedOn())
            {
                res = Member.GetCurrentMember().Text;
            }
            return res;
        }

        private string UrlDecodeAndEscapeAllLinks(string str)
        {
            var res = "";
            var regex = new Regex(@"((?:https?|ftp|file)(?::|%3a)(?:\/|%2f)(?:\/|%2f)[-a-zA-Z0-9+&@#\/%?=~_|!:,.;()]*[-a-zA-Z0-9+&@#()\/%=~_|()])");
            var match = regex.Match(str);
            res = HttpUtility.UrlDecode(str);
            for (int i = 1; i < match.Groups.Count; i++)
            {
                var urlDecodedUrlStr = HttpUtility.UrlDecode(match.Groups[i].ToString());
                res = res.Replace(urlDecodedUrlStr, Uri.EscapeUriString(urlDecodedUrlStr));
            }
            return res;
        }

        #endregion
    }
}