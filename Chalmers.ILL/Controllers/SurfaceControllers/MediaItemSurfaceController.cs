using Chalmers.ILL.MediaItems;
using Chalmers.ILL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class MediaItemSurfaceController : SurfaceController
    {
        private IMediaItemManager _mediaItemManager;

        public MediaItemSurfaceController(IMediaItemManager mediaItemManager)
        {
            _mediaItemManager = mediaItemManager;
        }

        [HttpGet]
        public ActionResult GetMediaItem(string id)
        {
            ActionResult res = Json(new ResultResponse(false, "Unknown error."), JsonRequestBehavior.AllowGet);

            try
            {
                var storedMediaItem = _mediaItemManager.GetOne(id);
                if (storedMediaItem != null)
                {
                    var cd = new System.Net.Mime.ContentDisposition
                    {
                        FileName = storedMediaItem.Name,
                        Inline = true
                    };
                    Response.AppendHeader("Content-Disposition", cd.ToString());
                    res = File(storedMediaItem.Data, storedMediaItem.ContentType);
                }
                else
                {
                    res = Json(new ResultResponse(false, "Couldn't find stored media item with ID = " + id + "."), JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception e)
            {
                res = Json(new ResultResponse(false, "Error: " + e.Message), JsonRequestBehavior.AllowGet);
            }

            return res;
        }
    }
}