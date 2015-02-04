using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using umbraco;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{

    /* Model */

    public class SystemInfo
    {
        public DateTime Timestamp { get; set; }
        public bool EnableLogging { get; set; }
    }

    /* Get System Info */

    [MemberAuthorize(AllowType = "Standard")]
    public class SystemSurfaceController : SurfaceController
    {
        
        [HttpGet]
        public ActionResult GetSystemInfo()
        {
            // Statistics JSON
            var json = new SystemInfo();

            json.Timestamp = DateTime.Now;
            json.EnableLogging = UmbracoSettings.EnableLogging;

            return Json(json, JsonRequestBehavior.AllowGet);
        }
    }

}