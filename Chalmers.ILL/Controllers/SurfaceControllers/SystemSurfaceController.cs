using System;
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
using Chalmers.ILL.Mail;
using Chalmers.ILL.UmbracoApi;
using Microsoft.Practices.Unity;
using Chalmers.ILL.Models;
using System.Configuration;

namespace Chalmers.ILL.Controllers.SurfaceControllers
{
    public class SystemSurfaceController : SurfaceController
    {
        public static int TIME_BASED_UPDATE_OF_ORDER_EVENT_TYPE { get { return 19; } }

        IOrderItemManager _orderItemManager;
        INotifier _notifier;
        IExchangeMailWebApi _exchangeMailWebApi;
        IUmbracoWrapper _dataTypes;
        ISourceFactory _sourceFactory;
        IOrderItemSearcher _orderItemsSearcher;
        IAutomaticMailSendingEngine _automaticMailSendingEngine;
        IUmbracoWrapper _umbraco;

        public SystemSurfaceController(IOrderItemManager orderItemManager, INotifier notifier, IExchangeMailWebApi exchangeMailWebApi, 
            IUmbracoWrapper dataTypes, ISourceFactory sourceFactory, IOrderItemSearcher orderItemsSearcher,
            IAutomaticMailSendingEngine automaticMailSendingEngine, IUmbracoWrapper umbraco)
        {
            _orderItemManager = orderItemManager;
            _notifier = notifier;
            _exchangeMailWebApi = exchangeMailWebApi;
            _dataTypes = dataTypes;
            _sourceFactory = sourceFactory;
            _orderItemsSearcher = orderItemsSearcher;
            _automaticMailSendingEngine = automaticMailSendingEngine;
            _umbraco = umbraco;
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
                if (IsRequestAuthorized())
                {
                    ConvertOrdersWithExpiredFollowUpDateAndCertainStatusToNewStatus();

                    SignalExpiredFollowUpDates();

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
            }
            catch (Exception e)
            {
                LogHelper.Error<SystemSurfaceController>("Error while running regular update.", e);
            }

            return Json(res, JsonRequestBehavior.DenyGet);
        }

        /// <summary>
        /// Send out all the automatic e-mails that should be sent out.
        /// </summary>
        /// <remarks>Should be called once a day.</remarks>
        /// <returns>Json</returns>
        [HttpPost]
        public ActionResult SendOutAutomaticMailsThatAreDue()
        {
            var res = new ResultResponse();

            try
            {
                if (IsRequestAuthorized())
                {
                    _automaticMailSendingEngine.SendOutMailsThatAreDue();
                    res.Success = true;
                    res.Message = "Successfully sent out all the mail that should be sent out.";
                }
                else
                {
                    res.Success = false;
                    res.Message = "Failed to send out mail.";
                }
            }
            catch (Exception e)
            {
                res.Success = false;
                res.Message = "Failed to send out mail: " + e.Message;
            }

            return Json(res, JsonRequestBehavior.AllowGet);
        }

        #region Private methods.

        private bool IsRequestAuthorized()
        {
            var isLocalhost = Request.ServerVariables["SERVER_NAME"] == "localhost";
            var isTestServer = Request.ServerVariables["SERVER_NAME"] == ConfigurationManager.AppSettings["testServer"];

            string clientIpAddr = string.Empty;
            if (Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != null)
            {
                clientIpAddr = Request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToString().Split(':').First();
            }
            else if (Request.UserHostAddress.Length != 0)
            {
                clientIpAddr = Request.UserHostAddress.Split(':').First();
            }

            var allowedIp = ConfigurationManager.AppSettings["cronServerIpAddress"];
            var res = isLocalhost || isTestServer || (!String.IsNullOrWhiteSpace(allowedIp) && clientIpAddr == allowedIp);

            if (!res)
            {
                _umbraco.LogWarn<SystemSurfaceController>("Denied access to system APIs for IP: " + clientIpAddr);
            }

            return res;
        }

        private void SignalExpiredFollowUpDates()
        {
            try
            {
                var query = @"nodeTypeAlias:ChalmersILLOrderItem AND 
                    status:03\:Beställd AND 
                    followUpDate:[" + DateTime.Now.AddMinutes(-60).ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + " TO " + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + "]";

                // Search for our items and signal the ones that have expired recently.
                var results = _orderItemsSearcher.Search(query);
                foreach (var item in results)
                {
                    // -1 means that we haven't checked edited by properly and should disregard it
                    var memberId = -1;
                    _notifier.UpdateOrderItemUpdate(item.NodeId, memberId.ToString(), "", true, true);
                }
            }
            catch (Exception e)
            {
                LogHelper.Error<SystemSurfaceController>("Failed to signal expired follow up dates.", e);
            }
        }

        private void ConvertOrdersWithExpiredFollowUpDateAndCertainStatusToNewStatus()
        {
            var query = @"nodeTypeAlias:ChalmersILLOrderItem AND 
                Status:04\:Väntar AND 
                FollowUpDate:[1975-01-01T00:00:00.000Z TO " + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + "]";

            // -1 means that we haven't checked edited by properly and should disregard it
            var memberId = -1;

            // Search for our items and signal the ones that have expired recently.
            var ids = _orderItemsSearcher.Search(query).Select(x => x.NodeId).ToList();
            foreach (var id in ids)
            {
                var eventId = _orderItemManager.GenerateEventId(TIME_BASED_UPDATE_OF_ORDER_EVENT_TYPE);
                _orderItemManager.AddLogItem(id, "LOG", "Automatisk statusändring på grund av att uppföljningsdatum löpt ut.", eventId, false, false);
                _orderItemManager.SetStatus(id, _dataTypes.GetAvailableStatuses().First(x => x.Value.Contains("Åtgärda")).Id, eventId);
                _notifier.UpdateOrderItemUpdate(id, memberId.ToString(), "", true, true);
            }
        }

        #endregion
    }
}