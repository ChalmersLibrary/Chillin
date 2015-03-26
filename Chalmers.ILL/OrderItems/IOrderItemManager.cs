using Chalmers.ILL.Models;
using Chalmers.ILL.Models.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;

namespace Chalmers.ILL.OrderItems
{
    public interface IOrderItemManager
    {
        OrderItemModel GetOrderItem(int nodeId);
        List<LogItem> GetLogItems(int nodeId);

        void AddExistingMediaItemAsAnAttachment(int orderNodeId, int mediaNodeId, string title, string link, bool doReindex = true, bool doSignal = true);
        void AddLogItem(int OrderItemNodeId, string Type, string Message, bool doReindex = true, bool doSignal = true);
        void AddSierraDataToLog(int orderItemNodeId, SierraModel sm, bool doReindex = true, bool doSignal = true);

        void SetFollowUpDateWithoutLogging(int nodeId, DateTime date, bool doReindex = true, bool doSignal = true);
        void SetDrmWarningWithoutLogging(int orderNodeId, bool status, bool doReindex = true, bool doSignal = true);
        void SetFollowUpDate(int nodeId, DateTime date, bool doReindex = true, bool doSignal = true);
        void SetDueDate(int nodeId, DateTime date, bool doReindex = true, bool doSignal = true);
        void SetProviderDueDate(int nodeId, DateTime date, bool doReindex = true, bool doSignal = true);
        void SetCancellationReason(int orderNodeId, int cancellationReasonId, bool doReindex = true, bool doSignal = true);
        void SetDeliveryLibrary(int orderNodeId, int deliveryLibraryId, bool doReindex = true, bool doSignal = true);
        void SetDrmWarning(int orderNodeId, bool status, bool doReindex = true, bool doSignal = true);
        void SetPurchasedMaterial(int orderNodeId, int purchasedMaterialId, bool doReindex = true, bool doSignal = true);
        void SetStatus(int orderNodeId, int statusId, bool doReindex = true, bool doSignal = true);
        void SetType(int orderNodeId, int typeId, bool doReindex = true, bool doSignal = true);
        void SetBookId(int nodeId, string bookId, bool doReindex = true, bool doSignal = true);
        void SetProviderInformation(int nodeId, string bookId, bool doReindex = true, bool doSignal = true);

        /// <summary>
        /// Creates a new OrderItem from a MailQueueModel
        /// </summary>
        /// <param name="model">MailQueueModel</param>
        /// <returns>Created nodeId</returns>
        int CreateOrderItemInDbFromMailQueueModel(MailQueueModel model, bool doReindex = true, bool doSignal = true);

        /// <summary>
        /// Creates a new OrderItem from a OrderItemSeedModel.
        /// </summary>
        /// <param name="model">The model with the initial order data.</param>
        /// <param name="doReindex">Should we reindex the node.</param>
        /// <param name="doSignal">Should we signal other things about or save.</param>
        /// <returns>Created nodeId</returns>
        int CreateOrderItemInDbFromOrderItemSeedModel(OrderItemSeedModel model, bool doReindex = true, bool doSignal = true);

        /// <summary>
        /// Saves content without triggering events in Umbraco, then triggers redindexing of the 
        /// content in Lucene and waits for its completion. After the reindexing is done it signals
        /// the ChalmersILL clients using SignalR.
        /// </summary>
        /// <remarks>
        /// This method will wait until all indexing jobs in the indexer queue are finished before exiting.
        /// This should work fine with a few simultaneous users, but if there would be a lot of users
        /// constantly requesting indexing operations the system could appear extremely slow and 
        /// unresponsive.
        /// </remarks>
        /// <param name="cs">The ContentService which this method is called on as an extension.</param>
        /// <param name="content">The content which should be saved.</param>
        /// <param name="doReindex">Indicates if we should do a reindex operation of the saved node.</param>
        /// <param name="doSignal">Indicates if we should emit Signal R events or not.</param>
        void SaveWithoutEventsAndWithSynchronousReindexing(IContent content, bool doReindex = true, bool doSignal = true);
    }
}
