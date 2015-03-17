using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using Umbraco.Web;
using umbraco.cms.businesslogic.member;
using umbraco.cms.businesslogic.datatype;
using umbraco.cms;
using Chalmers.ILL.Models;
using System.Globalization;
using Umbraco.Core;
using UmbracoExamine;
using Examine;
using Examine.SearchCriteria;
using Umbraco.Core.Logging;
using Chalmers.ILL.Extensions;
using Newtonsoft.Json;
using System.Configuration;
using Umbraco.Core.Models;
using Chalmers.ILL.Utilities;
using System.Threading;
using Umbraco.Core.Services;
using Chalmers.ILL.SignalR;
using Chalmers.ILL.Logging;

namespace Chalmers.ILL.OrderItems
{
    public class OrderItemManager : IOrderItemManager
    {
        INotifier _notifier;
        IInternalDbLogger _internalDbLogger;

        public void SetNotifier(INotifier notifier)
        {
            _notifier = notifier;
        }

        public void SetInternalDbLogger(IInternalDbLogger internalDbLogger)
        {
            _internalDbLogger = internalDbLogger;
        }

        public OrderItemModel GetOrderItem(int nodeId)
        {
            SearchResult contentNode = null;

            // Query ChalmersILLOrderItemsSearcher in Examine for the node.
            try
            {
                var searcher = ExamineManager.Instance.SearchProviderCollection["ChalmersILLOrderItemsSearcher"];
                var searchCriteria = searcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
                var query = searchCriteria.Id(nodeId);
                var searchResult = searcher.Search(query.Compile());

                var searchResultCount = searchResult.Count();
                if (searchResultCount == 0)
                {
                    LogHelper.Warn<OrderItemManager>("GetOrderItem: Couldn't find any node with the ID " + nodeId + " while querying with Examine.");
                }
                else if (searchResultCount > 1)
                {
                    // should never happen
                    LogHelper.Warn<OrderItemManager>("GetOrderItem: Found more than one node with the ID " + nodeId + " while querying with Examine.");
                }
                else
                {
                    contentNode = searchResult.First();
                }
            }
            catch (Exception e)
            {
                LogHelper.Error<OrderItemManager>("Failed to query node.", e);
            }

            // The list of statuses to return binds to the model
            var orderItem = new OrderItemModel();

            if (contentNode != null)
            {
                // Set values
                orderItem.CreateDate = DateTime.ParseExact(contentNode.Fields.GetValueString("createDate"), "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None);
                orderItem.UpdateDate = DateTime.ParseExact(contentNode.Fields.GetValueString("updateDate"), "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None);
                orderItem.NodeId = contentNode.Id;

                orderItem.OrderId = contentNode.Fields.GetValueString("OrderId");
                orderItem.OriginalOrder = contentNode.Fields.GetValueString("OriginalOrder");
                orderItem.Reference = contentNode.Fields.GetValueString("Reference");

                if (contentNode.Fields.GetValueString("FollowUpDate") != null && contentNode.Fields.GetValueString("FollowUpDate") != "")
                {
                    try
                    {
                        orderItem.FollowUpDate = DateTime.ParseExact(contentNode.Fields.GetValueString("FollowUpDate"), "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None);
                        orderItem.FollowUpDateIsDue = orderItem.FollowUpDate <= DateTime.Now ? true : false;
                    }
                    catch (Exception)
                    {
                    }
                }

                orderItem.PatronName = contentNode.Fields.GetValueString("PatronName");
                orderItem.PatronEmail = contentNode.Fields.GetValueString("PatronEmail");
                orderItem.PatronCardNo = contentNode.Fields.GetValueString("PatronCardNo");

                orderItem.ProviderName = contentNode.Fields.GetValueString("ProviderName") != null ? contentNode.Fields.GetValueString("ProviderName") : "";
                orderItem.ProviderOrderId = contentNode.Fields.GetValueString("ProviderOrderId") != null ? contentNode.Fields.GetValueString("ProviderOrderId") : "";
                orderItem.ProviderInformation = contentNode.Fields.GetValueString("ProviderInformation") != null ? contentNode.Fields.GetValueString("ProviderInformation") : "";

                // Parse out the integer of status and type
                int OrderStatusId = Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionName"], contentNode.Fields.GetValueString("Status"));
                int OrderTypeId = Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderTypeDataTypeDefinitionName"], contentNode.Fields.GetValueString("Type"));
                int OrderDeliveryLibraryId = Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], contentNode.Fields.GetValueString("DeliveryLibrary"));
                int OrderCancellationReasonId = Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderCancellationReasonDataTypeDefinitionName"], contentNode.Fields.GetValueString("CancellationReason"));
                int OrderPurchasedMaterialId = Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderPurchasedMaterialDataTypeDefinitionName"], contentNode.Fields.GetValueString("PurchasedMaterial"));

                // Status (id, whole prevalue "xx:yyyy" and just string "yyyy")
                orderItem.Status = OrderStatusId;
                orderItem.StatusString = OrderStatusId != -1 ? umbraco.library.GetPreValueAsString(OrderStatusId).Split(':').Last() : "";
                orderItem.StatusPrevalue = OrderStatusId != -1 ? umbraco.library.GetPreValueAsString(OrderStatusId) : "";

                // Type (id and prevalue)
                orderItem.Type = OrderTypeId;
                orderItem.TypePrevalue = OrderTypeId != -1 ? umbraco.library.GetPreValueAsString(OrderTypeId) : "";

                // Delivery Library (id and prevalue)
                orderItem.DeliveryLibrary = OrderDeliveryLibraryId;
                orderItem.DeliveryLibraryPrevalue = OrderDeliveryLibraryId != -1 ? umbraco.library.GetPreValueAsString(OrderDeliveryLibraryId) : "";

                // Cancellation reason (id and prevalue)
                orderItem.CancellationReason = OrderCancellationReasonId;
                orderItem.CancellationReasonPrevalue = OrderCancellationReasonId != -1 ? umbraco.library.GetPreValueAsString(OrderCancellationReasonId) : "";

                // Purchased material (id and prevalue)
                orderItem.PurchasedMaterial = OrderPurchasedMaterialId;
                orderItem.PurchasedMaterialPrevalue = OrderPurchasedMaterialId != -1 ? umbraco.library.GetPreValueAsString(OrderPurchasedMaterialId) : "";

                // List of LogItems bound to this OrderItem
                //orderItem.LogItemsList = Logging.GetLogItems(nodeId);
                if (!String.IsNullOrEmpty(contentNode.Fields.GetValueString("Log")))
                {
                    orderItem.LogItemsList = JsonConvert.DeserializeObject<List<LogItem>>(contentNode.Fields.GetValueString("Log"));
                    orderItem.LogItemsList.Reverse();
                }
                else
                {
                    orderItem.LogItemsList = new List<LogItem>();
                }

                // List of attachments bound to this OrderItem
                if (!String.IsNullOrEmpty(contentNode.Fields.GetValueString("Attachments")))
                {
                    orderItem.AttachmentList = JsonConvert.DeserializeObject<List<OrderAttachment>>(contentNode.Fields.GetValueString("Attachments"));
                }
                else
                {
                    orderItem.AttachmentList = new List<OrderAttachment>();
                }

                orderItem.SierraInfoStr = contentNode.Fields.GetValueString("SierraInfo");
                if (orderItem.SierraInfoStr == null || orderItem.SierraInfoStr == "")
                {
                    orderItem.SierraInfoStr = JsonConvert.SerializeObject(new SierraModel());
                }
                orderItem.SierraInfo = JsonConvert.DeserializeObject<SierraModel>(orderItem.SierraInfoStr);
                orderItem.DrmWarning = contentNode.Fields.GetValueString("DrmWarning");
            }

            // NOTE: These values are no longer stored in the actual node, only added in OrderItemSurfaceController after checking relations.
            orderItem.EditedBy = "";
            orderItem.EditedByMemberName = "";
            orderItem.EditedByCurrentMember = false;

            // Include the Content Version Count in Umbraco db
            // FIXME: Always set to zero to avoid ContentService calls. Never used. Should be removed from model?
            orderItem.ContentVersionsCount = 0;

            orderItem.DeliveryLibrarySameAsHomeLibrary = IsDeliveryLibrarySameAsHomeLibrary(orderItem);

            // Return the populated object
            return orderItem;
        }

        public int CreateOrderItemInDbFromMailQueueModel(MailQueueModel model, bool doReindex = true, bool doSignal = true)
        {
            IContentService cs = new Umbraco.Core.Services.ContentService();

            // Temporary OrderId with MD5 Hash
            var orderId = "cthb-" + Helpers.CalculateMD5Hash(DateTime.Now.Ticks.ToString());
            var contentName = orderId;

            // Create the OrderItem
            var uh = new Umbraco.Web.UmbracoHelper(Umbraco.Web.UmbracoContext.Current);
            IContent content = cs.CreateContent(contentName, uh.TypedContentAtXPath("//" + ConfigurationManager.AppSettings["umbracoOrderListContentDocumentType"]).First().Id, "ChalmersILLOrderItem");

            // Set properties
            content.SetValue("originalOrder", HttpUtility.UrlDecode(model.OriginalOrder));
            content.SetValue("reference", HttpUtility.UrlDecode(model.OriginalOrder));
            content.SetValue("patronName", model.PatronName);
            content.SetValue("patronEmail", model.PatronEmail);
            content.SetValue("patronCardNo", model.PatronCardNo);
            content.SetValue("followUpDate", DateTime.Now);
            content.SetValue("editedBy", "");
            content.SetValue("status", Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionName"], "01:Ny"));
            content.SetValue("pType", model.SierraPatronInfo.ptype);
            content.SetValue("homeLibrary", model.SierraPatronInfo.home_library);
            content.SetValue("log", JsonConvert.SerializeObject(new List<LogItem>()));
            content.SetValue("attachments", JsonConvert.SerializeObject(new List<OrderAttachment>()));
            content.SetValue("sierraInfo", JsonConvert.SerializeObject(model.SierraPatronInfo));
            content.SetValue("dueDate", "");
            content.SetValue("bookId", "");
            content.SetValue("providerInformation", "");

            if (!String.IsNullOrEmpty(model.SierraPatronInfo.home_library))
            {
                if (model.SierraPatronInfo.home_library.ToLower() == "hbib")
                {
                    content.SetValue("deliveryLibrary", Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Huvudbiblioteket"));
                }
                else if (model.SierraPatronInfo.home_library.ToLower() == "abib")
                {
                    content.SetValue("deliveryLibrary", Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Arkitekturbiblioteket"));
                }
                else if (model.SierraPatronInfo.home_library.ToLower() == "lbib")
                {
                    content.SetValue("deliveryLibrary", Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Lindholmenbiblioteket"));
                }
                else
                {
                    content.SetValue("deliveryLibrary", "");
                }
            }

            // Set Type directly if "IsPurchaseRequest" is true
            if (model.IsPurchaseRequest)
            {
                content.SetValue("type", Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderTypeDataTypeDefinitionName"], "Inköpsförslag"));
            }

            // Save the OrderItem to get an Id
            SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);

            // Shorten the OrderId and include the NodeId
            content.SetValue("orderId", orderId.Substring(0, 13) + "-" + content.Id.ToString());
            content.Name = orderId.Substring(0, 13) + "-" + content.Id.ToString();

            // Save
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);

            return content.Id;
        }

        public int CreateOrderItemInDbFromOrderItemSeedModel(OrderItemSeedModel model, bool doReindex = true, bool doSignal = true)
        {
            IContentService cs = new Umbraco.Core.Services.ContentService();

            // Temporary OrderId with MD5 Hash
            var orderId = "cthb-" + Helpers.CalculateMD5Hash(DateTime.Now.Ticks.ToString());
            var contentName = orderId;

            // Create the OrderItem
            var uh = new Umbraco.Web.UmbracoHelper(Umbraco.Web.UmbracoContext.Current);
            IContent content = cs.CreateContent(contentName, uh.TypedContentAtXPath("//" + ConfigurationManager.AppSettings["umbracoOrderListContentDocumentType"]).First().Id, "ChalmersILLOrderItem");

            // Set properties
            content.SetValue("originalOrder", model.Message);
            content.SetValue("reference", model.Message);
            content.SetValue("patronName", model.PatronName);
            content.SetValue("patronEmail", model.PatronEmail);
            content.SetValue("patronCardNo", model.PatronCardNumber);
            content.SetValue("followUpDate", DateTime.Now);
            content.SetValue("editedBy", "");
            content.SetValue("status", Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionName"], "01:Ny"));
            content.SetValue("pType", model.SierraPatronInfo.ptype);
            content.SetValue("homeLibrary", model.SierraPatronInfo.home_library);
            content.SetValue("log", JsonConvert.SerializeObject(new List<LogItem>()));
            content.SetValue("attachments", JsonConvert.SerializeObject(new List<OrderAttachment>()));
            content.SetValue("sierraInfo", JsonConvert.SerializeObject(model.SierraPatronInfo));
            content.SetValue("seedId", model.Id);


            if (model.DeliveryLibrarySigel == "Z")
            {
                content.SetValue("deliveryLibrary", Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Huvudbiblioteket"));
            }
            else if (model.DeliveryLibrarySigel == "ZL")
            {
                content.SetValue("deliveryLibrary", Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Lindholmenbiblioteket"));
            }
            else if (model.DeliveryLibrarySigel == "ZA")
            {
                content.SetValue("deliveryLibrary", Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Arkitekturbiblioteket"));
            }
            else if (!String.IsNullOrEmpty(model.SierraPatronInfo.home_library))
            {
                if (model.SierraPatronInfo.home_library.ToLower() == "hbib")
                {
                    content.SetValue("deliveryLibrary", Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Huvudbiblioteket"));
                }
                else if (model.SierraPatronInfo.home_library.ToLower() == "abib")
                {
                    content.SetValue("deliveryLibrary", Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Arkitekturbiblioteket"));
                }
                else if (model.SierraPatronInfo.home_library.ToLower() == "lbib")
                {
                    content.SetValue("deliveryLibrary", Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Lindholmenbiblioteket"));
                }
                else
                {
                    content.SetValue("deliveryLibrary", "");
                }
            }

            // Save the OrderItem to get an Id
            SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);

            // Shorten the OrderId and include the NodeId
            content.SetValue("orderId", orderId.Substring(0, 13) + "-" + content.Id.ToString());
            content.Name = orderId.Substring(0, 13) + "-" + content.Id.ToString();

            // Save
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);

            return content.Id;
        }

        public void AddOrderItemAttachment(int orderNodeId, int mediaNodeId, string title, string link, bool doReindex = true, bool doSignal = true)
        {
            try
            {
                if (!String.IsNullOrEmpty(title) && !String.IsNullOrEmpty(link))
                {
                    var cs = new Umbraco.Core.Services.ContentService();

                    var content = cs.GetById(orderNodeId);

                    string attachmentsStr = Convert.ToString(content.GetValue("attachments"));
                    List<OrderAttachment> attachmentList;
                    if (!String.IsNullOrEmpty(attachmentsStr))
                    {
                        attachmentList = JsonConvert.DeserializeObject<List<OrderAttachment>>(attachmentsStr);
                    }
                    else
                    {
                        attachmentList = new List<OrderAttachment>();
                    }

                    var att = new OrderAttachment();
                    att.Title = title;
                    att.Link = link;
                    att.MediaItemNodeId = mediaNodeId;
                    attachmentList.Add(att);

                    content.SetValue("attachments", JsonConvert.SerializeObject(attachmentList));
                    SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);
                    _internalDbLogger.WriteLogItemInternal(orderNodeId, "ATTACHMENT", "Nytt dokument bundet till ordern.", doReindex, doSignal);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool SetFollowUpDate(int nodeId, DateTime date, bool doReindex = true, bool doSignal = true)
        {
            // Connect to Umbraco ContentService
            // var cs = new Umbraco.Core.Services.ContentService();
            // var cs = ApplicationContext.Current.Services.ContentService;
            var cs = UmbracoContext.Current.Application.Services.ContentService;

            // Find OrderItem
            var content = cs.GetById(nodeId);

            try
            {
                // Set FollowUpDate
                content.SetValue("followUpDate", date);
            }
            catch (Exception e)
            {
                throw new Exception("Setting followUpDate to " + date + ": " + e.Message);
            }

            try
            {
                SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
            }
            catch (Exception ep)
            {
                throw new Exception("Save NodeId=" + content.Id + ", Published=" + content.Published + ", Status=" + content.Status + ", Trashed=" + content.Trashed + ", UpdateDate=" + content.UpdateDate + ": " + ep.Message);
            }

            return true;
        }

        public bool SetOrderItemCancellationReasonInternal(int orderNodeId, int cancellationReasonId, bool doReindex = true, bool doSignal = true)
        {
            var cs = new Umbraco.Core.Services.ContentService();

            try
            {
                var content = cs.GetById(orderNodeId);

                // Try and parse out the int of the Umbraco property, if it exists
                int currentCancellationReason = Helpers.GetPropertyValueAsInteger(content.GetValue("cancellationReason"));

                // Only make a change if the new value differs from the current
                if (currentCancellationReason != cancellationReasonId)
                {
                    content.SetValue("cancellationReason", cancellationReasonId);
                    SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);
                    _internalDbLogger.WriteLogItemInternal(orderNodeId, "ANNULLERINGSORSAK", "Annulleringsorsak ändrad till " + umbraco.library.GetPreValueAsString(cancellationReasonId), doReindex, doSignal);
                }

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool SetOrderItemDeliveryLibraryInternal(int orderNodeId, int deliveryLibraryId, bool doReindex = true, bool doSignal = true)
        {
            var cs = new Umbraco.Core.Services.ContentService();

            try
            {
                var content = cs.GetById(orderNodeId);

                // Try and parse out the int of the Umbraco property, if it exists
                int currentDeliveryLibrary = Helpers.GetPropertyValueAsInteger(content.GetValue("deliveryLibrary"));

                // Only make a change if the new value differs from the current
                if (currentDeliveryLibrary != deliveryLibraryId)
                {
                    content.SetValue("deliveryLibrary", deliveryLibraryId);
                    SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);
                    _internalDbLogger.WriteLogItemInternal(orderNodeId, "BIBLIOTEK", "Bibliotek ändrat från " + (currentDeliveryLibrary != -1 ? umbraco.library.GetPreValueAsString(currentDeliveryLibrary).Split(':').Last() : "Odefinierad") + " till " + umbraco.library.GetPreValueAsString(deliveryLibraryId).Split(':').Last(), doReindex, doSignal);
                }

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool SetOrderItemDeliveryReceivedInternal(int orderNodeId, string bookId, DateTime dueDate, string providerInformation, bool doReindex = true, bool doSignal = true)
        {
            var cs = new Umbraco.Core.Services.ContentService();
            bool infoChanged = false;

            try
            {
                var content = cs.GetById(orderNodeId);

                string currentBookId = content.GetValue("bookId").ToString();
                string currentProviderInformation = content.GetValue("providerInformation").ToString();

                if (content.GetValue("dueDate").ToString() != "")
                {
                    DateTime currentDueDate = Convert.ToDateTime(content.GetValue("dueDate"));
                    if (currentDueDate != dueDate)
                    {
                        content.SetValue("dueDate", dueDate);
                        infoChanged = true;
                    }
                }


                // Only make a change if the values differs from the current
                if (currentBookId != bookId)
                {
                    content.SetValue("bookId", bookId);
                    infoChanged = true;
                }
               
                if (currentProviderInformation != providerInformation)
                {
                    content.SetValue("providerInformation", providerInformation);
                    infoChanged = true;
                }

                if (infoChanged)
                {
                    SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);
                    _internalDbLogger.WriteLogItemInternal(orderNodeId, "BOKINFORMATION", "Bokinformation ändrat till bokid:"+bookId+" lånetid:"+dueDate+" leverantörsinformation:"+providerInformation, doReindex, doSignal);
                }

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool SetDrmWarning(int orderNodeId, bool status, bool doReindex = true, bool doSignal = true)
        {
            var cs = new Umbraco.Core.Services.ContentService();

            try
            {
                var content = cs.GetById(orderNodeId);
                content.SetValue("drmWarning", status);
                SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);
                _internalDbLogger.WriteLogItemInternal(orderNodeId, "DRM", "Kan innehålla drm-material!", doReindex, doSignal);
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool SetOrderItemPurchasedMaterialInternal(int orderNodeId, int purchasedMaterialId, bool doReindex = true, bool doSignal = true)
        {
            var cs = new Umbraco.Core.Services.ContentService();

            try
            {
                var content = cs.GetById(orderNodeId);

                // Try and parse out the int of the Umbraco property, if it exists
                int currentPurchasedMaterial = Helpers.GetPropertyValueAsInteger(content.GetValue("purchasedMaterial"));

                // Only make a change if the new value differs from the current
                if (currentPurchasedMaterial != purchasedMaterialId)
                {
                    content.SetValue("purchasedMaterial", purchasedMaterialId);
                    SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);
                    _internalDbLogger.WriteLogItemInternal(orderNodeId, "MATERIALINKÖP", "Inköpt material ändrat till " + umbraco.library.GetPreValueAsString(purchasedMaterialId), doReindex, doSignal);
                }

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool SetOrderItemStatusInternal(int orderNodeId, int statusId, bool doReindex = true, bool doSignal = true)
        {
            try
            {
                // Connect to the content service
                var cs = new Umbraco.Core.Services.ContentService();

                // Get node for the order item
                var content = cs.GetById(orderNodeId);

                // Try and parse out the int of the Umbraco property, if it exists
                int currentStatus = Helpers.GetPropertyValueAsInteger(content.GetValue("status"));

                // Only make a change if the new value differs from the current
                if (currentStatus != statusId)
                {
                    content.SetValue("status", statusId);
                    SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);
                    _internalDbLogger.WriteLogItemInternal(orderNodeId, "STATUS", "Status ändrad från " + (currentStatus != -1 ? umbraco.library.GetPreValueAsString(currentStatus).Split(':').Last() : "Odefinierad") + " till " + umbraco.library.GetPreValueAsString(statusId).Split(':').Last(), doReindex, doSignal);
                }

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool SetOrderItemTypeInternal(int orderNodeId, int typeId, bool doReindex = true, bool doSignal = true)
        {
            try
            {
                // Connect to the content service
                var cs = new Umbraco.Core.Services.ContentService();

                // Get node for the order item
                var content = cs.GetById(orderNodeId);

                // Try and parse out the int of the Umbraco property, if it exists
                int currentType = Helpers.GetPropertyValueAsInteger(content.GetValue("type"));

                // Only make a change if the new value differs from the current
                if (currentType != typeId)
                {
                    content.SetValue("type", typeId);
                    if (typeId == Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderTypeDataTypeDefinitionName"], "Artikel"))
                    {
                        content.SetValue("deliveryLibrary", Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Huvudbiblioteket"));
                    }
                    SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);
                    _internalDbLogger.WriteLogItemInternal(orderNodeId, "TYP", "Typ ändrad till " + umbraco.library.GetPreValueAsString(typeId), doReindex, doSignal);
                }

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void SaveWithoutEventsAndWithSynchronousReindexing(IContent content, bool doReindex = true, bool doSignal = true)
        {
            try
            {
                var cs = ApplicationContext.Current.Services.ContentService;

                cs.Save(content, 0, false);

                if (doReindex)
                {
                    // Get the order items indexer from Lucene so that we can listen for the IndexOperationComplete event.
                    var orderItemsIndexer = ExamineManager.Instance.IndexProviderCollection["ChalmersILLOrderItemsIndexer"];
                    UmbracoContentIndexer umbracoOrderItemsIndexer = null;

                    // Do some downcasting so that we have access to the IndexOperationComplete event.
                    if (orderItemsIndexer is UmbracoContentIndexer)
                    {
                        umbracoOrderItemsIndexer = (UmbracoContentIndexer)orderItemsIndexer;
                    }

                    Semaphore semLock = null;
                    EventHandler handler = null;

                    try
                    {
                        if (umbracoOrderItemsIndexer != null)
                        {
                            // Create a semaphore which we use for the synchronization between Lucenes indexing thread and our thread.
                            semLock = new Semaphore(0, 1);

                            // The event handler which will be called when the reindexing is complete.
                            handler = (sender, e) => IndexOperationComplete(sender, e, semLock);

                            // Register the event handler to the IndexOperationComplete event.
                            umbracoOrderItemsIndexer.IndexOperationComplete += handler;
                        }

                        // Start the actual reindexing of the content.
                        ExamineManager.Instance.ReIndexNode(content.ToXml(), "content");

                        if (umbracoOrderItemsIndexer != null)
                        {
                            // Wait for the indexing operations to complete.
                            semLock.WaitOne();

                            // Cleanup
                            semLock.Dispose();
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        if (umbracoOrderItemsIndexer != null && handler != null)
                        {
                            // Unregister the handler.
                            umbracoOrderItemsIndexer.IndexOperationComplete -= handler;
                        }
                    }
                }

                if (doSignal)
                {
                    // Signal our ChalmersILL clients.
                    _notifier.ReportNewOrderItemUpdate(content);
                }
            }
            catch (Exception e)
            {
                LogHelper.Error<OrderItemManager>("SaveWithoutEventsAndWithSynchronousReindexing: Error when saving content.", e);
            }
        }

        static void IndexOperationComplete(object sender, EventArgs e, Semaphore semLock)
        {
            // Indicate to the waiting thread that the index operation has completed.
            semLock.Release();
        }

        #region Private methods

        private bool IsDeliveryLibrarySameAsHomeLibrary(OrderItemModel orderItem)
        {
            return orderItem.SierraInfo.home_library == null || 
                (orderItem.DeliveryLibraryPrevalue == "Huvudbiblioteket" && orderItem.SierraInfo.home_library.Contains("hbib")) ||
                (orderItem.DeliveryLibraryPrevalue == "Lindholmenbiblioteket" && orderItem.SierraInfo.home_library.Contains("lbib")) ||
                (orderItem.DeliveryLibraryPrevalue == "Arkitekturbiblioteket" && orderItem.SierraInfo.home_library.Contains("abib"));
        }

        #endregion
    }
}