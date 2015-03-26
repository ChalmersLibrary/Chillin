using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using umbraco.cms.businesslogic.member;
using Chalmers.ILL.Models;
using Chalmers.ILL.Utilities;
using Chalmers.ILL.Extensions;
using System.Configuration;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Models.PartialPage;
using Examine;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{

    [MemberAuthorize(AllowType = "Standard")]
    public class OrderItemProviderSurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;

        public OrderItemProviderSurfaceController(IOrderItemManager orderItemManager)
        {
            _orderItemManager = orderItemManager;
        }

        [HttpGet]
        public ActionResult RenderProviderAction(int nodeId)
        {
            // Get a new OrderItem populated with values for this node
            var pageModel = new ChalmersILLActionProviderModel(_orderItemManager.GetOrderItem(nodeId));

            pageModel.Providers = FetchAndCreateListOfUsedProviders();

            // The return format depends on the client's Accept-header
            return PartialView("Chalmers.ILL.Action.Provider", pageModel);
        }

        [HttpGet]
        public ActionResult SetProvider(int nodeId, string providerName, string providerOrderId, string providerInformation, string newFollowUpDate)
        {
            var json = new ResultResponse();

            try
            {
                _orderItemManager.SetFollowUpDate(nodeId, Convert.ToDateTime(newFollowUpDate), false, false);
                _orderItemManager.SetProviderName(nodeId, providerName, false, false);
                _orderItemManager.SetProviderOrderId(nodeId, providerOrderId, false, false);
                _orderItemManager.SetProviderInformation(nodeId, providerInformation, false, false);
                _orderItemManager.SetStatus(nodeId, Helpers.DataTypePrevalueId(ConfigurationManager.AppSettings["umbracoOrderStatusDataTypeDefinitionName"], "03:Beställd"));

                json.Success = true;
                json.Message = "Sparade data för beställning.";
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Error: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        private List<String> FetchAndCreateListOfUsedProviders()
        {
            var res = new List<String>();

            var searcher = ExamineManager.Instance.SearchProviderCollection["ChalmersILLOrderItemsSearcher"];
            var searchCriteria = searcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);
            var allOrders = searcher.Search(searchCriteria.RawQuery("nodeTypeAlias:ChalmersILLOrderItem"));

            return allOrders.Where(x => x.Fields.ContainsKey("ProviderName") && x.Fields["ProviderName"] != "")
                .Select(x => x.Fields["ProviderName"])
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .Select(x => x.Key)
                .ToList();
        }
    }
}