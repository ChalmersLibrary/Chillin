using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using umbraco.cms.businesslogic.datatype;
using umbraco.cms.businesslogic.member;
using System.ComponentModel.DataAnnotations;
using Umbraco.Web.Mvc;
using Chalmers.ILL.Models;
using Chalmers.ILL.Models.PartialPage;
using System.Globalization;
using Chalmers.ILL.Utilities;
using Chalmers.ILL.OrderItems;
using Chalmers.ILL.UmbracoApi;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    [MemberAuthorize(AllowType = "Standard")]
    public class LogItemSurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;
        IUmbracoWrapper _dataTypes;

        public LogItemSurfaceController(IOrderItemManager orderItemManager, IUmbracoWrapper dataTypes)
        {
            _orderItemManager = orderItemManager;
            _dataTypes = dataTypes;
        }

        /// <summary>
        /// Render the Partial View for logging
        /// </summary>
        /// <param name="nodeId">OrderItem Node Id</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult RenderLogEntryAction(int nodeId)
        {
            var pageModel = new ChalmersILLActionLogEntryModel(_orderItemManager.GetOrderItem(nodeId));

            _dataTypes.PopulateModelWithAvailableValues(pageModel);

            // The return format depends on the client's Accept-header
            return PartialView("Chalmers.ILL.Action.LogEntry", pageModel);
        }

        /// <summary>
        /// Get LogItems for OrderItem and return a rendered Partial View
        /// </summary>
        /// <param name="nodeId">OrderItem</param>
        /// <returns>Partial View</returns>
        [HttpGet]
        public ActionResult GetLogItemsAsPartial(int nodeId)
        {
            // Call internal method to return List of LogItems for this OrderItem nodeId
            var logItems = _orderItemManager.GetLogItems(nodeId);

            // Return Partial View for LogItems bound to Model with LogItems
            return PartialView("Chalmers.ILL.LogItem", logItems);
        }

        /// <summary>
        /// Get LogItems for OrderItem and return a JsonResult
        /// </summary>
        /// <param name="nodeId">OrderItem</param>
        /// <returns>Json Result</returns>
        public JsonResult GetLogItems(int nodeId)
        {
            // The list of log entries to return binds to the model
            var logItems = _orderItemManager.GetLogItems(nodeId);

            // Return Json Result
            return Json(logItems, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Write LogItem without model binding
        /// </summary>
        /// <param name="OrderItemNodeId"></param>
        /// <param name="Type"></param>
        /// <param name="Message"></param>
        /// <returns></returns>
        [HttpPost, ValidateInput(false)]
        public ActionResult WriteLogItem(int nodeId, string Type, string Message, string newFollowUpDate)
        {
            // Connect to Umbraco ContentService
            var contentService = UmbracoContext.Application.Services.ContentService;

            // Find OrderItem
            var contentNode = contentService.GetById(nodeId);

            // Json response
            var json = new ResultResponse();

            try
            {
                // Set FollowUpDate property if it differs from current
                DateTime currentFollowUpDate = _orderItemManager.GetOrderItem(nodeId).FollowUpDate;

                if (!String.IsNullOrEmpty(newFollowUpDate))
                {
                    DateTime parsedNewFollowUpDate = Convert.ToDateTime(newFollowUpDate);
                    if (currentFollowUpDate != parsedNewFollowUpDate)
                    {
                        _orderItemManager.SetFollowUpDate(nodeId, parsedNewFollowUpDate, false, false);
                    }
                }

                // Use internal method to set type property and log the result
                _orderItemManager.AddLogItem(nodeId, Type, Message);

                // Construct JSON response for client (ie jQuery/getJSON)
                json.Success = true;
                json.Message = "Wrote log entry to node" + nodeId;
            }
            catch (Exception e)
            {
                json.Success = false;
                json.Message = "Error: " + e.Message;
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }
    }
}
