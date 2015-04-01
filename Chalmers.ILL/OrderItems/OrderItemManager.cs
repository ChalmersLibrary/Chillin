using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using Umbraco.Web;
using umbraco.cms.businesslogic.member;
using umbraco.cms.businesslogic.datatype;
using umbraco.cms;
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
using Chalmers.ILL.Models.Mail;
using Chalmers.ILL.Models;

namespace Chalmers.ILL.OrderItems
{
    public class OrderItemManager : IOrderItemManager
    {
        INotifier _notifier;
        IContentService _contentService;

        public void SetNotifier(INotifier notifier)
        {
            _notifier = notifier;
        }

        public void SetContentService(IContentService contentService)
        {
            _contentService = contentService;
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
                orderItem.ProviderDueDate = contentNode.Fields.GetValueString("ProviderDueDate") == "" ? DateTime.Now :
                    DateTime.ParseExact(contentNode.Fields.GetValueString("ProviderDueDate"), "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None);

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

                orderItem.DueDate = contentNode.Fields.GetValueString("DueDate") == "" ? DateTime.Now :
                    DateTime.ParseExact(contentNode.Fields.GetValueString("DueDate"), "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None);
                orderItem.BookId = contentNode.Fields.GetValueString("BookId");
                orderItem.ArrivedAtInfodiskDate = contentNode.Fields.GetValueString("ArrivedAtInfodiskDate") == "" ? new DateTime(1970, 1, 1) :
                    DateTime.ParseExact(contentNode.Fields.GetValueString("ArrivedAtInfodiskDate"), "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None);

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

        public List<LogItem> GetLogItems(int nodeId)
        {
            var res = GetLogItemsReverse(nodeId);
            res.Reverse();
            return res;
        }


        public void AddExistingMediaItemAsAnAttachment(int orderNodeId, int mediaNodeId, string title, string link, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(orderNodeId);

            if (!String.IsNullOrEmpty(title) && !String.IsNullOrEmpty(link))
            {
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
                AddLogItem(orderNodeId, "ATTACHMENT", "Nytt dokument bundet till ordern.", false, false);
            }

            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
        }

        public void AddLogItem(int OrderItemNodeId, string Type, string Message, bool doReindex = true, bool doSignal = true)
        {
            var contentNode = _contentService.GetById(OrderItemNodeId);

            if (!String.IsNullOrEmpty(Message))
            {
                List<LogItem> logItems = GetLogItemsReverse(OrderItemNodeId);

                LogItem newLog = new LogItem
                {
                    MemberName = GetCurrentUserOrSystem(),
                    Type = Type,
                    Message = Message,
                    CreateDate = DateTime.Now
                };

                logItems.Add(newLog);
                contentNode.SetValue("log", JsonConvert.SerializeObject(logItems));
            }

            SaveWithoutEventsAndWithSynchronousReindexing(contentNode, doReindex, doSignal);
        }

        public void AddSierraDataToLog(int orderItemNodeId, SierraModel sm, bool doReindex = true, bool doSignal = true)
        {
            if (!string.IsNullOrEmpty(sm.id))
            {
                string logtext = "Firstname: " + sm.first_name + " Lastname: " + sm.last_name + "\n" +
                                    "Barcode: " + sm.barcode + " Email: " + sm.email + " Ptyp: " + sm.ptype + "\n";
                AddLogItem(orderItemNodeId, "SIERRA", logtext, doReindex, doSignal);
            }
            else
            {
                AddLogItem(orderItemNodeId, "SIERRA", "Låntagaren hittades inte.", doReindex, doSignal);
            }
        }


        public void SetFollowUpDateWithoutLogging(int nodeId, DateTime date, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(nodeId);
            if (GetDateTimeFromContent(content, "followUpDate") != date)
            {
                SetContentValue(content, "followUpDate", date);
            }
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
        }

        public void SetDrmWarningWithoutLogging(int orderNodeId, bool status, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(orderNodeId);
            if (content.GetValue<bool>("drmWarning") != status)
            {
                content.SetValue("drmWarning", status);
            }
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
        }

        public void SetProviderNameWithoutLogging(int nodeId, string providerName, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(nodeId);
            var currentProviderName = content.GetValue<string>("providerName");
            if (currentProviderName != providerName)
            {
                content.SetValue("providerName", providerName);
            }
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
        }

        public void SetFollowUpDate(int nodeId, DateTime date, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(nodeId);
            if (GetDateTimeFromContent(content, "followUpDate") != date)
            {
                SetContentValue(content, "followUpDate", date);
                AddLogItem(nodeId, "DATE", "Följs upp senast " + date, false, false);
            }
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
        }

        public void SetDueDate(int nodeId, DateTime date, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(nodeId);
            if (GetDateTimeFromContent(content, "dueDate") != date)
            {
                SetContentValue(content, "dueDate", date);
                AddLogItem(nodeId, "DATE", "Återlämnas av låntagare senast " + date, false, false);
            }
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
        }

        public void SetProviderDueDate(int nodeId, DateTime date, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(nodeId);
            if (GetDateTimeFromContent(content, "providerDueDate") != date)
            {
                SetContentValue(content, "providerDueDate", date);
                AddLogItem(nodeId, "DATE", "Återlämnas till leverantör senast " + date, false, false);
            }
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
        }

        public void SetCancellationReason(int orderNodeId, int cancellationReasonId, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(orderNodeId);
            int currentCancellationReason = Helpers.GetPropertyValueAsInteger(content.GetValue("cancellationReason"));
            if (Helpers.GetPropertyValueAsInteger(content.GetValue("cancellationReason")) != cancellationReasonId)
            {
                SetContentValue(content, "cancellationReason", cancellationReasonId);
                AddLogItem(orderNodeId, "ANNULLERINGSORSAK", "Annulleringsorsak ändrad till " + umbraco.library.GetPreValueAsString(cancellationReasonId), false, false);
            }
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
        }

        public void SetDeliveryLibrary(int orderNodeId, int deliveryLibraryId, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(orderNodeId);
            int currentDeliveryLibrary = Helpers.GetPropertyValueAsInteger(content.GetValue("deliveryLibrary"));
            if (currentDeliveryLibrary != deliveryLibraryId)
            {
                SetContentValue(content, "deliveryLibrary", deliveryLibraryId);
                AddLogItem(orderNodeId, "BIBLIOTEK", "Bibliotek ändrat från " + (currentDeliveryLibrary != -1 ? umbraco.library.GetPreValueAsString(currentDeliveryLibrary).Split(':').Last() : "Odefinierad") + " till " + umbraco.library.GetPreValueAsString(deliveryLibraryId).Split(':').Last(), false, false);
            }
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
        }

        public void SetDrmWarning(int orderNodeId, bool status, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(orderNodeId);
            if (content.GetValue<bool>("drmWarning") != status)
            {
                content.SetValue("drmWarning", status);
                AddLogItem(orderNodeId, "DRM", "Kan innehålla drm-material!", false, false);
            }
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
        }

        public void SetPurchasedMaterial(int orderNodeId, int purchasedMaterialId, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(orderNodeId);
            int currentPurchasedMaterial = Helpers.GetPropertyValueAsInteger(content.GetValue("purchasedMaterial"));
            if (currentPurchasedMaterial != purchasedMaterialId)
            {
                content.SetValue("purchasedMaterial", purchasedMaterialId);
                AddLogItem(orderNodeId, "MATERIALINKÖP", "Inköpt material ändrat till " + umbraco.library.GetPreValueAsString(purchasedMaterialId), false, false);
            }
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
        }

        public void SetStatus(int orderNodeId, int statusId, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(orderNodeId);
            int currentStatus = Helpers.GetPropertyValueAsInteger(content.GetValue("status"));
            if (currentStatus != statusId)
            {
                content.SetValue("status", statusId);
                OnStatusChanged(content, statusId);
                AddLogItem(orderNodeId, "STATUS", "Status ändrad från " + (currentStatus != -1 ? umbraco.library.GetPreValueAsString(currentStatus).Split(':').Last() : "Odefinierad") + " till " + umbraco.library.GetPreValueAsString(statusId).Split(':').Last(), false, false);
            }
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
        }

        public void SetType(int orderNodeId, int typeId, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(orderNodeId);
            int currentType = Helpers.GetPropertyValueAsInteger(content.GetValue("type"));
            if (currentType != typeId)
            {
                content.SetValue("type", typeId);
                OnTypeChanged(content, typeId);
                AddLogItem(orderNodeId, "TYP", "Typ ändrad till " + umbraco.library.GetPreValueAsString(typeId), false, false);
            }
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
        }

        public void SetBookId(int nodeId, string bookId, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(nodeId);
            var currentBookId = content.GetValue<string>("bookId");
            if (currentBookId != bookId)
            {
                content.SetValue("bookId", bookId);
                AddLogItem(nodeId, "BOKINFO", "Bok-ID ändrat till " + bookId + ".", false, false);
            }
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
        }

        public void SetPatronEmail(int nodeId, string patronEmail, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(nodeId);
            var currentPatronEmail = content.GetValue<string>("patronEmail");
            if (currentPatronEmail != patronEmail)
            {
                content.SetValue("patronEmail", patronEmail);
                AddLogItem(nodeId, "MAIL_NOTE", "E-post mot låntagare ändrad till " + patronEmail, false, false);
            }
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
        }

        public void SetProviderName(int nodeId, string providerName, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(nodeId);
            var currentProviderName = content.GetValue<string>("providerName");
            if (currentProviderName != providerName)
            {
                content.SetValue("providerName", providerName);
                AddLogItem(nodeId, "ORDER", "Beställd från " + providerName, false, false);
            }
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
        }

        public void SetProviderOrderId(int nodeId, string providerOrderId, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(nodeId);
            var currentProviderOrderId = content.GetValue<string>("providerOrderId");
            if (currentProviderOrderId != providerOrderId)
            {
                content.SetValue("providerOrderId", providerOrderId);
                AddLogItem(nodeId, "ORDER", "Beställningsnr ändrat till " + providerOrderId, false, false);
            }
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
        }

        public void SetProviderInformation(int nodeId, string providerInformation, bool doReindex = true, bool doSignal = true)
        {
            var content = _contentService.GetById(nodeId);
            var currentProviderInformation = content.GetValue<string>("providerInformation");
            if (currentProviderInformation != providerInformation)
            {
                content.SetValue("providerInformation", providerInformation);
                AddLogItem(nodeId, "LEVERANTÖR", "Leverantörsinformation ändrad till \"" + providerInformation + "\".", false, false);
            }
            SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
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
            content.SetValue("dueDate", DateTime.Now);
            content.SetValue("providerDueDate", DateTime.Now);
            content.SetValue("arrivedAtInfodiskDate", new DateTime(1970, 1, 1));
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
            // Temporary OrderId with MD5 Hash
            var orderId = "cthb-" + Helpers.CalculateMD5Hash(DateTime.Now.Ticks.ToString());
            var contentName = orderId;

            // Create the OrderItem
            var uh = new Umbraco.Web.UmbracoHelper(Umbraco.Web.UmbracoContext.Current);
            IContent content = _contentService.CreateContent(contentName, uh.TypedContentAtXPath("//" + ConfigurationManager.AppSettings["umbracoOrderListContentDocumentType"]).First().Id, "ChalmersILLOrderItem");

            // Set properties
            content.SetValue("originalOrder", model.Message);
            content.SetValue("reference", model.MessagePrefix + model.Message);
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
                throw new SaveException("Save NodeId=" + content.Id + ", Published=" + content.Published + ", Status=" + content.Status + ", Trashed=" + content.Trashed + ", UpdateDate=" + content.UpdateDate + ".", e);

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

        private List<LogItem> GetLogItemsReverse(int nodeId)
        {
            var contentNode = _contentService.GetById(nodeId);

            string oldLogItems;
            var logItems = new List<LogItem>();

            if (!String.IsNullOrEmpty(contentNode.GetValue("log").ToString()))
            {
                oldLogItems = contentNode.GetValue("log").ToString();
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

        /* To be able to handle not yet assigned dates. */
        private DateTime GetDateTimeFromContent(IContent content, string key)
        {
            DateTime res = new DateTime(1970, 1, 1);

            try
            {
                res = content.GetValue<DateTime>(key);
            }
            catch (Exception)
            {
                // NOP
            }

            return res;
        }

        private void SetContentValue(IContent content, string key, object value)
        {
            try
            {
                content.SetValue(key, value);
            }
            catch (Exception e)
            {
                throw new Exception("Error when setting " + key + " to " + value.ToString() + ".", e);
            }
        }

        private void OnStatusChanged(IContent content, int newStatusId)
        {
            UpdateArrivedAtInfodiskDateWhenProper(content, newStatusId);
        }

        private void OnTypeChanged(IContent content, int newTypeId)
        {
            SetDeliveryLibraryIfNewTypeIsArtikel(content, newTypeId);
        }

        private void UpdateArrivedAtInfodiskDateWhenProper(IContent content, int newStatusId)
        {
            var arrivedAtInfodiskDateStr = content.GetValue("arrivedAtInfodiskDate").ToString();
            var arrivedAtInfodiskDate = arrivedAtInfodiskDateStr == "" ? new DateTime(1970, 1, 1) : Convert.ToDateTime(arrivedAtInfodiskDateStr);
            if (arrivedAtInfodiskDate.Year == 1970 && umbraco.library.GetPreValueAsString(newStatusId).Split(':').Last().Contains("Infodisk"))
            {
                content.SetValue("arrivedAtInfodiskDate", DateTime.Now);
            }
        }

        private void SetDeliveryLibraryIfNewTypeIsArtikel(IContent content, int newTypeId)
        {
            if (newTypeId == Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderTypeDataTypeDefinitionName"], "Artikel"))
            {
                content.SetValue("deliveryLibrary", Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderDeliveryLibraryDataTypeDefinitionName"], "Huvudbiblioteket"));
            }
        }

        #endregion
    }
}