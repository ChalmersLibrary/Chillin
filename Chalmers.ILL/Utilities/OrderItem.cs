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
using Umbraco.Core.Services;

namespace Chalmers.ILL.Utilities
{
    public class OrderItem
    {
        /// <summary>
        /// Return an OrderItem as a OrderItemModel for re-use in the application wherever needed
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>OrderItemModel with populated properties</returns>
        public static OrderItemModel GetOrderItem(int nodeId)
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
                    LogHelper.Warn<OrderItem>("GetOrderItem: Couldn't find any node with the ID " + nodeId + " while querying with Examine.");
                }
                else if (searchResultCount > 1)
                {
                    // should never happen
                    LogHelper.Warn<OrderItem>("GetOrderItem: Found more than one node with the ID " + nodeId + " while querying with Examine.");
                }
                else
                {
                    contentNode = searchResult.First();
                }
            }
            catch (Exception e)
            {
                LogHelper.Error<OrderItem>("Failed to query node.", e);
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

            // Return the populated object
            return orderItem;
        }

        /// <summary>
        /// Creates a new OrderItem from a MailQueueModel
        /// </summary>
        /// <param name="model">MailQueueModel</param>
        /// <returns>Created nodeId</returns>
        public static int WriteOrderItem(MailQueueModel model, bool doReindex = true, bool doSignal = true)
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
            cs.SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);

            // Shorten the OrderId and include the NodeId
            content.SetValue("orderId", orderId.Substring(0, 13) + "-" + content.Id.ToString());
            content.Name = orderId.Substring(0, 13) + "-" + content.Id.ToString();

            // Save
            cs.SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);

            return content.Id;
        }

        /// <summary>
        /// Sets the property for FollowUpDate for an OrderItem
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <param name="date">The date to set</param>
        /// <returns>True if everything went ok</returns>
        public static bool SetFollowUpDate(int nodeId, DateTime date, bool doReindex = true, bool doSignal = true)
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
                cs.SaveWithoutEventsAndWithSynchronousReindexing(content, doReindex, doSignal);
            }
            catch (Exception ep)
            {
                throw new Exception("Save NodeId=" + content.Id + ", Published=" + content.Published + ", Status=" + content.Status + ", Trashed=" + content.Trashed + ", UpdateDate=" + content.UpdateDate + ": " + ep.Message);
            }

            return true;
        }
    }
}