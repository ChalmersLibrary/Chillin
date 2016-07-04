using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using Chalmers.ILL.Models;
using Chalmers.ILL.Utilities;
using umbraco.cms.businesslogic.member;
using System.Configuration;
using Examine;
using UmbracoExamine;
using Newtonsoft.Json;
using System.Globalization;
using Chalmers.ILL.Statistics;
using Chalmers.ILL.OrderItems;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class StatisticsSurfaceController : SurfaceController
    {
        private IOrderItemSearcher _orderItemSearcher;

        public StatisticsSurfaceController(IOrderItemSearcher orderItemSearcher)
        {
            _orderItemSearcher = orderItemSearcher;
        }

        /// <summary>
        /// Get statistics data given a specific statistics request.
        /// </summary>
        /// <remarks>Using HTTP POST here because the query data could be fairly large, even though the method won't change anything.</remarks>
        /// <param name="json">The json data representing a statistics request.</param>
        /// <returns>JsonResult containing the statistics result.</returns>
        [HttpPost]
        public ActionResult GetData(string json)
        {
            var sReq = JsonConvert.DeserializeObject<StatisticsRequest>(json);

            var res = new StatisticsResult();

            try
            {
                IStatisticsManager statMngr = new DefaultStatMngr(_orderItemSearcher, new DefaultStatCalc());

                statMngr.CalculateAllData(sReq);

                res.Success = true;
                res.Message = "Succeessfully fetched statistics.";
                res.StatisticsData = sReq.StatisticsData;
            }
            catch (Exception e)
            {
                res.Success = false;
                res.Message = "Failed to import document from data: " + e.Message;
            }

            return Json(res, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Get available values from the ChalmersILLOrderItemsSearcher for a list of keys.
        /// </summary>
        /// <param name="req">The KeyValueRequest specifying what keys we want to fetch values for.</param>
        /// <returns>JsonResult containing the list of keys and the fetched available values for each key.</returns>
        [HttpGet]
        public ActionResult GetAvailableValues(KeyValueRequest req)
        {
            var res = new KeyValueResult();
            res.KeyValues = new List<KeyValues>();

            try
            {
                var searcher = ExamineManager.Instance.SearchProviderCollection["ChalmersILLOrderItemsSearcher"];
                var searchCriteria = searcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
                var allOrders = searcher.Search(searchCriteria.RawQuery("nodeTypeAlias:ChalmersILLOrderItem"));

                foreach (var k in req.Keys) {
                    var keyValues = new KeyValues();
                    keyValues.Key = k;
                    SetPrettyName(keyValues);
                    keyValues.AvailableValues = allOrders.Where(x => x.Fields.ContainsKey(k)).Select(x => x.Fields[k]).Distinct().OrderBy(x => x).ToList();
                    res.KeyValues.Add(keyValues);
                }

                res.Success = true;
                res.Message = "Successfully fetched available values for keys.";
            }
            catch (Exception e)
            {
                res.Success = false;
                res.Message = "Failed to fetch available values for keys: " + e.Message;
            }

            return Json(res, JsonRequestBehavior.AllowGet);
        }

        #region Private

        private void SetPrettyName(KeyValues kv)
        {
            if (kv.Key == "Type")
            {
                kv.Name = "Typ";
            }
            else if (kv.Key == "ProviderName")
            {
                kv.Name = "Leverantörsnamn";
            }
            else if (kv.Key == "pType")
            {
                kv.Name = "P-Typ";
            }
            else if (kv.Key == "DeliveryLibrary")
            {
                kv.Name = "Ägandebibliotek";
            }
            else if (kv.Key == "HomeLibrary")
            {
                kv.Name = "Hembibliotek";
            }
            else if (kv.Key == "CancellationReason")
            {
                kv.Name = "Annulleringsorsak";
            }
            else if (kv.Key == "PurchasedMaterial")
            {
                kv.Name = "Inköpt Material";
            }
            else
            {
                kv.Name = kv.Key;
            }
        }

        #endregion
    }
}