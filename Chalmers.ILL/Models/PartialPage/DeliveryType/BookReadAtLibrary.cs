﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Models.PartialPage.DeliveryType
{
    public class BookReadAtLibrary : OrderItemPageModelBase
    {
        public string BookAvailableForReadingAtLibraryMailTemplate { get; set; }

        public BookReadAtLibrary(OrderItemModel orderItemModel) : base(orderItemModel) { }
    }
}