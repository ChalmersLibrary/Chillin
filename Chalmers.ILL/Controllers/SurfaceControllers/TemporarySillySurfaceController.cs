using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Core.Logging;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class TemporarySillySurfaceController : SurfaceController
    {
        [HttpGet]
        public ActionResult DeleteEverything(int parentId)
        {
            LogHelper.Info<TemporarySillySurfaceController>("Time to delete everything.");

            var messages = new List<string>();

            var count = 0;
            LogHelper.Info<TemporarySillySurfaceController>("Enumerating all childs on parent node...");
            foreach (var child in Services.ContentService.GetChildren(parentId))
            {
                try
                {
                    var childId = child.Id;
                    Services.ContentService.Delete(child);
                    count += 1;
                    LogHelper.Info<TemporarySillySurfaceController>("Removed child " + childId + ", total count = " + count);
                }
                catch (Exception e)
                {
                    messages.Add(e.Message);
                    LogHelper.Error<TemporarySillySurfaceController>("Error when removing child.", e);
                }
            }

            try
            {
                var parent = Services.ContentService.GetById(parentId);
                Services.ContentService.Delete(parent);
                count += 1;
                LogHelper.Info<TemporarySillySurfaceController>("Removed parent " + parentId + ", total count = " + count);
            }
            catch (Exception e)
            {
                messages.Add(e.Message);
                LogHelper.Error<TemporarySillySurfaceController>("Error when removing parent.", e);
            }

            return Json(new { DeleteCount = count, Troubles = String.Join(" | ", messages) }, JsonRequestBehavior.AllowGet);
        }
    }
}