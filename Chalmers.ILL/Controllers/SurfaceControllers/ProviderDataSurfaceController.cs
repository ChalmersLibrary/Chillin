using Chalmers.ILL.Models;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Providers;
using Chalmers.ILL.Templates;
using Chalmers.ILL.UmbracoApi;
using Examine;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class ProviderDataSurfaceController : SurfaceController
    {
        ITemplateService _templateService;
        IProviderService _providerService;
        ISearcher _orderItemsSearcher;
        IOrderItemManager _orderItemManager;

        public ProviderDataSurfaceController(ITemplateService templateService, IProviderService providerService, 
            [Dependency("OrderItemsSearcher")] ISearcher orderItemsSearcher, IOrderItemManager orderItemManager)
        {
            _templateService = templateService;
            _providerService = providerService;
            _orderItemsSearcher = orderItemsSearcher;
            _orderItemManager = orderItemManager;
        }

        [HttpGet]
        public ActionResult RenderModifyProviderDataAction()
        {
            var pageModel = new Models.PartialPage.Settings.ModifyProviderData();

            pageModel.Providers = _providerService.FetchAndCreateListOfUsedProviders();

            return PartialView("Settings/ModifyProviderData", pageModel);
        }

        [HttpGet]
        public ActionResult GetNodeIdsForOrderItemsWithGivenProviderName(string providerName)
        {
            var ids = new List<int>();

            try
            {
                var searchCriteria = _orderItemsSearcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
                ids = _orderItemsSearcher.Search(searchCriteria.RawQuery("ProviderName:\"" + providerName + "\""))
                    .Where(x => x.Fields["ProviderName"].ToString() == providerName)
                    .Select(x => x.Id)
                    .ToList();
            }
            catch (Exception)
            {
                // NOP. Just return empty list if we fail, this should be enough indication that something is wrong and needs investigation.
            }

            return Json(ids, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SetProviderName(int nodeId, string providerName)
        {
            var res = new ResultResponse();

            try
            {
                _orderItemManager.SetProviderNameWithoutLogging(nodeId, providerName, true, false);

                res.Success = true;
                res.Message = "Genomförde ändring av leverantörsnamn på order.";
            }
            catch (Exception e)
            {
                res.Success = false;
                res.Message = "Misslyckades med att ändra leverantörsnamn på order: " + e.Message;
            }

            return Json(res, JsonRequestBehavior.AllowGet);
        }
    }
}
