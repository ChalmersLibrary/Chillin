using Examine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.ILL.Mail
{
    public class AutomaticMailSendingEngine : IAutomaticMailSendingEngine
    {
        ISearcher _orderItemSearcher;

        public AutomaticMailSendingEngine(ISearcher orderItemSearcher)
        {
            _orderItemSearcher = orderItemSearcher;
        }

        public void SendAll()
        {
            var orderItems = GetOrderItemsThatAreRelevantForAutomaticMailSending();

            foreach (var orderItem in orderItems)
            {
                if (orderItem.Fields["Status"].Contains("Infodisk"))
                {
                    // TODO: Skicka en påminnelse om bok har legat för hämtning i en vecka och det är mer än en vecka kvar av lånetiden.
                    // TODO: Skicka speciell påminnelse om bok har legat för hämtning i en vecka och det är mindre än en vecka kvar av lånetiden.
                }
                else if (orderItem.Fields["Status"].Contains("Utlånad"))
                {
                    // TODO: Skicka påminnelse varje vecka om lånetid har gått ut.
                    // TODO: Skicka påminnelse när lånetiden snart slut.
                    // TODO: Skicka påminnelse när lånetiden har tagit slut.
                    // TODO: Skicka arg påminnelse när lånetiden har tagit slut och några dagar har gått.
                    // TODO: Skicka jättearg påminnelse när lånetiden har tagit slut och många dagar har gått.
                }
            }
        }

        #region Private methods.

        private ISearchResults GetOrderItemsThatAreRelevantForAutomaticMailSending()
        {
            var searchCriteria = _orderItemSearcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
            return _orderItemSearcher.Search(searchCriteria.RawQuery("Type:Bok AND (Status:Infodisk OR Status:Utlånad)"));
        }

        #endregion
    }
}