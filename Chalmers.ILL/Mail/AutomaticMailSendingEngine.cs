using Examine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Chalmers.ILL.Extensions;
using System.Globalization;
using Chalmers.ILL.Templates;
using Chalmers.ILL.Models.Mail;
using Chalmers.ILL.OrderItems;

namespace Chalmers.ILL.Mail
{
    public class AutomaticMailSendingEngine : IAutomaticMailSendingEngine
    {
        ISearcher _orderItemSearcher;
        ITemplateService _templateService;
        IOrderItemManager _orderItemManager;
        IMailService _mailService;

        public AutomaticMailSendingEngine(ISearcher orderItemSearcher, ITemplateService templateService, IOrderItemManager orderItemManager,
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

            foreach (var orderItem in orderItems)
            {
                var dueDate = orderItem.Fields.GetValueString("DueDate") == "" ? DateTime.Now :
                    DateTime.ParseExact(orderItem.Fields.GetValueString("DueDate"), "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None);

                var mail = new OutgoingMailModel();
                mail.OrderId = orderItem.Fields.GetValueString("OrderId");
                mail.recipientName = orderItem.Fields.GetValueString("PatronName");
                mail.recipientEmail = orderItem.Fields.GetValueString("PatronEmail");

                if (now.Date == dueDate.AddDays(-5).Date)
                {
                    mail.message = _templateService.GetTemplateData("CourtesyNoticeMailTemplate", _orderItemManager.GetOrderItem(orderItem.Id));
                    _mailService.SendMail(mail);
                    _orderItemManager.AddLogItem(orderItem.Id, "MAIL_NOTE", "Skickat automatiskt \"courtesy notice\" till " + mail.recipientEmail, false, false);
                    _orderItemManager.AddLogItem(orderItem.Id, "MAIL", mail.message, true, true);
                }
                else if (now.Date == dueDate.AddDays(1).Date)
                {
                    mail.message = _templateService.GetTemplateData("LoanPeriodOverMailTemplate", _orderItemManager.GetOrderItem(orderItem.Id));
                    _mailService.SendMail(mail);
                    _orderItemManager.AddLogItem(orderItem.Id, "MAIL_NOTE", "Skickat automatiskt påminnelsemail nummer ett till " + mail.recipientEmail, false, false);
                    _orderItemManager.AddLogItem(orderItem.Id, "MAIL", mail.message, true, true);
                }
                else if (now.Date == dueDate.AddDays(5).Date)
                {
                    mail.message = _templateService.GetTemplateData("LoanPeriodReallyOverMailTemplate", _orderItemManager.GetOrderItem(orderItem.Id));
                    _mailService.SendMail(mail);
                    _orderItemManager.AddLogItem(orderItem.Id, "MAIL_NOTE", "Skickat automatiskt påminnelsemail nummer två till " + mail.recipientEmail, false, false);
                    _orderItemManager.AddLogItem(orderItem.Id, "MAIL", mail.message, true, true);
                }
                else if (now.Date == dueDate.AddDays(10).Date)
                {
                    mail.message = _templateService.GetTemplateData("LoanPeriodReallyReallyOverMailTemplate", _orderItemManager.GetOrderItem(orderItem.Id));
                    _mailService.SendMail(mail);
                    _orderItemManager.AddLogItem(orderItem.Id, "MAIL_NOTE", "Skickat automatiskt påminnelsemail nummer tre till " + mail.recipientEmail, false, false);
                    _orderItemManager.AddLogItem(orderItem.Id, "MAIL", mail.message, true, true);
                }
            }
        }

        #region Private methods.

        private ISearchResults GetOrderItemsThatAreRelevantForAutomaticMailSending()
        {
            var searchCriteria = _orderItemSearcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
            return _orderItemSearcher.Search(searchCriteria.RawQuery("Type:Bok AND (Status:Utlånad OR Status:Krävd)"));
        }

        #endregion
    }
}