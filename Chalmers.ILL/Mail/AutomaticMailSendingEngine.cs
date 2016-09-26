using System;
using System.Collections.Generic;
using Chalmers.ILL.Templates;
using Chalmers.ILL.Models.Mail;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Models;

namespace Chalmers.ILL.Mail
{
    public class AutomaticMailSendingEngine : IAutomaticMailSendingEngine
    {
        public static int EVENT_TYPE { get { return 15; } }

        IOrderItemSearcher _orderItemSearcher;
        ITemplateService _templateService;
        IOrderItemManager _orderItemManager;
        IMailService _mailService;

        public AutomaticMailSendingEngine(IOrderItemSearcher orderItemSearcher, ITemplateService templateService, IOrderItemManager orderItemManager,
            IMailService mailService)
        {
            _orderItemSearcher = orderItemSearcher;
            _templateService = templateService;
            _orderItemManager = orderItemManager;
            _mailService = mailService;
        }

        public void SendOutMailsThatAreDue()
        {
            // Grab the date and use it for all e-mails during this run.
            var now = DateTime.Now;

            var orderItems = GetOrderItemsThatAreRelevantForAutomaticMailSending();

            var delayedMailOperations = new List<DelayedMailOperation>();

            foreach (var orderItem in orderItems)
            {
                var dueDate = orderItem.DueDate;
                var deliveryDate = orderItem.DeliveryDate;
                var status = orderItem.Status;

                var delayedMailOperation = new DelayedMailOperation(
                    orderItem.NodeId,
                    orderItem.OrderId,
                    orderItem.PatronName,
                    orderItem.PatronEmail);

                if (status.Contains("Utlånad") || status.Contains("Krävd"))
                {
                    if (status.Contains("Utlånad") && now.Date == dueDate.AddDays(-5).Date)
                    {
                        delayedMailOperation.Mail.message = _templateService.GetTemplateData("CourtesyNoticeMailTemplate", _orderItemManager.GetOrderItem(orderItem.NodeId));
                        delayedMailOperation.LogMessages.Add(new LogMessage("MAIL_NOTE", "Skickat automatiskt \"courtesy notice\" till " + delayedMailOperation.Mail.recipientEmail));
                        delayedMailOperation.LogMessages.Add(new LogMessage("MAIL", delayedMailOperation.Mail.message));
                        delayedMailOperation.ShouldBeProcessed = true;
                    }
                    else if (status.Contains("Utlånad") && now.Date == dueDate.AddDays(1).Date)
                    {
                        delayedMailOperation.Mail.message = _templateService.GetTemplateData("LoanPeriodOverMailTemplate", _orderItemManager.GetOrderItem(orderItem.NodeId));
                        delayedMailOperation.LogMessages.Add(new LogMessage("MAIL_NOTE", "Skickat automatiskt påminnelsemail nummer ett till " + delayedMailOperation.Mail.recipientEmail));
                        delayedMailOperation.LogMessages.Add(new LogMessage("MAIL", delayedMailOperation.Mail.message));
                        delayedMailOperation.ShouldBeProcessed = true;
                    }
                    else if (now.Date == dueDate.AddDays(5).Date)
                    {
                        delayedMailOperation.Mail.message = _templateService.GetTemplateData("LoanPeriodReallyOverMailTemplate", _orderItemManager.GetOrderItem(orderItem.NodeId));
                        delayedMailOperation.LogMessages.Add(new LogMessage("MAIL_NOTE", "Skickat automatiskt påminnelsemail nummer två till " + delayedMailOperation.Mail.recipientEmail));
                        delayedMailOperation.LogMessages.Add(new LogMessage("MAIL", delayedMailOperation.Mail.message));
                        delayedMailOperation.ShouldBeProcessed = true;
                    }
                    else if (now.Date == dueDate.AddDays(10).Date)
                    {
                        delayedMailOperation.Mail.message = _templateService.GetTemplateData("LoanPeriodReallyReallyOverMailTemplate", _orderItemManager.GetOrderItem(orderItem.NodeId));
                        delayedMailOperation.LogMessages.Add(new LogMessage("MAIL_NOTE", "Skickat automatiskt påminnelsemail nummer tre till " + delayedMailOperation.Mail.recipientEmail));
                        delayedMailOperation.LogMessages.Add(new LogMessage("MAIL", delayedMailOperation.Mail.message));
                        delayedMailOperation.ShouldBeProcessed = true;
                    }
                    else if (now.Date >= dueDate.AddDays(17).Date)
                    {
                        delayedMailOperation.LogMessages.Add(new LogMessage("LOG", "Bok mycket försenad."));
                        delayedMailOperation.Mail = null;
                        delayedMailOperation.NewStatus = "02:Åtgärda";
                        delayedMailOperation.ShouldBeProcessed = true;
                    }
                }

                if (delayedMailOperation.ShouldBeProcessed)
                {
                    delayedMailOperations.Add(delayedMailOperation);
                }
            }

            // Send out all the delayed mails now, so that the IndexReader is not used and gets broken.
            foreach (var delayedMailOperation in delayedMailOperations) {
                var eventId = _orderItemManager.GenerateEventId(EVENT_TYPE);

                if (delayedMailOperation.Mail != null)
                {
                    _mailService.SendMail(delayedMailOperation.Mail);
                }

                for (int i=0; i<delayedMailOperation.LogMessages.Count; i++)
                {
                    var logMsg = delayedMailOperation.LogMessages[i];
                    var shouldReindexAndSignal = String.IsNullOrWhiteSpace(delayedMailOperation.NewStatus) && i == delayedMailOperation.LogMessages.Count - 1;
                    _orderItemManager.AddLogItem(delayedMailOperation.InternalOrderId, logMsg.type, logMsg.message, eventId, shouldReindexAndSignal, shouldReindexAndSignal);
                }

                if (!String.IsNullOrWhiteSpace(delayedMailOperation.NewStatus))
                {
                    _orderItemManager.SetStatus(delayedMailOperation.InternalOrderId, delayedMailOperation.NewStatus, eventId, true, true);
                }
            }
        }

        #region Private methods.

        private IEnumerable<OrderItemModel> GetOrderItemsThatAreRelevantForAutomaticMailSending()
        {
            return _orderItemSearcher.Search("Status:Utlånad OR Status:Krävd");
        }

        private class LogMessage
        {
            public string type { get; set; }
            public string message { get; set; }

            public LogMessage(string type, string message)
            {
                this.type = type;
                this.message = message;
            }
        }

        private class DelayedMailOperation
        {
            public int InternalOrderId { get; set; }
            public OutgoingMailModel Mail { get; set; }
            public List<LogMessage> LogMessages { get; set; }
            public string NewStatus { get; set; }
            public bool ShouldBeProcessed { get; set; }

            public DelayedMailOperation(int internalOrderId, OutgoingMailModel mail)
            {
                this.InternalOrderId = internalOrderId;
                this.Mail = mail;
                LogMessages = new List<LogMessage>();
                ShouldBeProcessed = false;
            }

            public DelayedMailOperation(int orderId) : this(orderId, null) {}
            public DelayedMailOperation(int internalOrderId, string orderId, string patronName, string patronEmail) : 
                this(internalOrderId, new OutgoingMailModel(orderId, patronName, patronEmail)) {}
        }

        #endregion
    }
}