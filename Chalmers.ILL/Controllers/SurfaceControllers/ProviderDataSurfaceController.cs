using Chalmers.ILL.Providers;
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
        IProviderService _providerService;

        public ProviderDataSurfaceController(ITemplateService templateService, IProviderService providerService)
        {
            _templateService = templateService;
            _providerService = providerService;
        }

        [HttpGet]
        public ActionResult RenderModifyProviderDataAction()
        {
            var pageModel = new Models.PartialPage.Settings.ModifyProviderData();

            pageModel.Providers = _providerService.FetchAndCreateListOfUsedProviders();

            return PartialView("Settings/ModifyProviderData", pageModel);
        }
    }
}
