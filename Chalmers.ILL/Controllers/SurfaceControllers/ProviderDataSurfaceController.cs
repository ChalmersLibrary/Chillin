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

        [HttpPost]
        public ActionResult ModifyProviderDataHistory(string from, string to)
        {
            var res = new ResultResponse();
            
            try
            {
                var searchCriteria = _orderItemsSearcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
                var ids = _orderItemsSearcher.Search(searchCriteria.RawQuery("ProviderName:\"" + from + "\"")).Select(x => x.Id).ToList();

                var maxCount = 20;
                var count = 0;
                foreach (var id in ids)
                {
                    _orderItemManager.SetProviderName(id, to);

                    count++;
                    if (count >= maxCount)
                    {
                        break;
                    }
                }

                res.Success = true;
                res.Message = "Ändrade " + ids.Count() + " ordrar med leverantörsnamn \"" + from + "\" till att ha leverantörsnamn \"" + to + "\".";
            }
            catch (Exception e)
            {
                res.Success = false;
                res.Message = "Misslyckades med att ändra alla ordrar med leverantörsnamn \"" + from + "\" till att ha leverantörsnamn \"" + to + "\": " + e.Message;
            }

            return Json(res, JsonRequestBehavior.AllowGet);
        }
    }
}
