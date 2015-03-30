using Examine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Chalmers.ILL.Extensions;
using System.Globalization;

namespace Chalmers.ILL.Mail
{
    public class AutomaticMailSendingEngine : IAutomaticMailSendingEngine
    {
        ISearcher _orderItemSearcher;

        public AutomaticMailSendingEngine(ISearcher orderItemSearcher)
        {
            _orderItemSearcher = orderItemSearcher;
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

                if (orderItem.Fields["Status"].Contains("Utlånad"))
                {
                    if (now.Date == dueDate.AddDays(-5).Date)
                    {
                        // TODO: Skicka påminnelse när lånetiden snart slut.
                    }
                    else if (now.Date == dueDate.Date)
                    {
                        // TODO: Skicka påminnelse när lånetiden har tagit slut.
                    }
                    else if (now.Date == dueDate.AddDays(5).Date)
                    {
                        // TODO: Skicka arg påminnelse när lånetiden har tagit slut och några dagar har gått.
                    }
                    else if (now.Date == dueDate.AddDays(10).Date)
                    {
                        // TODO: Skicka jättearg påminnelse när lånetiden har tagit slut och många dagar har gått.
                    }
                }
                else if (orderItem.Fields["Status"].Contains("Krävd"))
                {
                    if (now.Date == dueDate.AddDays(5).Date)
                    {
                        // TODO: Skicka arg påminnelse när boken är krävd och några dagar har gått efter att lånetiden tagit slut.
                    }
                    else if (now.Date == dueDate.AddDays(10).Date)
                    {
                        // TODO: Skicka jättearg påminnelse när boken är krävd och många dagar har gått efter att lånetiden tagit slut.
                    }
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