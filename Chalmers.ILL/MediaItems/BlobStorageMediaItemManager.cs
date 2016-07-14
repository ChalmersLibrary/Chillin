using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Chalmers.ILL.Models;

namespace Chalmers.ILL.MediaItems
{
    public class BlobStorageMediaItemManager : IMediaItemManager
    {
        public MediaItemModel CreateMediaItem(string name, int orderItemNodeId, string orderId, Stream data)
        {
            throw new NotImplementedException();
        }

        public IList<MediaItemIdAndOrderItemId> DeleteOlderThan(DateTime date)
        {
            throw new NotImplementedException();
        }

        public MediaItemModel GetOne(int id)
        {
            throw new NotImplementedException();
        }
    }
}