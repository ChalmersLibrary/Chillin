using System;
using System.Collections.Generic;
using Chalmers.ILL.Models;
using Examine;
using Chalmers.ILL.Extensions;
using System.Globalization;

namespace Chalmers.ILL.OrderItems
{
    public class UmbracoOrderItemSearcher : IOrderItemSearcher
    {
        IEnumerable<OrderItemModel> IOrderItemSearcher.Search(string query)
        {
            var res = new List<OrderItemModel>();
            var searcher = ExamineManager.Instance.SearchProviderCollection["ChalmersILLOrderItemsSearcher"];
            var searchCriteria = searcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
            var results = searcher.Search(searchCriteria.RawQuery(query));

            foreach (var item in results)
            {
                var newOrderItem = new OrderItemModel();
                newOrderItem.NodeId = item.Id;
                newOrderItem.DueDate = item.Fields.GetValueString("DueDate") == "" ? DateTime.Now :
                    DateTime.ParseExact(item.Fields.GetValueString("DueDate"), "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None);
                newOrderItem.DeliveryDate = item.Fields.GetValueString("DeliveryDate") == "" ? DateTime.Now :
                    DateTime.ParseExact(item.Fields.GetValueString("DeliveryDate"), "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None);
                newOrderItem.StatusPrevalue = item.Fields.GetValueString("Status");
                newOrderItem.OrderId = item.Fields.GetValueString("OrderId");
                newOrderItem.PatronName = item.Fields.GetValueString("PatronName");
                newOrderItem.PatronEmail = item.Fields.GetValueString("PatronEmail");
                newOrderItem.ProviderOrderId = item.Fields.GetValueString("ProviderOrderId");
                newOrderItem.FollowUpDate = item.Fields.GetValueString("FollowUpDate") == "" ? DateTime.Now :
                    DateTime.ParseExact(item.Fields.GetValueString("FollowUpDate"), "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None);
                newOrderItem.DeliveryLibraryPrevalue = item.Fields.GetValueString("DeliveryLibrary");
                newOrderItem.TypePrevalue = item.Fields.GetValueString("Type");
                newOrderItem.Reference = item.Fields.GetValueString("Reference");
                newOrderItem.CreateDate = item.Fields["createDate"] == "" ? DateTime.Now :
                    DateTime.ParseExact(item.Fields.GetValueString("createDate"), "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None);
                newOrderItem.Log = item.Fields.GetValueString("Log");
                res.Add(newOrderItem);
            }

            return res;
        }
    }
}