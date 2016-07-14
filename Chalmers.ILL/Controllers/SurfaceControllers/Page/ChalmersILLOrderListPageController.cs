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
                customModel.PendingOrderItems = _orderItemSearcher.Search(@"status:01\:Ny OR status:02\:Åtgärda OR status:09\:Mottagen OR 
                     (status:03\:Beställd AND followUpDate:[1975-01-01T00:00:00.000Z TO " + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + @"]) OR
                     (status:14\:Infodisk AND dueDate:[1975-01-01T00:00:00.000Z TO " + DateTime.Now.AddDays(5).Date.ToString("yyyy-MM-ddT") + @"23:59:59.999Z])");
            }

            return CurrentTemplate(customModel);
        }
    }
}
