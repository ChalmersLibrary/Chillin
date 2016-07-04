using Chalmers.ILL.Members;
using Chalmers.ILL.Models.Page;
using Chalmers.ILL.OrderItems;
using System;
using System.Web.Mvc;
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

            if (!String.IsNullOrEmpty(Request.QueryString["query"]))
            {
                customModel.PendingOrderItems = _orderItemSearcher.Search(Request.Params["query"].ToString());
            }
            else
            {
                customModel.PendingOrderItems = _orderItemSearcher.Search(@"nodeTypeAlias:ChalmersILLOrderItem AND 
                    (Status:01\:Ny OR 
                     Status:02\:Åtgärda OR
                     Status:09\:Mottagen OR 
                     (Status:03\:Beställd AND FollowUpDate:[197501010000000 TO " + DateTime.Now.ToString("yyyyMMddHHmmssfff") + @"]) OR
                     (Status:14\:Infodisk AND DueDate:[197501010000000 TO " + DateTime.Now.AddDays(5).Date.ToString("yyyyMMdd") + @"999999999]))");
            }

            return CurrentTemplate(customModel);
        }
    }
}
