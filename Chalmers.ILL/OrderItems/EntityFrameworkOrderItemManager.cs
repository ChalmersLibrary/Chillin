using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Chalmers.ILL.Models;
using Chalmers.ILL.Models.Mail;
using Umbraco.Core.Models;

namespace Chalmers.ILL.OrderItems
{
    public class EntityFrameworkOrderItemManager : IOrderItemManager
    {
        public void AddExistingMediaItemAsAnAttachment(int orderNodeId, int mediaNodeId, string title, string link, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void AddLogItem(int OrderItemNodeId, string Type, string Message, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void AddSierraDataToLog(int orderItemNodeId, SierraModel sm, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public int CreateOrderItemInDbFromMailQueueModel(MailQueueModel model, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public int CreateOrderItemInDbFromOrderItemSeedModel(OrderItemSeedModel model, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public string GenerateEventId(int type)
        {
            throw new NotImplementedException();
        }

        public List<LogItem> GetLogItems(int nodeId)
        {
            throw new NotImplementedException();
        }

        public OrderItemModel GetOrderItem(int nodeId)
        {
            throw new NotImplementedException();
        }

        public void RemoveConnectionToMediaItem(int orderNodeId, int mediaNodeId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SaveWithoutEventsAndWithSynchronousReindexing(int nodeId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SaveWithoutEventsAndWithSynchronousReindexing(IContent content, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetBookId(int nodeId, string bookId, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetCancellationReason(int orderNodeId, int cancellationReasonId, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetDeliveryLibrary(int orderNodeId, string deliveryLibraryPrevalue, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetDeliveryLibrary(int orderNodeId, int deliveryLibraryId, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetDrmWarning(int orderNodeId, bool status, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetDrmWarningWithoutLogging(int orderNodeId, bool status, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetDueDate(int nodeId, DateTime date, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetFollowUpDate(int nodeId, DateTime date, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetFollowUpDateWithoutLogging(int nodeId, DateTime date, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetPatronData(int nodeId, string sierraInfo, int sierraPatronRecordId, int pType, string homeLibrary, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetPatronEmail(int nodeId, string email, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetProviderDueDate(int nodeId, DateTime date, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetProviderInformation(int nodeId, string providerInformation, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetProviderName(int nodeId, string providerName, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetProviderNameWithoutLogging(int nodeId, string providerName, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetProviderOrderId(int nodeId, string providerOrderId, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetPurchasedMaterial(int orderNodeId, int purchasedMaterialId, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetReference(int nodeId, string reference, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetStatus(int orderNodeId, string statusPrevalue, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetStatus(int orderNodeId, int statusId, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }

        public void SetType(int orderNodeId, int typeId, string eventId, bool doReindex = true, bool doSignal = true)
        {
            throw new NotImplementedException();
        }
    }
}