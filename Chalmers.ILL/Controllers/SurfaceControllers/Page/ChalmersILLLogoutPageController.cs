using Chalmers.ILL.Members;
using Chalmers.ILL.Models.Page;
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

namespace Chalmers.ILL.Controllers.SurfaceControllers.Page
{
    public class ChalmersILLLogoutPageController : RenderMvcController
    {
        IMemberInfoManager _memberInfoManager;

        public ChalmersILLLogoutPageController(IMemberInfoManager memberInfoManager)
        {
            _memberInfoManager = memberInfoManager;
        }

        public override ActionResult Index(RenderModel model)
        {
            var customModel = new ChalmersILLLogoutPageModel();

            _memberInfoManager.PopulateModelWithMemberData(Request, Response, customModel);

            if (library.IsLoggedOn())
            {
                var memberId = _memberInfoManager.GetCurrentMemberId(Request, Response);
                FormsAuthentication.SignOut();
                _memberInfoManager.ClearMemberCache(Response);
            }

            return CurrentTemplate(customModel);
        }
    }
}
