using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models.PartialPage
{
    public class ChalmersILLActionLogEntryModel 
    {
        private OrderItemModel _orderItemModel;
        public OrderItemModel OrderItem { get { return _orderItemModel; } }

        public List<UmbracoDropdownListNtextDataType> AvailableTypes { get; set; }
        public List<UmbracoDropdownListNtextDataType> AvailableStatuses { get; set; }
        public List<UmbracoDropdownListNtextDataType> AvailableDeliveryLibraries { get; set; }
        public List<UmbracoDropdownListNtextDataType> AvailableCancellationReasons { get; set; }
        public List<UmbracoDropdownListNtextDataType> AvailablePurchasedMaterials { get; set; }

        public ChalmersILLActionLogEntryModel(OrderItemModel orderItemModel)
        {
            _orderItemModel = orderItemModel;
        }
    }
}