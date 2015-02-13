using Chalmers.ILL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.Logging
{
    public interface IInternalDbLogger
    {
        /// <summary>
        /// Internal method for writing a LogItem for an OrderItem
        /// </summary>
        /// <param name="OrderItemNodeId">OrderItem</param>
        /// <param name="Type">Type of logging</param>
        /// <param name="Message">Log message</param>
        /// <returns>true if LogItem was written</returns>
        bool WriteLogItemInternal(int OrderItemNodeId, string Type, string Message, bool doReindex = true, bool doSignal = true);

        /// <summary>
        /// Internal method to get LogItems for an OrderItem
        /// </summary>
        /// <param name="nodeId">OrderItem</param>
        /// <returns>List of LogItem</returns>
        List<LogItem> GetLogItems(int nodeId);

        void WriteSierraDataToLog(int orderItemNodeId, SierraModel sm, bool doReindex = true, bool doSignal = true);
    }
}
