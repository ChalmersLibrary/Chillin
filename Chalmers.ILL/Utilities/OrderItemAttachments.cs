using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core.Models;
using Chalmers.ILL.Extensions;
using Chalmers.ILL.Models;
using Newtonsoft.Json;

namespace Chalmers.ILL.Utilities
{
    public class OrderItemAttachments
    {
        /// <summary>
        /// Internal method to add a reference of an attachment to an order item.
        /// </summary>
        /// <param name="orderNodeId">OrderItem Node Id</param>
        /// <param name="name">The name of </param>
        /// <param name="link">Status to set using statusID</param>
        /// <returns>True if everything went ok</returns>
        public static void AddOrderItemAttachment(int orderNodeId, int mediaNodeId, string title, string link, bool doReindex = true, bool doSignal = true)
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
                    cs.SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);
                    Logging.WriteLogItemInternal(orderNodeId, "ATTACHMENT", "Nytt dokument bundet till ordern.", doReindex, doSignal);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}