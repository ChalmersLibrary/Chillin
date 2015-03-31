using Chalmers.ILL.Templates;
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

        public ProviderDataSurfaceController(ITemplateService templateService)
        {
            _templateService = templateService;
        }

        [HttpGet]
        public ActionResult RenderModifyProviderDataAction()
        {
            var pageModel = new Models.PartialPage.Settings.ModifyProviderData();

            return PartialView("Settings/ModifyProviderData", pageModel);
        }
    }
}
