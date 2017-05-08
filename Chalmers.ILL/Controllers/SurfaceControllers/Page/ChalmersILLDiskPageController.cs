using Chalmers.ILL.Members;
using Chalmers.ILL.Models.Page;
using Chalmers.ILL.OrderItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers.Page
{
    public class ChalmersILLDiskPageController : RenderMvcController
    {
        IMemberInfoManager _memberInfoManager;
        IOrderItemSearcher _searcher;

        public ChalmersILLDiskPageController(IMemberInfoManager memberInfoManager, IOrderItemSearcher searcher)
        {
            _memberInfoManager = memberInfoManager;
            _searcher = searcher;
        }

        public override ActionResult Index(RenderModel model)
        {
            var customModel = new ChalmersILLDiskPageModel();

            _memberInfoManager.PopulateModelWithMemberData(Request, Response, customModel);

            if (!String.IsNullOrEmpty(Request.QueryString["query"]))
            {
                customModel.OrderItems = _searcher.Search("((type:Bok AND status:(Infodisk OR Utlånad OR Transport OR Krävd)) OR (type:Artikel AND status:Transport)) AND " + 
                    "\"" + Request.Params["query"].Trim() + "\"");
            }

            return CurrentTemplate(customModel);
        }
    }
}
