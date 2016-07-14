﻿using Examine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Chalmers.ILL.Extensions;
using System.Globalization;

namespace Chalmers.ILL.OrderItems
{
    public class BulkDataManager : IBulkDataManager
    {
        IOrderItemSearcher _orderItemsSearcher;

        public BulkDataManager(IOrderItemSearcher orderItemsSearcher)
        {
            _orderItemsSearcher = orderItemsSearcher;
        }

        public List<SimplifiedOrderItem> GetChillinDataForSierraPatron(int recordId, string lang)
        {
            var searchResults = _orderItemsSearcher.Search("SierraPatronRecordId:\"" + recordId + "\" AND " +
                "(Status:Ny OR Status:Åtgärda OR Status:Beställd OR Status:Väntar OR Status:Mottagen OR Status:Krävd OR Status:Utlånad OR Status:Transport OR " +
                    "(Status:Levererad AND DeliveryDate:[" + DateTime.Now.AddDays(-7).ToString("yyyyMMddHHmmssfff") + " TO " + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "]))");

            var items = new List<SimplifiedOrderItem>();

            foreach (var orderItem in searchResults)
            {
                var newSimplifiedOrderItem = new SimplifiedOrderItem();

                var rawDueDate = orderItem.DueDate;
                var rawType = orderItem.Type;
                var rawStatus = orderItem.Status;
                var rawReference = orderItem.OriginalOrder;
                var rawDeliveryDate = orderItem.DeliveryDate;

                if (rawType == "Artikel")
                {
                    newSimplifiedOrderItem.Type = Translate("Artikel", lang);
                }
                else if (rawType == "Bok" || rawType == "Inköpsförslag")
                {
                    newSimplifiedOrderItem.Type = Translate("Bok", lang);
                }
                else
                {
                    newSimplifiedOrderItem.Type = Translate("Okänd", lang);
                }

                if (rawType == "Artikel")
                {
                    if (rawStatus.Contains("Levererad"))
                    {
                        newSimplifiedOrderItem.Status = Translate("Levererad", lang);
                    }
                    else
                    {
                        newSimplifiedOrderItem.Status = Translate("Ej tillgänglig än", lang);
                    }
                }
                else if (rawType == "Bok")
                {
                    if (rawStatus.Contains("Krävd"))
                    {
                        newSimplifiedOrderItem.Status = Translate("Krävd", lang) + " *";
                    }
                    else if (!rawStatus.Contains("Återsänd") && DateIsValid(rawDeliveryDate) && DateIsValid(rawDueDate) && DateTime.Now.Date > rawDueDate)
                    {
                        newSimplifiedOrderItem.Status = Translate("Försenad", lang) + " **";
                    }
                    else if (!rawStatus.Contains("Återsänd") && DateIsValid(rawDeliveryDate) && DateIsValid(rawDueDate) && DateTime.Now.Date <= rawDueDate)
                    {
                        newSimplifiedOrderItem.Status = Translate("ÅTER", lang) + " " + rawDueDate.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        newSimplifiedOrderItem.Status = Translate("Ej tillgänglig än", lang);
                    }
                }
                else
                {
                    newSimplifiedOrderItem.Status = Translate("Ej tillgänglig än", lang);
                }

                newSimplifiedOrderItem.Reference = rawReference.Length <= 120 ? rawReference : (rawReference.Substring(0, 117) + "...");

                items.Add(newSimplifiedOrderItem);
            }

            return items;
        }

        #region Private methods

        private Boolean DateIsValid(DateTime dueDate)
        {
            return dueDate != null && dueDate.Year != 1970;
        }

        private Dictionary<string, string> _swedishEnglishDictionary = new Dictionary<string, string>()
        {
            { "Artikel", "Article" },
            { "Bok", "Book" },
            { "Okänd", "Unknown" },
            { "Ej behandlad", "Unprocessed" },
            { "Krävd", "Recalled" },
            { "Behandlas", "Processing" },
            { "Ej tillgänglig än", "Not available yet" },
            { "Levererad", "Delivered" },
            { "ÅTER", "DUE" },
            { "Försenad", "Late" }
        };

        private string Translate(string word, string lang)
        {
            var res = word;

            if (lang != "sv")
            {
                // Default to english
                res = _swedishEnglishDictionary[word];
            }

            return res;
        }

        #endregion
    }
}