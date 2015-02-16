﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Exchange.WebServices.Data;

namespace Chalmers.ILL.Models
{
    public enum MailQueueType
    {
        NEW,
        REPLY,
        UNKNOWN,
        DELIVERY,
        ERROR
    }

    public class MailQueueModel
    {
        public ItemId Id { get; set; }
        public FolderId ArchiveFolderId { get; set; }
        public MailQueueType Type { get; set; }

        public string MsgRef { get; set; }
        public string To { get; set; }
        public string From { get; set; }
        public string Sender { get; set; }
        public string Subject { get; set; }
        public string MessageBody { get; set; }
        public string DateTimeReceived { get; set; }

        public string PatronName { get; set; }
        public string PatronEmail { get; set; }
        public string PatronCardNo { get; set; }
        public string OriginalOrder { get; set; }

        public bool IsPurchaseRequest { get; set; }

        public string OrderId { get; set; }
        public int OrderItemNodeId { get; set; }

        public string Debug { get; set; }
        public string StatusResult { get; set; }
        public string ParseErrorMessage { get; set; }

        public SierraModel SierraPatronInfo { get; set; }

        public List<MailAttachment> Attachments { get; set; }
    }

}