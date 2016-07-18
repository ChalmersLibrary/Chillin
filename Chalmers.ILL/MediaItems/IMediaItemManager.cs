using Chalmers.ILL.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chalmers.ILL.MediaItems
{
    public interface IMediaItemManager
    {
        /// <summary>
        /// Creates a media item in the media storage and returns it.
        /// </summary>
        /// <param name="name">The name of the media item.</param>
        /// <param name="orderItemNodeId">The ID for the order item which the media item is connected to.</param>
        /// <param name="data">A stream containing the data for the media item.</param>
        /// <returns></returns>
        MediaItemModel CreateMediaItem(string name, int orderItemNodeId, string orderId, Stream data, string contentType);

        /// <summary>
        /// Delete all media items that are older than a given date.
        /// </summary>
        /// <param name="date">The limiting date.</param>
        /// <returns>A list with the identifiers for the deleted media items.</returns>
        IList<MediaItemIdAndOrderItemId> DeleteOlderThan(DateTime date);

        /// <summary>
        /// Fetch one media item with the given ID.
        /// </summary>
        /// <param name="id">An integer that identifies the media item.</param>
        /// <returns></returns>
        MediaItemModel GetOne(string id);
    }
}
