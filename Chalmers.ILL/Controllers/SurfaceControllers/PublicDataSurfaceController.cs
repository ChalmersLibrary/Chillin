using Examine;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using Chalmers.ILL.Extensions;
using System.Globalization;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class PublicDataSurfaceController : SurfaceController
    {
        ISearcher _orderItemsSearcher;

        public PublicDataSurfaceController([Dependency("OrderItemsSearcher")] ISearcher orderItemsSearcher)
        {
            _orderItemsSearcher = orderItemsSearcher;
        }

        [HttpGet]
        public ActionResult GetChillinDataForSierraPatron(int recordId, string lang)
        {
            var res = new PublicChillinDataConnectedToPatron();

            if (lang == null)
            {
                lang = "en";
            }

            try
            {
                var searchCriteria = _orderItemsSearcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
                var searchResults = _orderItemsSearcher.Search(searchCriteria.RawQuery("SierraPatronRecordId:\"" + recordId + "\" AND " + 
                    "(Status:Ny OR Status:Åtgärda OR Status:Beställd OR Status:Väntar OR Status:Mottagen OR Status:Krävd OR Status:Utlånad OR Status:Transport)"));

                res.Items = new List<SimplifiedOrderItem>();

                foreach (var orderItem in searchResults)
                {
                    var newSimplifiedOrderItem = new SimplifiedOrderItem();

                    var rawDueDate = orderItem.Fields.GetValueString("DueDate") == "" ? DateTime.Now :
                        DateTime.ParseExact(orderItem.Fields.GetValueString("DueDate"), "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None);
                    var rawType = orderItem.Fields["Type"];
                    var rawStatus = orderItem.Fields["Status"];
                    var rawReference = orderItem.Fields["OriginalOrder"];

                    if (rawType == "Bok")
                    {
                        if (lang == "sv")
                        {
                            if (rawDueDate.Year != 1970)
                            {
                                newSimplifiedOrderItem.LoanPeriod = rawDueDate.ToString("yyyy-MM-dd");
                            }
                        }
                        else
                        {
                            // Default to english
                            if (rawDueDate.Year != 1970)
                            {
                                newSimplifiedOrderItem.LoanPeriod = rawDueDate.ToString("MM/dd/yyyy");
                            }
                        }
                    }

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

                    if (rawStatus.Contains("Ny"))
                    {
                        newSimplifiedOrderItem.Status = Translate("Ej behandlad", lang);
                    }
                    else if (rawStatus.Contains("Krävd") || (DueDateIsValid(rawDueDate) && DateTime.Now.Date > rawDueDate))
                    {
                        newSimplifiedOrderItem.Status = Translate("Krävd", lang);
                    }
                    else
                    {
                        newSimplifiedOrderItem.Status = Translate("Behandlas", lang);
                    }

                    newSimplifiedOrderItem.Reference = rawReference.Length <= 120 ? rawReference : (rawReference.Substring(0, 117) + "...");

                    res.Items.Add(newSimplifiedOrderItem);
                }

                res.Success = true;
                res.Message = "Successfully fetched data.";
            }
            catch (Exception e)
            {
                res.Success = false;
                res.Message = "Failed to get data.";
            }

            return Json(res, JsonRequestBehavior.AllowGet);
        }

        #region Private methods

        private Boolean DueDateIsValid(DateTime dueDate)
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
            { "Behandlas", "Processing" }
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

        private class PublicChillinDataConnectedToPatron
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public List<SimplifiedOrderItem> Items { get; set; }
        }

        private class SimplifiedOrderItem
        {
            public SimplifiedOrderItem()
            {
                Type = "";
                Reference = "";
                Status = "";
                LoanPeriod = "";
            }

            public string Type { get; set; }
            public string Reference { get; set; }
            public string Status { get; set; }
            public string LoanPeriod { get; set; }
        }
    }
}
