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
    public class ChalmersILLSettingsPageController : RenderMvcController
    {
        IMemberInfoManager _memberInfoManager;

        public ChalmersILLSettingsPageController(IMemberInfoManager memberInfoManager)
        {
            _memberInfoManager = memberInfoManager;
        }

        public override ActionResult Index(RenderModel model)
        {
            var customModel = new ChalmersILLSettingsPageModel();

            _memberInfoManager.PopulateModelWithMemberData(Request, Response, customModel);

            return CurrentTemplate(customModel);
        }
    }
}
