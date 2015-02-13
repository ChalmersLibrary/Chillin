using Chalmers.ILL.Members;
using Chalmers.ILL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Security;
using umbraco;
using umbraco.cms.businesslogic.member;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class ChalmersILLLogoutPageController : RenderMvcController
    {
        public override ActionResult Index(RenderModel model)
        {
            var customModel = new ChalmersILLLogoutPageModel();

            var memberInfoManager = new MemberInfoManager();

            customModel.CurrentMemberId = memberInfoManager.GetCurrentMemberId(Request, Response);
            customModel.CurrentMemberText = memberInfoManager.GetCurrentMemberText(Request, Response);
            customModel.CurrentMemberLoginName = memberInfoManager.GetCurrentMemberLoginName(Request, Response);

            if (library.IsLoggedOn())
            {
                var memberId = memberInfoManager.GetCurrentMemberId(Request, Response);
                FormsAuthentication.SignOut();
                memberInfoManager.ClearMemberCache(Response);
            }

            return CurrentTemplate(customModel);
        }
    }
}
