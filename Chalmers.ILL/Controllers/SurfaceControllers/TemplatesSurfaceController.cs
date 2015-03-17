using Chalmers.ILL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class TemplatesSurfaceController : SurfaceController
    {
        public TemplatesSurfaceController()
        {

        }

        [HttpGet]
        public ActionResult RenderEditTemplatesAction()
        {
            return PartialView("Chalmers.ILL.EditTemplates", new TemplatesModel());
        }
    }
}
