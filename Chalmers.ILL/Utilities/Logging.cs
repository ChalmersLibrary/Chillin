using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Umbraco.Core;
using umbraco.cms.businesslogic.member;
using Umbraco.Web;
using Chalmers.ILL.Models;
using Umbraco.Web.Mvc;
using umbraco.cms.businesslogic.datatype;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Examine;
using UmbracoExamine;
using Examine.SearchCriteria;
using Umbraco.Core.Logging;
using Chalmers.ILL.Extensions;
using Newtonsoft.Json;
using Umbraco.Core.Models;

namespace Chalmers.ILL.Utilities
{
    public class Logging
    {
        /// <summary>
        /// Internal method for writing a LogItem for an OrderItem
        /// </summary>
        /// <param name="OrderItemNodeId">OrderItem</param>
        /// <param name="Type">Type of logging</param>
        /// <param name="Message">Log message</param>
        /// <returns>true if LogItem was written</returns>
        public static bool WriteLogItemInternal(int OrderItemNodeId, string Type, string Message, bool doReindex = true, bool doSignal = true)
        {
            // Connect to the content service
            var cs = UmbracoContext.Current.Application.Services.ContentService;

            // Get node for the order item
            var contentNode = cs.GetById(OrderItemNodeId);

            // Read previous log items
            string oldLogItems;
            List<LogItem> logItems = new List<LogItem>();

            if (!String.IsNullOrEmpty(contentNode.GetValue("log").ToString()))
            {
                oldLogItems = contentNode.GetValue("log").ToString();
                logItems = JsonConvert.DeserializeObject<List<LogItem>>(oldLogItems);
            }
            
            // Set user to current user or system
            string user;
            if (Member.IsLoggedOn())
            {
                user = Member.GetCurrentMember().Text; 
            }
            else
            {
                user = "System";
            }

            LogItem newLog = new LogItem
            {
                MemberName = user,
                Type = Type,
                Message = Message,
                CreateDate = DateTime.Now
            };

            //logItems.Insert(0, newLog);
            logItems.Add(newLog);

            contentNode.SetValue("log", JsonConvert.SerializeObject(logItems));

            cs.SaveWithoutEventsAndWithSynchronousReindexing(contentNode, doReindex, doSignal);

            return true;
        }

        /// <summary>
        /// Internal method to get LogItems for an OrderItem
        /// </summary>
        /// <param name="nodeId">OrderItem</param>
        /// <returns>List of LogItem</returns>
        public static List<LogItem> GetLogItems(int nodeId)
        {
            // Connect to the content service
            var cs = UmbracoContext.Current.Application.Services.ContentService;

            // Get node for the order item
            var contentNode = cs.GetById(nodeId);

            // Read previous log items
            string oldLogItems;
            var logItems = new List<LogItem>();

            if (!String.IsNullOrEmpty(contentNode.GetValue("log").ToString()))
                {
                oldLogItems = contentNode.GetValue("log").ToString();
                logItems = JsonConvert.DeserializeObject<List<LogItem>>(oldLogItems);
            }
            logItems.Reverse();
            return logItems;
        }
    }
}