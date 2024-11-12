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

        public IEnumerable<MailOperationResult> SendOutMailsThatAreDue()
        {
            var res = new List<MailOperationResult>();

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
                else if (status.Contains("Transport"))
                {
                    if (now.Date >= AddBusinessDays(deliveryDate, 4).Date)
                    {
                        delayedMailOperation.LogMessages.Add(new LogMessage("LOG", "Transport antas vara genomförd."));
                        delayedMailOperation.Mail.message = _templateService.GetTemplateData("ArticleAvailableInInfodiskMailTemplate", _orderItemManager.GetOrderItem(orderItem.NodeId));
                        delayedMailOperation.LogMessages.Add(new LogMessage("MAIL_NOTE", "Skickat automatiskt leveransmail till " + delayedMailOperation.Mail.recipientEmail));
                        delayedMailOperation.LogMessages.Add(new LogMessage("MAIL", delayedMailOperation.Mail.message));
                        delayedMailOperation.NewStatus = "05:Levererad";
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
                var mailOperationResult = new MailOperationResult()
                {
                    Success = true,
                    Message = "Mail operation successfull.",
                    MailOperation = delayedMailOperation
                };
                try
                {
                    var eventId = _orderItemManager.GenerateEventId(EVENT_TYPE);

                    if (delayedMailOperation.Mail != null)
                    {
                        _mailService.SendMail(delayedMailOperation.Mail);
                    }

                    for (int i = 0; i < delayedMailOperation.LogMessages.Count; i++)
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
                catch (Exception e)
                {
                    mailOperationResult.Success = false;
                    mailOperationResult.Message = "Delayed mail operation failed: " + e.Message;
                }
                res.Add(mailOperationResult);
            }

            return res;
        }

        public void RemoveOldSentMails()
        {
            _mailService.DeleteOldMessagesFromFolder("sentitems", DateTime.Now.AddYears(-1));
        }

        public static DateTime AddBusinessDays(DateTime date, int days)
        {
            var res = date;
            var totalBusinessDaysToAdd = days;
            var businessDaysAddCount = 0;

            if (res.DayOfWeek == DayOfWeek.Saturday)
            {
                res = res.AddDays(1);
            }
            if (res.DayOfWeek == DayOfWeek.Sunday)
            {
                res = res.AddDays(1);
            }

            while (businessDaysAddCount < totalBusinessDaysToAdd)
            {
                res = res.AddDays(1);
                if (res.DayOfWeek == DayOfWeek.Saturday)
                {
                    res = res.AddDays(1);
                }
                if (res.DayOfWeek == DayOfWeek.Sunday)
                {
                    res = res.AddDays(1);
                }
                businessDaysAddCount += 1;
            }
            return res;
        }

        #region Private methods.

        private IEnumerable<OrderItemModel> GetOrderItemsThatAreRelevantForAutomaticMailSending()
        {
            return _orderItemSearcher.Search("status:Utlånad OR status:Krävd OR status:Transport");
        }

        #endregion
    }
}