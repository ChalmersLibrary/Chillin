using Chalmers.ILL.Models;
using Chalmers.ILL.Utilities;
using Chalmers.ILL.Extensions;
using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using Newtonsoft.Json;
using Chalmers.ILL.Patron;
using System.Configuration;
using System.Net;
using System.IO;
using Chalmers.ILL.OrderItems;
using Umbraco.Core.Models;
using Chalmers.ILL.UmbracoApi;
using Microsoft.Practices.Unity;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemPatronDataSurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;
        IPatronDataProvider _patronDataProviderSierraCache;
        IPatronDataProvider _patronDataProviderSierra;
        IUmbracoWrapper _umbraco;

        public OrderItemPatronDataSurfaceController(IOrderItemManager orderItemManager, IPatronDataProvider patronDataProviderSierraCache, 
            [Dependency("Sierra")] IPatronDataProvider patronDataProviderSierra, IUmbracoWrapper umbraco)
        {
            _orderItemManager = orderItemManager;
            _patronDataProviderSierraCache = patronDataProviderSierraCache;
            _patronDataProviderSierra = patronDataProviderSierra;
            _umbraco = umbraco;
        }

        [HttpGet]
        public ActionResult RenderPatronDataView(int nodeId)
        {
            // Get a new OrderItem populated with values for this node
            var orderItem = _orderItemManager.GetOrderItem(nodeId);

            return PartialView("Chalmers.ILL.Action.PatronData", orderItem);
        }

        /// <summary>
        /// Query data from our Solr cache.
        /// </summary>
        /// <param name="pnr">The query string.</param>
        /// <returns>Returns a result indicating how the request went.</returns>
        [HttpGet]
        public ActionResult QueryPatronDataFromCache(string query)
        {
            var json = new ResultResponse();

            try
            {
                HttpWebRequest fileReq = (HttpWebRequest)HttpWebRequest.Create(ConfigurationManager.AppSettings["patronCacheSolrQueryUrl"] + query + "&wt=json");
                fileReq.CookieContainer = new CookieContainer();
                fileReq.AllowAutoRedirect = true;
                HttpWebResponse fileResp = (HttpWebResponse)fileReq.GetResponse();
                var outputStream = fileResp.GetResponseStream();

                var sr = new StreamReader(outputStream);
                json.Message = sr.ReadToEnd();
                json.Success = true;
            }
            catch (Exception e)
            {
                json.Message = e.Message;
                json.Success = false;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Retrieves the data from Sierra using first library card number and secondly personnummer if the first 
        /// fails. After successfully fetching the data it binds it to the order item with the given id.
        /// </summary>
        /// <param name="orderItemNodeId">The id of the order item which the document should be bound to.</param>
        /// <param name="lcn">The library card number which we should search for.</param>
        /// <param name="pnr">The "personnummer" which we should search for.</param>
        /// <returns>Returns a result indicating how the request went.</returns>
        [HttpPost]
        public ActionResult FetchPatronDataUsingLcnOrPnr(int orderItemNodeId, string lcn, string pnr, bool cache=true)
        {
            var json = new ResultResponse();

            try
            {
                var cs = Services.ContentService;
                var content = cs.GetById(orderItemNodeId);

                SierraModel sm = null;
                if (cache)
                {
                    sm = _patronDataProviderSierraCache.GetPatronInfoFromLibraryCardNumberOrPersonnummer(lcn, pnr);
                }
                else
                {
                    sm = _patronDataProviderSierra.GetPatronInfoFromLibraryCardNumberOrPersonnummer(lcn, pnr);
                }

                if (!String.IsNullOrEmpty(sm.id))
                {
                    content.SetValue("sierraInfo", JsonConvert.SerializeObject(sm));
                    content.SetValue("sierraPatronRecordId", sm.record_id);
                    content.SetValue("pType", sm.ptype);
                    content.SetValue("homeLibrary", sm.home_library);
                    UpdateDeliveryLibraryIfNeeded(content.Id, sm);
                    _orderItemManager.SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);
                }
                _orderItemManager.AddSierraDataToLog(orderItemNodeId, sm);

                json.Success = true;
                json.Message = "Succcessfully loaded Sierra data from \"personnummer\" or library card number.";
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Failed to load Sierra data from \"personnummer\" or library card number: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Retrieves the data from Sierra using the library card number and binds the data to the order item
        /// with the given id.
        /// </summary>
        /// <param name="orderItemNodeId">The id of the order item which the document should be bound to.</param>
        /// <param name="lcn">The library card number which we should search for.</param>
        /// <returns>Returns a result indicating how the request went.</returns>
        [HttpPost]
        public ActionResult FetchPatronDataUsingLcn(int orderItemNodeId, string lcn, bool cache=true)
        {
            var json = new ResultResponse();

            try
            {
                var cs = Services.ContentService;
                var content = cs.GetById(orderItemNodeId);

                SierraModel sm = null;
                if (cache)
                {
                    sm = _patronDataProviderSierraCache.GetPatronInfoFromLibraryCardNumber(lcn);
                }
                else
                {
                    sm = _patronDataProviderSierra.GetPatronInfoFromLibraryCardNumber(lcn);
                }

                if (!String.IsNullOrEmpty(sm.id))
                {
                    content.SetValue("sierraInfo", JsonConvert.SerializeObject(sm));
                    content.SetValue("sierraPatronRecordId", sm.record_id);
                    content.SetValue("pType", sm.ptype);
                    content.SetValue("homeLibrary", sm.home_library);
                    UpdateDeliveryLibraryIfNeeded(content.Id, sm);
                    _orderItemManager.SaveWithoutEventsAndWithSynchronousReindexing(content, false, false);
                }
                _orderItemManager.AddSierraDataToLog(orderItemNodeId, sm);

                json.Success = true;
                json.Message = "Succcessfully loaded Sierra data from library card number.";
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Failed to load Sierra data from library card number: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        #region Private methods

        private void UpdateDeliveryLibraryIfNeeded(int nodeId, SierraModel sierraModel)
        {
            var orderItem = _orderItemManager.GetOrderItem(nodeId);

            if (orderItem.TypePrevalue.Contains("Bok"))
            {
                if (sierraModel.home_library != null && sierraModel.home_library.Contains("hbib"))
                {
                    _orderItemManager.SetDeliveryLibrary(nodeId, "Huvudbiblioteket", false, false);
                }
                else if (sierraModel.home_library != null && sierraModel.home_library.Contains("abib"))
                {
                    _orderItemManager.SetDeliveryLibrary(nodeId, "Arkitekturbiblioteket", false, false);
                }
                else if (sierraModel.home_library != null && sierraModel.home_library.Contains("lbib"))
                {
                    _orderItemManager.SetDeliveryLibrary(nodeId, "Lindholmenbiblioteket", false, false);
                }
            }
        }

        #endregion
    }
}
