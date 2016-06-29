using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chalmers.ILL.Models;
using Umbraco.Core.Services;
using System.Configuration;
using System.Threading;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using System.Web.Hosting;

namespace Chalmers.ILL.MediaItems
{
    public class UmbracoMediaItemManager : IMediaItemManager
    {
        private IMediaService _mediaService;

        public UmbracoMediaItemManager(IMediaService mediaService)
        {
            _mediaService = mediaService;
        }

        public MediaItemModel CreateMediaItem(string name, int orderItemNodeId, string orderId, Stream data)
        {
            MediaItemModel res = null;

            var mainFolder = _mediaService.GetChildren(-1).First(m => m.Name == ConfigurationManager.AppSettings["umbracoOrderItemAttachmentsMediaFolderName"]);

            using (Semaphore semLock = new Semaphore(0, 1))
            {
                TypedEventHandler<IMediaService, SaveEventArgs<IMedia>> handler = (sender, e) => { semLock.Release(e.SavedEntities.Count()); };
                try
                {
                    MediaService.Saved += handler;

                    if (String.IsNullOrEmpty(name))
                    {
                        throw new Exception("Not a valid document.");
                    }
                    else
                    {
                        var media = _mediaService.CreateMedia(orderId + "-" + name, mainFolder, "OrderItemAttachment");
                        media.SetValue("file", name, data);
                        media.SetValue("orderItemNodeId", orderItemNodeId);

                        // Save the media and wait until it is finished so that we can retrieve the link to the item.
                        _mediaService.Save(media);
                        semLock.WaitOne();

                        res = new MediaItemModel();
                        res.CreateDate = media.CreateDate;
                        res.Id = media.Id;
                        res.Name = name;
                        res.OrderItemNodeId = orderItemNodeId;
                        res.Url = media.GetValue("file").ToString();
                        res.Data = data;
                    }
                }
                finally
                {
                    MediaService.Saved -= handler;
                }
            }

            return res;
        }

        public IList<MediaItemIdAndOrderItemId> DeleteOlderThan(DateTime date)
        {
            var res = new List<MediaItemIdAndOrderItemId>();

            bool removedMedia = false;
            foreach (var media in _mediaService.GetChildren(_mediaService.GetChildren(-1).First(m => m.Name == ConfigurationManager.AppSettings["umbracoOrderItemAttachmentsMediaFolderName"]).Id))
            {
                if (media.CreateDate < date)
                {
                    var idForRemovedMediaItem = media.Id;
                    var idForContentConnectedToRemovedMediaItem = media.GetValue<int>("orderItemNodeId");
                    _mediaService.Delete(media);
                    res.Add(new MediaItemIdAndOrderItemId(idForRemovedMediaItem, idForContentConnectedToRemovedMediaItem));
                    removedMedia = true;
                }
            }
            if (removedMedia)
            {
                _mediaService.EmptyRecycleBin();
            }

            return res;
        }

        public MediaItemModel GetOne(int id)
        {
            MediaItemModel res = null;
            var media = _mediaService.GetById(id);
            if (media != null)
            {
                res = new MediaItemModel();
                res.Id = media.Id;
                res.Name = media.Name;
                res.CreateDate = media.CreateDate;
                res.OrderItemNodeId = media.GetValue<int>("orderItemNodeId");
                res.Url = media.GetValue("file").ToString();
                res.Data = new MemoryStream(System.IO.File.ReadAllBytes(HostingEnvironment.MapPath(media.GetValue("file").ToString())));
            }
            return res;
        }
    }
}