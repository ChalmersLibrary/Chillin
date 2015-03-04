﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using umbraco;
using Chalmers.ILL.OrderItems;
using Umbraco.Core.Logging;
using Examine;
using Chalmers.ILL.SignalR;
using Chalmers.ILL.Logging;
using Chalmers.ILL.Mail;
using Chalmers.ILL.UmbracoApi;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class SystemInfo
    {
        public DateTime Timestamp { get; set; }
        public bool EnableLogging { get; set; }
    }

    /* Get System Info */
    [MemberAuthorize(AllowType = "Standard")]
    public class SystemSurfaceController : SurfaceController
    {
        IOrderItemManager _orderItemManager;
        INotifier _notifier;
        IInternalDbLogger _internalDbLogger;
        IExchangeMailWebApi _exchangeMailWebApi;
        IUmbracoWrapper _dataTypes;
        ISourceFactory _sourceFactory;

        public SystemSurfaceController(IOrderItemManager orderItemManager, INotifier notifier, 
            IInternalDbLogger internalDbLogger, IExchangeMailWebApi exchangeMailWebApi, IUmbracoWrapper dataTypes,
            ISourceFactory sourceFactory)
        {
            _orderItemManager = orderItemManager;
            _notifier = notifier;
            _internalDbLogger = internalDbLogger;
            _exchangeMailWebApi = exchangeMailWebApi;
            _dataTypes = dataTypes;
            _sourceFactory = sourceFactory;
        }
        
        [HttpGet]
        public ActionResult GetSystemInfo()
        {
            // Statistics JSON
            var json = new SystemInfo();

            json.Timestamp = DateTime.Now;
            json.EnableLogging = UmbracoSettings.EnableLogging;

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Updates the system.
        /// Checks if statuses should be changed, if something should be notified, polls sources, etc.
        /// </summary>
        /// <remarks>Should be called regularly.</remarks>
        /// <returns>Json</returns>
        [HttpPost]
        public ActionResult Update()
        {
            List<SourcePollingResult> res = new List<SourcePollingResult>();

            try
            {
                convertOrdersWithExpiredFollowUpDateAndCertainStatusToNewStatus();

                signalExpiredFollowUpDates();

                foreach (var source in _sourceFactory.Sources())
                {
                    try
                    {
                        res.Add(source.Poll());
                    }
                    catch (Exception e)
                    {
                        LogHelper.Error<SystemSurfaceController>("Error while polling source.", e);
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.Error<SystemSurfaceController>("Error while running regular update.", e);
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        private void signalExpiredFollowUpDates()
        {
            try
            {
                // Connect to an Examine Search Provider
                var searcher = ExamineManager.Instance.SearchProviderCollection["ChalmersILLOrderItemsSearcher"];

                // Specify Search Criteria
                var searchCriteria = searcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);

                // Specify the query
                var query = searchCriteria.RawQuery(@"nodeTypeAlias:ChalmersILLOrderItem AND 
                    Status:03\:Beställd AND 
                    FollowUpDate:[" + DateTime.Now.AddMinutes(-60).ToString("yyyyMMddHHmmssfff") + " TO " + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "]");

                // Search for our items and signal the ones that have expired recently.
                var results = searcher.Search(query);
                foreach (var item in results)
                {
                    // -1 means that we haven't checked edited by properly and should disregard it
                    var memberId = -1;
                    _notifier.UpdateOrderItemUpdate(item.Id, memberId.ToString(), "", true, true);
                }
            }
            catch (Exception e)
            {
                LogHelper.Error<SystemSurfaceController>("Failed to signal expired follow up dates.", e);
            }
        }

        private void convertOrdersWithExpiredFollowUpDateAndCertainStatusToNewStatus()
        {
            // Connect to an Examine Search Provider
            var searcher = ExamineManager.Instance.SearchProviderCollection["ChalmersILLOrderItemsSearcher"];

            // Specify Search Criteria
            var searchCriteria = searcher.CreateSearchCriteria(Examine.SearchCriteria.BooleanOperation.Or);

            // Specify the query
            var query = searchCriteria.RawQuery(@"nodeTypeAlias:ChalmersILLOrderItem AND 
                Status:04\:Väntar AND 
                FollowUpDate:[197501010000000 TO " + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "]");

            // -1 means that we haven't checked edited by properly and should disregard it
            var memberId = -1;

            // Search for our items and signal the ones that have expired recently.
            var ids = searcher.Search(query).Select(x => x.Id).ToList();
            foreach (var id in ids)
            {
                _internalDbLogger.WriteLogItemInternal(id, "LOG", "Automatisk statusändring på grund av att uppföljningsdatum löpt ut.", false, false);

                _orderItemManager.SetOrderItemStatusInternal(id,
                    _dataTypes.GetAvailableStatuses().First(x => x.Value.Contains("Åtgärda")).Id,
                    true, true);

                _notifier.UpdateOrderItemUpdate(id, memberId.ToString(), "", true, true);
            }
        }
    }
}