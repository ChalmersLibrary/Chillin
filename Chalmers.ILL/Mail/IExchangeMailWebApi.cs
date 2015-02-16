using Chalmers.ILL.Models;
using Microsoft.Exchange.WebServices.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.Mail
{
    public interface IExchangeMailWebApi
    {
        void ConnectToExchangeService(string username, string password);
        List<MailQueueModel> ReadMailQueue();
        FolderId ArchiveMailMessage(ItemId Id);
        void ForwardMailMessage(ItemId Id, string recipientAddress, string extraText = "", bool delete = true);
        void SendMailMessage(string orderId, string body, string subject, string recipientName, string recipientAddress, IDictionary<string, byte[]> attachments);
        void SendPlainMailMessage(string body, string subject, string recipientAddress);
    }
}
