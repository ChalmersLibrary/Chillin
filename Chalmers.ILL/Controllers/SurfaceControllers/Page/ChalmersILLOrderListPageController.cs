using Chalmers.ILL.Members;
using Chalmers.ILL.Models;
using Chalmers.ILL.Models.Page;
using Chalmers.ILL.OrderItems;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Windows.Documents;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers.Page
{
    public class ChalmersILLOrderListPageController : RenderMvcController
    {
        IMemberInfoManager _memberInfoManager;
        IOrderItemSearcher _orderItemSearcher;

        public ChalmersILLOrderListPageController(IMemberInfoManager memberInfoManager, IOrderItemSearcher orderItemSearcher)
        {
            _memberInfoManager = memberInfoManager;
            _orderItemSearcher = orderItemSearcher;
        }

        public override ActionResult Index(RenderModel model)
        {
            var customModel = new ChalmersILLOrderListPageModel();

            _memberInfoManager.PopulateModelWithMemberData(Request, Response, customModel);

            int start = Request.QueryString["start"] != null ? Int32.Parse(Request.QueryString["start"]) : 0;

            if (!String.IsNullOrEmpty(Request.QueryString["query"]))
            {
                var queryString = Request.QueryString["query"].Trim();

                if (IsOrderId(queryString))
                {
                    queryString = "\"" + queryString + "\"";
                }

                customModel.PendingOrderItems = _orderItemSearcher.Search(queryString, start, 50);
                customModel.ManualAnonymizationItems = new SearchResult {
                    Items = new List<OrderItemModel>(),
                    Count = 0
                };
            }
            else
            {
                customModel.PendingOrderItems = _orderItemSearcher.Search(@"status:01\:Ny OR status:02\:Åtgärda OR status:09\:Mottagen OR 
                     (status:03\:Beställd AND followUpDate:[1975-01-01T00:00:00.000Z TO " + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + @"]) OR
                     (status:14\:Infodisk AND dueDate:[1975-01-01T00:00:00.000Z TO " + DateTime.Now.AddDays(5).Date.ToString("yyyy-MM-ddT") + @"23:59:59.999Z])", start, 50);

                var implementationDate = new DateTime(2024, 11, 13);
                var differenceBetweenNowAndImplementationDate = DateTime.Now - implementationDate;
                var daysToSubtract = differenceBetweenNowAndImplementationDate.Days * 5; // We go back five days for each day forward.
                var manualAnonymizationDateLimit = implementationDate.AddDays(-daysToSubtract);
                var manualAnonymizationQueryString = "createDate:[" + manualAnonymizationDateLimit.ToString("yyyy-MM-dd") + " TO *] AND isAnonymized:false AND isAnonymizedAutomatically:true";
                customModel.ManualAnonymizationItems = _orderItemSearcher.Search(manualAnonymizationQueryString, 0, 5);
            }

            return CurrentTemplate(customModel);
        }

        #region Private methods

        private bool IsOrderId(string text)
        {
            var orderIdPattern = new Regex("^cthb-[a-zA-Z0-9]{8}-[0-9]+$");

            return orderIdPattern.IsMatch(text);
        }

        #endregion
    }
}
