using Chalmers.ILL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using Newtonsoft.Json;
using Umbraco.Core.Logging;
using Examine;
using UmbracoExamine;
using System.Configuration;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.MediaItems;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class MaintenanceSurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;
        IMediaItemManager _mediaItemManager;

        public MaintenanceSurfaceController(IOrderItemManager orderItemManager, IMediaItemManager mediaItemManager)
        {
            _orderItemManager = orderItemManager;
            _mediaItemManager = mediaItemManager;
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
                var deletedMediaItemIds = _mediaItemManager.DeleteOlderThan(DateTime.Now.AddDays(-30));

                foreach (var idCollection in deletedMediaItemIds)
                {
                    _orderItemManager.RemoveConnectionToMediaItem(idCollection.OrderItemId, idCollection.MediaItemId);
                }
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
                foreach (var orderItemsIndexer in ExamineManager.Instance.IndexProviderCollection)
                {
                    UmbracoContentIndexer umbracoOrderItemsIndexer = null;

                    if (orderItemsIndexer is UmbracoContentIndexer)
                    {
                        umbracoOrderItemsIndexer = (UmbracoContentIndexer)orderItemsIndexer;
                        umbracoOrderItemsIndexer.OptimizeIndex();
                    }

                    res.Success &= true;
                }
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
