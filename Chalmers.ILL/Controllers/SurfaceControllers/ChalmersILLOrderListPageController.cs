using Chalmers.ILL.Members;
using Chalmers.ILL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class ChalmersILLOrderListPageController : RenderMvcController
    {
        public override ActionResult Index(RenderModel model)
        {
            var customModel = new ChalmersILLOrderListPageModel();

            var memberInfoManager = new MemberInfoManager();

            customModel.CurrentMemberId = memberInfoManager.GetCurrentMemberId(Request, Response);
            customModel.CurrentMemberText = memberInfoManager.GetCurrentMemberText(Request, Response);
            customModel.CurrentMemberLoginName = memberInfoManager.GetCurrentMemberLoginName(Request, Response);

            return CurrentTemplate(customModel);
        }
    }
}
