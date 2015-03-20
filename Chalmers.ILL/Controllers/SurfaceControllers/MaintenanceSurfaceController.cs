using Chalmers.ILL.Models;
using Chalmers.ILL.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using Newtonsoft.Json;
using Umbraco.Core.Logging;
using Examine;
using UmbracoExamine;
using System.Configuration;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.Mail;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class MaintenanceSurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;

        public MaintenanceSurfaceController(IOrderItemManager orderItemManager)
        {
            _orderItemManager = orderItemManager;
        }

        /// <summary>
        /// Method which will run different maintenance jobs.
        /// </summary>
        /// <returns>Json</returns>
        [HttpPost]
        public ActionResult RunMaintenanceJobs()
        {
            var json = new ResultResponse();

            json.Success = true;

            removeOldMediaItems(json);

            optimizeIndexes(json);

            if (json.Success)
            {
                json.Message = "All maintenance jobs ran successfully.";
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        private void removeOldMediaItems(ResultResponse res)
        {
            try
            {
                var ms = UmbracoContext.Application.Services.MediaService;
                var cs = UmbracoContext.Application.Services.ContentService;

                bool removedMedia = false;
                foreach (var m in ms.GetChildren(ms.GetChildren(-1).First(m => m.Name == ConfigurationManager.AppSettings["umbracoOrderItemAttachmentsMediaFolderName"]).Id))
                {
                    if (m.CreateDate < DateTime.Now.AddDays(-30))
                    {
                        var c = cs.GetById(m.GetValue<int>("orderItemNodeId"));
                        var attachmentList = JsonConvert.DeserializeObject<List<OrderAttachment>>(c.GetValue<string>("attachments"));
                        if (attachmentList == null)
                        {
                            attachmentList = new List<OrderAttachment>();
                        }
                        else
                        {
                            attachmentList.RemoveAll(i => i.MediaItemNodeId == m.Id);
                        }
                        c.SetValue("attachments", JsonConvert.SerializeObject(attachmentList));
                        _orderItemManager.SaveWithoutEventsAndWithSynchronousReindexing(c);
                        ms.Delete(m);
                        removedMedia = true;
                    }
                }
                if (removedMedia)
                {
                    ms.EmptyRecycleBin();
                }

                res.Success &= true;
            }
            catch (Exception e)
            {
                LogHelper.Error<SystemSurfaceController>("Failed to remove old media items.", e);
                res.Success = false;
                res.Message += "Failed to remove old media items. ";
            }
        }

        private void optimizeIndexes(ResultResponse res)
        {
            try
            {
                var orderItemsIndexer = ExamineManager.Instance.IndexProviderCollection["ChalmersILLOrderItemsIndexer"];
                UmbracoContentIndexer umbracoOrderItemsIndexer = null;

                if (orderItemsIndexer is UmbracoContentIndexer)
                {
                    umbracoOrderItemsIndexer = (UmbracoContentIndexer)orderItemsIndexer;
                    umbracoOrderItemsIndexer.OptimizeIndex();
                }

                res.Success &= true;
            }
            catch (Exception e)
            {
                LogHelper.Error<SystemSurfaceController>("Failed to optimize indexes.", e);
                res.Success = false;
                res.Message += "Failed to optimize indexes. ";
            }
        }
    }
}
