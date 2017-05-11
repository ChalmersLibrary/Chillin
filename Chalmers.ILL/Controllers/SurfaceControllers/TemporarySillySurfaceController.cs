using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class TemporarySillySurfaceController : SurfaceController
    {
        [HttpGet]
        public ActionResult DeleteEverything(int parentId)
        {
            var messages = new List<string>();

            var count = 0;
            foreach (var child in Services.ContentService.GetChildren(parentId))
            {
                try
                {
                    Services.ContentService.Delete(child);
                    count += 1;
                }
                catch (Exception e)
                {
                    messages.Add(e.Message);
                }
            }

            try
            {
                var parent = Services.ContentService.GetById(parentId);
                Services.ContentService.Delete(parent);
                count += 1;
            }
            catch (Exception e)
            {
                messages.Add(e.Message);
            }

            return Json(new { DeleteCount = count, Troubles = String.Join(" | ", messages) }, JsonRequestBehavior.AllowGet);
        }
    }
}